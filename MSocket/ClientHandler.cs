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
    public class ClientHandler
    {
        public readonly Thread HandlingThread;
        public readonly Socket Socket;
        public readonly string Name;

        private static readonly Random _random = new Random();
        private const string _chars = "AaBbCcDdEeFfGgHhIiJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz";
        private const int _nameLength = 5;

        public ClientHandler(Thread thread, Socket socket, string name)
        {
            HandlingThread = thread;
            Socket = socket;
            Name = name;
        }

        public ClientHandler(Thread thread, Socket socket) 
            : this(thread, socket, GenerateRandomString(_nameLength))
        {
        }

        public IPEndPoint GetRemoteEndPoint()
        {
            return Socket.RemoteEndPoint as IPEndPoint;
        }

        private static string GenerateRandomString(int length)
        {
            char[] buffer = new char[length];
            for (int i = 0; i < length; i++)
            {
                buffer[i] = _chars[_random.Next(_chars.Length)];
            }
            return new string(buffer);
        }
    }
}
