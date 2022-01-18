using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;
using common;

namespace server
{
    public interface IChatRoom
    {
        System.Action<CommandAndPayload> OnMessageSent { get; set; }

        public void SendAll(CommandAndPayload data);
        public void Bind(string endpoint);
        public string GetEndpoint();
        public ISocketPollable GetPollableHandle();
    }
}
