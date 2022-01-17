using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using NetMQ;
using NetMQ.Sockets;
using common;

namespace client.impl
{
    public class ZMQChatRoomMessageListener : IBroadcastMessageListener
    {
        private string kEndpoint = "tcp://localhost:{0}";
        private SubscriberSocket m_chatRoomSocket = null;

        public void CloseConnection()
        {
            m_chatRoomSocket?.Dispose();
            m_chatRoomSocket = null;
        }

        /// <summary>
        /// An async-pipe to grab broadcast messages
        /// </summary>
        /// <param name="chatRoomPort"></param>
        /// <param name="onNewMessage"></param>
        /// <returns></returns>
        private async Task CommandHandlerAsync(string chatRoomPort, System.Action<CommandAndPayload> onNewMessage)
        {
            m_chatRoomSocket = new SubscriberSocket();

            string endpoint = String.Format(kEndpoint, chatRoomPort);
            m_chatRoomSocket.Connect(endpoint);
            m_chatRoomSocket.SubscribeToAnyTopic();

            while (true)
            {
                var (cmd, more) = await m_chatRoomSocket.ReceiveFrameStringAsync();

                var payload = "";
                while (more)
                    payload += m_chatRoomSocket.ReceiveFrameString(out more);

                onNewMessage?.Invoke(new CommandAndPayload(cmd, payload));

                // Save CPU
                Thread.Sleep(500);
            }
        }

        /// <summary>
        /// Run a listener for new chat messages
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="chatRoomPort"></param>
        /// <param name="onNewMessage"></param>
        public void RunServerListener(CancellationToken cancellationToken, string chatRoomPort, System.Action<CommandAndPayload> onNewMessage)
        {
            // Make use of NetMQRuntime in order to handle async operations on ZMQ socket
            using (var runtime = new NetMQRuntime())
            {
                runtime.Run(cancellationToken, CommandHandlerAsync(chatRoomPort, onNewMessage));
            }
        }
    }
}
