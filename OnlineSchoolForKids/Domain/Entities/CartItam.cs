namespace Domain.Entities
{
    public class CartItem : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public string CourseId { get; set; } = string.Empty;
        public decimal Price { get; set; }

        // Navigation Properties (Ignored in MongoDB)
        public User User { get; set; } = null!;
        public Course Course { get; set; } = null!;
    }
}