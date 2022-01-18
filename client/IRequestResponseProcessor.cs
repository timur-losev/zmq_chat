using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using common;
using System.Threading;

namespace client
{
    public interface IRequestResponseProcessor
    {
        /// <summary>
        /// This method is used to asynchronously send commands to the server and receive response.
        /// Typically server must return the same command as it receives to indicate successful operation
        /// TODO: Also we can return some response codes from the server if any needed
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="onResponse"></param>
        /// <param name="onConnectionLost"></param>
        public Task RunAsync(CancellationToken cancellationToken, System.Action<CommandAndPayload> onResponse, System.Action onConnectionLost);
        /// <summary>
        /// Send data to the server
        /// </summary>
        /// <param name="data"></param>
        public void SendData(common.CommandAndPayload data);

        /// <summary>
        /// Initiate a connection between Client and Server
        /// </summary>
        /// <param name="inPort"></param>
        /// <param name="connectionRequestPayload"></param>
        /// <param name="onSuccess"></param>
        /// <param name="onConnectionFailed"></param>
        public void InitiateConnection(
            string inPort, // Port where server is running at
            string connectionRequestPayload, // First data sent to server
            System.Action<CommandAndPayload> onSuccess, // On success callback
            System.Action onConnectionFailed); // On fail callback
        public void CloseConnection();
    }
}
