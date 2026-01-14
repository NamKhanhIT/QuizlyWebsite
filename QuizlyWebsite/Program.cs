using Microsoft.EntityFrameworkCore;
using QuizlyWebsite.Models;
using QuizlyWebsite.Middleware;
using QuizlyWebsite.Services;
using QuizlyWebsite.Data;
using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();

// Configure Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Session timeout 30 minutes
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Add DbContext
builder.Services.AddDbContext<QuizlyDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Add custom services
builder.Services.AddScoped<IXpService, XpService>();
builder.Services.AddScoped<IApprovalService, ApprovalService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IAccessControlService, AccessControlService>();
builder.Services.AddScoped<ILessonPreviewService, LessonPreviewService>();
builder.Services.AddScoped<IQuestionParserService, QuestionParserService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<IVNPayService>(provider =>
{
    var config = builder.Configuration;
    var tmnCode = config["VNPay:TmnCode"] ?? "";
    var hashSecret = config["VNPay:HashSecret"] ?? "";
    var paymentUrl = config["VNPay:Url"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
    var baseReturnUrl = $"{config["AppUrl"]}/subscription/vnpay-callback";
    return new VNPayService(tmnCode, hashSecret, paymentUrl, baseReturnUrl);
});
builder.Services.AddScoped<QuizlySeeder>();

// Cấu hình EPPlus License
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;    

var app = builder.Build();

// Seed database with demo data
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<QuizlySeeder>();
    try
    {
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError($"Error seeding database: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();

// Add custom middleware to map session to User.Identity
app.UseMiddleware<SessionToClaimsMiddleware>();

app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

