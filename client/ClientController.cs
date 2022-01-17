using System;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;
using System.Threading;
using System.Text.Json;
using common;

namespace client
{
    public class ClientController
    {
        public static double kServerCommunicationTimeout = 2.0; //Connection timeout in seconds

        private string m_userName = "";

        private CancellationTokenSource m_ctsRequestResponsePipe = null;
        private CancellationTokenSource m_ctsChatRoom = null;

        private RequestResponseProcessor m_requestResponseProcessor = new RequestResponseProcessor(kServerCommunicationTimeout);
        private ChatRoom m_chatRoom = new ChatRoom();

        /// <summary>
        /// Uber close all
        /// </summary>
        private void CloseAllConnections()
        {
            m_ctsChatRoom?.Cancel();
            m_ctsRequestResponsePipe?.Cancel();

            Thread.Sleep(500);
            m_requestResponseProcessor.CloseConnection();
            m_chatRoom.CloseConnection();
        }

        /// <summary>
        /// Sending a chat message
        /// User's display name is backed into the message
        /// </summary>
        /// <param name="inMessageText"></param>
        public void SendChatMessage(string inMessageText)
        {
            var payload = JsonSerializer.Serialize(new MessagePacket
            {
                MessageText = String.Format("{0}:{1}", m_userName, inMessageText)
            });

            m_requestResponseProcessor.SendData(new common.CommandAndPayload(NetworkCommands.kSendMessageCMD, payload));
        }

        // Send leave the server command
        // Client will disconnect automatically
        public void SendLeaveTheServer()
        {
            var payload = JsonSerializer.Serialize(new LeaveRequest
            {
                UserName = m_userName
            });

            // Send leave the server, communication will be interrupted on response
            m_requestResponseProcessor.SendData(new common.CommandAndPayload(NetworkCommands.kLeaveTheServerCMD, payload));

            // Close the Chat Message Pipe because communication is going to interrupt
            m_ctsChatRoom?.Cancel();
        }

        /// <summary>
        /// Shutdown the server
        /// Server should send a broadcast message about the shutdown, client disconnects after that
        /// </summary>
        public void SendServerShutdown()
        {
            m_requestResponseProcessor.SendData(new common.CommandAndPayload(NetworkCommands.kShutDownServerCMD, ""));
        }

        /// <summary>
        /// Perform a connection with time out
        /// </summary>
        /// <param name="displayName"></param>
        /// <param name="inPort"></param>
        /// <param name="onConnected"></param>
        /// <param name="onConnectionFailed"></param>
        /// <param name="onNewChatMessage"></param>
        /// <param name="onServerShutDown"></param>
        public void Connect(string displayName, string inPort, System.Action<string> onConnected, System.Action onConnectionFailed, System.Action<string> onNewChatMessage, System.Action onServerShutDown)
        {
            CloseAllConnections();

            m_userName = displayName;

            m_ctsRequestResponsePipe = new CancellationTokenSource();

            // Run operation in background
            Task.Run(() =>
            {
                m_requestResponseProcessor.InitiateConnection(
                    inPort,
                    // Provide connection payload
                    JsonSerializer.Serialize(new ConnectionRequest
                    {
                        UserName = displayName
                    }),
                    // onSuccess
                    (string chatHistory, string chatRoomPort) =>
                    {
                        // Enter the given chat room
                        m_ctsChatRoom = new CancellationTokenSource();
                        Task.Run(() =>
                        {
                            m_chatRoom.RunMessageListener(m_ctsChatRoom.Token, chatRoomPort, onNewChatMessage, onServerShutDown);
                        });

                        // Provide chat history to the caller
                        onConnected(chatHistory);

                        // Run a communication channel between client and server
                        m_requestResponseProcessor.Run(m_ctsRequestResponsePipe.Token,
                            // onResponse
                            (CommandAndPayload data) =>
                            {
                                // Server accepter our leave
                                if (data.Command == NetworkCommands.kLeaveTheServerCMD)
                                {
                                    // Cancel all operations
                                    m_ctsRequestResponsePipe.Cancel();
                                }
                            },
                            // onConnectionLost callback
                            onServerShutDown);
                    },
                    // onConnectionFailed
                    () =>
                    {
                        // Connection time out
                        // Break all ongoing tasks
                        CloseAllConnections();

                        onConnectionFailed();
                    });
            }, m_ctsRequestResponsePipe.Token);
        }
    }
}
