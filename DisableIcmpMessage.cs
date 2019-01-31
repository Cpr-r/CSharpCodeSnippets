/* 
I am in reference to the error that occurs when the client does not handle disconnect logic properly. That is, the UDP server crashes because the UDP client does not exit gracefully or crashes, thereby forcing a 'hard-disconnect'.  If you suspect that you have this error, a simple trick to reproduce it is to close--all at once--dozens of clients who are sending packets to your server.
Note: this bug will only occur if you are using asynchronous methods for transmitting packets.

The explanation for why this issue occurs is simple: The UDP protocol is also listening to ICMP messages, specifically, a packet that lets the server know that the client could not be reached. However, it lets the server know this information by means of an exception. And if you do not handle that exception, you know what happens to your server...

Instead of worrying about trying to catch that thrown exception, you can simply disable this ICMP message. (Who needs it anyway? After all, UDP is connection-less so we don't really care if the client did not receive our packet.)

The code below shows you how to disable that particular ICMP message, thereby removing the error you are experiencing.
As quoted from a Microsoft document (https://docs.microsoft.com/en-us/windows/desktop/WinSock/winsock-ioctls):

SIO_UDP_CONNRESET (opcode setting: I, T==3)
Windows XP: Controls whether UDP PORT_UNREACHABLE messages are reported. 
Set to TRUE to enable reporting. Set to FALSE to disable reporting. 
*/




// The enum code for 'SIO_UDP_CONNRESET'.
private const int SOCKET_IO_UDP_CONNRESET = -1744830452;

// Your instantiation logic for your UDP client.
private UdpClient udpClient = New UdpClient(endPoint);

// Your initialisation logic for your UDP client.
private void SomeInitialisationFunction()
{
    // This line fixes the error, so make sure to execute this before sending packets through your UDP socket.
    udpClient.Client.IOControl((IOControlCode)SOCKET_IO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);
}
