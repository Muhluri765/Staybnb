using System;
using System.Collections.Generic;
using Staybnb.Models;

namespace Staybnb.ViewModels.Communication
{
    public class ConversationSummaryDto
    {
        public string OtherUserId { get; set; } = string.Empty;
        public string OtherUserName { get; set; } = string.Empty;
        public string? OtherUserProfilePicture { get; set; }
        public string LastMessageContent { get; set; } = string.Empty;
        public DateTime LastMessageTime { get; set; }
        public bool IsOutgoing { get; set; }
    }

    public class InboxViewModel
    {
        public List<ConversationSummaryDto> Conversations { get; set; } = new List<ConversationSummaryDto>();
    }

    public class ConversationViewModel
    {
        public string OtherUserId { get; set; } = string.Empty;
        public string OtherUserName { get; set; } = string.Empty;
        public string? OtherUserProfilePicture { get; set; }
        public List<Message> Messages { get; set; } = new List<Message>();
        public string NewMessageContent { get; set; } = string.Empty;
    }
}
