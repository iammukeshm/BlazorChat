using System.Threading.Tasks;

namespace BlazorChat.Shared
{
    public interface IChatServerHub
    {
        Task SendMessageAsync(ChatMessage message, string userName);
        Task ChatNotificationAsync(string message, string receiverUserId, string senderUserId);
        Task UpdateConversationStatusAsync(string receiverUserId, string targetUserId, ChatStatus status);
        Task UpdateMessageStatusAsync(long msgId,string receiverUserId, ChatStatus status);
    }
}
