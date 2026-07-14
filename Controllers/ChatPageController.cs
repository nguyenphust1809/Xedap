using Microsoft.AspNetCore.Mvc;

namespace Xedap.Controllers
{
    public class ChatPageController : Controller
    {
        [HttpGet("/chat")]
        public IActionResult Index()
        {
            return View("Views/Chat/Index.cshtml");
        }
    }
}
