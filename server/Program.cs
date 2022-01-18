using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server
{
    class Program
    {
        static void Main(string[] args)
        {
            ServerController server = new ServerController();
            server.Run(args, new impl.ZMQRequestResponseCommandHandler(), new impl.ZMQChatRoom());
        }
    }
}
