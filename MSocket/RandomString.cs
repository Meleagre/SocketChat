using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSocket
{
    public static class RandomString
    {
        private static readonly Random _random = new Random();
        private const string _chars = "AaBbCcDdEeFfGgHhIiJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz";

        public static string New(int length)
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
