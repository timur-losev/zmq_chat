using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;
using common;

namespace server.impl
{
    public class ZMQChatRoom: IChatRoom
    {
        CommandAndPayload m_broadcastBuffer = null;

        PublisherSocket m_chatRoomSocket = new PublisherSocket();
        
        public System.Action<CommandAndPayload> OnMessageSent { get; set; }

        /// <summary>
        /// Send message to all SUBs
        /// </summary>
        /// <param name="chatMessage"></param>
        public void SendAll(CommandAndPayload data)
        {
            m_broadcastBuffer = data;
        }

        public void Bind(string endpoint)
        {
            m_chatRoomSocket.Bind(endpoint);
            m_chatRoomSocket.SendReady += new EventHandler<NetMQSocketEventArgs>(pubSocket_SendReady);
        }

        public string GetEndpoint()
        {
            return m_chatRoomSocket.Options.LastEndpoint;
        }

        public ISocketPollable GetPollableHandle()
        {
            return m_chatRoomSocket;
        }

        // Ready to broadcast
        void pubSocket_SendReady(object sender, NetMQSocketEventArgs e)
        {
            if (m_broadcastBuffer != null)
            {
                e.Socket.SendMoreFrame(m_broadcastBuffer.Command).SendFrame(m_broadcastBuffer.Payload);

                OnMessageSent?.Invoke(m_broadcastBuffer);

                m_broadcastBuffer = null; // Consume the buffer
            }
        }
    }
}
