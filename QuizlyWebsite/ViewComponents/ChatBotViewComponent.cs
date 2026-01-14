using Microsoft.AspNetCore.Mvc;

namespace QuizlyWebsite.ViewComponents
{
    public class ChatBotViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}
