using System;
using System.Collections.Generic;

namespace Core.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public string EntraId { get; set; } = null!;
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Role { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<Board> OwnedBoards { get; set; } = new List<Board>();
        public ICollection<BoardMember> BoardMemberships { get; set; } = new List<BoardMember>();
        public ICollection<Task> AssignedTasks { get; set; } = new List<Task>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
