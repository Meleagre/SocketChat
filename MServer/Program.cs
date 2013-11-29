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
        static void MessageHandler(object source, ServerEventArgs arg)
        {
            Console.WriteLine(arg.Message);
        }

        static void Main(string[] args)
        {
            SocketListener.Instance.ServerEvent += MessageHandler;
            SocketListener.Instance.StartListening();
        }
    }
}
