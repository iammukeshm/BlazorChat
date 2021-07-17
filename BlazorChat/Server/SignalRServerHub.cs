using BlazorChat.Shared;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace BlazorChat.Server
{
    [Authorize]
    public class SignalRServerHub : Hub<IChatClientHub>, IChatServerHub
    {
        public Task SendMessageAsync(ChatMessage message, string receiverUserId)
        => Clients.User(receiverUserId).ReceiveMessage(message);
        public Task ChatNotificationAsync(string message, string receiverUserId, string senderUserId)
        => Clients.User(receiverUserId).ReceiveChatNotification(message,senderUserId);
        public Task UpdateConversationStatusAsync(string receiverUserId, string senderUserId, ChatStatus status)
        => Clients.User(receiverUserId).ReceiveConversationStatus(senderUserId, status);
        public Task UpdateMessageStatusAsync(long msgId,string receiverUserId, ChatStatus status)
        => Clients.User(receiverUserId).ReceiveMessageStatus(msgId, status);
    }
}
