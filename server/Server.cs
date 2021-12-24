using System;
using NetMQ;
using NetMQ.Sockets;
using System.Threading;
using System.Text.Json;
using common;
using System.Collections.Generic;

namespace server
{
    class Server
    {
        static string messagePort = "6425";
        static string history = "";
        static bool notifySubs = false;

        static Queue<KeyValuePair<string, string>> communicationQueue = new Queue<KeyValuePair<string, string>>();
        static Queue<KeyValuePair<string, string>> messageQueue = new Queue<KeyValuePair<string, string>>();

        static void repSocket_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            var more = false;
            var cmd = e.Socket.ReceiveFrameString(out more);
            var payload = "";

            while (more)
            {
                payload += e.Socket.ReceiveFrameString(out more);
            }

            switch (cmd)
            {
                case NetworkCommands.kHelloKittyCMD:
                    var newClient = JsonSerializer.Deserialize<ConnectionRequest>(payload);

                    // Format "joined" message
                    var newLine = "[" + DateTime.Now.ToShortDateString() + "] " + newClient.UserName + " joined.\n";
                    history += newLine;

                    notifySubs = true;

                    Console.WriteLine(newLine);

                    var acceptedClient = new AcceptedClient
                    {
                        MessagingPort = messagePort,
                        ChatHistory = history
                    };

                    // Accept the client and send back the chat history
                    communicationQueue.Enqueue(new KeyValuePair<string, string>(NetworkCommands.kAcceptedClientCMD, JsonSerializer.Serialize(acceptedClient)));

                    break;

                case NetworkCommands.kSendMessageCMD:
                    var newMessage = JsonSerializer.Deserialize<SendMessageRequest>(payload);

                    // Format message
                    newMessage.MessageText = "[" + DateTime.Now.ToShortDateString() + "] " + newMessage.MessageText + "\n";
                    Console.WriteLine(newMessage.MessageText);

                    history += newMessage.MessageText;

                    communicationQueue.Enqueue(new KeyValuePair<string, string>(NetworkCommands.kServerGotTheMessage, ""));

                    notifySubs = true;
                    break;
            }
        }

        static void repSocket_SendReady(object sender, NetMQSocketEventArgs e)
        {
            while (communicationQueue.Count > 0)
            {
                var cmd = communicationQueue.Dequeue();
                e.Socket.SendMoreFrame(cmd.Key).SendFrame(cmd.Value);
            }
        }

        static void pubSocket_SendReady(object sender, NetMQSocketEventArgs e)
        {
            if (notifySubs)
            {
                notifySubs = false;
                e.Socket.SendFrame(history);
            }
        }

        static void Main(string[] args)
        {
            using (var handShakeSocket = new ResponseSocket())
            {
                handShakeSocket.Bind("tcp://*:3366");

                using (var chatRoomSocket = new PublisherSocket())
                {
                    chatRoomSocket.Bind("tcp://*:" + messagePort);

                    using (var poller = new NetMQPoller { chatRoomSocket, handShakeSocket })
                    {
                        handShakeSocket.SendReady += new EventHandler<NetMQSocketEventArgs>(repSocket_SendReady);
                        handShakeSocket.ReceiveReady += new EventHandler<NetMQSocketEventArgs>(repSocket_ReceiveReady);

                        chatRoomSocket.SendReady += new EventHandler<NetMQSocketEventArgs>(pubSocket_SendReady);

                        poller.Run();
                    }
                }
            }
        }
    }
}
