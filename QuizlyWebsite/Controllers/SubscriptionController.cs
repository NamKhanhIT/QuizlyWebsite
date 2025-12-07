using Microsoft.AspNetCore.Mvc;
using QuizlyWebsite.Services;

namespace QuizlyWebsite.Controllers
{
    public class SubscriptionController : Controller
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly ILogger<SubscriptionController> _logger;

        public SubscriptionController(ISubscriptionService subscriptionService, ILogger<SubscriptionController> logger)
        {
            _subscriptionService = subscriptionService;
            _logger = logger;
        }

        // GET: Subscription/Pricing
        public async Task<IActionResult> Pricing()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            
            var subscription = userId.HasValue 
                ? await _subscriptionService.GetActiveSubscriptionAsync(userId.Value)
                : null;

            ViewBag.CurrentSubscription = subscription;
            return View();
        }

        // POST: Subscription/UpgradeTo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpgradeTo(string planType)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Auth");

            // Validate plan type
            var validPlans = new[] { "BASIC", "PREMIUM", "VIP" };
            if (!validPlans.Contains(planType))
            {
                TempData["Error"] = "Invalid subscription plan";
                return RedirectToAction(nameof(Pricing));
            }

            // Determine subscription duration (in days) based on plan
            var daysValid = planType switch
            {
                "BASIC" => 30,
                "PREMIUM" => 30,
                "VIP" => 30,
                _ => 30
            };

            try
            {
                await _subscriptionService.CreateSubscriptionAsync(userId.Value, planType, daysValid);
                _logger.LogInformation($"User {userId} upgraded to {planType} plan");
                TempData["Success"] = $"You have successfully upgraded to {planType} plan!";
                return RedirectToAction("Index", "Profile", new { area = "" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error upgrading subscription: {ex.Message}");
                TempData["Error"] = "Failed to upgrade subscription";
                return RedirectToAction(nameof(Pricing));
            }
        }

        // GET: Subscription/Status
        public async Task<IActionResult> Status()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Auth");

            var subscription = await _subscriptionService.GetActiveSubscriptionAsync(userId.Value);
            var allSubscriptions = await _subscriptionService.GetUserSubscriptionsAsync(userId.Value);

            ViewBag.CurrentSubscription = subscription;
            ViewBag.AllSubscriptions = allSubscriptions;

            return View();
        }
    }
}
