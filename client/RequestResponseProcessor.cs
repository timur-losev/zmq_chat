using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;
using System.Threading;
using System.Text.Json;
using common;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace client
{
    class RequestResponseProcessor
    {
        private RequestSocket m_requestResponseSocket = null;
        private double m_serverCommunicationTimeout = 2;
        private ConcurrentQueue<common.CommandAndPayload> m_communicationQueue = new ConcurrentQueue<common.CommandAndPayload>();

        public RequestResponseProcessor(double serverCommunicationTimeout)
        {
            m_serverCommunicationTimeout = serverCommunicationTimeout;
        }

        public void CloseConnection()
        {
            m_requestResponseSocket?.Dispose();
            m_requestResponseSocket = null;
        }

        private bool TryRequestResponse(CommandAndPayload data, System.Action<CommandAndPayload> onResponse)
        {
            var timeout = TimeSpan.FromSeconds(m_serverCommunicationTimeout);

            // Send command to server with payload
            if (m_requestResponseSocket.TrySendFrame(timeout, data.Command, true))
            {
                if (m_requestResponseSocket.TrySendFrame(timeout, data.Payload, false))
                {
                    var responseCmd = "";
                    var more = false;
                    // Wait for the response.
                    // The response must be the same command as sent
                    if (m_requestResponseSocket.TryReceiveFrameString(timeout, out responseCmd, out more))
                    {
                        Debug.Assert(data.Command == responseCmd);

                        string payload = "";
                        while (more)
                            payload += m_requestResponseSocket.ReceiveFrameString(out more);

                        onResponse?.Invoke(new CommandAndPayload(responseCmd, payload));

                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// This method is used to asynchronously send commands to the server and receive response.
        /// Typically server must return the same command and it receives to indicate a positive response
        /// Also we can return some response codes from the server if any needed
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="onResponse"></param>
        /// <param name="onConnectionLost"></param>
        public void Run(CancellationToken cancellationToken, System.Action<CommandAndPayload> onResponse, System.Action onConnectionLost)
        {
            Task.Run(() =>
            {
                CommandAndPayload data;
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (m_communicationQueue.TryDequeue(out data))
                    {
                        // Try sending command to server
                        bool isConnectionStillAlive = TryRequestResponse(data, onResponse);
                        if (!isConnectionStillAlive)
                        {
                            // If timeout or error, break this thread
                            onConnectionLost?.Invoke();

                            break;
                        }
                    }

                    Thread.Sleep(100);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Send data to the server
        /// </summary>
        /// <param name="data"></param>
        public void SendData(common.CommandAndPayload data)
        {
            m_communicationQueue.Enqueue(data);
        }

        /// <summary>
        /// Initiate a connection between Client and Server
        /// </summary>
        /// <param name="inPort"></param>
        /// <param name="inConnectionRequestPayload"></param>
        /// <param name="onSuccess"></param>
        /// <param name="onConnectionFailed"></param>
        public void InitiateConnection(
            string inPort, // Port where server is running at
            string inConnectionRequestPayload, // First data sent to server
            System.Action<string, string> onSuccess, // On success callback
            System.Action onConnectionFailed) // On fail callback
        {
            Debug.Assert(inPort.Length > 0);

            m_requestResponseSocket = new RequestSocket();

            string reqRepEndPoint = String.Format("tcp://localhost:{0}", inPort);

            // Connect a socket
            m_requestResponseSocket.Connect(reqRepEndPoint);

            // Perform a sanity wait
            Thread.Sleep(500);

            Debug.Assert(m_requestResponseSocket.HasOut);
            Debug.Assert(inConnectionRequestPayload.Length > 0);

            // Instantiate new connection with the server
            // Send HelloKitty command with the appropriate payload
            m_requestResponseSocket.SendMoreFrame(NetworkCommands.kHelloKittyCMD).SendFrame(inConnectionRequestPayload);

            bool more = false;
            var cmd = "";
            // Try a positive response with timeout
            if (m_requestResponseSocket.TryReceiveFrameString(TimeSpan.FromSeconds(m_serverCommunicationTimeout), out cmd, out more) &&
                cmd == NetworkCommands.kAcceptedClientCMD)
            {
                string payload = "";
                while (more)
                    payload += m_requestResponseSocket.ReceiveFrameString(out more);

                var connectionResponse = JsonSerializer.Deserialize<AcceptedClient>(payload);
                // Server returns a messaging port and chat history
                onSuccess?.Invoke(connectionResponse.ChatHistory, connectionResponse.MessagingPort);
            }
            else
            {
                onConnectionFailed?.Invoke();
            }
        }
    }
}
