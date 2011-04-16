/*
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
using System.Reflection;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace DotCheck
{
    static class Check
    {
        private static int NumChecks = 100;

        /// <summary>
        /// Gets the Arbitrary methods defined for typeName. If the type does not implement it, it returns an empty list.
        /// </summary>
        /// <param name="typeName">The type to check.</param>
        /// <returns>The list of Arbitrary methods.</returns>
        public static IEnumerable<MethodInfo> GetArbitraryMethods(this Type typeName)
        {
            // Get the extension methods for the Input type by querying all types and methods
            // in the current assembly.
            // See also: http://stackoverflow.com/questions/299515/c-reflection-to-identify-extension-methods
            var methods = typeof(Check).Assembly.GetTypes().Where(t => t.IsSealed && !t.IsGenericType && !t.IsNested)
                          .SelectMany(type => type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));
            var extensionMethods = from method in methods
                                   where method.IsDefined(typeof(ExtensionAttribute), false)
                                   && method.Name == "Arbitrary"
                                   select method;

            // If typeName is a generic type, check for methods that implement a return type for the generic type signature.
            if (typeName.IsGenericType)
            {
                Type[] typeArguments = typeName.GetGenericArguments();
                // Look for "Arbitrary" methods that have generic type signatures.
                var genericExtensionMethods = extensionMethods.Where(method => method.ReturnType.IsGenericType);
                // Get the generic arguments for typeName and check that they have Arbitrary methods implemented.
                foreach (var genericArgument in typeName.GetGenericArguments())
                {
                    // Return an empty list if the type parameters had no arbitrary things.
                    if (!genericArgument.HasArbitrary())
                        return new List<MethodInfo>();
                }

                // Filter unplausible methods.
                var arbitraryMethods = from method in genericExtensionMethods
                                       // Check if typeName implements any of the interfaces in the method,
                                       // i.e. checks if "List" implements "IEnumerable".
                                       let isAssignable = method.ReturnType.GetInterfaces().Join(typeName.GetInterfaces(),
                                                          t1 => t1.Name, t2 => t2.Name, (t1, t2) => t1 == t2)
                                       where isAssignable.All(foo => foo == true) && method.GetGenericArguments().Count() == typeName.GetGenericArguments().Count()
                                       select method;
                // Turn the methods into generic ones.
                arbitraryMethods = arbitraryMethods.Select(method => method.MakeGenericMethod(typeName.GetGenericArguments()));
                return arbitraryMethods;
            }
            else
            {
                var arbitraryMethods = from method in extensionMethods
                                       where method.GetParameters().Count() == 1
                                       && method.ReturnType == typeName
                                       select method;
                return arbitraryMethods;
            }
        }

        /// <summary>
        /// Returns true if the type has the Arbitrary method implemented.
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public static bool HasArbitrary(this Type typeName)
        {
            return GetArbitraryMethods(typeName).Any();
        }

        /// <summary>
        /// Calls the Arbitrary method for this type.
        /// </summary>
        /// <typeparam name="Input">The type parameter to call Arbitrary for.</typeparam>
        /// <returns>A value returned by Arbitrary.</returns>
        /// <exception cref="ArgumentException"></exception>
        public static Input CallArbitrary<Input>()
        {
            Type inputType = typeof(Input);
            if (HasArbitrary(inputType))
            {
                var arbitraryMethods = GetArbitraryMethods(inputType);
                MethodInfo method = arbitraryMethods.First();
                return (Input)method.Invoke(null, new object[] { null });
            }
            else
            {
                throw new ArgumentException("The supplied type parameter `" + inputType.Name + "' does not have the `Arbitrary' method implemented!");
            }
        }


        /// <summary>
        /// Checks that the invariant func matches for arbitrary inputs of Input.
        /// </summary>
        /// <typeparam name="Input">The type to generate random inputs of.</typeparam>
        /// <param name="func">The function invariant to test.</param>
        public static void Quick<Input>(this Func<Input, bool> func)
        {
            var arbitraryMethods = GetArbitraryMethods(typeof(Input));

            // Check if Arbitrary is implemented.
            if (arbitraryMethods.Any())
            {
                // Pop the first one.
                MethodInfo method = arbitraryMethods.First();
                int checks = 0;
                Input arbitraryValue = default(Input);
                for (; checks < NumChecks; checks++)
                {
                    arbitraryValue = (Input)method.Invoke(null, new object[] { null });
                    // Call the Arbitrary method and cast to Input, tossing the function a discardable null as parameter.
                    // Test the function against the arbitrary value.
                    if (!func(arbitraryValue))
                    {
                        // Check failed!
                        break;
                    }
                }

                if (checks == NumChecks)
                {
                    Console.WriteLine("Passed " + NumChecks + " tests.");
                }

                else
                {
                    Console.WriteLine("Failed after " + checks + " tests, with input `" + arbitraryValue.ToString() + "'");
                }
            }
            else
            {
                throw new ArgumentException("The provided type parameter `" + typeof(Input) + "' has no Arbitrary extension method implemented for it.");
            }
        }
    }
}
