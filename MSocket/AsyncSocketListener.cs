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
    public class AsyncSocketListener
    {
        public static ManualResetEvent allDone = new ManualResetEvent(false);
        public static readonly int Port = 11000;

        public AsyncSocketListener() { }

        public static void StartListening()
        {
            // Data buffer for incoming data.
            byte[] buffer = new byte[1024];

            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            //IPAddress ipAddress2 = ipHostInfo.AddressList.First(addr => addr.AddressFamily == AddressFamily.l);
            IPAddress ipAddress = IPAddress.Parse("192.168.1.2");
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, Port);

            Socket listener = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(backlog:100);


                while (true)
                {
                    // Set the event to nonsignaled state.
                    allDone.Reset();

                    // Start an asynchrounous socket to listen for connections.
                    Console.WriteLine("Waiting for a connection...");
                    listener.BeginAccept(
                        new AsyncCallback(AcceptCallback), listener);

                    // Wait until a connection is made before continuing.
                    allDone.WaitOne();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();
        }

        private static void AcceptCallback(IAsyncResult ar)
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

        private static void ReadCallback(IAsyncResult ar)
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
                if (content.IndexOf("<EOF>") > -1)
                {
                    // All the data has been read from the client.
                    // Display it on the console.
                    Console.WriteLine("Read {0} bytes from socket. \n Data: {1}",
                        content.Length, content);
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

        private static void Send(Socket handler, string data)
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

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to remote device.
                int bytesSent = handler.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to client.", bytesSent);

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
