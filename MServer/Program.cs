using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MSocket;

namespace MServer
{
    class Program
    {
        static void Server_Notification(object source, ServerEventArgs e)
        {
            Console.WriteLine(e.Message);
        }

        static void Server_NewConnection(object source, NewConnectionEventArgs e)
        {
            Console.WriteLine("New connection from {0}:{1} to {2}:{3}.",
                e.RemoteEndPoint.Address, e.RemoteEndPoint.Port,
                e.LocalEndPoint.Address, e.LocalEndPoint.Port);
        }

        static void Server_MessageRecieved(object source, MessageReceivedEventArgs e)
        {
            Console.WriteLine(e.Message);
        }

        static void Main(string[] args)
        {
            SocketListener.Instance.ServerEvent += Server_Notification;
            SocketListener.Instance.NewConnection += Server_NewConnection;
            SocketListener.Instance.MessageRecieved += Server_MessageRecieved;
            SocketListener.Instance.StartListening();
        }

    }
}
