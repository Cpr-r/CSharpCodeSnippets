using System;
using System.Net;
using System.Text;
using System.Net.Sockets;

namespace MyProgram.Networking
{
    // It's typically wise to give this class its own file space.
    internal sealed class StateObject
    {
        internal byte[] Buffer = new byte[UdpSocket.BUFFER_SIZE];
        internal Socket Socket = null;
    }

    internal sealed class UdpSocket
    {  
        /// <summary>
        /// The size of the UDP buffer.
        /// </summary>
        internal const int BUFFER_SIZE = 8 * 1_024;

        /// <summary>
        /// The enum code for disabling ICMP messages on disconnected UDP sockets.
        /// </summary>
        private const int SOCKET_IO_UDP_CONNRESET = -1_744_830_452;

        /// <summary>
        /// The UDP socket, in effect.
        /// </summary>
        private readonly StateObject stateObject = new StateObject();

        /// <summary>
        /// The end point to 'listen' on. That is, this socket can receive messages from all ports.
        /// </summary>
        private EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);

        /// <summary>
        /// For the asynchronous socket receive/send implementation.
        /// </summary>
        private AsyncCallback receiveCallback = null;

        /// <summary>
        /// Initialise and configure the underlying socket. 
        /// </summary>
        internal void BeginTransmission(int port)
        {
            this.stateObject.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            this.stateObject.Socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
            this.stateObject.Socket.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), port));
            // See DisableIcmpMessage.cs
            this.stateObject.Socket.IOControl((IOControlCode)SOCKET_IO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);

            // Begin the actual transmission.
            this.Receive();
        }

        /// <summary>
        /// Sends a message to the destination end point.
        /// </summary>
        internal void Send(byte[] message, EndPoint destination)
        {
            this.stateObject.Socket.BeginSendTo(message, 0, message.Length, SocketFlags.None, destination, (asyncResult) =>
            {
                int bytes = this.stateObject.Socket.EndSendTo(asyncResult);
            },
            this.stateObject);
        }

        /// <summary>
        /// Receives a message from all ports on this local machine.
        /// </summary>
        private void Receive()
        {
            this.stateObject.Socket.BeginReceiveFrom(this.stateObject.Buffer, 0, BUFFER_SIZE, SocketFlags.None, ref this.endPoint, this.receiveCallback = (asyncResult) =>
            {
                var localStateObject = (StateObject)asyncResult.AsyncState;

                int bytes = this.stateObject.Socket.EndReceiveFrom(asyncResult, ref this.endPoint);
                byte[] message = this.stateObject.Buffer;

                // Begin the async receive loop.
                this.stateObject.Socket.BeginReceiveFrom(localStateObject.Buffer, 0, BUFFER_SIZE, SocketFlags.None, ref this.endPoint, this.receiveCallback, localStateObject);
            },
            this.stateObject);
        }
    }
}
