using System;
using System.Collections.Generic;

namespace Core.Entities
{
    public class Task
    {
        public Guid Id { get; set; }
        public Guid BoardId { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public int StateId { get; set; }
        public Guid? AssigneeId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DueDate { get; set; }

        public Board Board { get; set; } = null!;
        public State State { get; set; } = null!;
        public User? Assignee { get; set; }
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
