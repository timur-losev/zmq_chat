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
    class ServerController
    {
        string m_chatRoomPort = "0";
        string m_accumulatedChatHistory = "";
        
        NetMQPoller m_poller = null;
        IChatRoom m_chatRoom = null;

        /// <summary>
        /// Accumulate history and redirect message to the chat room
        /// </summary>
        /// <param name="newLine"></param>
        void ApplyChatMessage(string newLine)
        {
            m_accumulatedChatHistory += newLine;

            // Send chat message to all clients
            var messagePacket = new MessagePacket
            {
                MessageText = newLine
            };
            m_chatRoom.SendAll(new CommandAndPayload(NetworkCommands.kNewMessageCMD, JsonSerializer.Serialize(messagePacket)));

            Console.WriteLine(newLine);
        }

        /// <summary>
        /// Register protocol command handlers
        /// </summary>
        /// <param name="handler"></param>
        void RegisterCommandHandlers(ICommandHandler handler)
        {
            // New Client received 
            handler.RegisterCommandHandler(NetworkCommands.kHelloKittyCMD, inPayload =>
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
            handler.RegisterCommandHandler(NetworkCommands.kSendMessageCMD, inPayload =>
            {
                var newMessage = JsonSerializer.Deserialize<MessagePacket>(inPayload);

                // Format chat message
                newMessage.MessageText = String.Format("[{0}]: {1}\n", DateTime.Now.ToShortTimeString(), newMessage.MessageText);

                ApplyChatMessage(newMessage.MessageText);

                return new CommandAndPayload(NetworkCommands.kSendMessageCMD, "");
            });

            // Client intentionally disconnected
            handler.RegisterCommandHandler(NetworkCommands.kLeaveTheServerCMD, inPayload =>
            {
                var leaver = JsonSerializer.Deserialize<LeaveRequest>(inPayload);

                // Format "disconnected" message
                var line = String.Format("[{0}]: {1} disconnected.\n", DateTime.Now.ToShortTimeString(), leaver.UserName);

                ApplyChatMessage(line);

                return new CommandAndPayload(NetworkCommands.kLeaveTheServerCMD, "");
            });

            handler.RegisterCommandHandler(NetworkCommands.kShutDownServerCMD, inPayload =>
            {
                return new CommandAndPayload(NetworkCommands.kShutDownServerCMD, "");
            });
        }

        /// <summary>
        /// Setup a ReqRep listener socket on a given port
        /// </summary>
        /// <param name="inPort"></param>
        /// <returns></returns>
        void SetupRequestResponseChannel(string inPort, ICommandHandler requestResponseHandler)
        {
            requestResponseHandler.Bind(String.Format("tcp://*:{0}", inPort));

            // On server sent command to the client
            requestResponseHandler.OnCommandSent += commandAndPayload =>
            {
                // We need to check if our "shut down" command was handled and then server will forward "shut down" to all the clients and stop
                if (commandAndPayload.Command == NetworkCommands.kShutDownServerCMD)
                {
                    // Server broadcasts "shutdown" signal through the PUB-SUB communication channel
                    m_chatRoom?.SendAll(new CommandAndPayload(NetworkCommands.kShutDownServerCMD, ""));
                }
            };

            RegisterCommandHandlers(requestResponseHandler);

        }

        /// <summary>
        /// Setup chat room
        /// </summary>
        /// <param name="inPort"></param>
        /// <param name="chatRoom"></param>
        void SetupChatRoom(string inPort, IChatRoom chatRoom)
        {
            chatRoom.Bind(String.Format("tcp://*:{0}", inPort));

            // After we notified all clients about the shut down, the server will stop
            chatRoom.OnMessageSent += commandAndPayload => {
                if (commandAndPayload.Command == NetworkCommands.kShutDownServerCMD)
                {
                    m_poller.Stop();
                }
            };
        }
        /// <summary>
        /// Run the server
        /// </summary>
        /// <param name="args"></param>
        /// <param name="requestResponseHandler"></param>
        /// <param name="chatRoom"></param>
        public void Run(string[] args, ICommandHandler requestResponseHandler, IChatRoom chatRoom)
        {
            // Read command line args to determine the port number
            var port = "0";
            if (args.Length == 1)
            {
                port = args[0].Substring(1);
            }

            SetupRequestResponseChannel(port, requestResponseHandler);
            Console.WriteLine(String.Format("Starting at port {0}", port));

            m_chatRoom = chatRoom;
            SetupChatRoom(m_chatRoomPort, chatRoom);

            Console.WriteLine(String.Format("Message pipe: {0}", m_chatRoom.GetEndpoint()));

            // Determine the given port from the sockopt
            m_chatRoomPort = m_chatRoom.GetEndpoint().Split(':')[2];

            m_poller = new NetMQPoller { m_chatRoom.GetPollableHandle(), requestResponseHandler.GetPollableHandle() };

            m_poller.Run();
        }
    }
}
