using Microsoft.EntityFrameworkCore;
using QuizlyWebsite.Models;

namespace QuizlyWebsite.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly QuizlyDbContext _context;
        private readonly ILogger<SubscriptionService> _logger;

        public SubscriptionService(QuizlyDbContext context, ILogger<SubscriptionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<TbUserSubscription?> GetActiveSubscriptionAsync(int userId)
        {
            return await _context.TbUserSubscriptions
                .Include(s => s.Plan)
                .Where(s => s.UserId == userId && 
                           s.IsActive == true && 
                           s.EndDate > DateTime.UtcNow)
                .OrderByDescending(s => s.EndDate)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> HasActiveSubscriptionAsync(int userId)
        {
            var subscription = await GetActiveSubscriptionAsync(userId);
            return subscription != null;
        }

        public async Task<bool> IsPremiumPlanAsync(int userId)
        {
            var subscription = await GetActiveSubscriptionAsync(userId);
            if (subscription == null)
                return false;

            return subscription.PlanType == "PREMIUM" || subscription.PlanType == "VIP";
        }

        public async Task<bool> CanAccessPremiumContentAsync(int userId)
        {
            return await HasActiveSubscriptionAsync(userId);
        }

        public async Task<TbUserSubscription> CreateSubscriptionAsync(int userId, string planType, int daysValid, int? planId = null)
        {
            var existingSubscription = await GetActiveSubscriptionAsync(userId);
            
            if (existingSubscription != null && existingSubscription.EndDate > DateTime.UtcNow)
            {
                throw new InvalidOperationException($"Bạn đang có gói hội viên đang hoạt động (hết hạn: {existingSubscription.EndDate:dd/MM/yyyy}). Vui lòng đợi hết hạn trước khi đăng ký gói mới.");
            }
            
            var subscription = new TbUserSubscription
            {
                UserId = userId,
                PlanId = planId,
                PlanType = planType,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(daysValid),
                IsActive = true
            };
            
            _context.TbUserSubscriptions.Add(subscription);
            await _context.SaveChangesAsync();
            return subscription;
        }

        public async Task<IEnumerable<TbUserSubscription>> GetUserSubscriptionsAsync(int userId)
        {
            return await _context.TbUserSubscriptions
                .Include(s => s.Plan)
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.StartDate)
                .ToListAsync();
        }
    }
}
