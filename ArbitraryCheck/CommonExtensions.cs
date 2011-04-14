using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArbitraryCheck
{
    static class CommonExtensions
    {
        public static Int32 Arbitrary(this Int32 something)
        {
            Random random = new Random();
            return random.Next(Int32.MinValue, Int32.MaxValue);
        }

        public static List<T> Arbitrary<T>(this List<T> something)
        {
            Random random = new Random();
            Type genericType = typeof(T);
            if (ArbitraryCheck.HasArbitrary(genericType))
            {
                int listSize = random.Next(50);
                List<T> myList = new List<T>(listSize);

                for (int i = 0; i < listSize; i++)
                {
                    myList.Add(ArbitraryCheck.CallArbitrary<T>());
                }

                return myList;
            }
            else
            {
                throw new InvalidOperationException("WTF!");
            }
        }
    }
}
