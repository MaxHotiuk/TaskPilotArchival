using System;

namespace Core.DTOs
{
    public class CommentDto
    {
        public Guid Id { get; set; }
        public Guid AuthorId { get; set; }
        public string? Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
