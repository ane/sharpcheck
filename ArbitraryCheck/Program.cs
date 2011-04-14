using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArbitraryCheck
{
    class ArbitraryTest
    {
        static void Main(string[] args)
        {
            ArbitraryCheck.Check<Int32>(input => Math.Abs(input) >= 0);
            ArbitraryCheck.Check<List<int>>(inp => inp.Take(5).Count() <= 5);
            Console.Read();
        }
    }
}
