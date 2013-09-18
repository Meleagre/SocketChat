using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MSocket
{
    public delegate void ServerEventHandler(object source, ServerEventArgs arg);

    public class AsyncSocketListener
    {
        public static AsyncSocketListener Instance { get { return instance; } }
        public IPAddress IPAddress { get { return ipAddress; } }
        public event ServerEventHandler ServerEvent;

        public static readonly int Port = 11000;
        public static readonly int Backlog = 100;
        public static readonly string EofTag = "<EOF>";
        
        private static readonly AsyncSocketListener instance = new AsyncSocketListener();
        private ManualResetEvent allDone = new ManualResetEvent(false);
        private IPAddress ipAddress;

        protected AsyncSocketListener() { }
        
        public void StartListening()
        {
            // Data buffer for incoming data.
            byte[] buffer = new byte[1024];

            //IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            this.ipAddress = IPAddress.Any;
            IPEndPoint localEndPoint = new IPEndPoint(this.ipAddress, Port);

            Socket listener = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(backlog: Backlog);
                Notify("Waiting for a connection...");
                while (true)
                {
                    // Set the event to nonsignaled state.
                    allDone.Reset();

                    //SendMessage("Waiting for a connection...");

                    // Start an asynchrounous socket to listen for connections.
                    listener.BeginAccept(
                        new AsyncCallback(AcceptCallback), listener);

                    // Wait until a connection is made before continuing.
                    allDone.WaitOne();
                }
            }
            catch (Exception e)
            {
                Notify(e.ToString());
            }

            Notify("\nPress ENTER to continue...");
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.
            allDone.Set();

            // Get the socket that handles the client request.
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            // Create the state object.
            StateObject state = new StateObject();
            state.WorkSocket = handler;
            handler.BeginReceive(
                buffer: state.Buffer, 
                offset: 0, 
                size: StateObject.BufferSize, 
                socketFlags: 0,
                callback: new AsyncCallback(ReadCallback), 
                state: state);
        }

        private void ReadCallback(IAsyncResult ar)
        {
            string content = String.Empty;

            // Retrieve the state object and the handler socket
            // from asynchronous state object.
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.WorkSocket;

            // Read data from the client socket.
            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                state.StringBuilder.Append(
                    Encoding.ASCII.GetString(state.Buffer, 0, bytesRead));

                // Check for end-of-file tag. If it is not there, read
                // more data.
                content = state.StringBuilder.ToString();
                if (content.IndexOf(EofTag) > -1)
                {
                    // All the data has been read from the client.
                    // Display it on the console.
                    Notify(String.Format("Read {0} bytes from socket. \n Data: {1}",
                        content.Length, content));

                    // Echo the data back to the client.
                    Send(handler, content);
                }
                else
                {
                    // Not all data received. Get more.
                    handler.BeginReceive(
                        buffer: state.Buffer,
                        offset: 0,
                        size: StateObject.BufferSize,
                        socketFlags: 0,
                        callback: new AsyncCallback(ReadCallback),
                        state: state);
                }
            }
        }

        private void Send(Socket handler, string data)
        {
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.
            handler.BeginSend(buffer: byteData,
                offset: 0,
                size: byteData.Length,
                socketFlags: 0,
                callback: new AsyncCallback(SendCallback),
                state: handler);
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to remote device.
                int bytesSent = handler.EndSend(ar);
                Notify(String.Format("Sent {0} bytes to client.", bytesSent));

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
            catch (Exception e)
            {
                Notify(e.ToString());
            }
        }

        private void Notify(string message)
        {
            ServerEventArgs arg = new ServerEventArgs();
            if (ServerEvent != null)
            {
                arg.Message = message;
                ServerEvent(this, arg);
            }
        }
    }
}
