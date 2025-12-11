using QuizlyWebsite.Models;

namespace QuizlyWebsite.Services
{
    public interface ISubscriptionService
    {
        Task<TbUserSubscription?> GetActiveSubscriptionAsync(int userId);
        Task<bool> HasActiveSubscriptionAsync(int userId);
        Task<bool> IsPremiumPlanAsync(int userId);
        Task<bool> CanAccessPremiumContentAsync(int userId);
        Task<TbUserSubscription> CreateSubscriptionAsync(int userId, string planType, int daysValid, int? planId = null);
        Task<IEnumerable<TbUserSubscription>> GetUserSubscriptionsAsync(int userId);
    }
}
