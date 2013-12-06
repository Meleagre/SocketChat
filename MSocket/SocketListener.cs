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
    public delegate void ServerEventHandler(object source, ServerEventArgs e);
    public delegate void StartedListeningHandler(object source, EventArgs e);
    public delegate void NewConnectionHandler(object source, NewConnectionEventArgs e);
    public delegate void MessageReceivedHandler(object source, MessageReceivedEventArgs e);

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
        private IPAddress _ipAddress;
        private IPEndPoint _localEndPoint;
        private Socket _listener;
        private List<Socket> _clientHandlers;
        private readonly int _bufferSize = 1024;
        private List<string> _messageHistory;

        protected SocketListener()
        {
            _clientHandlers = new List<Socket>();
            _messageHistory = new List<string>();
            MessageRecieved += SocketListener_MessageRecieved;
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
                BroadcastInfo();
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
                var httpListener = new HttpListener();
                httpListener.Prefixes.Add("http://localhost:9999/");
                httpListener.Start();
                var context = httpListener.GetContext();
                var request = context.Request;
                var response = context.Response;
                string responseString = GetMessagesAsHtml();
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                output.Close();
                httpListener.Stop();
            });
            thread.IsBackground = true;
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
                        handler.ReceiveTimeout = 10;
                        _clientHandlers.Add(handler);
                        var remoteEndPoint = handler.RemoteEndPoint as IPEndPoint;
                        var localEndPoint = handler.LocalEndPoint as IPEndPoint;
                        OnNewConnection(localEndPoint, remoteEndPoint);
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
                var name = RandomString.New(5);
                var message = String.Empty;
                var buffer = new Byte[_bufferSize];
                while (true)
                {
                    try
                    {
                        int bytesReceived;
                        lock (handler) bytesReceived = handler.Receive(buffer);
                        message += Encoding.UTF8.GetString(buffer, 0, bytesReceived);
                        if (message.IndexOf(Protocol.EofTag) > -1)
                        {
                            message = name + " : " + message.Replace(Protocol.EofTag, String.Empty);
                            OnMessageReceived(message, handler.RemoteEndPoint as IPEndPoint);
                            lock (_messageHistory) _messageHistory.Add(message);
                            message = String.Empty;
                        }
                        if (message.IndexOf(Protocol.CloseTag) > -1)
                        {
                            lock (handler)
                            {
                                handler.Shutdown(SocketShutdown.Both);
                                handler.Close();
                            }
                            break;
                        }
                    }
                    catch (SocketException) { }
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }

        public void BroadcastInfo()
        {
            Thread thread = new Thread(() =>
            {
                var socket = new Socket(AddressFamily.InterNetwork,
                    SocketType.Dgram, ProtocolType.Udp);
                socket.EnableBroadcast = true;
                socket.DontFragment = true;
                var localEndPoint = new IPEndPoint(IPAddress.Broadcast, Port);
                socket.Connect(localEndPoint);
                var byteMessage = Encoding.UTF8.GetBytes(Protocol.ImaServerTag);
                while (true)
                {
                    socket.Send(byteMessage);
                    Thread.Sleep(1000);
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }

        private void SocketListener_MessageRecieved(object source, MessageReceivedEventArgs e)
        {
            foreach (var handler in _clientHandlers)
            {
                lock (handler)
                {
                    if (handler.Connected == false) break;
                    var byteMessage = Encoding.UTF8.GetBytes(e.Message + Protocol.EofTag);
                    handler.Send(byteMessage);
                }
            }
        }

        private string GetMessagesAsHtml()
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
            var e = new ServerEventArgs();
            e.Message = message;
            ServerEvent(this, e);
        }

        private void OnStartedListening()
        {
            if (StartedListening == null) return;
            var e = new EventArgs();
            StartedListening(this, e);
        }

        private void OnNewConnection(IPEndPoint localEP, IPEndPoint remoteEP)
        {
            if (NewConnection == null) return;
            var e = new NewConnectionEventArgs();
            e.LocalEndPoint = localEP;
            e.RemoteEndPoint = remoteEP;
            NewConnection(this, e);
        }

        private void OnMessageReceived(string message, IPEndPoint remoteEP)
        {
            if (MessageRecieved == null) return;
            var e = new MessageReceivedEventArgs();
            e.Message = message;
            e.RemoteEndPoint = remoteEP;
            MessageRecieved(this, e);
        }
    }
}