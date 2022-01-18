using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using common;
using NetMQ;
using NetMQ.Sockets;

namespace server
{
    // Command to CommandAndPayload
    using CommandHandlerSignature = Func<string, CommandAndPayload>;
    public interface ICommandHandler
    {
        System.Action<CommandAndPayload> OnCommandSent { get; set; }

        public ISocketPollable GetPollableHandle();
        public void RegisterCommandHandler(string cmd, CommandHandlerSignature onNewCommand);
        public void UnRegisterCommandHandler(string cmd);
        public void Bind(string endpoint);
    }
}
