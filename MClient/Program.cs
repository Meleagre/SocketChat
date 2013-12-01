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
        static void PrintAnswer(object source, ClientEventArgs arg)
        {
            Console.WriteLine("Server reports: " + arg.Message);
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
                string answer = client.SendMessage(message);
                Console.WriteLine(answer);
            }
            client.Stop();
            Console.Read();
        }
    }
}
