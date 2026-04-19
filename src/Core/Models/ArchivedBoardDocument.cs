using Core.DTOs;
using System.Text.Json.Serialization;

namespace Core.Models;

public class ArchivedBoardDocument
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("partitionKey")]
    public string PartitionKey { get; set; } = null!;

    [JsonPropertyName("boardData")]
    public BoardArchivalDto BoardData { get; set; } = null!;

    [JsonPropertyName("archivedAt")]
    public DateTime ArchivedAt { get; set; }

    public static ArchivedBoardDocument FromDto(BoardArchivalDto boardDto)
    {
        var boardId = boardDto.BoardId.ToString();
        return new ArchivedBoardDocument
        {
            Id = boardId,
            PartitionKey = boardId,
            BoardData = boardDto,
            ArchivedAt = DateTime.UtcNow
        };
    }
}
