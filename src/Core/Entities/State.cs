using System;
using System.Collections.Generic;

namespace Core.Entities
{
    public class State
    {
        public int Id { get; set; }
        public Guid BoardId { get; set; }
        public string Name { get; set; } = null!;
        public int Order { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public Board Board { get; set; } = null!;
        public ICollection<Task> Tasks { get; set; } = new List<Task>();
    }
}
