using System;
namespace Core.Models;

public class BoardArchivalMessage
{
    public Guid BoardId { get; set; }
    public string BoardName { get; set; } = null!;
}