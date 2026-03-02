using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities.Content.Progress
{
    public class Note : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public string CourseId { get; set; } = string.Empty;
        public string LessonId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int? VideoPosition { get; set; }
    }
}
