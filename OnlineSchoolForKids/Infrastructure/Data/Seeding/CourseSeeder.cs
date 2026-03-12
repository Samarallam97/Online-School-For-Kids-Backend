using Domain.Entities.Content;
using Domain.Entities.Content.Progress;
using Domain.Enums.Content;
using Domain.Interfaces.Repositories.Content;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace Infrastructure.Data.Seeding;

public static class CourseSeeder
{
    public static async Task SeedAsync(IHost app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Course>>();

        try
        {
            var courseRepo = services.GetRequiredService<ICourseRepository>();
            var categoryRepo = services.GetRequiredService<ICategoryRepository>();

            var existing = await courseRepo.GetAllAsync();
            if (existing.Any())
            {
                logger.LogInformation("Courses already seeded — skipping.");
                return;
            }

            var allCategories = await categoryRepo.GetAllAsync();

            var categoryDefs = new (
                string Name, int DisplayOrder,
                string Description, string ImageUrl
            )[]
            {
                ("Early Childhood Education",1,"Nurturing the youngest minds through play, wonder, and exploration for ages 2–6.","https://images.unsplash.com/photo-1503676260728-1c00da094a0b?w=800&q=80"),
                ("Primary Education",2,"Build strong academic foundations with proven strategies for children ages 6–12.","https://images.unsplash.com/photo-1427504494785-3a9ca7044f45?w=800&q=80"),
                ("Child Development",3,"Science-backed insights into how children grow, think, and feel — from birth to adolescence.","https://images.unsplash.com/photo-1555252333-9f8e92e65df9?w=800&q=80"),
                ("STEM for Kids",4,"Ignite curiosity in science, technology, engineering, and maths through hands-on fun.","https://images.unsplash.com/photo-1532094349884-543559a8f8e3?w=800&q=80"),
                ("Arts & Creativity",5,"Unleash self-expression through drawing, music, drama, and crafts.","https://images.unsplash.com/photo-1452860606245-08befc0ff44b?w=800&q=80"),
                ("Language & Literacy",6,"From phonics to creative writing and bilingual learning — build confident communicators.","https://images.unsplash.com/photo-1456513080510-7bf3a84b82f8?w=800&q=80"),
                ("Special Needs & Inclusion",7,"Empowering educators and parents to support children with diverse learning needs.","https://images.unsplash.com/photo-1573497019940-1c28c88b4f3e?w=800&q=80"),
                ("Social & Emotional Learning",8,"Help children develop empathy, resilience, and strong social skills for lifelong wellbeing.","https://images.unsplash.com/photo-1529156069898-49953e39b3ac?w=800&q=80"),
                ("Physical Education & Health",9,"Inspire healthy habits, movement, and active play that support overall wellbeing.","https://images.unsplash.com/photo-1472162072942-cd5147eb3902?w=800&q=80"),
                ("Parenting & Family",10,"Compassionate, practical guidance for every stage of parenthood — newborns to teens.","https://images.unsplash.com/photo-1476703993599-0035a21b17a9?w=800&q=80"),
            };

            var categoryMap = new Dictionary<string, string>();

            foreach (var def in categoryDefs)
            {
                var cat = allCategories.FirstOrDefault(c =>
                    c.Name.Equals(def.Name, StringComparison.OrdinalIgnoreCase));

                if (cat is null)
                {
                    cat = new Category
                    {
                        Id           = ObjectId.GenerateNewId().ToString(),
                        Name         = def.Name,
                        DisplayOrder = def.DisplayOrder,
                        Description  = def.Description,
                        ImageUrl     = def.ImageUrl,
                    };
                    await categoryRepo.CreateAsync(cat, CancellationToken.None);
                }
                categoryMap[def.Name] = cat.Id;
            }

            const string instructorId = "699b5f2e060aeacaafa75dae";

            // ── Helper: build a Section with Lessons ──────────────────────────
            static Section MakeSection(string courseId, int order, string title,
                IEnumerable<(string Title, int DurationSecs, bool IsFree)> lessons)
            {
                var lessonList = lessons.Select((l, i) => new Lesson
                {
                    Id          = ObjectId.GenerateNewId().ToString(),
                    CourseId    = courseId,
                    SectionId   = string.Empty, // filled below
                    Title       = l.Title,
                    Duration    = l.DurationSecs,
                    Order       = i + 1,
                    IsFree      = l.IsFree,
                    IsPreview   = l.IsFree,
                    IsPublished = true,
                    VideoUrl    = string.Empty,
                    Materials   = new List<Material>(),
                }).ToList();

                var section = new Section
                {
                    Id           = ObjectId.GenerateNewId().ToString(),
                    CourseId     = courseId,
                    Title        = title,
                    Order        = order,
                    IsPublished  = true,
                    LessonsCount = lessonList.Count,
                    Lessons      = lessonList,
                };

                // back-fill SectionId on every lesson
                foreach (var l in lessonList) l.SectionId = section.Id;
                return section;
            }

            // ══════════════════════════════════════════════════════════════════
            // FEATURED COURSE — mirrors the mock data exactly
            // ══════════════════════════════════════════════════════════════════
            var featuredId = ObjectId.GenerateNewId().ToString();
            var featuredSections = new List<Section>
            {
                MakeSection(featuredId, 1, "Front-End Web Development", new[]
                {
                    ("What You'll Get From This Course",  272, true),
                    ("How to Get Help",                   135, true),
                    ("How Websites Work",                 765, false),
                    ("Your First Webpage",               1110, false),
                    ("HTML Tags and Attributes",          920, false),
                }),
                MakeSection(featuredId, 2, "Introduction to HTML", new[]
                {
                    ("HTML Document Structure",  615, false),
                    ("Headings and Paragraphs",  525, false),
                    ("Lists and Links",          750, false),
                }),
                MakeSection(featuredId, 3, "CSS Styling", new[]
                {
                    ("Introduction to CSS",  860,  false),
                    ("CSS Selectors",        1125, false),
                    ("CSS Box Model",        1330, false),
                }),
                MakeSection(featuredId, 4, "JavaScript Fundamentals", new[]
                {
                    ("Variables and Data Types", 1215, false),
                    ("Functions",                1530, false),
                    ("DOM Manipulation",         1845, false),
                }),
            };

            var featured = new Course
            {
                Id           = featuredId,
                Title        = "Complete Web Development Bootcamp 2024",
                Description  = "Welcome to the Complete Web Development Bootcamp, the only course you need to learn to code and become a full-stack web developer.\n\nAt 52+ hours, this Web Development course is without a doubt the most comprehensive web development course available online. Even if you have zero programming experience, this course will take you from beginner to mastery.\n\nThe course includes over 52 hours of HD video tutorials and builds 16 real-world projects with step-by-step guidance. By the end of this course, you will be fluent in cutting-edge front-end and back-end technologies.",
                InstructorId = instructorId,
                CategoryId   = categoryMap["STEM for Kids"],
                AgeGroup     = AgeGroup.Teenagers,
                Price        = 199.99m,
                DiscountPrice= 89.99m,
                Rating       = 4.8m,
                TotalStudents= 567890,
                DurationHours= 52,
                ThumbnailUrl = "https://images.unsplash.com/photo-1498050108023-c5249f4df085?w=800",
                Language     = "English",
                IsPublished  = true,
                IsFeatured   = true,
                IsVisible    = true,
                Sections     = featuredSections,
            };

            // ══════════════════════════════════════════════════════════════════
            // ALL OTHER COURSES — each gets 3 realistic sections with lessons
            // ══════════════════════════════════════════════════════════════════

            Course WithSections(Course c, List<Section> sections)
            {
                c.Sections = sections;
                return c;
            }

            var courses = new List<Course> { featured };

            // ── Early Childhood Education ─────────────────────────────────────
            var c1Id = ObjectId.GenerateNewId().ToString();
            courses.Add(WithSections(new Course { Id = c1Id, Title = "Foundations of Early Childhood Education", Description = "A comprehensive introduction to teaching children ages 3–6, covering play-based learning, classroom management, and developmental milestones.", InstructorId = instructorId, CategoryId = categoryMap["Early Childhood Education"], AgeGroup = AgeGroup.ForEducators, Price = 49.99m, DiscountPrice = 29.99m, Rating = 4.8m, TotalStudents = 12400, DurationHours = 22, ThumbnailUrl = "https://images.unsplash.com/photo-1503676260728-1c00da094a0b?w=600", Language = "English", IsPublished = true, IsFeatured = true, IsVisible = true }, new List<Section>
            {
                MakeSection(c1Id, 1, "Introduction to Early Childhood", new[] { ("Course Overview", 272, true), ("Why Early Years Matter", 480, true), ("Key Theorists & Frameworks", 720, false), ("Learning Environments", 540, false) }),
                MakeSection(c1Id, 2, "Play-Based Learning", new[] { ("Types of Play", 600, false), ("Setting Up Play Spaces", 780, false), ("Observing & Documenting Play", 660, false) }),
                MakeSection(c1Id, 3, "Developmental Milestones", new[] { ("Cognitive Development Ages 3–6", 840, false), ("Language Development", 720, false), ("Social-Emotional Development", 900, false) }),
            }));

            var c2Id = ObjectId.GenerateNewId().ToString();
            courses.Add(WithSections(new Course { Id = c2Id, Title = "Montessori Method for Early Learners", Description = "Learn the core principles of Montessori education and how to apply them at home or in the classroom for children aged 2–7.", InstructorId = instructorId, CategoryId = categoryMap["Early Childhood Education"], AgeGroup = AgeGroup.ForParents, Price = 59.99m, DiscountPrice = 39.99m, Rating = 4.9m, TotalStudents = 8700, DurationHours = 18, ThumbnailUrl = "https://images.unsplash.com/photo-1544717305-2782549b5136?w=600", Language = "English", IsPublished = true, IsFeatured = true, IsVisible = true }, new List<Section>
            {
                MakeSection(c2Id, 1, "Montessori Philosophy", new[] { ("What Is Montessori?", 300, true), ("Maria Montessori's Legacy", 420, true), ("The Prepared Environment", 600, false) }),
                MakeSection(c2Id, 2, "Practical Life Activities", new[] { ("Pouring & Sorting", 540, false), ("Dressing Frames", 480, false), ("Care of the Environment", 600, false) }),
                MakeSection(c2Id, 3, "Sensorial & Academic Materials", new[] { ("Pink Tower & Knobbed Cylinders", 720, false), ("Sandpaper Letters", 660, false), ("Number Rods", 780, false) }),
            }));

            var c3Id = ObjectId.GenerateNewId().ToString();
            courses.Add(WithSections(new Course { Id = c3Id, Title = "Sensory Play & Learning Activities", Description = "Discover how sensory play builds neural pathways in young children. Includes 50+ activity ideas for toddlers and preschoolers.", InstructorId = instructorId, CategoryId = categoryMap["Early Childhood Education"], AgeGroup = AgeGroup.Toddlers, Price = 34.99m, DiscountPrice = null, Rating = 4.7m, TotalStudents = 5300, DurationHours = 10, ThumbnailUrl = "https://images.unsplash.com/photo-1596464716127-f2a82984de30?w=600", Language = "English", IsPublished = true, IsFeatured = false, IsVisible = true }, new List<Section>
            {
                MakeSection(c3Id, 1, "The Science of Sensory Play", new[] { ("Why Sensory Play?", 240, true), ("Brain Development & the Senses", 540, false), ("Safety Considerations", 360, false) }),
                MakeSection(c3Id, 2, "Messy Play Activities", new[] { ("Slime & Gloop", 480, false), ("Sand & Water Play", 420, false), ("Finger Painting", 360, false) }),
                MakeSection(c3Id, 3, "Calm Sensory Activities", new[] { ("Sensory Bins", 480, false), ("Light Tables", 420, false), ("Texture Boards", 300, false) }),
            }));

            // ── Primary Education ─────────────────────────────────────────────
            var c4Id = ObjectId.GenerateNewId().ToString();
            courses.Add(WithSections(new Course { Id = c4Id, Title = "Teaching Mathematics to Primary School Children", Description = "Practical strategies and manipulative-based techniques to make maths engaging and accessible for children ages 6–12.", InstructorId = instructorId, CategoryId = categoryMap["Primary Education"], AgeGroup = AgeGroup.EarlyPrimary, Price = 54.99m, DiscountPrice = 34.99m, Rating = 4.8m, TotalStudents = 14200, DurationHours = 24, ThumbnailUrl = "https://images.unsplash.com/photo-1509228468518-180dd4864904?w=600", Language = "English", IsPublished = true, IsFeatured = true, IsVisible = true }, new List<Section>
            {
                MakeSection(c4Id, 1, "Number Sense Foundations", new[] { ("Counting Strategies", 360, true), ("Place Value with Manipulatives", 600, true), ("Addition & Subtraction Fluency", 720, false), ("Word Problems Made Easy", 660, false) }),
                MakeSection(c4Id, 2, "Multiplication & Division", new[] { ("Arrays & Equal Groups", 540, false), ("Times Tables Strategies", 780, false), ("Long Division Step-by-Step", 900, false) }),
                MakeSection(c4Id, 3, "Fractions & Geometry", new[] { ("Introducing Fractions", 720, false), ("Equivalent Fractions", 660, false), ("2D & 3D Shapes", 600, false) }),
            }));

            var c5Id = ObjectId.GenerateNewId().ToString();
            courses.Add(WithSections(new Course { Id = c5Id, Title = "Differentiated Instruction in the Primary Classroom", Description = "Strategies to meet every learner's needs — from gifted students to those with learning differences — within a single classroom.", InstructorId = instructorId, CategoryId = categoryMap["Primary Education"], AgeGroup = AgeGroup.ForEducators, Price = 74.99m, DiscountPrice = 54.99m, Rating = 4.9m, TotalStudents = 5600, DurationHours = 30, ThumbnailUrl = "https://images.unsplash.com/photo-1497633762265-9d179a990aa6?w=600", Language = "English", IsPublished = true, IsFeatured = true, IsVisible = true }, new List<Section>
            {
                MakeSection(c5Id, 1, "Understanding Differentiation", new[] { ("What Is DI?", 300, true), ("Learning Styles & Profiles", 540, true), ("Pre-Assessment Strategies", 660, false) }),
                MakeSection(c5Id, 2, "Tiered Tasks & Flexible Grouping", new[] { ("Designing Tiered Activities", 780, false), ("Flexible Grouping Structures", 720, false), ("Learning Centres", 840, false) }),
                MakeSection(c5Id, 3, "Assessment & Feedback", new[] { ("Formative Assessment Tools", 720, false), ("Giving Effective Feedback", 660, false), ("Tracking Individual Progress", 600, false) }),
            }));

            // ── Child Development ─────────────────────────────────────────────
            var c6Id = ObjectId.GenerateNewId().ToString();
            courses.Add(WithSections(new Course { Id = c6Id, Title = "Understanding Child Development: Ages 0–12", Description = "A deep dive into cognitive, social, emotional, and physical development stages from infancy through middle childhood.", InstructorId = instructorId, CategoryId = categoryMap["Child Development"], AgeGroup = AgeGroup.ForParents, Price = 49.99m, DiscountPrice = 29.99m, Rating = 4.8m, TotalStudents = 18600, DurationHours = 20, ThumbnailUrl = "https://images.unsplash.com/photo-1555252333-9f8e92e65df9?w=600", Language = "English", IsPublished = true, IsFeatured = true, IsVisible = true }, new List<Section>
            {
                MakeSection(c6Id, 1, "Infancy & Toddlerhood (0–3)", new[] { ("Newborn Reflexes & Senses", 480, true), ("Attachment Theory", 600, true), ("Language Acquisition Begins", 540, false) }),
                MakeSection(c6Id, 2, "Early Childhood (3–6)", new[] { ("Piaget's Preoperational Stage", 720, false), ("Play & Social Development", 660, false), ("Emotional Regulation", 780, false) }),
                MakeSection(c6Id, 3, "Middle Childhood (6–12)", new[] { ("Concrete Operational Thinking", 720, false), ("Peer Relationships", 660, false), ("Self-Concept & Motivation", 600, false) }),
            }));

            var c7Id = ObjectId.GenerateNewId().ToString();
            courses.Add(WithSections(new Course { Id = c7Id, Title = "Child Psychology: Behaviour & Learning", Description = "Understand the psychological principles behind how children think, learn, and behave.", InstructorId = instructorId, CategoryId = categoryMap["Child Development"], AgeGroup = AgeGroup.ForEducators, Price = 64.99m, DiscountPrice = 44.99m, Rating = 4.9m, TotalStudents = 11300, DurationHours = 26, ThumbnailUrl = "https://images.unsplash.com/photo-1503454537195-1dcabb73ffb9?w=600", Language = "English", IsPublished = true, IsFeatured = true, IsVisible = true }, new List<Section>
            {
                MakeSection(c7Id, 1, "Theories of Learning", new[] { ("Behaviourism in the Classroom", 600, true), ("Vygotsky's Zone of Proximal Development", 720, false), ("Bandura's Social Learning Theory", 660, false) }),
                MakeSection(c7Id, 2, "Motivation & Behaviour", new[] { ("Intrinsic vs Extrinsic Motivation", 540, false), ("Positive Behaviour Support", 780, false), ("Handling Challenging Behaviour", 900, false) }),
                MakeSection(c7Id, 3, "Cognitive Development", new[] { ("Executive Function Skills", 720, false), ("Memory & Learning", 660, false), ("Critical Thinking in Children", 600, false) }),
            }));

            // ── STEM for Kids ─────────────────────────────────────────────────
            var c8Id = ObjectId.GenerateNewId().ToString();
            courses.Add(WithSections(new Course { Id = c8Id, Title = "Teaching Coding to Kids with Scratch", Description = "A beginner-friendly course teaching children ages 7–14 to code using Scratch.", InstructorId = instructorId, CategoryId = categoryMap["STEM for Kids"], AgeGroup = AgeGroup.EarlyPrimary, Price = 44.99m, DiscountPrice = 24.99m, Rating = 4.9m, TotalStudents = 31500, DurationHours = 16, ThumbnailUrl = "https://images.unsplash.com/photo-1580894742597-87bc8789db3d?w=600", Language = "English", IsPublished = true, IsFeatured = true, IsVisible = true }, new List<Section>
            {
                MakeSection(c8Id, 1, "Getting Started with Scratch", new[] { ("What Is Scratch?", 272, true), ("Creating Your Account", 180, true), ("The Scratch Interface Tour", 480, false), ("Your First Sprite", 540, false) }),
                MakeSection(c8Id, 2, "Animation & Movement", new[] { ("Move with Arrow Keys", 600, false), ("Costumes & Animations", 720, false), ("Broadcasting Messages", 660, false) }),
                MakeSection(c8Id, 3, "Build a Complete Game", new[] { ("Design Your Game Level", 780, false), ("Add Score & Lives", 840, false), ("Publish & Share", 420, false) }),
            }));

            var c9Id = ObjectId.GenerateNewId().ToString();
            courses.Add(WithSections(new Course { Id = c9Id, Title = "Robotics for Kids: LEGO Mindstorms", Description = "Guide children through building and programming LEGO Mindstorms robots.", InstructorId = instructorId, CategoryId = categoryMap["STEM for Kids"], AgeGroup = AgeGroup.LatePrimary, Price = 69.99m, DiscountPrice = 49.99m, Rating = 4.8m, TotalStudents = 8900, DurationHours = 22, ThumbnailUrl = "https://images.unsplash.com/photo-1561144257-e32e8506646d?w=600", Language = "English", IsPublished = true, IsFeatured = true, IsVisible = true }, new List<Section>
            {
                MakeSection(c9Id, 1, "Introduction to Robotics", new[] { ("What Is a Robot?", 300, true), ("LEGO Mindstorms Overview", 480, true), ("Building Your First Robot", 900, false) }),
                MakeSection(c9Id, 2, "Programming with LEGO", new[] { ("Move & Steer Blocks", 720, false), ("Sensors & Loops", 840, false), ("Line Following Program", 960, false) }),
                MakeSection(c9Id, 3, "Robotics Challenge Projects", new[] { ("Maze Navigation", 1020, false), ("Sumo Bot Challenge", 900, false), ("Showcase Your Robot", 480, false) }),
            }));

            // ── Arts & Creativity ─────────────────────────────────────────────
            var c10Id = ObjectId.GenerateNewId().ToString();
            courses.Add(WithSections(new Course { Id = c10Id, Title = "Drawing & Painting for Children", Description = "Step-by-step drawing and painting lessons for children ages 5–12.", InstructorId = instructorId, CategoryId = categoryMap["Arts & Creativity"], AgeGroup = AgeGroup.EarlyPrimary, Price = 34.99m, DiscountPrice = 19.99m, Rating = 4.8m, TotalStudents = 24600, DurationHours = 14, ThumbnailUrl = "https://images.unsplash.com/photo-1452860606245-08befc0ff44b?w=600", Language = "English", IsPublished = true, IsFeatured = true, IsVisible = true }, new List<Section>
            {
                MakeSection(c10Id, 1, "Drawing Fundamentals", new[] { ("Lines, Shapes & Forms", 360, true), ("Shading Techniques", 540, true), ("Drawing from Observation", 660, false) }),
                MakeSection(c10Id, 2, "Painting Basics", new[] { ("Watercolour Washes", 600, false), ("Colour Mixing", 540, false), ("Brushwork Techniques", 480, false) }),
                MakeSection(c10Id, 3, "Creative Projects", new[] { ("Draw an Animal Portrait", 780, false), ("Paint a Landscape", 840, false), ("Mixed Media Artwork", 720, false) }),
            }));

            var c11Id = ObjectId.GenerateNewId().ToString();
            courses.Add(WithSections(new Course { Id = c11Id, Title = "Music Education for Young Children", Description = "Introduce children ages 3–8 to rhythm, melody, and basic music theory.", InstructorId = instructorId, CategoryId = categoryMap["Arts & Creativity"], AgeGroup = AgeGroup.Preschool, Price = 39.99m, DiscountPrice = null, Rating = 4.9m, TotalStudents = 17300, DurationHours = 12, ThumbnailUrl = "https://images.unsplash.com/photo-1514320291840-2e0a9bf2a9ae?w=600", Language = "English", IsPublished = true, IsFeatured = false, IsVisible = true }, new List<Section>
            {
                MakeSection(c11Id, 1, "Rhythm & Beat", new[] { ("Clapping Rhythms", 300, true), ("Percussion Instruments", 480, false), ("Keep the Beat Activities", 420, false) }),
                MakeSection(c11Id, 2, "Melody & Singing", new[] { ("Pitch Matching Games", 360, false), ("Simple Songs for Little Voices", 540, false), ("Movement & Music", 480, false) }),
                MakeSection(c11Id, 3, "Introduction to Music Theory", new[] { ("Notes on the Staff", 600, false), ("Treble Clef Basics", 540, false), ("Reading Simple Sheet Music", 660, false) }),
            }));

            // ── Language & Literacy ───────────────────────────────────────────
            var c12Id = ObjectId.GenerateNewId().ToString();
            courses.Add(WithSections(new Course { Id = c12Id, Title = "Teaching Reading: Phonics & Phonemic Awareness", Description = "A systematic phonics programme to help children ages 4–7 crack the reading code.", InstructorId = instructorId, CategoryId = categoryMap["Language & Literacy"], AgeGroup = AgeGroup.Preschool, Price = 44.99m, DiscountPrice = 29.99m, Rating = 4.9m, TotalStudents = 27800, DurationHours = 18, ThumbnailUrl = "https://images.unsplash.com/photo-1456513080510-7bf3a84b82f8?w=600", Language = "English", IsPublished = true, IsFeatured = true, IsVisible = true }, new List<Section>
            {
                MakeSection(c12Id, 1, "Phonemic Awareness", new[] { ("What Is a Phoneme?", 300, true), ("Rhyming & Alliteration", 420, true), ("Segmenting & Blending Sounds", 600, false) }),
                MakeSection(c12Id, 2, "Systematic Phonics", new[] { ("Consonants & Short Vowels", 720, false), ("Digraphs & Blends", 780, false), ("Long Vowel Patterns", 840, false) }),
                MakeSection(c12Id, 3, "Fluency & Comprehension", new[] { ("Decodable Readers", 660, false), ("Reading Aloud Strategies", 600, false), ("Comprehension Questions", 540, false) }),
            }));

            var c13Id = ObjectId.GenerateNewId().ToString();
            courses.Add(WithSections(new Course { Id = c13Id, Title = "Arabic Language for Children: Beginner", Description = "A fun, story-based Arabic course for children ages 5–10, teaching letters and basic conversation.", InstructorId = instructorId, CategoryId = categoryMap["Language & Literacy"], AgeGroup = AgeGroup.EarlyPrimary, Price = 49.99m, DiscountPrice = 34.99m, Rating = 4.8m, TotalStudents = 14500, DurationHours = 20, ThumbnailUrl = "https://images.unsplash.com/photo-1546410531-bb4caa6b424d?w=600", Language = "Arabic", IsPublished = true, IsFeatured = true, IsVisible = true }, new List<Section>
            {
                MakeSection(c13Id, 1, "The Arabic Alphabet", new[] { ("Letters ا to ذ", 480, true), ("Letters ر to ض", 540, true), ("Letters ط to غ", 600, false), ("Letters ف to ي", 600, false) }),
                MakeSection(c13Id, 2, "Basic Vocabulary", new[] { ("Colours & Numbers", 660, false), ("Family Members", 600, false), ("Animals & Food", 720, false) }),
                MakeSection(c13Id, 3, "Simple Conversations", new[] { ("Greetings & Introductions", 540, false), ("At School & Home", 660, false), ("Telling the Time", 600, false) }),
            }));

            // ── Special Needs & Inclusion ─────────────────────────────────────
            var c14Id = ObjectId.GenerateNewId().ToString();
            courses.Add(WithSections(new Course { Id = c14Id, Title = "Understanding Autism Spectrum Disorder", Description = "A compassionate, practical guide for parents and educators on supporting autistic children.", InstructorId = instructorId, CategoryId = categoryMap["Special Needs & Inclusion"], AgeGroup = AgeGroup.ForParents, Price = 59.99m, DiscountPrice = 39.99m, Rating = 4.9m, TotalStudents = 16200, DurationHours = 22, ThumbnailUrl = "https://images.unsplash.com/photo-1573497019940-1c28c88b4f3e?w=600", Language = "English", IsPublished = true, IsFeatured = true, IsVisible = true }, new List<Section>
            {
                MakeSection(c14Id, 1, "Understanding ASD", new[] { ("What Is the Autism Spectrum?", 480, true), ("Early Signs & Diagnosis", 600, true), ("Strengths-Based Approaches", 660, false) }),
                MakeSection(c14Id, 2, "Communication Strategies", new[] { ("AAC Tools & Visual Supports", 780, false), ("Social Stories", 660, false), ("Building Communication Routines", 720, false) }),
                MakeSection(c14Id, 3, "Supporting Daily Life", new[] { ("Sensory Environments at Home", 720, false), ("Transitions & Routines", 660, false), ("Working with Schools", 780, false) }),
            }));

            // ── Social & Emotional Learning ───────────────────────────────────
            var c15Id = ObjectId.GenerateNewId().ToString();
            courses.Add(WithSections(new Course { Id = c15Id, Title = "Teaching Empathy & Kindness in Early Childhood", Description = "Practical lessons to cultivate empathy and pro-social behaviour in young children ages 3–8.", InstructorId = instructorId, CategoryId = categoryMap["Social & Emotional Learning"], AgeGroup = AgeGroup.ForEducators, Price = 39.99m, DiscountPrice = 24.99m, Rating = 4.8m, TotalStudents = 13500, DurationHours = 12, ThumbnailUrl = "https://images.unsplash.com/photo-1529156069898-49953e39b3ac?w=600", Language = "English", IsPublished = true, IsFeatured = true, IsVisible = true }, new List<Section>
            {
                MakeSection(c15Id, 1, "The Foundations of Empathy", new[] { ("What Is Empathy?", 300, true), ("How Empathy Develops", 480, true), ("Mirror Neurons & Learning", 540, false) }),
                MakeSection(c15Id, 2, "Classroom Empathy Lessons", new[] { ("Circle Time Activities", 600, false), ("Emotion Vocabulary", 540, false), ("Perspective-Taking Games", 660, false) }),
                MakeSection(c15Id, 3, "Creating a Kind Classroom Culture", new[] { ("Random Acts of Kindness", 480, false), ("Conflict Resolution Role-Play", 600, false), ("Celebrating Differences", 540, false) }),
            }));

            // ── Physical Education & Health ───────────────────────────────────
            var c16Id = ObjectId.GenerateNewId().ToString();
            courses.Add(WithSections(new Course { Id = c16Id, Title = "PE Games & Sports for Primary Schools", Description = "A toolkit of cooperative games and fitness challenges for primary school PE lessons.", InstructorId = instructorId, CategoryId = categoryMap["Physical Education & Health"], AgeGroup = AgeGroup.ForEducators, Price = 44.99m, DiscountPrice = 29.99m, Rating = 4.8m, TotalStudents = 7700, DurationHours = 16, ThumbnailUrl = "https://images.unsplash.com/photo-1546519638405-a9d1b0e7e988?w=600", Language = "English", IsPublished = true, IsFeatured = true, IsVisible = true }, new List<Section>
            {
                MakeSection(c16Id, 1, "Cooperative Games", new[] { ("Why Cooperative PE?", 300, true), ("Tag Game Variations", 480, false), ("Team Building Challenges", 600, false) }),
                MakeSection(c16Id, 2, "Fundamental Movement Skills", new[] { ("Running & Jumping", 540, false), ("Throwing & Catching", 600, false), ("Balance & Coordination", 660, false) }),
                MakeSection(c16Id, 3, "Fitness Circuits & Sports", new[] { ("HIIT for Kids", 720, false), ("Mini Football & Basketball", 780, false), ("Athletics Day Activities", 660, false) }),
            }));

            // ── Parenting & Family ────────────────────────────────────────────
            var c17Id = ObjectId.GenerateNewId().ToString();
            courses.Add(WithSections(new Course { Id = c17Id, Title = "Positive Discipline: From Toddler to Teen", Description = "A whole-family approach to discipline that builds cooperation and mutual respect.", InstructorId = instructorId, CategoryId = categoryMap["Parenting & Family"], AgeGroup = AgeGroup.ForParents, Price = 54.99m, DiscountPrice = 34.99m, Rating = 4.9m, TotalStudents = 33200, DurationHours = 20, ThumbnailUrl = "https://images.unsplash.com/photo-1476703993599-0035a21b17a9?w=600", Language = "English", IsPublished = true, IsFeatured = true, IsVisible = true }, new List<Section>
            {
                MakeSection(c17Id, 1, "Foundations of Positive Discipline", new[] { ("Why Traditional Punishment Doesn't Work", 420, true), ("Mutual Respect Model", 540, true), ("Natural vs Logical Consequences", 720, false) }),
                MakeSection(c17Id, 2, "Age-Specific Strategies", new[] { ("Toddler Tantrums & Boundaries", 780, false), ("Primary Age Cooperation", 660, false), ("Teen Autonomy & Limits", 840, false) }),
                MakeSection(c17Id, 3, "Family Meetings & Connection", new[] { ("Running a Family Meeting", 600, false), ("Repair After Conflict", 660, false), ("Building Long-Term Connection", 720, false) }),
            }));

            var c18Id = ObjectId.GenerateNewId().ToString();
            courses.Add(WithSections(new Course { Id = c18Id, Title = "Newborn Care: The First 12 Weeks", Description = "Everything new parents need to know about feeding, sleep, and bonding in the early weeks.", InstructorId = instructorId, CategoryId = categoryMap["Parenting & Family"], AgeGroup = AgeGroup.ForParents, Price = 39.99m, DiscountPrice = 24.99m, Rating = 4.9m, TotalStudents = 28700, DurationHours = 14, ThumbnailUrl = "https://images.unsplash.com/photo-1555252333-9f8e92e65df9?w=600", Language = "English", IsPublished = true, IsFeatured = true, IsVisible = true }, new List<Section>
            {
                MakeSection(c18Id, 1, "Feeding Your Newborn", new[] { ("Breastfeeding Basics", 480, true), ("Bottle Feeding Guide", 420, true), ("Feeding Schedules", 540, false) }),
                MakeSection(c18Id, 2, "Sleep & Settling", new[] { ("Safe Sleep Guidelines", 480, false), ("Settling Techniques", 660, false), ("Sleep Cycles in Newborns", 540, false) }),
                MakeSection(c18Id, 3, "Bonding & Development", new[] { ("Skin-to-Skin Contact", 360, false), ("Tummy Time", 420, false), ("Tracking Milestones Week by Week", 600, false) }),
            }));

            foreach (var course in courses)
                await courseRepo.CreateAsync(course, CancellationToken.None);

            logger.LogInformation("Seeded {Count} courses across {CatCount} categories.", courses.Count, categoryMap.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to seed courses: {Message}", ex.Message);
            throw;
        }
    }
}