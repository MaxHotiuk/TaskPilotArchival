using System;
using System.Collections.Generic;

namespace Core.Entities
{
    public class Board
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public Guid OwnerId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsArchived { get; set; }
        public DateTime? ArchivedAt { get; set; }
        public string? ArchivalReason { get; set; }

        public User Owner { get; set; } = null!;
        public ICollection<State> States { get; set; } = new List<State>();
        public ICollection<Task> Tasks { get; set; } = new List<Task>();
        public ICollection<BoardMember> Members { get; set; } = new List<BoardMember>();
    }
}
