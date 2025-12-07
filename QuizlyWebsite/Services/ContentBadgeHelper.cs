namespace QuizlyWebsite.Services
{
    public class ContentBadgeHelper
    {
        public enum BadgeType
        {
            Official,      // Admin created exam
            Community,     // User created exam
            Premium,       // Locked premium content
            Free,          // Free content
            Preview        // Preview lesson
        }

        public static string GetBadgeIcon(BadgeType type)
        {
            return type switch
            {
                BadgeType.Official => "🛡",
                BadgeType.Community => "👥",
                BadgeType.Premium => "🔒",
                BadgeType.Free => "✅",
                BadgeType.Preview => "👁",
                _ => "•"
            };
        }

        public static string GetBadgeText(BadgeType type)
        {
            return type switch
            {
                BadgeType.Official => "Official",
                BadgeType.Community => "Community",
                BadgeType.Premium => "Premium",
                BadgeType.Free => "Free",
                BadgeType.Preview => "Preview",
                _ => "Unknown"
            };
        }

        public static string GetBadgeColor(BadgeType type)
        {
            return type switch
            {
                BadgeType.Official => "badge-info",
                BadgeType.Community => "badge-primary",
                BadgeType.Premium => "badge-warning",
                BadgeType.Free => "badge-success",
                BadgeType.Preview => "badge-light",
                _ => "badge-secondary"
            };
        }

        public static string GetBadgeCSS(BadgeType type)
        {
            return type switch
            {
                BadgeType.Official => "background: #17a2b8; color: white;",
                BadgeType.Community => "background: #667eea; color: white;",
                BadgeType.Premium => "background: #ffc107; color: #333;",
                BadgeType.Free => "background: #28a745; color: white;",
                BadgeType.Preview => "background: #e0e0e0; color: #333;",
                _ => "background: #6c757d; color: white;"
            };
        }
    }
}
