using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DotCheck
{
    class Arbitrary<T>
    {
        public Func<T> Generator;

        public Func<T> Shrinker;

        public Arbitrary(Func<T> gen, Func<T> shrinker)
        {
            Generator = gen;
            Shrinker = shrinker;
        }

        public Arbitrary()
        {
            // TODO: Complete member initialization
        }
    }
}
