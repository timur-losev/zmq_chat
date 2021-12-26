using System.Diagnostics;
using System;
using Xunit;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace server_tests
{
    public class UnitTest1
    {
        [Fact]
        public void ServerInitialTest()
        {
            bool gotJoined = false;
            bool gotText = false;
            bool gotDisconnectedText = false;
            bool gotDisconnected = false;

            Task.Factory.StartNew(() => {
                common.Common.StartServer("51515", Directory.GetCurrentDirectory(), true);
                Thread.Sleep(500);

                var client = new voicemod_test.Client();
                client.OnNewMessage += new EventHandler<voicemod_test.ClientEventArgs>((object o, voicemod_test.ClientEventArgs args) =>{
                    if (!gotJoined)
                    {
                        gotJoined = args.MessageText.Contains("TEST joined");
                    }

                    if (!gotText)
                    {
                        gotText = args.MessageText.Contains("TEST TEST TEST");
                    }

                    if (!gotDisconnectedText)
                    {
                        gotDisconnectedText = args.MessageText.Contains("SERVER HAS BEEN SHUT DOWN");
                    }
                });

                client.OnServerDisconnected += new EventHandler<voicemod_test.ClientEventArgs>((object o, voicemod_test.ClientEventArgs args) =>
                {
                    gotDisconnected = true;
                });

                client.Connect("TEST", "51515", success =>
                {
                    Debug.Assert(success);
                });

                Thread.Sleep(500);

                client.SendChatMessage("TEST TEST TEST");

                client.SendServerShutdown();
            });

            var totaltime = 0;
            while (!gotDisconnected && totaltime < 10000)
            {
                Thread.Sleep(100);
                totaltime += 100;
            }

            Debug.Assert(gotDisconnected && gotJoined && gotText && gotDisconnectedText);
        }
    }
}
