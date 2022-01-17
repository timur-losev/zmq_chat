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
        public void Run(CancellationToken cancellationToken, System.Action<CommandAndPayload> onResponse, System.Action onConnectionLost);
        public void SendData(common.CommandAndPayload data);
        public void InitiateConnection(
            string inPort, // Port where server is running at
            string connectionRequestPayload, // First data sent to server
            System.Action<CommandAndPayload> onSuccess, // On success callback
            System.Action onConnectionFailed); // On fail callback
        public void CloseConnection();
    }
}
