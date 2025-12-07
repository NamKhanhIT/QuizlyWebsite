using QuizlyWebsite.Models;

namespace QuizlyWebsite.Services
{
    public interface IAccessControlService
    {
        Task<bool> CanAccessCourseAsync(int userId, TbCourse course);
        Task<bool> CanAccessLessonAsync(int userId, TbLesson lesson);
        Task<bool> CanAccessExamAsync(int userId, TbExam exam);
        Task<bool> IsLessonPreviewAsync(TbLesson lesson);
    }
}
