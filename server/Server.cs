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
        static string messagePort = "0";
        static string history = "";
        static bool notifyShutdown = false;

        static NetMQPoller poller = null;

        static Queue<KeyValuePair<string, string>> communicationQueue = new Queue<KeyValuePair<string, string>>();
        static string broadcastBuffer = "";

        // Receive commands from the Client
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
                // New Client received 
                case NetworkCommands.kHelloKittyCMD:
                    {
                        var newClient = JsonSerializer.Deserialize<ConnectionRequest>(payload);

                        // Format "joined" message
                        var newLine = "[" + DateTime.Now.ToShortTimeString() + "] " + newClient.UserName + " joined.\n";
                        history += newLine;
                        broadcastBuffer = newLine;

                        Console.WriteLine(newLine);

                        var acceptedClient = new AcceptedClient
                        {
                            MessagingPort = messagePort,
                            ChatHistory = history
                        };

                        // Accept the client and send back the chat history
                        communicationQueue.Enqueue(new KeyValuePair<string, string>(NetworkCommands.kAcceptedClientCMD, JsonSerializer.Serialize(acceptedClient)));

                        break;
                    }

                // Client sending a message, Server transmits that message to the PUB socket
                case NetworkCommands.kSendMessageCMD:
                    {
                        var newMessage = JsonSerializer.Deserialize<MessagePacket>(payload);

                        // Format message
                        newMessage.MessageText = "[" + DateTime.Now.ToShortTimeString() + "] " + newMessage.MessageText + "\n";
                        Console.WriteLine(newMessage.MessageText);

                        broadcastBuffer = newMessage.MessageText;
                        history += newMessage.MessageText;

                        communicationQueue.Enqueue(new KeyValuePair<string, string>(NetworkCommands.kSendMessageCMD, ""));

                        break;
                    }

                // Client intentionally disconnected
                case NetworkCommands.kLeaveTheServerCMD:
                    {
                        var leaver = JsonSerializer.Deserialize<LeaveRequest>(payload);

                        // Format "disconnected" message
                        var line = "[" + DateTime.Now.ToShortTimeString() + "] " + leaver.UserName + " disconnected.\n";
                        history += line;
                        broadcastBuffer = line;

                        Console.WriteLine(line);

                        communicationQueue.Enqueue(new KeyValuePair<string, string>(NetworkCommands.kLeaveTheServerCMD, ""));

                        break;
                    }

                case NetworkCommands.kShutDownServerCMD:
                    {
                        // Server is broadcasting "shutdown" signal through the message pipe
                        notifyShutdown = true;
                        break;
                    }
            }
        }

        // Ready to send response
        static void repSocket_SendReady(object sender, NetMQSocketEventArgs e)
        {
            // Sending back commands to the Client
            while (communicationQueue.Count > 0)
            {
                var cmd = communicationQueue.Dequeue();
                e.Socket.SendMoreFrame(cmd.Key).SendFrame(cmd.Value);
            }
        }

        // Ready to broadcast
        static void pubSocket_SendReady(object sender, NetMQSocketEventArgs e)
        {
            if (broadcastBuffer.Length > 0)
            {
                var messagePacket = new MessagePacket
                {
                    MessageText = broadcastBuffer
                };

                // Consume the buffer
                broadcastBuffer = "";

                e.Socket.SendMoreFrame(NetworkCommands.kNewMessageCMD).SendFrame(JsonSerializer.Serialize(messagePacket));
            }

            // Broadcast shutdown event to subs and exit
            if (notifyShutdown)
            {
                notifyShutdown = false;

                e.Socket.SendFrame(NetworkCommands.kShutDownServerCMD);

                Thread.Sleep(500);
                poller.Stop();
            }
        }

        static void Main(string[] args)
        {
            using (var handShakeSocket = new ResponseSocket())
            {
                // Read command line args to determine the port number
                var handShakePort = "0";
                if (args.Length == 1)
                {
                    handShakePort = args[0].Substring(1);
                }

                handShakeSocket.Bind("tcp://*:" + handShakePort);

                Console.WriteLine("Starting at port " + handShakePort);


                using (var chatRoomSocket = new PublisherSocket())
                {
                    chatRoomSocket.Bind("tcp://*:" + messagePort);

                    Console.WriteLine("Message pipe: " + chatRoomSocket.Options.LastEndpoint);

                    // Determine the given port from the sockopt
                    messagePort = chatRoomSocket.Options.LastEndpoint.Split(':')[2];

                    using (poller = new NetMQPoller { chatRoomSocket, handShakeSocket })
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
