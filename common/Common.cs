using System;
using System.Collections.Generic;

namespace common
{
    public class CommandAndPayload
    {
        public string Command;
        public string Payload;

        public CommandAndPayload(string inCommand, string inPayload)
        {
            Command = inCommand;
            Payload = inPayload;
        }
    }
    public class Config
    {
        public static double kServerCommunicationTimeout = 2.0; //Connection timeout in seconds
    }

    public class Common
    {
        public static void StartServer(string port, string path, bool unitTestEnv = false)
        {
            var isDebug = false;
#if DEBUG
            isDebug = true;
#endif

            if (!isDebug || unitTestEnv)
            {
                System.Diagnostics.Process.Start(String.Format("{0}/server.exe", path), String.Format("-{0}", port));
            }
            else
            {
                System.Diagnostics.Process.Start(String.Format("{0}/../../../../server/bin/Debug/net5.0/server.exe", path), String.Format("-{0}", port));
            }
        }
    }

    public class NetworkCommands
    {
        public const string kHelloKittyCMD = "HelloKitty";
        public const string kAcceptedClientCMD = "Accepted";
        public const string kSendMessageCMD = "SendMessage";
        public const string kLeaveTheServerCMD = "LeaveTheServer";
        public const string kShutDownServerCMD = "ServerShutDown";
        public const string kNewMessageCMD = "NewMessage";
    }

    public class AcceptedClient
    {
        public string ChatRoomPort { get; set; }
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
