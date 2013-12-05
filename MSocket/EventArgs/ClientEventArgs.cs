using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSocket
{
    public class ClientEventArgs : EventArgs
    {
        public string Message { get; set; }
    }
}
