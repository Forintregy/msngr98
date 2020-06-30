using System;
using System.Collections.Generic;
using System.Text;

namespace msngrDAL
{
    public static class HelperMethods
    {
        public static void WriteInColor(ConsoleColor color, string message)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}
