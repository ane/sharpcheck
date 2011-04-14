﻿/*
 * ArbitraryCheck - A port of QuickCheck to C#
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

        public static char Arbitrary(this char something)
        {
            Random rand = new Random();
            return (char) rand.Next(Char.MinValue, Char.MaxValue);
        }

        public static string Arbitrary(this string something)
        {
            StringBuilder sb = new StringBuilder();
            Random rand = new Random();
            int strLength = rand.Next(0, 255);

            for (int i = 0; i < strLength; i++)
            {
                sb.Append(ArbitraryCheck.CallArbitrary<char>());
            }

            return sb.ToString();
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
