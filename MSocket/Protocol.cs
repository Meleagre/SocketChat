using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSocket
{
    internal static class Protocol
    {
        public const string CloseTag = "<CLOSE>";
        public const string EofTag = "<EOF>";
        public const string ImaServerTag = "<IMASERVER>";
        public const string WhoIsServerTag = "<WHOISSERVER?>";
    }
}
