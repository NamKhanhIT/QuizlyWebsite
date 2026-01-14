using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizlyWebsite.Models;
using QuizlyWebsite.Services;
using System.Text.Json;

namespace QuizlyWebsite.Controllers
{
    public class SubscriptionController : Controller
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly IVNPayService _vnPayService;
        private readonly ILogger<SubscriptionController> _logger;
        private readonly QuizlyDbContext _context;

        public SubscriptionController(
            ISubscriptionService subscriptionService,
            IVNPayService vnPayService,
            ILogger<SubscriptionController> logger,
            QuizlyDbContext context)
        {
            _subscriptionService = subscriptionService;
            _vnPayService = vnPayService;
            _logger = logger;
            _context = context;
        }

        [Route("subscription/pricing")]
        [Route("payment")]
        public async Task<IActionResult> Pricing()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var plans = await _context.TbMembershipPlans.ToListAsync();
            ViewBag.Plans = plans;
            ViewBag.UserId = userId;
            
            if (userId.HasValue)
            {
                var subscription = await _subscriptionService.GetActiveSubscriptionAsync(userId.Value);
                var isPremium = subscription != null 
                    && subscription.IsActive.HasValue 
                    && subscription.IsActive.Value 
                    && subscription.EndDate > DateTime.UtcNow;
                
                ViewBag.CurrentSubscription = subscription;
                ViewBag.IsPremium = isPremium;
            }
            else
            {
                ViewBag.CurrentSubscription = null;
                ViewBag.IsPremium = false;
            }
            
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpgradeTo(string planType)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Auth");

            var validPlans = new[] { "PLUS", "PREMIUM", "VIP", "BASIC" };
            if (!validPlans.Contains(planType.ToUpper()))
            {
                TempData["Error"] = "Invalid subscription plan";
                return RedirectToAction(nameof(Pricing));
            }

            var daysValid = planType switch
            {
                "BASIC" => 30,
                "PREMIUM" => 30,
                "VIP" => 30,
                _ => 30
            };

            try
            {
                await _subscriptionService.CreateSubscriptionAsync(userId.Value, planType.ToUpper(), daysValid);
                _logger.LogInformation($"User {userId} upgraded to {planType} plan");
                TempData["Success"] = $"Bạn đã đăng ký thành công gói {planType}!";
                return RedirectToAction("Index", "Profile", new { area = "" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"User {userId} tried to upgrade but has active subscription: {ex.Message}");
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Pricing));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error upgrading subscription: {ex.Message}");
                TempData["Error"] = "Có lỗi xảy ra khi đăng ký gói. Vui lòng thử lại.";
                return RedirectToAction(nameof(Pricing));
            }
        }

        // POST: Subscription/ProcessPayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("subscription/process-payment")]
        public async Task<IActionResult> ProcessPayment(int planId, string planType, string paymentMethod, string billingCycle)
        {
            _logger.LogInformation("ProcessPayment called. PlanId: {PlanId}, PlanType: {PlanType}, PaymentMethod: {PaymentMethod}, BillingCycle: {BillingCycle}", 
                planId, planType, paymentMethod, billingCycle);
            
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                _logger.LogWarning("User not logged in");
                return RedirectToAction("Login", "Account");
            }

            _logger.LogInformation("User ID: {UserId}", userId.Value);

            try
            {
                // Validate planId
                if (planId <= 0)
                {
                    _logger.LogWarning("Invalid planId: {PlanId}", planId);
                    TempData["Error"] = "Vui lòng chọn gói hội viên trước khi thanh toán";
                    return RedirectToAction("Pricing");
                } 

                var plan = await _context.TbMembershipPlans.FindAsync(planId);
                if (plan == null)
                {
                    _logger.LogWarning("Plan not found: {PlanId}", planId);
                    TempData["Error"] = "Gói hội viên không tồn tại";
                    return RedirectToAction("Pricing");
                }
                
                _logger.LogInformation("Plan found: {PlanTitle}, Price: {Price}, DurationDays: {DurationDays}", plan.Title, plan.Price, plan.DurationDays);

                // Calculate duration and price based on billing cycle
                var baseDurationDays = plan.DurationDays ?? 30;
                var basePrice = plan.Price ?? 0;
                
                decimal price;
                int durationDays;
                
                if (billingCycle == "yearly")
                {
                    // For yearly: calculate how many periods fit in 365 days, then apply 20% discount
                    var periodsPerYear = (int)Math.Floor(365m / baseDurationDays);
                    price = basePrice * periodsPerYear * 0.8m; // 20% discount
                    durationDays = periodsPerYear * baseDurationDays;
                }
                else
                {
                    // For monthly: use base price and duration
                    price = basePrice;
                    durationDays = baseDurationDays;
                }

                var payment = new TbPayment
                {
                    UserId = userId.Value,
                    Amount = price,
                    Provider = paymentMethod,
                    Status = "Pending",
                    CreatedAt = DateTime.Now
                };
                _context.TbPayments.Add(payment);
                await _context.SaveChangesAsync();

                // Only VNPay is supported - always redirect to VNPay
                _logger.LogInformation("Processing VNPay payment. PaymentId: {PaymentId}, Amount: {Amount}, PlanType: {PlanType}", 
                    payment.Id, price, planType);
                
                // Clean orderInfo - remove special characters that might cause issues
                var orderInfo = $"Thanh toan goi {planType} {billingCycle}";
                orderInfo = orderInfo.Replace("-", "").Trim();
                var returnUrl = $"{Request.Scheme}://{Request.Host}/subscription/vnpay-callback";
                
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                if (ipAddress == "::1" || string.IsNullOrEmpty(ipAddress))
                    ipAddress = "127.0.0.1";
                
                _logger.LogInformation("Creating VNPay URL. OrderId: {OrderId}, Amount: {Amount}, ReturnUrl: {ReturnUrl}, IpAddress: {IpAddress}", 
                    payment.Id, price, returnUrl, ipAddress);
                
                var paymentUrl = _vnPayService.CreatePaymentUrl(
                    payment.Id, 
                    payment.Id.ToString(), 
                    price, 
                    orderInfo, 
                    "", 
                    "", 
                    "", 
                    ipAddress, 
                    returnUrl
                );
                
                _logger.LogInformation("VNPay URL created successfully. Length: {Length}", paymentUrl.Length);
                _logger.LogInformation("Full VNPay URL: {FullUrl}", paymentUrl);
                
                // Validate URL
                if (string.IsNullOrEmpty(paymentUrl) || !paymentUrl.StartsWith("http"))
                {
                    _logger.LogError("Invalid VNPay URL generated: {Url}", paymentUrl);
                    TempData["Error"] = "Lỗi khi tạo URL thanh toán. Vui lòng thử lại.";
                    return RedirectToAction("Pricing");
                }
                
                HttpContext.Session.SetInt32("PendingPaymentId", payment.Id);
                HttpContext.Session.SetString("PendingPlanType", planType);
                HttpContext.Session.SetInt32("PendingPlanId", planId);
                HttpContext.Session.SetInt32("PendingDurationDays", durationDays);
                
                _logger.LogInformation("Session saved. Redirecting to VNPay...");
                
                // Redirect to VNPay payment gateway
                return Redirect(paymentUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment: {Message}", ex.Message);
                _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
                TempData["Error"] = $"Có lỗi xảy ra khi xử lý thanh toán: {ex.Message}";
                return RedirectToAction("Pricing");
            }
        }

        [Route("subscription/vnpay-callback")]
        public async Task<IActionResult> VNPayCallback()
        {
            try
            {
                // Log all query parameters for debugging
                _logger.LogInformation("VNPay Callback received. Query: {Query}", Request.QueryString);

                // Convert query parameters to dictionary
                var vnpParams = new Dictionary<string, string>();
                foreach (var key in Request.Query.Keys)
                {
                    if (key.StartsWith("vnp_"))
                    {
                        vnpParams.Add(key, Request.Query[key].ToString());
                    }
                }

                if (!vnpParams.ContainsKey("vnp_SecureHash") || !vnpParams.ContainsKey("vnp_ResponseCode") || !vnpParams.ContainsKey("vnp_TxnRef"))
                {
                    _logger.LogWarning("VNPay callback missing required parameters");
                    TempData["Error"] = "Thiếu thông tin từ VNPay";
                    return RedirectToAction("Pricing");
                }

                var vnpTxnRef = vnpParams["vnp_TxnRef"];
                _logger.LogInformation("VNPay Callback - TxnRef: {TxnRef}", vnpTxnRef);

                // Process payment response and validate signature
                var paymentResponse = _vnPayService.ProcessPaymentResponse(vnpParams);
                
                if (!paymentResponse.Success)
                {
                    _logger.LogWarning("VNPay signature validation failed or payment unsuccessful. ResponseCode: {ResponseCode}", paymentResponse.ResponseCode);
                    TempData["Error"] = paymentResponse.ResponseCode == "00" ? "Chữ ký không hợp lệ" : "Thanh toán thất bại";
                    return RedirectToAction("Pricing");
                }

                // Get payment from database
                if (!int.TryParse(vnpTxnRef, out var paymentId))
                {
                    _logger.LogWarning("Invalid payment ID: {TxnRef}", vnpTxnRef);
                    TempData["Error"] = "Mã giao dịch không hợp lệ";
                    return RedirectToAction("Pricing");
                }

                var payment = await _context.TbPayments.FindAsync(paymentId);
                if (payment == null)
                {
                    _logger.LogWarning("Payment not found: {PaymentId}", paymentId);
                    TempData["Error"] = "Không tìm thấy giao dịch";
                    return RedirectToAction("Pricing");
                }

                // Get userId from payment or session
                var userId = payment.UserId;
                if (userId == 0)
                {
                    userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                }

                if (userId == 0)
                {
                    TempData["Error"] = "Vui lòng đăng nhập để xử lý thanh toán";
                    return RedirectToAction("Login", "Account");
                }

                // Only update if payment is still pending
                if (payment.Status == "Pending")
                {
                    payment.Status = "Completed";
                    payment.ProviderTransId = paymentResponse.TransactionId;
                    await _context.SaveChangesAsync();

                    // Get subscription info from session
                    var planId = HttpContext.Session.GetInt32("PendingPlanId") ?? 0;
                    var durationDays = HttpContext.Session.GetInt32("PendingDurationDays") ?? 30;

                    // Get plan from database to ensure we have the correct planType
                    TbMembershipPlan? plan = null;
                    string planType = "PREMIUM"; // Default fallback
                    
                    if (planId > 0)
                    {
                        plan = await _context.TbMembershipPlans.FindAsync(planId);
                        if (plan != null)
                        {
                            // Determine planType from plan title
                            var titleLower = plan.Title?.ToLower() ?? "";
                            if (titleLower.Contains("vip"))
                                planType = "VIP";
                            else if (titleLower.Contains("premium"))
                                planType = "PREMIUM";
                            else if (titleLower.Contains("plus"))
                                planType = "PLUS";
                            else
                                planType = "BASIC";
                        }
                    }

                    // Create subscription with correct planId and planType
                    try
                    {
                        await _subscriptionService.CreateSubscriptionAsync(userId, planType, durationDays, planId > 0 ? planId : null);

                        // Clear session
                        HttpContext.Session.Remove("PendingPaymentId");
                        HttpContext.Session.Remove("PendingPlanType");
                        HttpContext.Session.Remove("PendingPlanId");
                        HttpContext.Session.Remove("PendingDurationDays");

                        _logger.LogInformation("Payment successful. PaymentId: {PaymentId}, UserId: {UserId}, PlanId: {PlanId}, PlanType: {PlanType}", 
                            paymentId, userId, planId, planType);
                        TempData["Success"] = "Thanh toán thành công! Gói hội viên đã được kích hoạt.";
                        return RedirectToAction("Index", "Profile", new { tab = "membership" });
                    }
                    catch (InvalidOperationException ex)
                    {
                        // Nếu có subscription đang active, hoàn tiền
                        _logger.LogWarning("Payment successful but user has active subscription. Refunding payment {PaymentId}: {Message}", 
                            paymentId, ex.Message);
                        
                        // Hoàn tiền
                        payment.Status = "Refunded";
                        await _context.SaveChangesAsync();
                        
                        TempData["Error"] = ex.Message + " Số tiền đã được hoàn lại vào tài khoản của bạn.";
                        return RedirectToAction("Index", "Profile", new { tab = "membership" });
                    }
                }
                else
                {
                    _logger.LogInformation("Payment already processed. PaymentId: {PaymentId}, Status: {Status}", paymentId, payment.Status);
                    TempData["Info"] = "Giao dịch đã được xử lý trước đó.";
                    return RedirectToAction("Index", "Profile", new { tab = "membership" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing VNPay callback");
                TempData["Error"] = "Có lỗi xảy ra khi xử lý thanh toán. Vui lòng liên hệ hỗ trợ.";
                return RedirectToAction("Pricing");
            }
        }

        // GET: Subscription/Status
        public async Task<IActionResult> Status()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return RedirectToAction("Login", "Account");

            var subscription = await _subscriptionService.GetActiveSubscriptionAsync(userId.Value);
            var allSubscriptions = await _subscriptionService.GetUserSubscriptionsAsync(userId.Value);

            ViewBag.CurrentSubscription = subscription;
            ViewBag.AllSubscriptions = allSubscriptions;

            return View();
        }

        // POST: Subscription/CancelSubscription
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("subscription/cancel")]
        public async Task<IActionResult> CancelSubscription(
            string AccountHolderName,
            string AccountNumber,
            string BankName,
            string BankBranch,
            string PhoneNumber,
            string Notes)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    return RedirectToAction("Login", "Account");
                }

                // Validate required fields
                if (string.IsNullOrWhiteSpace(AccountHolderName))
                {
                    TempData["Error"] = "Vui lòng nhập tên chủ tài khoản";
                    return RedirectToAction("Index", "Profile", new { tab = "membership" });
                }

                if (string.IsNullOrWhiteSpace(AccountNumber))
                {
                    TempData["Error"] = "Vui lòng nhập số tài khoản ngân hàng";
                    return RedirectToAction("Index", "Profile", new { tab = "membership" });
                }

                if (string.IsNullOrWhiteSpace(BankName))
                {
                    TempData["Error"] = "Vui lòng nhập tên ngân hàng";
                    return RedirectToAction("Index", "Profile", new { tab = "membership" });
                }

                if (string.IsNullOrWhiteSpace(PhoneNumber))
                {
                    TempData["Error"] = "Vui lòng nhập số điện thoại liên hệ";
                    return RedirectToAction("Index", "Profile", new { tab = "membership" });
                }

                var subscription = await _subscriptionService.GetActiveSubscriptionAsync(userId.Value);
                if (subscription == null)
                {
                    TempData["Error"] = "Không tìm thấy gói hội viên để hủy";
                    return RedirectToAction("Index", "Profile", new { tab = "membership" });
                }

                var now = DateTime.UtcNow;
                var totalDays = (subscription.EndDate - subscription.StartDate).TotalDays;
                var remainingDays = (subscription.EndDate - now).TotalDays;
                
                decimal refundAmount = 0;
                if (remainingDays > 0 && totalDays > 0 && subscription.Plan != null && subscription.Plan.Price.HasValue)
                {
                    var originalPrice = subscription.Plan.Price.Value;
                    refundAmount = originalPrice * (decimal)(remainingDays / totalDays);
                    
                    // Làm tròn đến 2 chữ số thập phân
                    refundAmount = Math.Round(refundAmount, 2);
                    
                    // Lưu thông tin ngân hàng dạng JSON vào ProviderTransId
                    var bankInfo = new
                    {
                        AccountHolderName = AccountHolderName.Trim(),
                        AccountNumber = AccountNumber.Trim(),
                        BankName = BankName.Trim(),
                        BankBranch = BankBranch?.Trim() ?? "",
                        PhoneNumber = PhoneNumber.Trim(),
                        Notes = Notes?.Trim() ?? ""
                    };
                    var bankInfoJson = System.Text.Json.JsonSerializer.Serialize(bankInfo);
                    
                    // Tạo payment record cho refund
                    var refundPayment = new TbPayment
                    {
                        UserId = userId.Value,
                        Amount = -refundAmount, 
                        Provider = "Refund",
                        Status = "Pending", // Chờ xử lý hoàn tiền
                        CreatedAt = DateTime.Now,
                        Currency = "VND",
                        ProviderTransId = bankInfoJson // Lưu thông tin ngân hàng
                    };
                    
                    _context.TbPayments.Add(refundPayment);
                    _logger.LogInformation("Refund request created for subscription {SubscriptionId}: {RefundAmount} VND (Remaining days: {RemainingDays}/{TotalDays}). Bank: {BankName}, Account: {AccountNumber}", 
                        subscription.Id, refundAmount, remainingDays, totalDays, BankName, AccountNumber);
                }

                // Deactivate subscription
                subscription.IsActive = false;
                subscription.EndDate = now;
                _context.TbUserSubscriptions.Update(subscription);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} cancelled subscription {SubscriptionId}. Refund: {RefundAmount} VND", 
                    userId.Value, subscription.Id, refundAmount);
                
                if (refundAmount > 0)
                {
                    TempData["Success"] = $"Gói hội viên của bạn đã được hủy thành công. Yêu cầu hoàn tiền {refundAmount:N0} VND đã được gửi. Chúng tôi sẽ xử lý và chuyển khoản vào tài khoản {AccountNumber} ({BankName}) trong vòng 3-5 ngày làm việc.";
                }
                else
                {
                    TempData["Success"] = "Gói hội viên của bạn đã được hủy thành công.";
                }
                
                return RedirectToAction("Index", "Profile", new { tab = "membership" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling subscription: {Message}", ex.Message);
                TempData["Error"] = "Có lỗi xảy ra khi hủy gói. Vui lòng thử lại.";
                return RedirectToAction("Index", "Profile", new { tab = "membership" });
            }
        }

        [Route("subscription/test-vnpay")]
        public IActionResult TestVNPay()
        {
            try
            {
                var testOrderId = 999999;
                var testAmount = 100000m;
                var testOrderInfo = "Test payment";
                var testReturnUrl = $"{Request.Scheme}://{Request.Host}/subscription/vnpay-callback";
                var testIpAddress = "127.0.0.1";

                var paymentUrl = _vnPayService.CreatePaymentUrl(
                    testOrderId, 
                    testOrderId.ToString(), 
                    testAmount, 
                    testOrderInfo, 
                    "Test", 
                    "test@test.com", 
                    "0123456789", 
                    testIpAddress, 
                    testReturnUrl
                );
                
                return Content($"<html><body><h1>VNPay Test</h1><p>URL Length: {paymentUrl.Length}</p><p>URL: <a href='{paymentUrl}' target='_blank'>{paymentUrl}</a></p></body></html>", "text/html");
            }
            catch (Exception ex)
            {
                return Content($"<html><body><h1>Error</h1><p>{ex.Message}</p><p>{ex.StackTrace}</p></body></html>", "text/html");
            }
        }
    }
}
