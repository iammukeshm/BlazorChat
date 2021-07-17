using BlazorChat.Server.Data;
using BlazorChat.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BlazorChat.Shared.Extensions;
using System.Collections.Generic;

namespace BlazorChat.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        public ChatController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }
        [HttpGet("{contactId}")]
        public Task<List<ChatMessage>> GetConversationAsync(string contactId)
        {
            var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).Select(a => a.Value).FirstOrDefault();
            return _context.ChatMessages
                    .Where(h => (h.FromUserId == contactId && h.ToUserId == userId) || (h.FromUserId == userId && h.ToUserId == contactId))
                    .OrderBy(a => a.CreatedDate)
                    .Include(a => a.FromUser)
                    .Include(a => a.ToUser)
                    .Select(x => new ChatMessage
                    {
                        FromUserId = x.FromUserId,
                        Message = x.Message,
                        CreatedDate = x.CreatedDate,
                        Id = x.Id,
                        ToUserId = x.ToUserId,
                        Status = x.Status,
                        ToUser = x.ToUser,
                        FromUser = x.FromUser
                    }).ToListAsync();
        }
        [HttpGet("users")]
        public Task<List<ApplicationUser>> GetUsersAsync()
        {
            var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).Select(a => a.Value).FirstOrDefault();
            return _context.Users
                .Where(user => user.Id != userId)
                .Select(user => new
                {
                    User = user,
                    UnReadCount =
                        _context.ChatMessages.Count(msg => msg.FromUserId == user.Id && msg.Status != ChatStatus.Seen)
                })
                .ToListAsync()
                .OnCompletion(x => x.Select(arg =>
                {
                    arg.User.UnreadCount = arg.UnReadCount;
                    return arg.User;
                })
                .ToList());
        }

        [HttpGet("users/{userId}")]
        public Task<ApplicationUser> GetUserDetailsAsync(string userId)
        => _context.Users
                   .Where(user => user.Id == userId)
                   .FirstOrDefaultAsync();

        [HttpPatch("users/{userId}/status/{status}")]
        public async Task<IActionResult> UpdateConversationStatus(string userId, ChatStatus status)
        {
            var messages = await _context.ChatMessages
                .Where(x => x.FromUserId == userId && x.Status != ChatStatus.Seen)
                .ToListAsync();
            messages.ForEach(x => x.Status = status);
            return Ok(await _context.SaveChangesAsync());
        }
        [HttpPatch("users/messages/{msgId:long}/status/{status:int}")]
        public async Task<ChatMessage> UpdateMsgStatus(long msgId, int status)
        {

            var msg = await _context.ChatMessages.FirstOrDefaultAsync(x => x.Id == msgId);
            msg.Status = (ChatStatus) status;
            await _context.SaveChangesAsync();
            return msg;
        }
        [HttpPost]
        public async Task<ChatMessage> SaveMessageAsync(ChatMessage message)
        {
            var userId = User.Claims.Where(a => a.Type == ClaimTypes.NameIdentifier).Select(a => a.Value).FirstOrDefault();
            message.FromUserId = userId;
            message.CreatedDate = DateTime.Now;
            var result = await (from user in _context.Users
                                where user.Id == message.ToUserId
                          select new
                          {
                              ToUser = user,
                              FromUser = _context.Users.Where(user => user.Id == message.FromUserId).FirstOrDefault(),
                          }).FirstOrDefaultAsync();
            message.ToUser = result.ToUser;
            message.FromUser = result.FromUser;
            await _context.ChatMessages.AddAsync(message);
            await _context.SaveChangesAsync();
            return message;
        }
    }
}
