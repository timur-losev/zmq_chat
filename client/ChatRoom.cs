using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using NetMQ;
using NetMQ.Sockets;
using common;
using System.Text.Json;

namespace client
{
    class ChatRoom
    {
        private SubscriberSocket m_chatRoomSocket = null;

        public void CloseConnection()
        {
            m_chatRoomSocket?.Dispose();
            m_chatRoomSocket = null;
        }

        // An async-pipe to grab broadcast messages
        // Can be closed by canceling the token
        private async Task CommandHandlerAsync(string inChatRoomPort, System.Action<string> onNewChatMessage, System.Action onServerShutDown)
        {
            m_chatRoomSocket = new SubscriberSocket();

            string messagePipeEndpoint = String.Format("tcp://localhost:{0}", inChatRoomPort);
            m_chatRoomSocket.Connect(messagePipeEndpoint);
            m_chatRoomSocket.SubscribeToAnyTopic();

            while (true)
            {
                var (cmd, more) = await m_chatRoomSocket.ReceiveFrameStringAsync();

                switch (cmd)
                {
                    case NetworkCommands.kNewMessageCMD:
                        {
                            var payload = "";
                            while (more)
                                payload += m_chatRoomSocket.ReceiveFrameString(out more);

                            var newMessage = JsonSerializer.Deserialize<MessagePacket>(payload);

                            onNewChatMessage?.Invoke(newMessage.MessageText);
                            break;
                        }
                    case NetworkCommands.kShutDownServerCMD:
                        {
                            onServerShutDown?.Invoke();
                            // Close chat room
                            return;
                        }
                }

                // Save CPU
                Thread.Sleep(500);
            }
        }

        /// <summary>
        /// Run a listener for new chat messages
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="chatRoomPort"></param>
        /// <param name="onNewChatMessage"></param>
        /// <param name="onServerShutDown"></param>
        public void RunMessageListener(CancellationToken cancellationToken, string chatRoomPort, System.Action<string> onNewChatMessage, System.Action onServerShutDown)
        {
            // Make use of NetMQRuntime in order to handle async operations on ZMQ socket
            using (var runtime = new NetMQRuntime())
            {
                runtime.Run(cancellationToken, CommandHandlerAsync(chatRoomPort, onNewChatMessage, onServerShutDown));
            }
        }


    }
}
