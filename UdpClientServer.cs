/*
This C# snippet can also be abstracted into two separate classes (for client and for server), as I have done with my current project. 
In saying that, this piece of code will form the backbone of your UDP network.
*/

using System;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace UdpClientServer
{
    public class UdpSocket
    {
        public class StateObject
        {
            public byte[] Buffer = new byte[BUFFER_SIZE];
            public Socket WorkSocket = null;
        }

        /// <summary>
        /// Required members for the udp network capabilities.
        /// </summary>
        private StateObject udpState = new StateObject();
        private EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
        private AsyncCallback receive = null;
       
        /// <summary>
        /// Used to determine the size of the state object's buffer array.
        /// </summary>
        private const int BUFFER_SIZE = 8 * 1024;

        /// <summary>
        /// Initialises the udp server via address and port; the parameters can be changed to EndPoint type if necessary.
        /// </summary>
        public void BeginServer(string address, int port)
        {
            // Instantiate a new socket, set it to the udp protocol, and include hosting information. Socket is instantiated here in   case we want to start/stop the server at run-time.
            this.udpState.WorkSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            this.udpState.WorkSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
            this.udpState.WorkSocket.Bind(new IPEndPoint(IPAddress.Parse(address), port));

            // Finally, begin receiving messages from the client(s).
            this.ReceiveMessage();
        }

        /// <summary>
        /// Initialises the udp client; the parameters can be changed to EndPoint type if necessary.
        /// </summary>
        public void BeginClient(string address, int port)
        {
            this.udpState.WorkSocket.Connect(IPAddress.Parse(address), port);
        }

        /// <summary>
        /// Sends a packet through the udp protocol asynchronously.
        /// </summary>
        public void SendMessage(string text)
        {
            byte[] message = Encoding.ASCII.GetBytes(text);

            this.udpState.WorkSocket.BeginSend(message, 0, message.Length, SocketFlags.None, (asyncResult) =>
            {
                int bytes = this.udpState.WorkSocket.EndSend(asyncResult);
                Console.WriteLine("Sent Message ({0} bytes): {1}", bytes, text);
            }, 
            this.udpState);
        }

        /// <summary>
        /// Receives a packet through the udp protocol asynchronously.
        /// </summary>
        private void ReceiveMessage()
        {
            this.udpState.WorkSocket.BeginReceiveFrom(this.udpState.Buffer, 0, BUFFER_SIZE, SocketFlags.None, ref this.endPoint, this.receive = (asyncResult) =>
            {
                var asyncUdpState = (StateObject)asyncResult.AsyncState;
                int bytes = this.udpState.WorkSocket.EndReceiveFrom(asyncResult, ref this.endPoint);

                Console.WriteLine("[" + DateTime.Now.ToString() + "] Message from {0} ({1} bytes): {2}", this.endPoint.ToString(), bytes, Encoding.ASCII.GetString(asyncUdpState.Buffer, 0, bytes));
                
                this.socket.BeginReceiveFrom(asyncUdpState.Buffer, 0, BUFFER_SIZE, SocketFlags.None, ref this.endPoint, this.receive, asyncUdpState);
            }, 
            this.udpState);
        }
    }
}
