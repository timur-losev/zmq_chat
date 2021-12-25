using System;

namespace common
{

    public class NetworkCommands
    {
        public const string kHelloKittyCMD = "HelloKitty";
        public const string kAcceptedClientCMD = "Accepted";
        public const string kServerGotTheMessage = "TextMessageAccepted";
        public const string kSendMessageCMD = "SendMessage";
        public const string kLeaveTheServerCMD = "Leave";
        public const string kShutDownServerCMD = "ServerShutDown";
        public const string kNewMessageCMD = "NewMessage";
    }

    public class AcceptedClient
    {
        public string MessagingPort { get; set; }
        public string ChatHistory { get; set; }
    }

    public class ConnectionRequest
    {
        public string UserName { get; set; }
    }

    public class LeaveRequest
    {
        public string UserName { get; set; }
    }

    public class MessagePacket
    {
        public string MessageText { get; set; }
    }
}
