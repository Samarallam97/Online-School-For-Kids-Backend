namespace Domain.Entities.Content
{
    public class Category : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public int? CoursesCount { get; set; }
    }
}
