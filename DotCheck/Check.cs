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
using System.Text;
using System.Reflection;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace DotCheck
{
    static class Check
    {
        private static int NumChecks = 100;

        private static Random rand = new Random((int)DateTime.Now.Ticks);


        /// <summary>
        /// Gets the Arbitrary methods defined for typeName. If the type does not implement it, it returns an empty list.
        /// </summary>
        /// <param name="typeName">The type to check.</param>
        /// <returns>The list of Arbitrary methods.</returns>
        public static IEnumerable<MethodInfo> GetGenerators(this Type typeName)
        {
            // Get the extension methods for the TInput type by querying all types and methods
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
                // Instantiate a generic type of Arbitrary<typeName>
                Type arbitraryType = typeof(Arbitrary<>).MakeGenericType(typeName);
                // Find all types in the current assembly, and look for fields of the above type
                // (e.g. given int, instances of Arbitrary<int>)
                var types = typeof(Check).Assembly.GetTypes().Where(t => t.IsSealed && !t.IsNested && !t.IsGenericType);
                var arbitraryInstanceFields = types.SelectMany(t => t.GetFields()).Where(fi => fi.FieldType == arbitraryType);
                // Map all generators from the above instances.
                FieldInfo generatorField = typeof(Arbitrary<>).MakeGenericType(typeName).GetField("Generator");
                var arbitraryGenerators = arbitraryInstanceFields.Select(fi => fi.GetValue(null)).Select(obj => generatorField.GetValue(obj));
                // Pop the first generator, there might be multiple.
                // TODO: Which sea^Wmethod should I take?
                var firstGenerator = (MulticastDelegate)arbitraryGenerators.First();
                // Get invocation information from the generator.
                return new[] { firstGenerator.Method };
            }
        }

        /// <summary>
        /// Returns true if the type has the Arbitrary method implemented.
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public static bool HasArbitrary(this Type typeName)
        {
            return GetGenerators(typeName).Any();
        }

        /// <summary>
        /// Calls the Arbitrary method for this type.
        /// </summary>
        /// <typeparam name="TInput">The type parameter to call Arbitrary for.</typeparam>
        /// <returns>A value returned by Arbitrary.</returns>
        /// <exception cref="ArgumentException"></exception>
        public static TInput CallArbitrary<TInput>(Random rand)
        {
            Type inputType = typeof(TInput);
            if (HasArbitrary(inputType))
            {
                var arbitraryMethods = GetGenerators(inputType);
                MethodInfo method = arbitraryMethods.First();
                return (TInput)method.Invoke(null, new object[] { null, rand });
            }
            else
            {
                throw new ArgumentException("The supplied type parameter `" + inputType.Name + "' does not have the `Arbitrary' method implemented!");
            }
        }

        public static void Quick<TInput>(this Func<TInput, bool> func)
        {
            Quick<TInput>(func, false);
        }

        public static void Verbose<TInput>(this Func<TInput, bool> func)
        {
            Quick<TInput>(func, true);
        }

        /// <summary>
        /// Checks that the invariant func matches for arbitrary TInputs of TInput.
        /// </summary>
        /// <typeparam name="TTInput">The type to generate random TInputs of.</typeparam>
        /// <param name="propertyFunc">The function invariant to test.</param>
        public static void Quick<TInput>(this Func<TInput, bool> propertyFunc, bool verbose)
        {
            var arbitraryMethods = GetGenerators(typeof(TInput));

            // Check if Arbitrary is implemented.
            if (arbitraryMethods.Any())
            {
                // Pop the first one.
                MethodInfo method = arbitraryMethods.First();
                int checks = 0;
                TInput arbitraryValue = default(TInput);
                // Random number generator.
                for (; checks < NumChecks; checks++)
                {
                    // Call Generator with rand.
                    arbitraryValue = (TInput)method.Invoke(null, new object[] { rand });
                    // Call the Arbitrary method and cast to TInput, tossing the function a discardable null as parameter.
                    // Test the function against the arbitrary value.
                    if (!Test<TInput>(arbitraryValue, propertyFunc, verbose))
                    {
                        // Check failed, start shrinking.
                        break;
                    }
                }

                // Begin shrinking.

                if (checks == NumChecks)
                {
                    Console.WriteLine("Passed " + NumChecks + " tests.");
                }

                else
                {
                    Console.WriteLine("Failed after " + checks + " tests, with TInput `" + arbitraryValue.ToString() + "'");
                }
            }
            else
            {
                throw new ArgumentException("The provided type parameter `" + typeof(TInput) + "' has no Arbitrary extension method implemented for it.");
            }
        }

        private static bool Test<TInput>(TInput arbitraryValue, Func<TInput, bool> func)
        {
            return Test<TInput>(arbitraryValue, func, false);
        }

        private static bool Test<TInput>(TInput arbitraryValue, Func<TInput, bool> func, bool verbose)
        {
            bool result = func(arbitraryValue);
            if (verbose)
            {
                Console.WriteLine("Testing with input `" + Util.Repr(arbitraryValue) + "': " + ((result) ? "OK" : "FAIL"));
            }
            return result;
        }
    }
}
