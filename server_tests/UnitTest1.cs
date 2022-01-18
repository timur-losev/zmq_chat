using System.Diagnostics;
using System;
using Xunit;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;
using client;
using common;

namespace server_tests
{
    class MockRequestResponse : IRequestResponseProcessor
    {
        public void Run(CancellationToken cancellationToken, System.Action<CommandAndPayload> onResponse, System.Action onConnectionLost)
        {

        }
        public void SendData(common.CommandAndPayload data)
        {

        }
        public void InitiateConnection(
            string inPort, // Port where server is running at
            string connectionRequestPayload, // First data sent to server
            System.Action<CommandAndPayload> onSuccess, // On success callback
            System.Action onConnectionFailed) // On fail callback
        {
            var acceptedClient = new AcceptedClient
            {
                ChatRoomPort = "777",
                ChatHistory = "TEST HISTORY"
            };

            onSuccess(new CommandAndPayload(NetworkCommands.kAcceptedClientCMD, JsonSerializer.Serialize(acceptedClient)));
        }
        public void CloseConnection()
        {

        }
    }

    class MockChatRoom : IBroadcastMessageListener
    {
        public void RunServerListener(CancellationToken cancellationToken, string chatRoomPort, System.Action<CommandAndPayload> onNewMessage)
        {
            Debug.Assert(chatRoomPort == "777");

            var messagePacket = new MessagePacket
            {
                MessageText = "NEW MESSAGE"
            };

            onNewMessage?.Invoke(new CommandAndPayload(NetworkCommands.kNewMessageCMD, JsonSerializer.Serialize(messagePacket)));
        }
        public void CloseConnection()
        {

        }
    }

    public class UnitTest1
    {
        [Fact]
        public void CommunicationMockTest()
        {
            var reqRepMock = new MockRequestResponse();
            var chatRoomMock = new MockChatRoom();

            var client = new ClientController(chatRoomMock, reqRepMock);

            var t = client.Connect("Timur", "0",
                chatHistory =>
                {
                    Debug.Assert(chatHistory == "TEST HISTORY");
                },
                null,
                newChatMessage =>
                {
                    Debug.Assert(newChatMessage == "NEW MESSAGE");
                },
                null
                );

            t.Wait();
        }
    }
}
