using System;

namespace Core.DTOs
{
    public class BoardMemberDto
    {
        public Guid UserId { get; set; }
        public string? Role { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
