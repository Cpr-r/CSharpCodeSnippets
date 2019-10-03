using System;
using System.Net;
using System.Text;
using System.Net.Sockets;

namespace MyProgram.Networking
{
    public sealed class StateObject
    {
        public byte[] Buffer = new byte[UdpSocket.Buffer_Size];
        public Socket Socket;
    }

    public sealed class UdpSocket
    {  
        /// <summary>
        /// The size of the buffer object (the object where packet data is stored).
        /// </summary>
        public const int Buffer_Size = 8 * 1_024;

        /// <summary>
        /// The enum code for disabling ICMP messages on disconnected UDP sockets.
        /// </summary>
        /// <remarks>See DisableIcmpMessage.cs.</remarks>
        private const int socket_io_udp_conn_reset = -1_744_830_452;

        /// <summary>
        /// The wrapper class for the UDP socket and buffer.
        /// </summary>
        private readonly StateObject stateObject = new StateObject();

        /// <summary>
        /// The end point to 'listen' on; this socket can receive messages from all ports.
        /// </summary>
        private EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);

        /// <summary>
        /// For the asynchronous socket receive/send implementation.
        /// </summary>
        private AsyncCallback receiveCallback;

        /// <summary>
        /// Initialises and configures the state object's socket. 
        /// </summary>
        public void BeginTransmission(int port)
        {
            this.stateObject.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            this.stateObject.Socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
            this.stateObject.Socket.Bind(new IPEndPoint(IPAddress.Parse("localhost"), port));
            this.stateObject.Socket.IOControl((IOControlCode)socket_io_udp_conn_reset, new byte[] { 0, 0, 0, 0 }, null);
            this.Receive();
        }

        /// <summary>
        /// An overload for Send that accepts string (instead of byte[]) and automatically converts the string to bytes.
        /// </summary>
        /// <remarks>Change ASCII string encoding if necessary.</remarks>
        public void Send(string message, EndPoint destination) => this.Send(Encoding.ASCII.GetBytes(message), destination);

        /// <summary>
        /// Sends passed message to passed destination end point.
        /// </summary>
        public void Send(byte[] message, EndPoint destination)
        {
            void SendCallback(IAsyncResult asyncResult)
            {
                int bytes = this.stateObject.Socket.EndSendTo(asyncResult);
            }
            this.stateObject.Socket.BeginSendTo(message, 0, message.Length, SocketFlags.None, destination, SendCallback, this.stateObject);
        }

        /// <summary>
        /// Receives messages from all ports on this local machine.
        /// </summary>
        private void Receive()
        {
            void ReceiveCallback(IAsyncResult asyncResult) 
            {
                var localStateObject = (StateObject)asyncResult.AsyncState;

                int bytes = this.stateObject.Socket.EndReceiveFrom(asyncResult, ref this.endPoint);
                byte[] message = this.stateObject.Buffer; // Remember, the buffer size is 8 * 1_024. Therefore, you must trim the excess bytes when passing it to external state.

                // Begin the async receive loop.
                this.stateObject.Socket.BeginReceiveFrom(localStateObject.Buffer, 0, Buffer_Size, SocketFlags.None, ref this.endPoint, this.receiveCallback, localStateObject);
            }
            this.stateObject.Socket.BeginReceiveFrom(this.stateObject.Buffer, 0, Buffer_Size, SocketFlags.None, ref this.endPoint, this.receiveCallback = ReceiveCallback, this.stateObject);
        }
    }
}
