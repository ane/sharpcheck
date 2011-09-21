using System;
using System.Collections.Generic;

namespace DotCheck
{
    internal sealed class Arbitrary<T>
    {
        public int GenerationSize;

        public Func<Random, T> Generator;

        public Func<T, IEnumerable<T>> Shrinker;

        public Arbitrary(Func<Random, T> gen, Func<T, IEnumerable<T>> shrinker)
        {
            Generator = gen;
            Shrinker = shrinker;
        }

        public Arbitrary(Func<Random, T> gen)
        {
            Generator = gen;
            Shrinker = null;
        }

        public Arbitrary()
        {
            // Default initializations
            Generator = null;
            Shrinker = null;
        }
    }
}