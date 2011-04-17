using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DotCheck
{
    class Arbitrary<T>
    {
        public Func<Random, T> Generator;

        public Func<T, IEnumerable<T>> Shrinker;

        public Arbitrary(Func<Random, T> gen, Func<T, IEnumerable<T>> shrinker)
        {
            Generator = gen;
            Shrinker = shrinker;
        }

        public Arbitrary()
        {
            // Default initializations
            Generator = null;
            Shrinker = null;
        }
    }
}
