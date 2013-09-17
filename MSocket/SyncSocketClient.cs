using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MSocket
{
    public class SyncSocketClient
    {
        private static readonly int port = 11000;

        public static void StartClient()
        {
            byte[] buffer = new byte[1024];

            // Connect to a remote device.
            try
            {
                IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);

                Socket sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // Connect the socket to the remote endpoint. Catch any errors.
                try
                {
                    sender.Connect(remoteEP);

                    Console.WriteLine("Socket connected to {0}",
                        sender.RemoteEndPoint.ToString());

                    // Encode the data into bytes and send through the socket.
                    byte[] message = Encoding.ASCII.GetBytes("This is a test<EOF>");
                    int bytesSent = sender.Send(message);

                    // Receive the response from the remote device.
                    int bytesRec = sender.Receive(buffer);
                    Console.WriteLine("Echoed test = {0}",
                        Encoding.ASCII.GetString(buffer, 0, bytesRec));

                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();
                }
                catch (ArgumentNullException ane)
                {
                    Console.WriteLine("ArgumentNullException: {0}", ane.ToString());
                }
                catch (SocketException se)
                {
                    Console.WriteLine("SocketException: {0}", se.ToString());
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception: {0}", e.ToString());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}
