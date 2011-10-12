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
using System.Linq;
using System.Reflection;
using System.Text;

namespace DotCheck
{
    internal static class Util
    {
        private static string EnumerableRepr<T>(this IEnumerable<T> list)
        {
            string delim = ", ";
            var sb = new StringBuilder();
            sb.Append("[");
            int length = list.Count();
            int i = 0;
            foreach (T item in list)
            {
                if (item is IEnumerable<T>)
                {
                    sb.Append(((IEnumerable<T>) item).EnumerableRepr());
                }
                else
                {
                    sb.Append(item.ToString());
                }

                if (i != length - 1)
                    sb.Append(delim);
                i++;
            }
            sb.Append("]");
            return sb.ToString();
        }

        internal static string Repr(object arbitraryValue)
        {
            Type type = arbitraryValue.GetType();
            if (type.IsGenericType)
            {
                if (type.GetInterfaces().Any(t => t.Name == "IEnumerable") && type.GetGenericArguments().Count() > 0)
                {
                    Type genericParam = type.GetGenericArguments().First();
                    MethodInfo enumerableRepr = typeof (Util).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                        .Where(method => method.Name == "EnumerableRepr")
                        .First();
                    MethodInfo generic = enumerableRepr.MakeGenericMethod(genericParam);
                    return (string) generic.Invoke(null, new[] {arbitraryValue});
                }
            }
            return arbitraryValue.ToString();
        }
    }
}