using System;

namespace Core.Entities
{
    public class BoardMember
    {
        public Guid BoardId { get; set; }
        public Guid UserId { get; set; }
        public string Role { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public Board Board { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
