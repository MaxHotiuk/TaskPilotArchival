using Microsoft.Extensions.Logging;
using Infrastructure.Helpers;
using Core.Models;
using Core.Abstractions.Repositories;
using Core.Abstractions.Services;
using Core.DTOs;
using Core.Entities;
using Task = Core.Entities.Task;

namespace Infrastructure.Services;

public class BoardArchivalService : IBoardArchivalService
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
}
