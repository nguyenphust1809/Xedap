using Xedap.Models;
using Xedap.Repository;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Xedap.Hubs
{
    public class ChatHub : Hub
    {
        private readonly DataContext _context;

        // Danh sách kết nối: <ConnectionId, DisplayName>
        private static readonly ConcurrentDictionary<string, string> ConnectedUsers = new();

        public ChatHub(DataContext context)
        {
            _context = context;
        }

        // 🟢 Khi người dùng kết nối
        public override async Task OnConnectedAsync()
        {
            var userName = Context.User?.Identity?.Name ?? $"Khách {Context.ConnectionId[..5]}";

            // Nếu là admin, thêm vào group "Admin"
            if (Context.User?.IsInRole("Admin") == true)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Admin");
                await Clients.Caller.SendAsync("UpdateUserList", ConnectedUsers.ToArray());
            }
            else
            {
                ConnectedUsers[Context.ConnectionId] = userName;
                Console.WriteLine($"✅ {userName} đã kết nối ({Context.ConnectionId})");

                // Cập nhật danh sách người dùng online cho admin
                await Clients.Group("Admin").SendAsync("UpdateUserList", ConnectedUsers.ToArray());
            }

            await base.OnConnectedAsync();
        }

        // 🔴 Khi user ngắt kết nối
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (ConnectedUsers.TryRemove(Context.ConnectionId, out var name))
            {
                Console.WriteLine($"❌ {name} đã thoát ({Context.ConnectionId})");
                await Clients.Group("Admin").SendAsync("UpdateUserList", ConnectedUsers.ToArray());
            }

            await base.OnDisconnectedAsync(exception);
        }

        // ✉️ User gửi tin đến admin
        public async Task SendMessageToAdmin(string message)
        {
            var senderName = Context.User?.Identity?.Name ?? $"Khách {Context.ConnectionId[..5]}";
            Console.WriteLine($"📨 {senderName} → Admin: {message}");

            // Gửi tin đến tất cả admin
            await Clients.Group("Admin").SendAsync("ReceiveMessage", senderName, message, Context.ConnectionId);

            // Lưu DB
            var chatMessage = new ChatMessage
            {
                SenderId = Context.ConnectionId,
                ReceiverId = "Admin",
                Message = message,
                Timestamp = DateTime.UtcNow,
                IsRead = false
            };
            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            // Gửi thông báo danh sách user mới
            await Clients.Group("Admin").SendAsync("UpdateUserList", ConnectedUsers.ToArray());
        }

        // ✉️ Admin gửi tin cho user
        public async Task SendMessageToUser(string connectionId, string message)
        {
            var adminName = Context.User?.Identity?.Name ?? "Admin";
            Console.WriteLine($"📩 Admin → {connectionId}: {message}");

            await Clients.Client(connectionId).SendAsync("ReceiveMessage", adminName, message);

            var chatMessage = new ChatMessage
            {
                SenderId = adminName,
                ReceiverId = connectionId,
                Message = message,
                Timestamp = DateTime.UtcNow,
                IsRead = false
            };
            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();
        }

        // API nội bộ cho controller nếu cần
        public static IEnumerable<KeyValuePair<string, string>> GetConnectedUsers()
        {
            return ConnectedUsers.ToArray();
        }
    }
}
