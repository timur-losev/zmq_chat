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
        public void BindToPort(string portString);
        public string GetEndpoint();
        public ISocketPollable GetPollableHandle();
    }
}
