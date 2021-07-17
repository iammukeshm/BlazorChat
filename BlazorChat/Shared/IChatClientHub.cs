using System.Threading.Tasks;

namespace BlazorChat.Shared
{
    public interface IChatClientHub
    {
        Task ReceiveMessage(ChatMessage message);
        Task ReceiveChatNotification(string message, string senderUserId);
        Task ReceiveConversationStatus(string userId, ChatStatus status);
        Task ReceiveMessageStatus(long msgId,ChatStatus status);

    }
}
