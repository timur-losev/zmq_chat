using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using NetMQ;
using NetMQ.Sockets;
using common;

namespace server.impl
{
    using CommandHandlerSignature = Func<string, CommandAndPayload>;
    public class ZMQRequestResponseCommandHandler: ICommandHandler
    {
        private Dictionary<string, CommandHandlerSignature> m_commandHandlers = new Dictionary<string, CommandHandlerSignature>();
        private Queue<CommandAndPayload> m_communicationQueue = new Queue<CommandAndPayload>();
        private ResponseSocket m_requestResponseSocket = new ResponseSocket();
        public System.Action<CommandAndPayload> OnCommandSent { get; set; }

        /// <summary>
        /// Bind the socket
        /// </summary>
        /// <param name="endpoint"></param>
        public void Bind(string endpoint)
        {
            m_requestResponseSocket.Bind(endpoint);

            m_requestResponseSocket.SendReady += new EventHandler<NetMQSocketEventArgs>(repSocket_SendReady);
            m_requestResponseSocket.ReceiveReady += new EventHandler<NetMQSocketEventArgs>(repSocket_ReceiveReady);
        }

        /// <summary>
        /// Returns a "native" handle
        /// </summary>
        /// <returns></returns>
        public ISocketPollable GetPollableHandle()
        {
            return m_requestResponseSocket;
        }

        /// <summary>
        /// Register command handler
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="onNewCommand"></param>
        public void RegisterCommandHandler(string cmd, CommandHandlerSignature onNewCommand)
        {
            m_commandHandlers[cmd] = onNewCommand;
        }

        /// <summary>
        /// Unregister command handler
        /// </summary>
        /// <param name="cmd"></param>
        public void UnRegisterCommandHandler(string cmd)
        {
            m_commandHandlers.Remove(cmd);
        }

        /// <summary>
        /// Handle command received from client
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void repSocket_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            var more = false;
            var cmd = e.Socket.ReceiveFrameString(out more);
            var payload = "";

            while (more)
            {
                payload += e.Socket.ReceiveFrameString(out more);
            }

            Debug.Assert(m_commandHandlers.ContainsKey(cmd));

            var handler = m_commandHandlers.GetValueOrDefault(cmd);

            // Compile response in order to send it to the client
            CommandAndPayload response = handler?.Invoke(payload);

            m_communicationQueue.Enqueue(response);
        }

        /// <summary>
        /// Ready to send response
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void repSocket_SendReady(object sender, NetMQSocketEventArgs e)
        {
            // Sending back commands to the Client
            while (m_communicationQueue.Count > 0)
            {
                var cmd = m_communicationQueue.Dequeue();
                e.Socket.SendMoreFrame(cmd.Command).SendFrame(cmd.Payload);

                OnCommandSent?.Invoke(cmd);
            }
        }
    }
}
