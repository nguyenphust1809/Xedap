using Xedap.Models;
using Xedap.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Xedap.Controllers
{
    [Route("chat")]
    public class ChatController : Controller
    {
        private readonly DataContext _context;
        public ChatController(DataContext context)
        {
            _context = context;
        }

        // Lấy lịch sử tin nhắn giữa user và admin
        [HttpGet("history/{userId}")]
        public async Task<IActionResult> GetChatHistory(string userId)
        {
            var chats = await _context.ChatMessages
                .Where(c => c.SenderId == userId || c.ReceiverId == userId)
                .OrderBy(c => c.Timestamp)
                .ToListAsync();

            // Đánh dấu đã đọc
            foreach (var msg in chats.Where(c => !c.IsRead && c.ReceiverId == "Admin"))
                msg.IsRead = true;

            await _context.SaveChangesAsync();

            return Ok(chats);
        }
    }
}
