using System;

namespace common
{

    public class NetworkCommands
    {
        public const string kHelloKittyCMD = "HelloKitty";
        public const string kAcceptedClientCMD = "Accepted";
        public const string kServerGotTheMessage = "TextMessageAccepted";
        public const string kSendMessageCMD = "SendMessage";
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

    public class SendMessageRequest
    {
        public string MessageText { get; set; }
    }

    public class Common
    {
        public static Int32 UnixTimeStamp()
        {
            return (Int32)(DateTime.Now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
    }
}
