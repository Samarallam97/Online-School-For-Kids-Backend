using Domain.Entities.Content;
using Domain.Enums.Content;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure
{
    public class MongoDbSeeder
    {
        private readonly IMongoDatabase _database;

        public MongoDbSeeder(IMongoDatabase database)
        {
            _database = database;
        }

        public async Task SeedDataAsync()
        {
            var categoriesCollection = _database.GetCollection<Category>("categories");
            var coursesCollection = _database.GetCollection<Course>("courses");

            // Check if data already exists
            if (await categoriesCollection.CountDocumentsAsync(FilterDefinition<Category>.Empty) > 0)
            {
                return; // Data already seeded
            }

            // Seed Categories
            var categories = new List<Category>
        {
            new Category
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = "Web Development",
                IconUrl = "https://cdn-icons-png.flaticon.com/512/1005/1005141.png",
                DisplayOrder = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new Category
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = "Mobile Development",
                IconUrl = "https://cdn-icons-png.flaticon.com/512/2941/2941807.png",
                DisplayOrder = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new Category
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = "Data Science",
                IconUrl = "https://cdn-icons-png.flaticon.com/512/2103/2103832.png",
                DisplayOrder = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new Category
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = "Artificial Intelligence",
                IconUrl = "https://cdn-icons-png.flaticon.com/512/4712/4712027.png",
                DisplayOrder = 4,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new Category
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = "Cloud Computing",
                IconUrl = "https://cdn-icons-png.flaticon.com/512/4215/4215428.png",
                DisplayOrder = 5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new Category
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = "Cybersecurity",
                IconUrl = "https://cdn-icons-png.flaticon.com/512/1087/1087815.png",
                DisplayOrder = 6,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new Category
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = "UI/UX Design",
                IconUrl = "https://cdn-icons-png.flaticon.com/512/3281/3281289.png",
                DisplayOrder = 7,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new Category
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = "DevOps",
                IconUrl = "https://cdn-icons-png.flaticon.com/512/2103/2103508.png",
                DisplayOrder = 8,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            }
        };

            await categoriesCollection.InsertManyAsync(categories);

            // Seed Courses
            var courses = new List<Course>
        {
            // Web Development Courses
            new Course
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Title = "Complete ASP.NET Core Bootcamp 2024",
                Description = "Master ASP.NET Core development from scratch. Learn MVC, Web API, Entity Framework Core, Identity, and deploy production-ready applications.",
                InstructorId = ObjectId.GenerateNewId().ToString(),
                CategoryId = categories[0].Id,
                Level = CourseLevel.Beginner,
                Price = 99.99m,
                DiscountPrice = 79.99m,
                Rating = 4.5m,
                TotalStudents = 1500,
                DurationHours = 40,
                ThumbnailUrl = "https://images.unsplash.com/photo-1516116216624-53e697fedbea?w=800",
                Language = "English",
                IsPublished = true,
                IsFeatured = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new Course
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Title = "Advanced React & Redux Masterclass",
                Description = "Deep dive into React, Redux, Hooks, Context API, and modern JavaScript. Build scalable web applications with best practices.",
                InstructorId = ObjectId.GenerateNewId().ToString(),
                CategoryId = categories[0].Id,
                Level = CourseLevel.Advanced,
                Price = 119.99m,
                DiscountPrice = 89.99m,
                Rating = 4.8m,
                TotalStudents = 3200,
                DurationHours = 55,
                ThumbnailUrl = "https://images.unsplash.com/photo-1633356122544-f134324a6cee?w=800",
                Language = "English",
                IsPublished = true,
                IsFeatured = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new Course
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Title = "Full Stack Web Development with Node.js",
                Description = "Learn full stack development with Node.js, Express, MongoDB, and React. Build and deploy complete web applications.",
                InstructorId = ObjectId.GenerateNewId().ToString(),
                CategoryId = categories[0].Id,
                Level = CourseLevel.Intermediate,
                Price = 109.99m,
                DiscountPrice = null,
                Rating = 4.6m,
                TotalStudents = 2100,
                DurationHours = 48,
                ThumbnailUrl = "https://images.unsplash.com/photo-1627398242454-45a1465c2479?w=800",
                Language = "English",
                IsPublished = true,
                IsFeatured = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },

            // Mobile Development Courses
            new Course
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Title = "React Native - Build iOS & Android Apps",
                Description = "Build professional mobile applications for iOS and Android using React Native. Learn navigation, state management, and native modules.",
                InstructorId = ObjectId.GenerateNewId().ToString(),
                CategoryId = categories[1].Id,
                Level = CourseLevel.Intermediate,
                Price = 129.99m,
                DiscountPrice = 99.99m,
                Rating = 4.7m,
                TotalStudents = 2800,
                DurationHours = 52,
                ThumbnailUrl = "https://images.unsplash.com/photo-1512941937669-90a1b58e7e9c?w=800",
                Language = "English",
                IsPublished = true,
                IsFeatured = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new Course
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Title = "Flutter & Dart Complete Development Course",
                Description = "Master Flutter and Dart to build beautiful, natively compiled mobile, web, and desktop applications from a single codebase.",
                InstructorId = ObjectId.GenerateNewId().ToString(),
                CategoryId = categories[1].Id,
                Level = CourseLevel.Beginner,
                Price = 94.99m,
                DiscountPrice = 74.99m,
                Rating = 4.9m,
                TotalStudents = 4500,
                DurationHours = 45,
                ThumbnailUrl = "https://images.unsplash.com/photo-1551650975-87deedd944c3?w=800",
                Language = "English",
                IsPublished = true,
                IsFeatured = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },

            // Data Science Courses
            new Course
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Title = "Python for Data Science & Machine Learning",
                Description = "Complete guide to Python programming for data analysis, visualization, and machine learning. Master NumPy, Pandas, Matplotlib, and Scikit-learn.",
                InstructorId = ObjectId.GenerateNewId().ToString(),
                CategoryId = categories[2].Id,
                Level = CourseLevel.Intermediate,
                Price = 149.99m,
                DiscountPrice = 119.99m,
                Rating = 4.8m,
                TotalStudents = 5200,
                DurationHours = 65,
                ThumbnailUrl = "https://images.unsplash.com/photo-1551288049-bebda4e38f71?w=800",
                Language = "English",
                IsPublished = true,
                IsFeatured = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new Course
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Title = "Data Analysis with SQL & Tableau",
                Description = "Learn SQL for data analysis and create stunning visualizations with Tableau. Perfect for aspiring data analysts.",
                InstructorId = ObjectId.GenerateNewId().ToString(),
                CategoryId = categories[2].Id,
                Level = CourseLevel.Beginner,
                Price = 79.99m,
                DiscountPrice = null,
                Rating = 4.5m,
                TotalStudents = 1800,
                DurationHours = 35,
                ThumbnailUrl = "https://images.unsplash.com/photo-1460925895917-afdab827c52f?w=800",
                Language = "English",
                IsPublished = true,
                IsFeatured = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },

            // AI Courses
            new Course
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Title = "Deep Learning & Neural Networks A-Z",
                Description = "Master deep learning and build neural networks with TensorFlow and Keras. Learn CNNs, RNNs, GANs, and transformers.",
                InstructorId = ObjectId.GenerateNewId().ToString(),
                CategoryId = categories[3].Id,
                Level = CourseLevel.Advanced,
                Price = 159.99m,
                DiscountPrice = 129.99m,
                Rating = 4.9m,
                TotalStudents = 3800,
                DurationHours = 70,
                ThumbnailUrl = "https://images.unsplash.com/photo-1677442136019-21780ecad995?w=800",
                Language = "English",
                IsPublished = true,
                IsFeatured = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },

            // Cloud Computing Courses
            new Course
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Title = "AWS Certified Solutions Architect 2024",
                Description = "Complete preparation course for AWS Solutions Architect certification. Learn EC2, S3, Lambda, VPC, and more.",
                InstructorId = ObjectId.GenerateNewId().ToString(),
                CategoryId = categories[4].Id,
                Level = CourseLevel.Intermediate,
                Price = 139.99m,
                DiscountPrice = 109.99m,
                Rating = 4.7m,
                TotalStudents = 6200,
                DurationHours = 58,
                ThumbnailUrl = "https://images.unsplash.com/photo-1451187580459-43490279c0fa?w=800",
                Language = "English",
                IsPublished = true,
                IsFeatured = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new Course
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Title = "Microsoft Azure Fundamentals",
                Description = "Get started with Microsoft Azure cloud services. Learn Azure basics and prepare for AZ-900 certification.",
                InstructorId = ObjectId.GenerateNewId().ToString(),
                CategoryId = categories[4].Id,
                Level = CourseLevel.Beginner,
                Price = 89.99m,
                DiscountPrice = 69.99m,
                Rating = 4.6m,
                TotalStudents = 2900,
                DurationHours = 42,
                ThumbnailUrl = "https://images.unsplash.com/photo-1544197150-b99a580bb7a8?w=800",
                Language = "English",
                IsPublished = true,
                IsFeatured = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },

            // Cybersecurity Courses
            new Course
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Title = "Ethical Hacking & Penetration Testing",
                Description = "Learn ethical hacking techniques, penetration testing, and network security. Prepare for CEH certification.",
                InstructorId = ObjectId.GenerateNewId().ToString(),
                CategoryId = categories[5].Id,
                Level = CourseLevel.Advanced,
                Price = 169.99m,
                DiscountPrice = 139.99m,
                Rating = 4.8m,
                TotalStudents = 4100,
                DurationHours = 75,
                ThumbnailUrl = "https://images.unsplash.com/photo-1550751827-4bd374c3f58b?w=800",
                Language = "English",
                IsPublished = true,
                IsFeatured = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },

            // UI/UX Design Courses
            new Course
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Title = "Complete UI/UX Design Bootcamp",
                Description = "Master user interface and user experience design with Figma. Learn design principles, wireframing, prototyping, and user research.",
                InstructorId = ObjectId.GenerateNewId().ToString(),
                CategoryId = categories[6].Id,
                Level = CourseLevel.Beginner,
                Price = 99.99m,
                DiscountPrice = 79.99m,
                Rating = 4.7m,
                TotalStudents = 3500,
                DurationHours = 44,
                ThumbnailUrl = "https://images.unsplash.com/photo-1561070791-2526d30994b5?w=800",
                Language = "English",
                IsPublished = true,
                IsFeatured = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },

            // DevOps Courses
            new Course
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Title = "Docker & Kubernetes Masterclass",
                Description = "Master containerization with Docker and orchestration with Kubernetes. Learn CI/CD pipelines and microservices deployment.",
                InstructorId = ObjectId.GenerateNewId().ToString(),
                CategoryId = categories[7].Id,
                Level = CourseLevel.Intermediate,
                Price = 134.99m,
                DiscountPrice = 104.99m,
                Rating = 4.9m,
                TotalStudents = 5800,
                DurationHours = 62,
                ThumbnailUrl = "https://images.unsplash.com/photo-1605745341075-d2eaa754f29c?w=800",
                Language = "English",
                IsPublished = true,
                IsFeatured = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            }
        };

            await coursesCollection.InsertManyAsync(courses);
        }
    }
}
