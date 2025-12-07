using Microsoft.AspNetCore.Mvc;
using QuizlyWebsite.Models;
using QuizlyWebsite.Services;

namespace QuizlyWebsite.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]/[action]")]
    public class ApprovalController : Controller
    {
        private readonly IApprovalService _approvalService;
        private readonly ILogger<ApprovalController> _logger;

        public ApprovalController(IApprovalService approvalService, ILogger<ApprovalController> logger)
        {
            _approvalService = approvalService;
            _logger = logger;
        }

        // Check if user is admin
        private bool IsAdmin()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            return userRole == "Admin";
        }

        // GET: Admin/Approval/Index
        public async Task<IActionResult> Index()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Auth");

            var pendingCourses = await _approvalService.GetPendingCoursesAsync();
            var pendingLessons = await _approvalService.GetPendingLessonsAsync();
            var pendingExams = await _approvalService.GetPendingExamsAsync();

            ViewData["PendingCourses"] = pendingCourses;
            ViewData["PendingLessons"] = pendingLessons;
            ViewData["PendingExams"] = pendingExams;

            return View();
        }

        // GET: Admin/Approval/Courses
        public async Task<IActionResult> Courses()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Auth");

            var pendingCourses = await _approvalService.GetPendingCoursesAsync();
            return View(pendingCourses);
        }

        // POST: Admin/Approval/ApproveCourse
        [HttpPost]
        public async Task<IActionResult> ApproveCourse(int id)
        {
            if (!IsAdmin())
                return Forbid();

            try
            {
                var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                await _approvalService.ApproveCourseAsync(id, userId);
                TempData["SuccessMessage"] = "Course approved successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error approving course: {ex.Message}");
                TempData["ErrorMessage"] = "Error approving course";
            }

            return RedirectToAction("Courses");
        }

        // POST: Admin/Approval/RejectCourse
        [HttpPost]
        public async Task<IActionResult> RejectCourse(int id, string reason)
        {
            if (!IsAdmin())
                return Forbid();

            try
            {
                var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                await _approvalService.RejectCourseAsync(id, userId, reason ?? "No reason provided");
                TempData["SuccessMessage"] = "Course rejected successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error rejecting course: {ex.Message}");
                TempData["ErrorMessage"] = "Error rejecting course";
            }

            return RedirectToAction("Courses");
        }

        // GET: Admin/Approval/Lessons
        public async Task<IActionResult> Lessons()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Auth");

            var pendingLessons = await _approvalService.GetPendingLessonsAsync();
            return View(pendingLessons);
        }

        // POST: Admin/Approval/ApproveLesson
        [HttpPost]
        public async Task<IActionResult> ApproveLesson(int id)
        {
            if (!IsAdmin())
                return Forbid();

            try
            {
                var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                await _approvalService.ApproveLessonAsync(id, userId);
                TempData["SuccessMessage"] = "Lesson approved successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error approving lesson: {ex.Message}");
                TempData["ErrorMessage"] = "Error approving lesson";
            }

            return RedirectToAction("Lessons");
        }

        // POST: Admin/Approval/RejectLesson
        [HttpPost]
        public async Task<IActionResult> RejectLesson(int id, string reason)
        {
            if (!IsAdmin())
                return Forbid();

            try
            {
                var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                await _approvalService.RejectLessonAsync(id, userId, reason ?? "No reason provided");
                TempData["SuccessMessage"] = "Lesson rejected successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error rejecting lesson: {ex.Message}");
                TempData["ErrorMessage"] = "Error rejecting lesson";
            }

            return RedirectToAction("Lessons");
        }

        // GET: Admin/Approval/Exams
        public async Task<IActionResult> Exams()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Auth");

            var pendingExams = await _approvalService.GetPendingExamsAsync();
            return View(pendingExams);
        }

        // POST: Admin/Approval/ApproveExam
        [HttpPost]
        public async Task<IActionResult> ApproveExam(int id)
        {
            if (!IsAdmin())
                return Forbid();

            try
            {
                var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                await _approvalService.ApproveExamAsync(id, userId);
                TempData["SuccessMessage"] = "Exam approved successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error approving exam: {ex.Message}");
                TempData["ErrorMessage"] = "Error approving exam";
            }

            return RedirectToAction("Exams");
        }

        // POST: Admin/Approval/RejectExam
        [HttpPost]
        public async Task<IActionResult> RejectExam(int id, string reason)
        {
            if (!IsAdmin())
                return Forbid();

            try
            {
                var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                await _approvalService.RejectExamAsync(id, userId, reason ?? "No reason provided");
                TempData["SuccessMessage"] = "Exam rejected successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error rejecting exam: {ex.Message}");
                TempData["ErrorMessage"] = "Error rejecting exam";
            }

            return RedirectToAction("Exams");
        }
    }
}
