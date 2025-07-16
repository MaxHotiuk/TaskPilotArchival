using System;
using System.Collections.Generic;

namespace Core.DTOs
{
    public class BoardArchivalDto
    {
        public Guid BoardId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public Guid OwnerId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<StateDto> States { get; set; } = new();
        public List<TaskDto> Tasks { get; set; } = new();
        public List<BoardMemberDto> Members { get; set; } = new();
    }
}
