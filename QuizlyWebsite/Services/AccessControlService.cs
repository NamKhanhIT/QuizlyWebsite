using QuizlyWebsite.Models;

namespace QuizlyWebsite.Services
{
    public class AccessControlService : IAccessControlService
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly ILogger<AccessControlService> _logger;

        public AccessControlService(ISubscriptionService subscriptionService, ILogger<AccessControlService> logger)
        {
            _subscriptionService = subscriptionService;
            _logger = logger;
        }

        public async Task<bool> CanAccessCourseAsync(int userId, TbCourse course)
        {
            // Free courses are accessible by everyone
            if (course.IsPaid != true)
                return true;

            // Paid courses require premium subscription
            return await _subscriptionService.CanAccessPremiumContentAsync(userId);
        }

        public async Task<bool> CanAccessLessonAsync(int userId, TbLesson lesson)
        {
            // Preview lessons are always accessible
            if (lesson.IsPreview == true)
                return true;

            // Get the course
            if (lesson.Course == null)
                return false;

            // Check course access
            return await CanAccessCourseAsync(userId, lesson.Course);
        }

        public async Task<bool> CanAccessExamAsync(int userId, TbExam exam)
        {
            // Free exams are accessible by everyone
            if (exam.IsPaid != true)
                return true;

            // Paid exams require premium subscription
            return await _subscriptionService.CanAccessPremiumContentAsync(userId);
        }

        public async Task<bool> IsLessonPreviewAsync(TbLesson lesson)
        {
            return lesson.IsPreview == true;
        }
    }
}
