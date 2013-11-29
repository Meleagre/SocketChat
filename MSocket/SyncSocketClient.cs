using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MSocket
{
    public delegate void ClientEventHandler(object source, ClientEventArgs arg);

    public class SyncSocketClient : IDisposable
    {
        public static readonly int Port = 55110;
        public static readonly string EofTag = "<EOF>";
        public event ClientEventHandler ClientEvent;
        public ClientStatus Status { get { return status; } }
        public IPAddress IPAddress { get { return IPAddress; } }

        private ClientStatus status;
        private IPAddress ipAddress;
        private Socket sender;

        public SyncSocketClient()
        {
            status = ClientStatus.Disconnected;
        }

        public void Start()
        {
            ipAddress = IPAddress.Parse("127.0.0.1");
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, Port);
            
            sender = new Socket(AddressFamily.InterNetwork, 
                SocketType.Stream, ProtocolType.Tcp);

            try {
                sender.Connect(remoteEP);
                status = ClientStatus.Connected;
                Notify(String.Format("Socket connected to {0}",
                    sender.RemoteEndPoint.ToString()));

            } catch (ArgumentNullException ane) {
                Notify(String.Format("ArgumentNullException: {0}", ane.ToString()));
            } catch (SocketException se) {
                Notify(String.Format("SocketException: {0}", se.ToString()));
            } catch (Exception e) {
                Notify(String.Format("Unexpected exception: {0}", e.ToString()));
            }
        }

        public string SendMessage(string message)
        {
            byte[] buffer = new byte[1024];

            byte[] byteMessage = Encoding.ASCII.GetBytes(message + EofTag);
            int bytesSent = sender.Send(byteMessage);

            //int bytesRec = sender.Receive(buffer);
            return "Sended Successfully.";
            //return Encoding.ASCII.GetString(buffer, 0, bytesRec);
        }

        public void Stop()
        {
            sender.Shutdown(SocketShutdown.Both);
            sender.Close();
        }

        public void StartClient()
        {
            byte[] buffer = new byte[1024];

            // Connect to a remote device.
            try
            {
                ipAddress = IPAddress.Parse("127.0.0.1");
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, Port);

                Socket sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                // Connect the socket to the remote endpoint. Catch any errors.
                try
                {
                    sender.Connect(remoteEP);

                    Notify(String.Format("Socket connected to {0}",
                        sender.RemoteEndPoint.ToString()));

                    // Encode the data into bytes and send through the socket.
                    byte[] message = Encoding.ASCII.GetBytes("This is a test" + EofTag);
                    int bytesSent = sender.Send(message);

                    // Receive the response from the remote device.
                    int bytesRec = sender.Receive(buffer);
                    Notify(String.Format("Echoed test = {0}",
                        Encoding.ASCII.GetString(buffer, 0, bytesRec)));

                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();
                }
                catch (ArgumentNullException ane)
                {
                    Notify(String.Format("ArgumentNullException: {0}", ane.ToString()));
                }
                catch (SocketException se)
                {
                    Notify(String.Format("SocketException: {0}", se.ToString()));
                }
                catch (Exception e)
                {
                    Notify(String.Format("Unexpected exception: {0}", e.ToString()));
                }
            }
            catch (Exception e)
            {
                Notify(String.Format(e.ToString()));
            }
        }

        public void Dispose()
        {
            DisposeManagedResources();
        }

        protected virtual void DisposeManagedResources()
        {
            sender.Dispose();
        }

        private void Notify(string message)
        {
            ClientEventArgs arg = new ClientEventArgs();
            if (ClientEvent != null)
            {
                arg.Message = message;
                ClientEvent(this, arg);
            }
        }
    }
}
