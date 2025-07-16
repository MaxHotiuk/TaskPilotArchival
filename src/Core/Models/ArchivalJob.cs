using System;
namespace Core.Models;

public class ArchivalJob
{
    public string id { get; set; } = Guid.NewGuid().ToString();
    public Guid BoardId { get; set; }
    public string JobType { get; set; } = "BoardArchival";
    public string Status { get; set; } = "Pending";
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? BlobPath { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ProcessedBy { get; set; }
    public string? Metadata { get; set; }
}