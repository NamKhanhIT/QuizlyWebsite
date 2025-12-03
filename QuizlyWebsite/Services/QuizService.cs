using QuizlyWebsite.Models;
using Microsoft.EntityFrameworkCore;

namespace QuizlyWebsite.Services
{
    public class QuizService
    {
        private readonly QuizlyDbContext _context;

        public QuizService(QuizlyDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Tính điểm cho bài thi
        /// </summary>
        public decimal CalculateScore(int correctAnswers, int totalQuestions)
        {
            if (totalQuestions == 0) return 0;
            return (decimal)correctAnswers / totalQuestions * 100;
        }

        /// <summary>
        /// Kiểm tra xem người dùng đã mua bộ đề này chưa
        /// </summary>
        public async Task<bool> HasUserPurchased(int userId, int examId)
        {
            var purchase = await _context.TbUserPurchases
                .FirstOrDefaultAsync(p => p.UserId == userId && p.ExamId == examId);
            
            if (purchase == null) return false;
            
            // Kiểm tra xem đã hết hạn chưa
            if (purchase.ExpiredAt.HasValue && purchase.ExpiredAt < DateTime.Now)
                return false;
            
            return true;
        }

        /// <summary>
        /// Cập nhật đánh giá bộ đề
        /// </summary>
        public async Task UpdateExamRating(int examId)
        {
            var exam = await _context.TbExams.FindAsync(examId);
            var reviews = await _context.TbExamReviews
                .Where(r => r.ExamId == examId)
                .ToListAsync();

            if (reviews.Any() && exam != null)
            {
                exam.AvgRating = (double)(reviews.Average(r => r.Rating ?? 0));
                exam.TotalReviews = reviews.Count;
                _context.Update(exam);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Lấy thống kê của người dùng
        /// </summary>
        public async Task<Dictionary<string, object>> GetUserStatistics(int userId)
        {
            var results = await _context.TbExamResults
                .Where(r => r.UserId == userId)
                .ToListAsync();

            var stats = new Dictionary<string, object>
            {
                { "TotalAttempts", results.Count },
                { "Passed", results.Count(r => r.Status == "Passed") },
                { "Failed", results.Count(r => r.Status == "Failed") },
                { "AverageScore", results.Any() ? (object)(results.Average(r => r.Score ?? 0)) : (object)0 },
                { "HighestScore", results.Any() ? (object)(results.Max(r => r.Score ?? 0)) : (object)0 }
            };

            return stats;
        }
    }
}
