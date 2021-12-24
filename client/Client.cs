using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using NetMQ;
using NetMQ.Sockets;
using System.Threading;
using System.Text.Json;
using common;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace voicelab_test
{
    class TheTest
    {
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var form = new Form1();
            Application.Run(form);
        }
    }

    class ClientEventArgs: EventArgs
    {
        public string MessageText;

        public ClientEventArgs(string msg)
        {
            MessageText = msg;
        }
    }

    class Client
    {
        private static string messagePort = "";
        private static string displayName = "";

        private static CancellationTokenSource ctsconnection = new CancellationTokenSource();
        private static CancellationTokenSource ctsmessagepipe = new CancellationTokenSource();

        public static EventHandler<ClientEventArgs> OnNewMessage;

        // Concurrent queue for communication across threads
        static ConcurrentQueue<KeyValuePair<string, string>> communicationQueue = new ConcurrentQueue<KeyValuePair<string, string>>();

        private static void ConnectionWorker(string displayName, string port)
        {
            using (var requester = new RequestSocket())
            {
                requester.Connect(String.Format("tcp://localhost:{0}", port));

                Thread.Sleep(500);

                Debug.Assert(requester.HasOut);

                var connectionReq = new ConnectionRequest
                {
                    UserName = displayName
                };

                var payload = JsonSerializer.Serialize(connectionReq);

                requester.SendMoreFrame(NetworkCommands.kHelloKittyCMD).SendFrame(payload);

                bool more = false;
                var cmd = requester.ReceiveFrameString(out more);

                payload = "";
                if (cmd == NetworkCommands.kAcceptedClientCMD)
                {
                    while (more)
                        payload += requester.ReceiveFrameString(out more);
                }

                var connectionResponse = JsonSerializer.Deserialize<AcceptedClient>(payload);
                messagePort = connectionResponse.MessagingPort;

                OnNewMessage(null, new ClientEventArgs(connectionResponse.ChatHistory));

                while (true)
                {
                    KeyValuePair<string, string> command;
                    if (communicationQueue.TryDequeue(out command))
                    {
                        switch (command.Key)
                        {
                            case NetworkCommands.kSendMessageCMD:
                                var sendMessage = new SendMessageRequest
                                {
                                    MessageText = command.Value
                                };
                                payload = JsonSerializer.Serialize(sendMessage);

                                requester.SendMoreFrame(NetworkCommands.kSendMessageCMD).SendFrame(payload);

                                more = false;
                                cmd = requester.ReceiveFrameString(out more);

                                Debug.Assert(cmd == NetworkCommands.kServerGotTheMessage);

                                // No operation, just fulfilling ZMQ state machine
                                while (more)
                                    requester.ReceiveFrameString(out more);

                                break;
                        }
                    }
                }
            }
        }

        public static void MessagePipeWorker()
        {
            using (var chatRoomSocket = new SubscriberSocket())
            {
                chatRoomSocket.Connect("tcp://localhost:" + Client.messagePort);
                chatRoomSocket.SubscribeToAnyTopic();

                while (true)
                {
                    var newMessage = chatRoomSocket.ReceiveFrameString();

                    OnNewMessage(null, new ClientEventArgs(newMessage));
                }
            }
        }

        public static void SendMessage(string messageText)
        {
            // Simple check if we connected
            if (messagePort.Length > 0)
            {
                // Sending message
                // User's display name is backed into the message
                communicationQueue.Enqueue(new KeyValuePair<string, string>(NetworkCommands.kSendMessageCMD, displayName + ":" + messageText));
            }
        }

        public static bool Connect(string displayName, string port)
        {
            Client.messagePort = "";
            Client.displayName = displayName;
            ThreadPool.QueueUserWorkItem(new WaitCallback((Object obj) => {
                Client.ConnectionWorker(displayName, port);
            }), ctsconnection.Token);

            // Connection timeout
            Thread.Sleep(1500);

            if (Client.messagePort.Length > 0) {
                ThreadPool.QueueUserWorkItem(new WaitCallback((Object obj) =>
                {
                    Client.MessagePipeWorker();
                }), ctsmessagepipe.Token);

                return true;
            }
            else
            {
                // Connection time out
                // Break the connection task
                ctsconnection.Cancel();
            }

            return false;
        }
    }
}
