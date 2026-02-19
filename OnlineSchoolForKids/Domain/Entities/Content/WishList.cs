using Domain.Entities.Users;

namespace Domain.Entities.Content
{
    public class Wishlist : BaseEntity
    {

        public string UserId { get; set; } = string.Empty;
        public string CourseId { get; set; } = string.Empty;


        // Navigation Properties
        public User User { get; set; } = null!;
        public Course Course { get; set; } = null!;
    }
}
