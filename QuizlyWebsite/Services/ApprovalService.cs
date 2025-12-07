using QuizlyWebsite.Models;
using Microsoft.EntityFrameworkCore;

namespace QuizlyWebsite.Services
{
    public interface IApprovalService
    {
        Task<List<TbCourse>> GetPendingCoursesAsync();
        Task<List<TbLesson>> GetPendingLessonsAsync();
        Task<List<TbExam>> GetPendingExamsAsync();
        
        Task ApproveCourseAsync(int courseId, int approvedBy);
        Task RejectCourseAsync(int courseId, int approvedBy, string reason);
        
        Task ApproveLessonAsync(int lessonId, int approvedBy);
        Task RejectLessonAsync(int lessonId, int approvedBy, string reason);
        
        Task ApproveExamAsync(int examId, int approvedBy);
        Task RejectExamAsync(int examId, int approvedBy, string reason);
    }

    public class ApprovalService : IApprovalService
    {
        private readonly QuizlyDbContext _context;
        private readonly ILogger<ApprovalService> _logger;

        public ApprovalService(QuizlyDbContext context, ILogger<ApprovalService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<TbCourse>> GetPendingCoursesAsync()
        {
            return await _context.TbCourses
                .Where(c => c.IsApproved == false)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<TbLesson>> GetPendingLessonsAsync()
        {
            return await _context.TbLessons
                .Where(l => l.IsApproved == false)
                .OrderByDescending(l => l.CreatedAt)
                .Include(l => l.Course)
                .ToListAsync();
        }

        public async Task<List<TbExam>> GetPendingExamsAsync()
        {
            return await _context.TbExams
                .Where(e => e.IsApproved == false)
                .OrderByDescending(e => e.CreatedAt)
                .Include(e => e.Subject)
                .ToListAsync();
        }

        public async Task ApproveCourseAsync(int courseId, int approvedBy)
        {
            var course = await _context.TbCourses.FindAsync(courseId);
            if (course == null)
                throw new Exception("Course not found");

            course.IsApproved = true;

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Course {courseId} approved");
        }

        public async Task RejectCourseAsync(int courseId, int approvedBy, string reason)
        {
            var course = await _context.TbCourses.FindAsync(courseId);
            if (course == null)
                throw new Exception("Course not found");

            course.IsApproved = false;

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Course {courseId} rejected");
        }

        public async Task ApproveLessonAsync(int lessonId, int approvedBy)
        {
            var lesson = await _context.TbLessons.FindAsync(lessonId);
            if (lesson == null)
                throw new Exception("Lesson not found");

            lesson.IsApproved = true;

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Lesson {lessonId} approved");
        }

        public async Task RejectLessonAsync(int lessonId, int approvedBy, string reason)
        {
            var lesson = await _context.TbLessons.FindAsync(lessonId);
            if (lesson == null)
                throw new Exception("Lesson not found");

            lesson.IsApproved = false;

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Lesson {lessonId} rejected");
        }

        public async Task ApproveExamAsync(int examId, int approvedBy)
        {
            var exam = await _context.TbExams.FindAsync(examId);
            if (exam == null)
                throw new Exception("Exam not found");

            exam.IsApproved = true;
            exam.ApprovedBy = approvedBy;
            exam.ApprovedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Exam {examId} approved by user {approvedBy}");
        }

        public async Task RejectExamAsync(int examId, int approvedBy, string reason)
        {
            var exam = await _context.TbExams.FindAsync(examId);
            if (exam == null)
                throw new Exception("Exam not found");

            exam.IsApproved = false;
            exam.ApprovedBy = approvedBy;
            exam.ApprovedAt = DateTime.Now;
            exam.RejectionReason = reason;

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Exam {examId} rejected by user {approvedBy}");
        }
    }
}
