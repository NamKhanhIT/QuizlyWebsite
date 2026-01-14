using System.Security.Claims;

namespace QuizlyWebsite.Middleware
{
    public class SessionToClaimsMiddleware
    {
        private readonly RequestDelegate _next;

        public SessionToClaimsMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var userId = context.Session.GetInt32("UserId");
            var username = context.Session.GetString("Username");
            var role = context.Session.GetString("Role");

            // If session has user info, create a ClaimsPrincipal
            if (userId.HasValue && !string.IsNullOrEmpty(username))
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()),
                    new Claim(ClaimTypes.Name, username),
                    new Claim("UserId", userId.Value.ToString()),
                    new Claim(ClaimTypes.Role, role ?? "User")
                };

                var identity = new ClaimsIdentity(claims, "SessionAuth");
                var principal = new ClaimsPrincipal(identity);
                context.User = principal;
            }

            await _next(context);
        }
    }
}
