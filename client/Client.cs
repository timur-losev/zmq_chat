using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;
using System.Threading;
using System.Text.Json;
using common;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace voicemod_test
{
    public class ClientEventArgs : EventArgs
    {
        public string MessageText;

        public ClientEventArgs(string msg)
        {
            MessageText = msg;
        }
    }

    enum ConnectionState
    {
        CONNECTING,
        CONNECTED,
        NOTCONNECTED
    }

    public class Client
    {
        private string messagePort = "";
        private string displayName = "";

        private CancellationTokenSource ctsconnection = null;
        private CancellationTokenSource ctsmessagepipe = null;

        private RequestSocket requester = null;
        private SubscriberSocket chatRoomSocket = null;

        private string reqRepEndPoint = "";
        private string messagePipeEndpoint = "";

        private ConnectionState connectionState = ConnectionState.CONNECTED;

        public EventHandler<ClientEventArgs> OnNewMessage;
        public EventHandler<ClientEventArgs> OnServerDisconnected;

        // Concurrent queue for communication across threads
        ConcurrentQueue<KeyValuePair<string, string>> communicationQueue = new ConcurrentQueue<KeyValuePair<string, string>>();

        // Uber close all
        private void CloseAllConnections()
        {
            if (ctsmessagepipe != null)
                ctsmessagepipe.Cancel();

            if (ctsconnection != null)
                ctsconnection.Cancel();

            Thread.Sleep(500);

            if (requester != null)
            {
                requester.Dispose();
                requester = null;
            }

            if (chatRoomSocket != null)
            {
                chatRoomSocket.Dispose();
                chatRoomSocket = null;
            }

            communicationQueue.Clear();
        }

        private bool TryReqRep(string cmd, string payload)
        {
            var timeout = TimeSpan.FromSeconds(2.5);

            if (requester.TrySendFrame(timeout, cmd, true))
            {
                if (requester.TrySendFrame(timeout, payload, false))
                {
                    var responseCmd = "";
                    var more = false;
                    if (requester.TryReceiveFrameString(timeout, out responseCmd, out more))
                    {
                        Debug.Assert(cmd == responseCmd);

                        // No operation, just fulfilling ZMQ state machine
                        while (more)
                            requester.ReceiveFrameString(out more);

                        return true;
                    }
                }
            }

            return false;
        }

        // Process communication between Client and Server
        private bool ProcessReqRepMessages()
        {
            KeyValuePair<string, string> command;
            while (true)
            {
                if (communicationQueue.TryDequeue(out command))
                {
                    switch (command.Key)
                    {
                        // Send new chat message
                        case NetworkCommands.kSendMessageCMD:
                            {
                                var sendMessage = new MessagePacket
                                {
                                    MessageText = command.Value
                                };
                                var payload = JsonSerializer.Serialize(sendMessage);

                                if (!TryReqRep(NetworkCommands.kSendMessageCMD, payload))
                                {
                                    return false;
                                }

                                break;
                            }
                        // Send disconnection
                        case NetworkCommands.kLeaveTheServerCMD:
                            {
                                var msg = new LeaveRequest
                                {
                                    UserName = displayName
                                };
                                var payload = JsonSerializer.Serialize(msg);

                                TryReqRep(NetworkCommands.kLeaveTheServerCMD, payload);

                                // Cancel the Chat Message Pipe
                                ctsmessagepipe.Cancel();

                                // Return from the worker thread
                                return true;
                            }

                        // Send shut down to the server
                        case NetworkCommands.kShutDownServerCMD:
                            {
                                requester.SendFrame(NetworkCommands.kShutDownServerCMD);
                                break;
                            }
                    }

                    // Save CPU
                    Thread.Sleep(100);
                }
            }
        }

        private void OnConnectionLost()
        {
            OnNewMessage(null, new ClientEventArgs("SERVER HAS BEEN SHUT DOWN"));

            if (OnServerDisconnected != null)
                OnServerDisconnected(null, null);
        }

        // Initiate a connection between Client and Server
        private void ConnectionWorker(string displayName, string port)
        {
            requester = new RequestSocket();

            reqRepEndPoint = String.Format("tcp://localhost:{0}", port);

            requester.Connect(reqRepEndPoint);

            Thread.Sleep(500);

            Debug.Assert(requester.HasOut);

            var connectionReq = new ConnectionRequest
            {
                UserName = displayName
            };

            var payload = JsonSerializer.Serialize(connectionReq);

            requester.SendMoreFrame(NetworkCommands.kHelloKittyCMD).SendFrame(payload);

            bool more = false;
            var cmd = "";
            // Try a positive response. Timeout is 2 secs
            if (requester.TryReceiveFrameString(TimeSpan.FromSeconds(2), out cmd, out more))
            {
                payload = "";
                if (cmd == NetworkCommands.kAcceptedClientCMD)
                {
                    while (more)
                        payload += requester.ReceiveFrameString(out more);
                }

                var connectionResponse = JsonSerializer.Deserialize<AcceptedClient>(payload);
                // Server returns a messaging port and chat history
                messagePort = connectionResponse.MessagingPort;
                connectionState = ConnectionState.CONNECTED;

                OnNewMessage(null, new ClientEventArgs(connectionResponse.ChatHistory));

                // A run loop from here
                var result = ProcessReqRepMessages();

                // Unexpected exit
                if (!result)
                {
                    OnConnectionLost();
                }
            }

            connectionState = ConnectionState.NOTCONNECTED;
        }

        // An async-pipe to grab broadcast messages
        // Can be closed by canceling the token
        private async Task MessagePipeWorkerAsync()
        {
            chatRoomSocket = new SubscriberSocket();

            messagePipeEndpoint = "tcp://localhost:" + messagePort;
            chatRoomSocket.Connect(messagePipeEndpoint);
            chatRoomSocket.SubscribeToAnyTopic();

            while (true)
            {
                var (cmd, more) = await chatRoomSocket.ReceiveFrameStringAsync();

                switch (cmd)
                {
                    case NetworkCommands.kNewMessageCMD:
                        {
                            var payload = "";
                            while (more)
                                payload += chatRoomSocket.ReceiveFrameString(out more);

                            var newMessage = JsonSerializer.Deserialize<MessagePacket>(payload);

                            OnNewMessage(null, new ClientEventArgs(newMessage.MessageText));
                            break;
                        }
                    case NetworkCommands.kShutDownServerCMD:
                        {
                            OnConnectionLost();
                            // Return from the message pipe thread
                            return;
                        }
                }

                // Save CPU
                Thread.Sleep(500);
            }
        }

        // Sending a chat message
        // User's display name is backed into the message
        public void SendChatMessage(string messageText)
        {
            if (connectionState == ConnectionState.CONNECTED)
            {
                communicationQueue.Enqueue(new KeyValuePair<string, string>(NetworkCommands.kSendMessageCMD, displayName + ":" + messageText));
            }
        }

        // Send leave the server command
        // Client will disconnect automatically
        public void SendLeaveTheServer()
        {
            if (connectionState == ConnectionState.CONNECTED)
            {
                communicationQueue.Enqueue(new KeyValuePair<string, string>(NetworkCommands.kLeaveTheServerCMD, ""));
            }
        }

        // Shutdown the server
        // Server should send a broadcast message about the shutdown, client disconnects after that
        public void SendServerShutdown()
        {
            if (connectionState == ConnectionState.CONNECTED)
            {
                communicationQueue.Enqueue(new KeyValuePair<string, string>(NetworkCommands.kShutDownServerCMD, ""));
            }
        }

        // Perform a connection with time out 2 secs
        public void Connect(string displayName, string port, System.Action<bool> onConnect)
        {
            CloseAllConnections();

            connectionState = ConnectionState.CONNECTING;

            messagePort = "";
            this.displayName = displayName;

            ctsconnection = new CancellationTokenSource();

            ThreadPool.QueueUserWorkItem(new WaitCallback((Object obj) =>
            {
                ConnectionWorker(displayName, port);
            }), ctsconnection.Token);

            while (connectionState == ConnectionState.CONNECTING)
            {
                Thread.Sleep(10);
            }

            if (messagePort.Length > 0 && connectionState == ConnectionState.CONNECTED)
            {
                ctsmessagepipe = new CancellationTokenSource();

                ThreadPool.QueueUserWorkItem(new WaitCallback((Object obj) =>
                {
                    using (var runtime = new NetMQRuntime())
                    {
                        runtime.Run(ctsmessagepipe.Token, MessagePipeWorkerAsync());
                    }
                }));

                onConnect(true);
            }
            else
            {
                // Connection time out
                // Break the connection task
                CloseAllConnections();

                onConnect(false);
            }
        }
    }
}
