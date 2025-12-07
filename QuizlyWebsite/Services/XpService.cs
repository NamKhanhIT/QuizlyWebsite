using QuizlyWebsite.Models;

namespace QuizlyWebsite.Services
{
    public interface IXpService
    {
        Task AddXpAsync(int userId, int xpAmount);
        Task<TbUserXp> GetUserXpAsync(int userId);
        int CalculateLevel(int xp);
        string GetLevelName(int level);
    }

    public class XpService : IXpService
    {
        private readonly QuizlyDbContext _context;
        private readonly ILogger<XpService> _logger;

        // XP thresholds for each level
        private const int Level1Max = 99;
        private const int Level2Max = 299;
        private const int Level3Max = 599;

        public XpService(QuizlyDbContext context, ILogger<XpService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task AddXpAsync(int userId, int xpAmount)
        {
            try
            {
                var userXp = await _context.TbUserXps.FindAsync(userId);

                if (userXp == null)
                {
                    // Create new XP record
                    userXp = new TbUserXp
                    {
                        UserId = userId,
                        Xp = xpAmount,
                        Level = 1
                    };
                    _context.TbUserXps.Add(userXp);
                }
                else
                {
                    // Update existing XP
                    userXp.Xp = userXp.Xp + xpAmount;
                    userXp.Level = CalculateLevel(userXp.Xp ?? 0);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding XP to user {userId}: {ex.Message}");
                throw;
            }
        }

        public async Task<TbUserXp> GetUserXpAsync(int userId)
        {
            var userXp = await _context.TbUserXps.FindAsync(userId);

            if (userXp == null)
            {
                // Return default if not found
                return new TbUserXp
                {
                    UserId = userId,
                    Xp = 0,
                    Level = 1
                };
            }

            return userXp;
        }

        public int CalculateLevel(int xp)
        {
            if (xp <= Level1Max)
                return 1;
            else if (xp <= Level2Max)
                return 2;
            else if (xp <= Level3Max)
                return 3;
            else
                return 4 + ((xp - Level3Max) / 300); // Every 300 XP after level 3 = 1 level
        }

        public string GetLevelName(int level)
        {
            return level switch
            {
                1 => "Beginner",
                2 => "Intermediate",
                3 => "Advanced",
                4 => "Expert",
                5 => "Master",
                _ => "Legend"
            };
        }
    }
}
