using System;
using NetMQ;
using NetMQ.Sockets;
using System.Threading;
using System.Text.Json;
using common;
using System.Collections.Generic;
using System.Diagnostics;

namespace server
{ 
    class BroadcastData
    {
        public bool NotifyShutdown = false;
        public string BroadcastBuffer { get; set; }

        public BroadcastData()
        {
            this.BroadcastBuffer = "";
        }
        public bool IsBroadcastBufferReady()
        {
            return BroadcastBuffer.Length > 0;
        }
        public string ConsumeBroadcastBuffer()
        {
            Debug.Assert(IsBroadcastBufferReady());

            string retval = BroadcastBuffer;
            BroadcastBuffer = "";

            return retval;
        }
    }
    class ServerController
    {
        string m_chatRoomPort = "0";
        string m_accumulatedChatHistory = "";
        
        NetMQPoller poller = null;

        BroadcastData m_broadcastData = new BroadcastData();
        impl.ZMQRequestResponseSocketWrapper m_reqRepSocket = new impl.ZMQRequestResponseSocketWrapper();

        void ApplyChatMessage(string newLine)
        {
            m_accumulatedChatHistory += newLine;

            m_broadcastData.BroadcastBuffer = newLine;

            Console.WriteLine(newLine);
        }

        // Ready to broadcast
        void pubSocket_SendReady(object sender, NetMQSocketEventArgs e)
        {
            if (m_broadcastData.IsBroadcastBufferReady())
            {
                var messagePacket = new MessagePacket
                {
                    MessageText = m_broadcastData.ConsumeBroadcastBuffer()
                };

                e.Socket.SendMoreFrame(NetworkCommands.kNewMessageCMD).SendFrame(JsonSerializer.Serialize(messagePacket));
            }

            // Send shutdown event to all subscribers and exit
            if (m_broadcastData.NotifyShutdown)
            {
                m_broadcastData.NotifyShutdown = false;

                e.Socket.SendFrame(NetworkCommands.kShutDownServerCMD);

                Thread.Sleep(500);
                poller.Stop();
            }
        }

        void RegisterCommandHandlers(impl.ZMQRequestResponseSocketWrapper socket)
        {
            // New Client received 
            socket.RegisterCommandHandler(NetworkCommands.kHelloKittyCMD, inPayload =>
            {
                var newClient = JsonSerializer.Deserialize<ConnectionRequest>(inPayload);

                // Format "joined" message

                var newLine = String.Format("[{0}]: {1} joined.\n", DateTime.Now.ToShortTimeString(), newClient.UserName);

                ApplyChatMessage(newLine);

                var acceptedClient = new AcceptedClient
                {
                    ChatRoomPort = m_chatRoomPort,
                    ChatHistory = m_accumulatedChatHistory
                };

                // Accept the client and send back the chat history
                return new CommandAndPayload(NetworkCommands.kAcceptedClientCMD, JsonSerializer.Serialize(acceptedClient));
            });

            // Client sending a message, Server transmits that message to the PUB socket
            socket.RegisterCommandHandler(NetworkCommands.kSendMessageCMD, inPayload =>
            {
                var newMessage = JsonSerializer.Deserialize<MessagePacket>(inPayload);

                // Format chat message
                newMessage.MessageText = String.Format("[{0}]: {1}\n", DateTime.Now.ToShortTimeString(), newMessage.MessageText);

                ApplyChatMessage(newMessage.MessageText);

                return new CommandAndPayload(NetworkCommands.kSendMessageCMD, "");
            });

            // Client intentionally disconnected
            socket.RegisterCommandHandler(NetworkCommands.kLeaveTheServerCMD, inPayload =>
            {
                var leaver = JsonSerializer.Deserialize<LeaveRequest>(inPayload);

                // Format "disconnected" message
                var line = String.Format("[{0}]: {1} disconnected.\n", DateTime.Now.ToShortTimeString(), leaver.UserName);

                ApplyChatMessage(line);

                return new CommandAndPayload(NetworkCommands.kLeaveTheServerCMD, "");
            });

            socket.RegisterCommandHandler(NetworkCommands.kShutDownServerCMD, inPayload =>
            {
                return new CommandAndPayload(NetworkCommands.kShutDownServerCMD, "");
            });
        }

        /// <summary>
        /// Setup a ReqRep listener socket on a given port
        /// </summary>
        /// <param name="inPort"></param>
        /// <returns></returns>
        impl.ZMQRequestResponseSocketWrapper CreateRequestResponseChannel(string inPort)
        {
            var requestResponseSocket = new impl.ZMQRequestResponseSocketWrapper(
            // On command sent to the client
            commandAndPayload =>
            {
                // We need to check if "shut down" command was handled and then server will send "shut down" to all the clients and stop
                if (commandAndPayload.Command == NetworkCommands.kShutDownServerCMD)
                {
                    // Server broadcasts "shutdown" signal through the PUB-SUB communication channel
                    m_broadcastData.NotifyShutdown = true;
                }
            });

            RegisterCommandHandlers(requestResponseSocket);

            requestResponseSocket.BindToPort(inPort);

            return requestResponseSocket;
        }
        public void Run(string[] args)
        {
            // Read command line args to determine the port number
            var port = "0";
            if (args.Length == 1)
            {
                port = args[0].Substring(1);
            }

            var requestResponseSocket = CreateRequestResponseChannel(port);

            Console.WriteLine("Starting at port " + port);

                using (var chatRoomSocket = new PublisherSocket())
                {
                    chatRoomSocket.Bind("tcp://*:" + m_chatRoomPort);

                    Console.WriteLine("Message pipe: " + chatRoomSocket.Options.LastEndpoint);

                    // Determine the given port from the sockopt
                    m_chatRoomPort = chatRoomSocket.Options.LastEndpoint.Split(':')[2];

                    using (poller = new NetMQPoller { chatRoomSocket, requestResponseSocket.GetHandle() })
                    {
                        
                        chatRoomSocket.SendReady += new EventHandler<NetMQSocketEventArgs>(pubSocket_SendReady);

                        poller.Run();
                    }
                }
            
        }
    }
}
