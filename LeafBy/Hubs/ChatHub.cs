using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using LeafBy.Data;
using LeafBy.Models;

namespace LeafBy.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager; // Using your custom Appuser class

        public ChatHub(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task SendDirectMessage(string receiverUsername, string message)
        {
            var senderUsername = Context.User.Identity.Name;
            var timestamp = DateTime.Now;

            // 1. Save to SQL Database
            var dm = new DirectMessage
            {
                SenderUsername = senderUsername ?? "Unknown",
                ReceiverUsername = receiverUsername,
                Message = message ?? "",
                Timestamp = timestamp
            };

            _context.DirectMessages.Add(dm);
            await _context.SaveChangesAsync();

            string timeFormatted = timestamp.ToString("h:mm tt");

            // 2. Find the Receiver's internal Identity ID
            var receiver = await _userManager.FindByNameAsync(receiverUsername);

            if (receiver != null)
            {
                // Send directly to the receiver's screen
                await Clients.User(receiver.Id).SendAsync("ReceiveDirectMessage", senderUsername, message, timeFormatted);
            }

            // 3. Bounce the message back to the sender's screen
            await Clients.Caller.SendAsync("ReceiveDirectMessage", senderUsername, message, timeFormatted);
        }
    }
}