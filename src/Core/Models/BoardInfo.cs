using System;
namespace Core.Models;

public class BoardInfo
{
    public Guid BoardId { get; set; }
    public string? BoardName { get; set; }
    public DateTime ArchivedAt { get; set; }
}