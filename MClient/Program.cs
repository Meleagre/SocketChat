using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MSocket;

namespace MClient
{
    class Program
    {
        static void PrintAnswer(object source, ClientEventArgs e)
        {
            Console.WriteLine(e.Message);
        }

        static void Main(string[] args)
        {
            SocketClient client = new SocketClient();
            client.ClientEvent += PrintAnswer;
            client.Start();
            while (true)
            {
                string message = Console.ReadLine();
                if (message.Contains("exit")) break;
                client.SendMessage(message);
            }
            client.Stop();
            Console.Read();
        }
    }
}
