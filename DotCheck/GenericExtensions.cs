using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DotCheck
{
    class GenericExtensions<T>
    {
        public static Arbitrary<IEnumerable<T>> ArbitraryEnumerable = new Arbitrary<IEnumerable<T>>()
        {
            GenerationSize = 0,
            Generator = (rand) =>
            {
                Type genericType = typeof(T);
                if (genericType.HasGenerators())
                {
                    int listsize = ArbitraryEnumerable.GenerationSize++;
                    List<T> myList = new List<T>();
                    for (int i = 0; i < listsize; i++)
                    {
                        myList.Add(Check.Generate<T>(rand));
                    }
                    return myList;
                }
                return Enumerable.Empty<T>();
            },
            // A list is shrunk to an empty list of lists.
            Shrinker = (list) =>
            {
                return Enumerable.Empty<List<T>>();    
            }
        };
    }
}
