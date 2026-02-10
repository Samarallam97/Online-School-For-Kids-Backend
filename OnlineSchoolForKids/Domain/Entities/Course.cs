using Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Domain.Entities
{

    public class Course : BaseEntity
    {
        [BsonElement("title")]
        public string Title { get; set; } = string.Empty;

        [BsonElement("description")]
        public string Description { get; set; } = string.Empty;

        [BsonElement("instructorId")]
        public string InstructorId { get; set; } = string.Empty;

        [BsonElement("categoryId")]
        public string CategoryId { get; set; } = string.Empty;

        [BsonElement("level")]
        public CourseLevel Level { get; set; }

        [BsonElement("price")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Price { get; set; }

        [BsonElement("discountPrice")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal? DiscountPrice { get; set; }

        [BsonElement("rating")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Rating { get; set; }

        [BsonElement("totalStudents")]
        public int TotalStudents { get; set; }

        [BsonElement("durationHours")]
        public int DurationHours { get; set; }

        [BsonElement("thumbnailUrl")]
        public string ThumbnailUrl { get; set; } = string.Empty;

        [BsonElement("language")]
        public string Language { get; set; } = "English";

        [BsonElement("isPublished")]
        public bool IsPublished { get; set; }

        [BsonElement("isFeatured")]
        public bool IsFeatured { get; set; }

        // Navigation Properties
        [BsonIgnore]
        public Category Category { get; set; } = null!;

        [BsonIgnore]
        public ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
    }
}