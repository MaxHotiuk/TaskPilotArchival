using Microsoft.Extensions.Logging;
using Infrastructure.Helpers;
using Core.Models;
using Core.Abstractions.Repositories;
using Core.Abstractions.Services;
using Core.DTOs;
using Core.Entities;
using Task = Core.Entities.Task;

namespace Infrastructure.Services;

public class BoardArchivalService : IBoardArchivalService, IBoardDearchivalService
{
    private readonly IBlobStorageService _blobStorageService;
    private readonly ILogger<BoardArchivalService> _logger;
    private readonly IBoardRepository _boardRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly ICommentRepository _commentRepository;
    private readonly IStateRepository _stateRepository;
    private readonly IBoardMemberRepository _boardMemberRepository;

    public BoardArchivalService(
        IBlobStorageService blobStorageService,
        ILogger<BoardArchivalService> logger,
        IBoardRepository boardRepository,
        ITaskRepository taskRepository,
        ICommentRepository commentRepository,
        IStateRepository stateRepository,
        IBoardMemberRepository boardMemberRepository)
    {
        _blobStorageService = blobStorageService;
        _logger = logger;
        _boardRepository = boardRepository;
        _taskRepository = taskRepository;
        _commentRepository = commentRepository;
        _stateRepository = stateRepository;
        _boardMemberRepository = boardMemberRepository;
    }

    public async System.Threading.Tasks.Task ArchiveBoardAsync(BoardArchivalMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            var (board, states, tasks, comments, members) = await LoadBoardDataAsync(message.BoardId, cancellationToken);
            var boardDto = MapToArchivalDto(board, states, tasks, comments, members);
            await SerializeAndUploadAsync(boardDto, cancellationToken);
            MarkBoardAsArchived(board);
            DeleteBoardRelatedEntities(comments, tasks, states, members);
            await _boardRepository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error archiving board {BoardId}", message.BoardId);
            throw;
        }
    }

    private async Task<(Board board, List<State> states, List<Task> tasks, List<Comment> comments, List<BoardMember> members)> LoadBoardDataAsync(Guid boardId, CancellationToken cancellationToken)
    {
        var board = await _boardRepository.GetByIdAsync(boardId, cancellationToken);
        if (board == null)
        {
            throw new InvalidOperationException($"Board {boardId} not found");
        }
        var states = (await _stateRepository.FindAsync(s => s.BoardId == board.Id, cancellationToken)).ToList();
        var tasks = (await _taskRepository.FindAsync(t => t.BoardId == board.Id, cancellationToken)).ToList();
        var taskIds = tasks.Select(t => t.Id).ToList();
        var comments = (await _commentRepository.FindAsync(c => taskIds.Contains(c.TaskId), cancellationToken)).ToList();
        var members = (await _boardMemberRepository.FindAsync(m => m.BoardId == board.Id, cancellationToken)).ToList();
        return (board, states, tasks, comments, members);
    }

    private BoardArchivalDto MapToArchivalDto(
        Board board,
        List<State> states,
        List<Task> tasks,
        List<Comment> comments,
        List<BoardMember> members)
    {
        return new BoardArchivalDto
        {
            BoardId = board.Id,
            Name = board.Name,
            Description = board.Description,
            OwnerId = board.OwnerId,
            CreatedAt = board.CreatedAt,
            UpdatedAt = board.UpdatedAt,
            States = states.Select(s => new StateDto
            {
                Id = s.Id,
                Name = s.Name,
                Order = s.Order,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            }).ToList(),
            Tasks = tasks.Select(t => new TaskDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                StateId = t.StateId,
                AssigneeId = t.AssigneeId,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                DueDate = t.DueDate,
                Comments = comments.Where(c => c.TaskId == t.Id).Select(c => new CommentDto
                {
                    Id = c.Id,
                    AuthorId = c.AuthorId,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                }).ToList()
            }).ToList(),
            Members = members.Select(m => new BoardMemberDto
            {
                UserId = m.UserId,
                Role = m.Role,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt
            }).ToList()
        };
    }

    private async System.Threading.Tasks.Task SerializeAndUploadAsync(BoardArchivalDto boardDto, CancellationToken cancellationToken)
    {
        var json = SerializationHelper.SerializeToJson(boardDto);
        var blobName = $"archivals/{boardDto.BoardId}_{DateTime.UtcNow:yyyyMMddHHmmss}.json";
        using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))
        {
            await _blobStorageService.UploadFileAsync(stream, blobName, "application/json", cancellationToken);
        }
        _logger.LogInformation("Board archived and uploaded to blob: {BlobName}", blobName);
    }

    private void MarkBoardAsArchived(Board board)
    {
        if (!board.IsArchived)
        {
            board.IsArchived = true;
            board.ArchivedAt = DateTime.UtcNow;
            _boardRepository.Update(board);
        }
    }

    private void DeleteBoardRelatedEntities(
        List<Comment> comments,
        List<Task> tasks,
        List<State> states,
        List<BoardMember> members)
    {
        foreach (var comment in comments)
        {
            _commentRepository.Remove(comment);
        }
        foreach (var task in tasks)
        {
            _taskRepository.Remove(task);
        }
        foreach (var state in states)
        {
            _stateRepository.Remove(state);
        }
        foreach (var member in members)
        {
            _boardMemberRepository.Remove(member);
        }
    }

    public async System.Threading.Tasks.Task DearchiveBoardAsync(BoardArchivalMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            var latestBlob = await GetLatestArchivalBlobNameAsync(message.BoardId, cancellationToken);
            var boardDto = await DownloadAndDeserializeBoardDtoAsync(latestBlob, cancellationToken);

            await RestoreBoardEntitiesAsync(boardDto, cancellationToken);
            await _boardRepository.SaveChangesAsync(cancellationToken);

            await _blobStorageService.DeleteFileAsync(latestBlob, cancellationToken);

            _logger.LogInformation("Board {BoardId} dearchived and restored from blob: {BlobName}", message.BoardId, latestBlob);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dearchiving board {BoardId}", message.BoardId);
            throw;
        }
    }

    private async Task<string> GetLatestArchivalBlobNameAsync(Guid boardId, CancellationToken cancellationToken)
    {
        var prefix = $"archivals/{boardId}_";
        var blobs = await _blobStorageService.ListFilesAsync(prefix, cancellationToken);
        var latestBlob = blobs
            .OrderByDescending(b => b)
            .FirstOrDefault();
        if (latestBlob == null)
        {
            throw new InvalidOperationException($"No archival blob found for board {boardId}");
        }
        return latestBlob;
    }

    private async Task<BoardArchivalDto> DownloadAndDeserializeBoardDtoAsync(string blobName, CancellationToken cancellationToken)
    {
        using (var stream = await _blobStorageService.DownloadFileAsync(blobName, cancellationToken))
        {
            return SerializationHelper.DeserializeFromJson<BoardArchivalDto>(stream);
        }
    }

    private async System.Threading.Tasks.Task RestoreBoardEntitiesAsync(BoardArchivalDto boardDto, CancellationToken cancellationToken)
    {
        var boardExists = await EnsureBoardExistsOrUpdateAsync(boardDto, cancellationToken);
        var stateIdMap = await AddStatesAndGetIdMapAsync(boardDto, cancellationToken);
        await AddTasksAsync(boardDto, stateIdMap, cancellationToken);
        await AddCommentsAsync(boardDto, cancellationToken);
        await AddMembersAsync(boardDto, cancellationToken);
    }

    private async Task<bool> EnsureBoardExistsOrUpdateAsync(BoardArchivalDto boardDto, CancellationToken cancellationToken)
    {
        var existingBoard = await _boardRepository.GetByIdAsync(boardDto.BoardId, cancellationToken);
        if (existingBoard != null)
        {
            var existingStates = (await _stateRepository.FindAsync(s => s.BoardId == boardDto.BoardId, cancellationToken)).ToList();
            var existingTasks = (await _taskRepository.FindAsync(t => t.BoardId == boardDto.BoardId, cancellationToken)).ToList();
            var existingTaskIds = existingTasks.Select(t => t.Id).ToList();
            var existingComments = (await _commentRepository.FindAsync(c => existingTaskIds.Contains(c.TaskId), cancellationToken)).ToList();
            var existingMembers = (await _boardMemberRepository.FindAsync(m => m.BoardId == boardDto.BoardId, cancellationToken)).ToList();

            foreach (var comment in existingComments)
                _commentRepository.Remove(comment);
            foreach (var task in existingTasks)
                _taskRepository.Remove(task);
            foreach (var state in existingStates)
                _stateRepository.Remove(state);
            foreach (var member in existingMembers)
                _boardMemberRepository.Remove(member);

            existingBoard.Name = boardDto.Name!;
            existingBoard.Description = boardDto.Description;
            existingBoard.OwnerId = boardDto.OwnerId;
            existingBoard.CreatedAt = boardDto.CreatedAt;
            existingBoard.UpdatedAt = DateTime.UtcNow;
            existingBoard.IsArchived = false;
            existingBoard.ArchivedAt = null;
            _boardRepository.Update(existingBoard);
            return true;
        }
        else
        {
            var board = new Board
            {
                Id = boardDto.BoardId,
                Name = boardDto.Name!,
                Description = boardDto.Description,
                OwnerId = boardDto.OwnerId,
                CreatedAt = boardDto.CreatedAt,
                UpdatedAt = DateTime.UtcNow,
                IsArchived = false,
                ArchivedAt = null
            };
            await _boardRepository.AddAsync(board, cancellationToken);
            return false;
        }
    }

    private async Task<Dictionary<int, int>> AddStatesAndGetIdMapAsync(BoardArchivalDto boardDto, CancellationToken cancellationToken)
    {
        var tempStateMap = new List<(int oldId, string name, int order, DateTime createdAt, DateTime updatedAt)>();
        foreach (var s in boardDto.States)
        {
            var state = new State
            {
                Name = s.Name!,
                Order = s.Order,
                BoardId = boardDto.BoardId,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            };
            await _stateRepository.AddAsync(state, cancellationToken);
            tempStateMap.Add((s.Id, s.Name!, s.Order, s.CreatedAt, s.UpdatedAt));
        }
        await _stateRepository.SaveChangesAsync(cancellationToken);

        var dbStates = (await _stateRepository.FindAsync(s => s.BoardId == boardDto.BoardId, cancellationToken)).ToList();
        var stateIdMap = new Dictionary<int, int>();
        foreach (var temp in tempStateMap)
        {
            var match = dbStates.FirstOrDefault(s => s.Name == temp.name && s.Order == temp.order && s.CreatedAt == temp.createdAt && s.UpdatedAt == temp.updatedAt);
            if (match != null)
                stateIdMap[temp.oldId] = match.Id;
        }
        return stateIdMap;
    }

    private async System.Threading.Tasks.Task AddTasksAsync(BoardArchivalDto boardDto, Dictionary<int, int> stateIdMap, CancellationToken cancellationToken)
    {
        var tasks = boardDto.Tasks.Select(t => new Task
        {
            Id = t.Id,
            Title = t.Title!,
            Description = t.Description,
            StateId = stateIdMap.ContainsKey(t.StateId) ? stateIdMap[t.StateId] : t.StateId,
            BoardId = boardDto.BoardId,
            AssigneeId = t.AssigneeId,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt,
            DueDate = t.DueDate
        }).ToList();
        foreach (var task in tasks)
        {
            await _taskRepository.AddAsync(task, cancellationToken);
        }
    }

    private async System.Threading.Tasks.Task AddCommentsAsync(BoardArchivalDto boardDto, CancellationToken cancellationToken)
    {
        var comments = boardDto.Tasks.SelectMany(t => t.Comments.Select(c => new Comment
        {
            Id = c.Id,
            TaskId = t.Id,
            AuthorId = c.AuthorId,
            Content = c.Content!,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        })).ToList();
        foreach (var comment in comments)
        {
            await _commentRepository.AddAsync(comment, cancellationToken);
        }
    }

    private async System.Threading.Tasks.Task AddMembersAsync(BoardArchivalDto boardDto, CancellationToken cancellationToken)
    {
        var members = boardDto.Members.Select(m => new BoardMember
        {
            BoardId = boardDto.BoardId,
            UserId = m.UserId,
            Role = m.Role!,
            CreatedAt = m.CreatedAt,
            UpdatedAt = m.UpdatedAt
        }).ToList();
        foreach (var member in members)
        {
            await _boardMemberRepository.AddAsync(member, cancellationToken);
        }
    }
}
