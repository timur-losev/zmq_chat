using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using common;
using System.Threading;

namespace client
{
    public interface IBroadcastMessageListener
    {
        void RunServerListener(CancellationToken cancellationToken, string chatRoomPort, System.Action<CommandAndPayload> onNewMessage);
        void CloseConnection();
    }
}
