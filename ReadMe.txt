
Brief explanation of the architecture.

The communication between Client and Server is described in the following way: 
Each time when the Client wants to opt in to the Chat Server, it sends "HelloKitty" command to the preliminary opened RequestSocket (ReqRep pipe). 
After that server must response with the chat history and a port number of the Chat Message Pipe implemented via PUB/SUB, one-to-many.
Client connects to the pipe, subscribes to all messages and waits for messages in a background thread
Once the Client sends a new chat message to the server, it uses the ReqRep pipe and "SendMessage" command. 
Server receives that command, appends chat history and redirects new chat message to the Chat Message Pipe.
Each SUB client (with the sender) receves new chat message.
Client uses Lazy Pirate strategy to perform client side reliablity. So that when the Server crashes, Client will safelly exit on the next communication attempt.

Client and Server are single-class projects and only represent the simplest asyncronious state machine


