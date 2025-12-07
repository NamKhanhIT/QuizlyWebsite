using QuizlyWebsite.Models;
using System.Security.Cryptography;
using System.Text;

namespace QuizlyWebsite.Data
{
    public class QuizlySeeder
    {
        private readonly QuizlyDbContext _context;
        private readonly ILogger<QuizlySeeder> _logger;

        public QuizlySeeder(QuizlyDbContext context, ILogger<QuizlySeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Simple password hashing function (replace with BCrypt if available)
        private static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        public async Task SeedAsync()
        {
            try
            {
                await _context.Database.EnsureCreatedAsync();

                // Check if data already seeded - if admin user exists, skip seeding
                if (_context.TbUsers.Any(u => u.Email == "admin@quizly.com"))
                {
                    _logger.LogInformation("Database already seeded");
                    return;
                }

                _logger.LogInformation("Starting database seeding...");

                // Create admin user
                var adminUser = new TbUser
                {
                    Username = "admin",
                    Email = "admin@quizly.com",
                    PasswordHash = HashPassword("admin123"),
                    FullName = "Administrator",
                    Role = "Admin",
                    CreatedAt = DateTime.Now
                };

                // Create sample users
                var teacher = new TbUser
                {
                    Username = "teacher1",
                    Email = "teacher@quizly.com",
                    PasswordHash = HashPassword("teacher123"),
                    FullName = "John Teacher",
                    Role = "Teacher",
                    CreatedAt = DateTime.Now
                };

                var student = new TbUser
                {
                    Username = "student1",
                    Email = "student@quizly.com",
                    PasswordHash = HashPassword("student123"),
                    FullName = "Jane Student",
                    Role = "Student",
                    CreatedAt = DateTime.Now
                };

                _context.TbUsers.Add(adminUser);
                _context.TbUsers.Add(teacher);
                _context.TbUsers.Add(student);

                await _context.SaveChangesAsync();
                _logger.LogInformation("Created sample users");

                // Create sample courses
                var course1 = new TbCourse
                {
                    Title = "Introduction to Web Development",
                    Description = "Learn the basics of web development including HTML, CSS, and JavaScript",
                    CreatedBy = teacher.Id,
                    CreatedAt = DateTime.Now,
                    IsApproved = true
                };

                var course2 = new TbCourse
                {
                    Title = "Advanced C# Programming",
                    Description = "Master advanced concepts in C# and .NET",
                    CreatedBy = teacher.Id,
                    CreatedAt = DateTime.Now,
                    IsApproved = false
                };

                _context.TbCourses.Add(course1);
                _context.TbCourses.Add(course2);

                await _context.SaveChangesAsync();
                _logger.LogInformation("Created sample courses");

                // Create sample lessons
                var lesson1 = new TbLesson
                {
                    CourseId = course1.Id,
                    Title = "HTML Basics",
                    Content = "<h2>Introduction to HTML</h2><p>HTML is the standard markup language for creating web pages.</p><h3>Basic Tags</h3><ul><li>&lt;html&gt;</li><li>&lt;body&gt;</li><li>&lt;h1&gt; to &lt;h6&gt;</li><li>&lt;p&gt;</li></ul>",
                    CreatedBy = teacher.Id,
                    CreatedAt = DateTime.Now,
                    IsApproved = true
                };

                var lesson2 = new TbLesson
                {
                    CourseId = course1.Id,
                    Title = "CSS Styling",
                    Content = "<h2>CSS (Cascading Style Sheets)</h2><p>CSS is used to style and layout web pages.</p><h3>Selectors</h3><ul><li>Element Selectors</li><li>Class Selectors</li><li>ID Selectors</li></ul>",
                    CreatedBy = teacher.Id,
                    CreatedAt = DateTime.Now,
                    IsApproved = false
                };

                _context.TbLessons.Add(lesson1);
                _context.TbLessons.Add(lesson2);

                await _context.SaveChangesAsync();
                _logger.LogInformation("Created sample lessons");

                // Create user XP records
                var studentXp = new TbUserXp
                {
                    UserId = student.Id,
                    Xp = 50,
                    Level = 1
                };

                var teacherXp = new TbUserXp
                {
                    UserId = teacher.Id,
                    Xp = 200,
                    Level = 2
                };

                _context.TbUserXps.Add(studentXp);
                _context.TbUserXps.Add(teacherXp);

                await _context.SaveChangesAsync();
                _logger.LogInformation("Created user XP records");

                // Create lesson progress
                var progress = new TbLessonProgress
                {
                    UserId = student.Id,
                    LessonId = lesson1.Id,
                    IsCompleted = true,
                    CompletedAt = DateTime.Now.AddDays(-1)
                };

                _context.TbLessonProgresses.Add(progress);

                await _context.SaveChangesAsync();
                _logger.LogInformation("Database seeding completed successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error seeding database: {ex.Message}");
                throw;
            }
        }
    }
}
