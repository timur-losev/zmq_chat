﻿using System;
using System.Collections.Generic;

namespace common
{
    public class CommandAndPayload
    {
        public string Command;
        public string Payload;

        public CommandAndPayload(string inCommand, string inPaylod)
        {
            Command = inCommand;
            Payload = inPaylod;
        }
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
                System.Diagnostics.Process.Start(path + "/server.exe", "-" + port);
            }
            else
            {
                System.Diagnostics.Process.Start(path + "/../../../../server/bin/Debug/net5.0/server.exe", "-" + port);
            }
        }
    }

    public class NetworkCommands
    {
        public const string kHelloKittyCMD = "HelloKitty";
        public const string kAcceptedClientCMD = "Accepted";
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
