using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MSocket
{
    public class MessageReceivedEventArgs
    {
        public string Message { get; set; }
        public IPEndPoint RemoteEndPoint { get; set; }
    }
}
