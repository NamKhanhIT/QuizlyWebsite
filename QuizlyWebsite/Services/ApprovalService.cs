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
        Task DeleteExamAsync(int examId, int deletedBy, string reason);
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
            // Chỉ lấy đề thi chờ duyệt (IsApproved == false hoặc null) và chưa bị từ chối (RejectionReason == null)
            return await _context.TbExams
                .Where(e => (e.IsApproved == false || e.IsApproved == null) && e.RejectionReason == null)
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
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Rejection reason is required", nameof(reason));

            var exam = await _context.TbExams.FindAsync(examId);
            if (exam == null)
                throw new Exception("Exam not found");

            // Set trạng thái từ chối
            exam.IsApproved = false;
            exam.RejectionReason = reason.Trim();
            
            // Clear thông tin duyệt (nếu có)
            exam.ApprovedBy = null;
            exam.ApprovedAt = null;

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Exam {examId} rejected by user {approvedBy} with reason: {reason}");
        }

        public async Task DeleteExamAsync(int examId, int deletedBy, string reason)
        {
            var exam = await _context.TbExams
                .Include(e => e.TbQuestions)
                .FirstOrDefaultAsync(e => e.Id == examId);

            if (exam == null)
                throw new Exception("Exam not found");

            // Delete related questions and their results first
            foreach (var question in exam.TbQuestions)
            {
                var resultDetails = await _context.TbExamResultDetails
                    .Where(r => r.QuestionId == question.Id)
                    .ToListAsync();
                _context.TbExamResultDetails.RemoveRange(resultDetails);
            }

            // Delete questions
            _context.TbQuestions.RemoveRange(exam.TbQuestions);

            // Delete exam results
            var examResults = await _context.TbExamResults
                .Where(r => r.ExamId == examId)
                .ToListAsync();
            _context.TbExamResults.RemoveRange(examResults);

            // Delete payments
            var payments = await _context.TbPayments
                .Where(p => p.ExamId == examId)
                .ToListAsync();
            _context.TbPayments.RemoveRange(payments);

            // Delete the exam
            _context.TbExams.Remove(exam);

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Exam {examId} deleted by user {deletedBy} for reason: {reason}");
        }
    }
}
