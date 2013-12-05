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
    public delegate void ClientEventHandler(object source, ClientEventArgs arg);

    public class SocketClient
    {
        public static readonly int Port = 55110;
        public event ClientEventHandler ClientEvent;
        public IPAddress IPAddress { get { return IPAddress; } }

        private IPAddress _ipAddress;
        private Socket _sender;
        private Thread _backgroundListener;
        private int _bufferSize = 1024;

        public SocketClient()
        {
        }

        public void FindServer()
        {
            var broadcast = IPAddress.Broadcast;
            var socket = new Socket(AddressFamily.InterNetwork, 
                SocketType.Dgram, ProtocolType.Udp);
            var localEndPoint = new IPEndPoint(broadcast, Port);
            socket.Connect(localEndPoint);
            var byteMessage = Encoding.UTF8.GetBytes(Protocol.WhoIsServerTag);
            while (true)
            {
            }
        }

        public void Start()
        {
            //FindServer();

            _ipAddress = IPAddress.Parse("127.0.0.1");
            var remoteEP = new IPEndPoint(_ipAddress, Port);
            _sender = new Socket(AddressFamily.InterNetwork, 
                SocketType.Stream, ProtocolType.Tcp);
            _sender.ReceiveTimeout = 10;
            try 
            {
                _sender.Connect(remoteEP);
                Notify(String.Format("Socket connected to {0}",
                    _sender.RemoteEndPoint.ToString()));

                _backgroundListener = new Thread(ListenForNewMessages);
                _backgroundListener.Start();

            } catch (ArgumentNullException ane) {
                Notify(String.Format("ArgumentNullException: {0}", ane.ToString()));
            } catch (SocketException se) {
                Notify(String.Format("SocketException: {0}", se.ToString()));
            } catch (Exception e) {
                Notify(String.Format("Unexpected exception: {0}", e.ToString()));
            }
        }

        public void SendMessage(string message)
        {
            byte[] byteMessage = Encoding.UTF8.GetBytes(message + Protocol.EofTag);
            lock(_sender) _sender.Send(byteMessage);
        }

        public void Stop()
        {
            _sender.Shutdown(SocketShutdown.Both);
            _sender.Close();
        }

        private void ListenForNewMessages()
        {
            var message = string.Empty;
            var buffer = new Byte[_bufferSize];
            Thread.Sleep(10);
            while (true)
            {
                try
                {
                    int bytesReceived;
                    lock (_sender) bytesReceived = _sender.Receive(buffer);
                    message += Encoding.UTF8.GetString(buffer, 0, bytesReceived);
                    if (message.IndexOf(Protocol.EofTag) > -1)
                    {
                        Notify(message);
                        message = string.Empty;
                    }
                }
                catch (SocketException) { }
            }
        }

        private void Notify(string message)
        {
            ClientEventArgs arg = new ClientEventArgs();
            if (ClientEvent != null)
            {
                arg.Message = message.Replace(Protocol.EofTag, String.Empty);
                ClientEvent(this, arg);
            }
        }
    }
}
