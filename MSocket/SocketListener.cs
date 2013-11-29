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
    public delegate void StartedListeningHandler(object source, EventArgs arg);
    public delegate void NewConnectionHandler(object source, NewConnectionEventArgs arg);
    public delegate void MessageReceivedHandler(object source, MessageReceivedEventArgs arg);

    public class SocketListener
    {
        public static SocketListener Instance { get { return _instance; } }
        public IPAddress IPAddress { get { return _ipAddress; } }
        
        public event ServerEventHandler ServerEvent;
        public event StartedListeningHandler StartedListening;
        public event NewConnectionHandler NewConnection;
        public event MessageReceivedHandler MessageRecieved;

        public readonly int Port = 55110;
        public readonly int Backlog = 10;

        private static readonly SocketListener _instance = new SocketListener();
        private ManualResetEvent _allDone = new ManualResetEvent(false);
        private IPAddress _ipAddress;
        private IPEndPoint _localEndPoint;
        private Socket _listener;
        private readonly int _bufferSize = 1024;

        protected SocketListener() { }

        public void StartListening()
        {
            _ipAddress = IPAddress.Any;
            _localEndPoint = new IPEndPoint(_ipAddress, Port);
            _listener = new Socket(AddressFamily.InterNetwork, 
                SocketType.Stream, ProtocolType.Tcp);

            try
            {
                _listener.Bind(_localEndPoint);
                _listener.Listen(Backlog);
                Notify("Waiting for a connection...");
                while (true)
                {
                    // Here program is suspended and waits for an incoming connection.
                    Socket handler = _listener.Accept();
                    var remoteEP = handler.RemoteEndPoint as IPEndPoint;
                    Notify(String.Format("Client {0} is connected to port {1}.", remoteEP.Address, remoteEP.Port));
                    HandleIncomingConnection(handler);
                }
            }
            catch (Exception e)
            {
                Notify(e.ToString());
            }
        }

        public void HandleIncomingConnection(Socket handler)
        {
            Thread thread = new Thread(() =>
            {
                string data = String.Empty;
                Byte[] buffer = new Byte[_bufferSize];
                while (true)
                {
                    int bytesRecieved = handler.Receive(buffer);
                    data += Encoding.ASCII.GetString(buffer, 0, bytesRecieved);
                    if (data.IndexOf("<EOF>") > -1)
                    {
                        Notify(String.Format("Read {0} bytes from socket. \n Data: {1}",
                            data.Length, data));
                        data = String.Empty;
                    }
                    if (data.IndexOf("<CLOSE>") > -1)
                    {
                        handler.Shutdown(SocketShutdown.Both);
                        handler.Close();
                        break;
                    }
                }
            });
            thread.Start();
        }

        private void Notify(string message)
        {
            if (ServerEvent == null) return;
            var arg = new ServerEventArgs();
            arg.Message = message;
            ServerEvent(this, arg);
        }

        private void OnStartedListening()
        {
            if (StartedListening == null) return;
            var arg = new EventArgs();
            StartedListening(this, arg);
        }

        private void OnNewConnection(IPEndPoint localEP, IPEndPoint remoteEP)
        {
            if (NewConnection == null) return;
            var arg = new NewConnectionEventArgs();
            arg.LocalEndPoint = localEP;
            arg.RemoteEndPoint = remoteEP;
            NewConnection(this, arg);
        }

        private void OnMessageReceived(string message, IPEndPoint remoteEP)
        {
            if (MessageRecieved == null) return;
            var arg = new MessageReceivedEventArgs();
            arg.Message = message;
            arg.RemoteEndPoint = remoteEP;
        }
    }
}