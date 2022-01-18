using System;
using System.Threading.Tasks;
using System.Threading;
using System.Text.Json;
using System.Diagnostics;
using common;

namespace client
{
    public class ClientController
    {
        private string m_userName = "";

        private CancellationTokenSource m_ctsRequestResponsePipe = null;
        private CancellationTokenSource m_ctsChatRoom = null;

        private IRequestResponseProcessor m_requestResponseProcessor = null;
        private IBroadcastMessageListener m_chatRoom = null;

        public ClientController(IBroadcastMessageListener broadcastMessageListener, IRequestResponseProcessor reqRepProc)
        {
            m_chatRoom = broadcastMessageListener;
            m_requestResponseProcessor = reqRepProc;
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

        /// <summary>
        /// Send leave the server command
        /// Client will disconnect automatically
        /// </summary>
        public void SendLeaveTheServer()
        {
            var payload = JsonSerializer.Serialize(new LeaveRequest
            {
                UserName = m_userName
            });

            // Send leave from the server, communication will be interrupted on response
            m_requestResponseProcessor.SendData(new common.CommandAndPayload(NetworkCommands.kLeaveTheServerCMD, payload));

            // Close the chat room because communication is going to interrupt
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
                    (CommandAndPayload response) =>
                    {
                        Debug.Assert(response.Command == NetworkCommands.kAcceptedClientCMD);
                        var connectionResponse = JsonSerializer.Deserialize<AcceptedClient>(response.Payload);

                        // Prepare a -cancellation token
                        m_ctsChatRoom = new CancellationTokenSource();
                        // Enter the given chat room
                        Task.Run(() =>
                        {
                            m_chatRoom.RunServerListener(m_ctsChatRoom.Token, connectionResponse.ChatRoomPort, 
                                // onNewMessage
                                (CommandAndPayload data) => {
                                    switch (data.Command)
                                    {
                                        case NetworkCommands.kNewMessageCMD:
                                            {
                                                var newMessage = JsonSerializer.Deserialize<MessagePacket>(data.Payload);

                                                onNewChatMessage?.Invoke(newMessage.MessageText);
                                                break;
                                            }
                                        case NetworkCommands.kShutDownServerCMD:
                                            {
                                                onServerShutDown?.Invoke();

                                                // Close the chat room
                                                m_ctsChatRoom.Cancel();
                                                break;
                                            }
                                    }
                                });
                        });

                        // Provide chat history to the caller
                        onConnected(connectionResponse.ChatHistory);

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


        /// <summary>
        /// Uber close all
        /// </summary>
        private void CloseAllConnections()
        {
            m_ctsChatRoom?.Cancel();
            m_ctsRequestResponsePipe?.Cancel();

            Thread.Sleep(100);
            m_requestResponseProcessor.CloseConnection();
            m_chatRoom.CloseConnection();
        }
    }
}
