using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizlyWebsite.Models;

namespace QuizlyWebsite.Controllers
{
    public class RankingController : Controller
    {
        private readonly QuizlyDbContext _context;

        public RankingController(QuizlyDbContext context)
        {
            _context = context;
        }

        // GET: /ranking or /leaderboard
        [Route("ranking")]
        [Route("leaderboard")]
        public async Task<IActionResult> Index(string? type = "xp")
        {
            type = type?.ToLower() ?? "xp";

            List<RankingViewModel> rankings = new List<RankingViewModel>();

            if (type == "xp" || type == "level")
            {
                // Ranking by XP/Level
                var userXps = await _context.TbUserXps
                    .Include(x => x.User)
                    .OrderByDescending(x => x.Xp ?? 0)
                    .ThenByDescending(x => x.Level ?? 1)
                    .Take(100)
                    .ToListAsync();

                // Get favorite subject for each user
                var userIds = userXps.Select(x => x.UserId).ToList();
                var examResults = await _context.TbExamResults
                    .Where(r => userIds.Contains(r.UserId))
                    .Include(r => r.Exam)
                    .ThenInclude(e => e.Subject)
                    .ToListAsync();

                var favoriteSubjectDict = examResults
                    .GroupBy(r => new { r.UserId, SubjectId = r.Exam.SubjectId, SubjectTitle = r.Exam.Subject.Title })
                    .Select(g => new
                    {
                        g.Key.UserId,
                        g.Key.SubjectTitle,
                        Count = g.Count()
                    })
                    .GroupBy(x => x.UserId)
                    .Select(g => new
                    {
                        UserId = g.Key,
                        FavoriteSubject = g.OrderByDescending(x => x.Count).First().SubjectTitle
                    })
                    .ToDictionary(x => x.UserId, x => x.FavoriteSubject);

                rankings = userXps.Select((x, index) => new RankingViewModel
                {
                    Rank = index + 1,
                    UserId = x.UserId,
                    Username = x.User?.Username ?? "Unknown",
                    FullName = x.User?.FullName ?? "Người dùng",
                    AvatarUrl = x.User?.AvatarUrl,
                    Score = type == "xp" ? (x.Xp ?? 0) : (x.Level ?? 1),
                    ScoreLabel = type == "xp" ? "XP" : "Cấp độ",
                    AdditionalInfo = favoriteSubjectDict.ContainsKey(x.UserId) ? favoriteSubjectDict[x.UserId] : null
                }).ToList();
            }
            else if (type == "exam")
            {
                // Ranking by exam results (average score)
                var examRankings = await _context.TbExamResults
                    .Where(r => r.Score.HasValue)
                    .Include(r => r.User)
                    .GroupBy(r => r.UserId)
                    .Select(g => new
                    {
                        UserId = g.Key,
                        AvgScore = g.Average(r => r.Score ?? 0),
                        TotalExams = g.Count(),
                        User = g.First().User
                    })
                    .OrderByDescending(x => x.AvgScore)
                    .ThenByDescending(x => x.TotalExams)
                    .Take(100)
                    .ToListAsync();

                // Get favorite subject for exam rankings
                var examUserIds = examRankings.Select(x => x.UserId).ToList();
                var examResultsForSubjects = await _context.TbExamResults
                    .Where(r => examUserIds.Contains(r.UserId))
                    .Include(r => r.Exam)
                    .ThenInclude(e => e.Subject)
                    .ToListAsync();

                var examFavoriteSubjectDict = examResultsForSubjects
                    .GroupBy(r => new { r.UserId, SubjectId = r.Exam.SubjectId, SubjectTitle = r.Exam.Subject.Title })
                    .Select(g => new
                    {
                        g.Key.UserId,
                        g.Key.SubjectTitle,
                        Count = g.Count()
                    })
                    .GroupBy(x => x.UserId)
                    .Select(g => new
                    {
                        UserId = g.Key,
                        FavoriteSubject = g.OrderByDescending(x => x.Count).First().SubjectTitle
                    })
                    .ToDictionary(x => x.UserId, x => x.FavoriteSubject);

                rankings = examRankings.Select((x, index) => new RankingViewModel
                {
                    Rank = index + 1,
                    UserId = x.UserId,
                    Username = x.User?.Username ?? "Unknown",
                    FullName = x.User?.FullName ?? "Người dùng",
                    AvatarUrl = x.User?.AvatarUrl,
                    Score = (int)Math.Round(x.AvgScore),
                    ScoreLabel = "Điểm TB",
                    AdditionalInfo = examFavoriteSubjectDict.ContainsKey(x.UserId) ? examFavoriteSubjectDict[x.UserId] : null
                }).ToList();
            }
            else if (type == "courses")
            {
                // Ranking by completed courses
                var courseRankings = await _context.TbLessonProgresses
                    .Where(p => p.IsCompleted == true)
                    .AsSplitQuery()
                    .Include(p => p.User)
                    .Include(p => p.Lesson)
                    .ThenInclude(l => l.Course)
                    .GroupBy(p => p.UserId)
                    .Select(g => new
                    {
                        UserId = g.Key,
                        CompletedLessons = g.Count(),
                        UniqueCourses = g.Select(p => p.Lesson.CourseId).Distinct().Count(),
                        User = g.First().User
                    })
                    .OrderByDescending(x => x.UniqueCourses)
                    .ThenByDescending(x => x.CompletedLessons)
                    .Take(100)
                    .ToListAsync();

                rankings = courseRankings.Select((x, index) => new RankingViewModel
                {
                    Rank = index + 1,
                    UserId = x.UserId,
                    Username = x.User?.Username ?? "Unknown",
                    FullName = x.User?.FullName ?? "Người dùng",
                    AvatarUrl = x.User?.AvatarUrl,
                    Score = x.UniqueCourses,
                    ScoreLabel = "Khóa học",
                    AdditionalInfo = $"{x.CompletedLessons} bài học"
                }).ToList();
            }

            ViewData["Type"] = type;
            ViewData["Rankings"] = rankings;

            // Get current user's rank if logged in
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId.HasValue)
            {
                var userRank = rankings.FirstOrDefault(r => r.UserId == userId.Value);
                ViewBag.UserRank = userRank;
            }

            return View(rankings);
        }
    }
}
