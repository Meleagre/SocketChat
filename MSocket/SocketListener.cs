using System;
using System.Collections.Generic;
using System.IO;
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
        public readonly int Backlog = 100;

        private static readonly SocketListener _instance = new SocketListener();
        private ManualResetEvent _allDone = new ManualResetEvent(false);
        private IPAddress _ipAddress;
        private IPEndPoint _localEndPoint;
        private Socket _listener;
        private readonly int _bufferSize = 1024;
        private List<string> _messageHistory;

        protected SocketListener() 
        {
            _messageHistory = new List<string>();
        }

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
                HandleIncomingConnections();
                HandleHttpConnection();
            }
            catch (Exception e)
            {
                Notify(e.ToString());
            }
        }

        private void HandleHttpConnection()
        {
            Thread thread = new Thread(() => 
            {
                HttpListener httpListener = new HttpListener();
                httpListener.Prefixes.Add("http://127.0.0.1:55225/");
                httpListener.Start();
                var context = httpListener.GetContext();
                var request = context.Request;
                var response = context.Response;
                string responseString = MessagesAsHtml();
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                output.Close();
                httpListener.Stop();
            });
            thread.Start();
        }

        private void HandleIncomingConnections()
        {
            Thread thread = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        Socket handler = _listener.Accept();
                        var remoteEndPoint = handler.RemoteEndPoint as IPEndPoint;
                        Notify(String.Format("Connection from {0}:{1} accepted.", 
                            remoteEndPoint.Address, remoteEndPoint.Port));
                        HandleClient(handler);
                    }
                    catch (Exception) { }
                }
            });
            thread.Start();
        }

        public void HandleClient(Socket handler)
        {
            Thread thread = new Thread(() =>
            {
                string message = String.Empty;
                Byte[] buffer = new Byte[_bufferSize];
                while (true)
                {
                    int bytesRecieved = handler.Receive(buffer);
                    message += Encoding.UTF8.GetString(buffer, 0, bytesRecieved);
                    if (message.IndexOf(Protocol.EofTag) > -1)
                    {
                        Notify(String.Format("Read {0} bytes from socket. \n Data: {1}",
                            message.Length, message));
                        lock (_messageHistory) _messageHistory.Add(message);
                        message = String.Empty;
                    }
                    if (message.IndexOf(Protocol.CloseTag) > -1)
                    {
                        handler.Shutdown(SocketShutdown.Both);
                        handler.Close();
                        break;
                    }
                }
            });
            thread.Start();
        }

        private string MessagesAsHtml()
        {
            var result = new StringBuilder();
            result.Append("<HTML><BODY>");
            lock (_messageHistory)
            {
                foreach (var msg in _messageHistory)
                {
                    result.Append(msg);
                    result.Append("<br>");
                }
            }
            result.Append("</BODY></HTML>");
            return result.ToString();
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