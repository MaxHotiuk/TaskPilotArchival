using System;

namespace Core.DTOs
{
    public class StateDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Order { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
