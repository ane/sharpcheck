/*
 * DotCheck - A port of QuickCheck to C#
 * 
 * Copyright (c) 2011, Antoine Kalmbach <ane@iki.fi>
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the author nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;

namespace DotCheck
{
    static class CommonExtensions
    {
        private static int arraySize;
        private static int stringSize;

        public static Arbitrary<int> ArbitraryInt = new Arbitrary<int>
                                                        {
                                                            // Returns a random number.
                                                            Generator =
                                                                (rand) => { return rand.Next(Int32.MinValue, Int32.MaxValue); },
                                                            Shrinker = ShrinkInteger
                                                        };

        public static IEnumerable<int> ShrinkInteger(int startValue)
        {
            int shrunk = startValue/2;
            while (Math.Abs(shrunk) >= 1)
            {
                if (shrunk < 0)
                    yield return -shrunk;
                else yield return shrunk;
                shrunk /= 2;
            }
        }

        //public static Int32 Arbitrary(this Int32 discard, Random rand)
        //{
        //    return rand.Next(Int32.MinValue, Int32.MaxValue);
        //}

        //public static char Arbitrary(this char discard, Random rand)
        //{
        //    return (char) rand.Next(49, 255);
        //}

        //public static bool Arbitrary(this bool discard, Random rand)
        //{
        //    return rand.Next(0, 2) == 1 ? true : false; 
        //}

        //public static string Arbitrary(this string discard, Random rand)
        //{
        //    StringBuilder sb = new StringBuilder();
        //    int strLength = stringSize++;

        //    for (int i = 0; i < strLength; i++)
        //    {
        //        sb.Append('a'.Arbitrary(rand));
        //    }

        //    return sb.ToString();
        //}

        //public static IEnumerable<T> Arbitrary<T>(this IEnumerable<T> discard, Random rand)
        //{
        //    Type genericType = typeof(T);
        //    if (genericType.HasArbitrary())
        //    {
        //        int listSize = arraySize++;
        //        List<T> myList = new List<T>(listSize);

        //        for (int i = 0; i < listSize; i++)
        //        {
        //            myList.Add(Check.CallArbitrary<T>(rand));
        //        }

        //        return myList;
        //    }
        //    else
        //    {
        //        throw new InvalidOperationException("WTF!");
        //    }
        //}
    }
}