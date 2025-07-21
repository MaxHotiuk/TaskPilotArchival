using System;
using System.Collections.Generic;

namespace Core.DTOs
{
    public class TaskDto
    {
        public Guid Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int StateId { get; set; }
        public Guid? AssigneeId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DueDate { get; set; }
        public List<CommentDto> Comments { get; set; } = new();
    }
}
