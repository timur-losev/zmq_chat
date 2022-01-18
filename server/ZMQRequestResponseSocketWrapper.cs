﻿using System;
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
    public class ZMQRequestResponseSocketWrapper
    {
        public delegate void OnCommandSentDelegate(CommandAndPayload data);

        private Dictionary<string, CommandHandlerSignature> m_commandHandlers = new Dictionary<string, CommandHandlerSignature>();
        private Queue<CommandAndPayload> m_communicationQueue = new Queue<CommandAndPayload>();
        private OnCommandSentDelegate m_onCommandSentDelegate = null;
        private RequestSocket m_requestResponseSocket = new RequestSocket();

        public ZMQRequestResponseSocketWrapper(OnCommandSentDelegate onCommandSent = null)
        {
            m_onCommandSentDelegate = onCommandSent;
        }

        public void BindToPort(string portString)
        {
            m_requestResponseSocket.Bind(String.Format("tcp://*:{0}", portString));

            m_requestResponseSocket.SendReady += new EventHandler<NetMQSocketEventArgs>(repSocket_SendReady);
            m_requestResponseSocket.ReceiveReady += new EventHandler<NetMQSocketEventArgs>(repSocket_ReceiveReady);
        }

        /// <summary>
        /// Return "native" handle
        /// </summary>
        /// <returns></returns>
        public RequestSocket GetHandle()
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

                m_onCommandSentDelegate?.Invoke(cmd);
            }
        }
    }
}
