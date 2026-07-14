using Microsoft.AspNetCore.Mvc;

namespace Xedap.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ChatController : Controller
    {
        // Giao diện chat cho admin
        public IActionResult Index()
        {
            return View();
        }
    }
}
