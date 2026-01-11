using Microsoft.EntityFrameworkCore;
using QuizlyWebsite.Models;

namespace QuizlyWebsite.Services
{
    public interface ILessonPreviewService
    {
        Task AssignPreviewLessonsAsync(int courseId);
        Task UpdatePreviewLessonsAsync(int courseId, int newFreeLessonCount);
    }

    public class LessonPreviewService : ILessonPreviewService
    {
        private readonly QuizlyDbContext _context;
        private readonly ILogger<LessonPreviewService> _logger;

        public LessonPreviewService(QuizlyDbContext context, ILogger<LessonPreviewService> logger)
        {
            _context = context;
            _logger = logger;
        }
        public async Task AssignPreviewLessonsAsync(int courseId)
        {
            try
            {
                var course = await _context.TbCourses.FindAsync(courseId);
                if (course == null)
                {
                    _logger.LogWarning($"Course {courseId} not found");
                    return;
                }

                var freeCount = course.FreeLessonCount ?? 2;
                var lessons = await _context.TbLessons
                    .Where(l => l.CourseId == courseId)
                    .OrderBy(l => l.CreatedAt)
                    .ToListAsync();

                for (int i = 0; i < lessons.Count; i++)
                {
                    lessons[i].IsPreview = i < freeCount;
                }

                _context.UpdateRange(lessons);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Assigned {Math.Min(freeCount, lessons.Count)} preview lessons for course {courseId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error assigning preview lessons: {ex.Message}");
            }
        }

        public async Task UpdatePreviewLessonsAsync(int courseId, int newFreeLessonCount)
        {
            try
            {
                var lessons = await _context.TbLessons
                    .Where(l => l.CourseId == courseId)
                    .OrderBy(l => l.CreatedAt)
                    .ToListAsync();

                for (int i = 0; i < lessons.Count; i++)
                {
                    lessons[i].IsPreview = i < newFreeLessonCount;
                }

                _context.UpdateRange(lessons);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Updated {lessons.Count} lessons preview status for course {courseId}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating preview lessons: {ex.Message}");
            }
        }
    }
}
