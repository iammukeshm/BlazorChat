using System;

namespace BlazorChat.Shared
{
    public class ChatMessage
    {
        public long Id { get; set; }
        public string FromUserId { get; set; }
        public string ToUserId { get; set; }
        public string Message { get; set; }
        public ChatStatus Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public virtual ApplicationUser FromUser { get; set; }
        public virtual ApplicationUser ToUser { get; set; }
    }

    public enum ChatStatus
    {
        Undelivered,
        Delivered,
        Seen
    }
}
