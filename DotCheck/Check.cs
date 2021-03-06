﻿/*
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

namespace DotCheck
{
    static class Check
    {
        public static int Tests = 100;

        private static readonly Random rand = new Random((int) DateTime.Now.Ticks);

        /// <summary>
        /// Gets the Generator methods defined for typeName. If the type does not implement it, it returns an empty list.
        /// </summary>
        /// <param name="typeName">The type to check.</param>
        /// <returns>The list of Arbitrary methods.</returns>
        public static IEnumerable<MethodInfo> GetGenerators(this Type typeName)
        {
            // If typeName is a generic type, check for methods that implement a return type for the generic type signature.
            if (typeName.IsGenericType)
            {
                return ExtractGenericGenerator(typeName, "Generator");
            }
            else
            {
                return ExtractGenerator(typeName, "Generator");
            }
        }

        private static IEnumerable<MethodInfo> ExtractGenericGenerator(Type typeName, string delegateName)
        {
            Type[] typeArguments = typeName.GetGenericArguments();
            foreach (Type typeArgument in typeArguments)
            {
                if (!typeArgument.HasGenerators())
                    throw new ArgumentException("The generic parameter type `" + typeArgument.Name + "' for `" +
                                                typeName.Name + "' has no generators defined!");
            }
            // First case: typeName does implement IEnumerable.
            Type enumerableType = typeof (IEnumerable<>).MakeGenericType(typeArguments);

            if (enumerableType.IsAssignableFrom(typeName) && typeArguments.Count() == 1)
            {
                // Initialize GenericExtesnions with the same type arguments.
                Type arbitraryEnumerable = typeof (GenericExtensions<>).MakeGenericType(typeArguments);
                object arbitrary = arbitraryEnumerable.GetField("ArbitraryEnumerable").GetValue(null);
                FieldInfo generatorField =
                    typeof (Arbitrary<>).MakeGenericType(typeof (IEnumerable<>).MakeGenericType(typeArguments)).GetField
                        (delegateName);
                var generator = (MulticastDelegate) generatorField.GetValue(arbitrary);

                return new[] {generator.Method};
            }

            return Enumerable.Empty<MethodInfo>();
        }

        private static IEnumerable<MethodInfo> ExtractGenerator(this Type typeName, string methodName)
        {
            IEnumerable<object> generators = ExtractDelegate(typeName, methodName);
            // Pop the first generator, there might be multiple.
            // TODO: Which sea^Wmethod should I take?
            MulticastDelegate firstDelegate = null;
            var errors = new List<string>();
            try
            {
                if (!generators.Any())
                    throw new NullReferenceException();
                firstDelegate = (MulticastDelegate) generators.First();

                // Do some sanity checking.
                if (firstDelegate != null)
                {
                    Type returnType = firstDelegate.Method.ReturnType;
                    if (returnType != typeName)
                        errors.Add("The definition of Arbitrary<" + typeName.Name + ">." + methodName +
                                   " has an invalid return type: expected " + typeName.Name + ", received `" +
                                   returnType.Name + "'");
                    if (firstDelegate.Method.GetParameters().Count() != 1)
                        errors.Add("The definition of Arbitrary<" + typeName.Name + ">." + methodName +
                                   " has too many arguments: " + firstDelegate.Method.GetParameters().Count() +
                                   " , limit is 1. ");
                }
                // Any errors?
                if (errors.Count() > 0)
                    throw new ArgumentException(errors.Aggregate((s, o) => { return s + o; }));

                // Get invocation information from the generator.
                return new[] {firstDelegate.Method};
            }
            catch (NullReferenceException nfe)
            {
                errors.Insert(0,
                              "Type `" + typeName.Name +
                              "' does not have an instance of Arbitrary<T> defined or it is null.");
                string exc = errors.Aggregate((str, orig) => { return str + orig; });
                // Check that the generator is not a null reference.
                throw new ArgumentException(exc, nfe);
            }
        }

        private static IEnumerable<object> ExtractDelegate(Type typeName, string methodName)
        {
            // Instantiate a generic type of Arbitrary<typeName>
            Type arbitraryType = typeof (Arbitrary<>).MakeGenericType(typeName);
            // Find all types in the current assembly, and look for fields of the above type
            // (e.g. given int, instances of Arbitrary<int>)
            IEnumerable<Type> types = typeof (Check).Assembly.GetTypes().Where(t => t.IsSealed && !t.IsNested);
            IEnumerable<FieldInfo> arbitraryInstanceFields =
                types.SelectMany(t => t.GetFields()).Where(fi => fi.FieldType == arbitraryType);
            // Map all generators from the above instances.
            FieldInfo generatorField = typeof (Arbitrary<>).MakeGenericType(typeName).GetField(methodName);
            IEnumerable<object> arbitraryMethods =
                arbitraryInstanceFields.Select(fi => fi.GetValue(null)).Select(obj => generatorField.GetValue(obj));
            return arbitraryMethods;
        }

        public static IEnumerable<MethodInfo> GetShrinker(this Type typeName, string shrinkerMethodName)
        {
            if (typeName.IsGenericType)
            {
                Type[] typeArguments = typeName.GetGenericArguments();
                foreach (Type typeArgument in typeArguments)
                {
                    if (!typeArgument.HasShrinkers())
                        throw new ArgumentException("The generic parameter type `" + typeArgument.Name + "' for `" +
                                                    typeName.Name + "' has no shrinkers defined.");
                }
                // First case: typeName does implement IEnumerable.
                Type enumerableType = typeof (IEnumerable<>).MakeGenericType(typeArguments);
                if (enumerableType.IsAssignableFrom(typeName) && typeArguments.Count() == 1)
                {
                    // Initialize GenericExtesnions with the same type arguments.
                    Type arbitraryEnumerable = typeof (GenericExtensions<>).MakeGenericType(typeArguments);
                    object arbitrary = arbitraryEnumerable.GetField("ArbitraryEnumerable").GetValue(null);
                    FieldInfo generatorField =
                        typeof (Arbitrary<>).MakeGenericType(typeof (IEnumerable<>).MakeGenericType(typeArguments)).
                            GetField(shrinkerMethodName);
                    var generator = (MulticastDelegate) generatorField.GetValue(arbitrary);

                    return new[] {generator.Method};
                }

                return Enumerable.Empty<MethodInfo>();
            }
            else
            {
                IEnumerable<object> shrinkers = ExtractDelegate(typeName, shrinkerMethodName);
                MulticastDelegate topShrinker = null;
                var errors = new List<string>();
                try
                {
                    if (!shrinkers.Any())
                        throw new NullReferenceException();
                    topShrinker = (MulticastDelegate) shrinkers.First();

                    // Do some sanity checking.
                    if (topShrinker != null)
                    {
                        Type returnType = topShrinker.Method.ReturnType;
                        Type shrinkerType = typeof (IEnumerable<>).MakeGenericType(typeName);
                        if (returnType != shrinkerType)
                            errors.Add("The definition of Arbitrary<" + typeName.Name + ">." + shrinkerMethodName +
                                       " has an invalid return type: expected " + shrinkerType.Name + ", received `" +
                                       returnType.Name + "'");
                        if (topShrinker.Method.GetParameters().Count() != 1)
                            errors.Add("The definition of Arbitrary<" + typeName.Name + ">." + shrinkerMethodName +
                                       " has too many arguments: " + topShrinker.Method.GetParameters().Count() +
                                       " , limit is 1. ");
                    }
                    // Any errors?
                    if (errors.Count() > 0)
                        throw new ArgumentException(errors.Aggregate((s, o) => { return s + o; }));

                    // Get invocation information from the generator.
                    return new[] {topShrinker.Method};
                }
                catch (NullReferenceException nfe)
                {
                    errors.Insert(0,
                                  "Type `" + typeName.Name +
                                  "' does not have an instance of Arbitrary<T> defined or it is null.");
                    string exc = errors.Aggregate((str, orig) => { return str + orig; });
                    // Check that the generator is not a null reference.
                    throw new ArgumentException(exc, nfe);
                }
            }
        }

        /// <summary>
        /// Returns true if the type has a Generator implemented somewhere in the current assembly.
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public static bool HasGenerators(this Type typeName)
        {
            return GetGenerators(typeName).Count() > 0;
        }

        /// <summary>
        /// Returns true if the type has the Arbitrary method implemented.
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public static bool HasShrinkers(this Type typeName)
        {
            return GetShrinkers(typeName).Count() > 0;
        }

        private static IEnumerable<MethodInfo> GetShrinkers(Type typeName)
        {
            return GetShrinker(typeName, "Shrinker");
        }

        /// <summary>
        /// Calls the Arbitrary method for this type. Useful if you want to generate a single random value of a type.
        /// </summary>
        /// <typeparam name="TInput">The type parameter to call Arbitrary for.</typeparam>
        /// <returns>A value returned by Arbitrary.</returns>
        /// <exception cref="ArgumentException"></exception>
        public static TInput Generate<TInput>(Random rand)
        {
            Type inputType = typeof (TInput);
            if (HasGenerators(inputType))
            {
                IEnumerable<MethodInfo> arbitraryMethods = GetGenerators(inputType);
                MethodInfo method = arbitraryMethods.First();
                return (TInput) method.Invoke(null, new object[] {rand});
            }
            else
            {
                throw new ArgumentException("The supplied type parameter `" + inputType.Name +
                                            "' does not have the `Arbitrary' method implemented!");
            }
        }

        public static void Quick<TInput>(this Func<TInput, bool> func)
        {
            Quick(func, null, false);
        }

        public static void Quick<TInput>(this Func<TInput, bool> propertyFunc, Arbitrary<TInput> arbitrary)
        {
            Quick(propertyFunc, arbitrary, false);
        }

        public static void Verbose<TInput>(this Func<TInput, bool> func)
        {
            Quick(func, null, true);
        }

        public static void Verbose<TInput>(this Func<TInput, bool> func, Arbitrary<TInput> arbitrary)
        {
            Quick(func, arbitrary, true);
        }

        /// <summary>
        /// Checks whether a given function invariant returns true for arbitrary inputs of a given type, using a supplied arbitrary generator and shrinker to generate said arbitrary inputs.
        /// </summary>
        /// <typeparam name="TInput">The type to supply arbitrary values of.</typeparam>
        /// <param name="propertyFunc">The invariant function.</param>
        /// <param name="arbitrary">The arbitrary supplier of inputs of type TInput.</param>
        /// <param name="verbose">Whether to show test input or not.</param>
        private static void Quick<TInput>(this Func<TInput, bool> propertyFunc, Arbitrary<TInput> arbitrary,
                                          bool verbose)
        {
            IEnumerable<MethodInfo> generatorMethods = null;
            IEnumerable<MethodInfo> shrinkerMethods = null;
            if (arbitrary != null)
            {
                generatorMethods = new[] {arbitrary.Generator.Method};
                if (arbitrary.Shrinker != null)
                    shrinkerMethods = new[] {arbitrary.Shrinker.Method};
                else
                    shrinkerMethods = GetShrinkers(typeof (TInput));
            }
            else
            {
                generatorMethods = GetGenerators(typeof (TInput));
                shrinkerMethods = GetShrinkers(typeof (TInput));
            }

            // If there's no shrinkers, don't shrink!
            bool shrink = shrinkerMethods.Count() > 0;

            // Check if Arbitrary is implemented.
            if (generatorMethods.Any())
            {
                // Pop the first one.
                MethodInfo method = generatorMethods.First();
                int checks = 0;
                TInput arbitraryValue = default(TInput);
                // Test the inputs.
                for (; checks < Tests; checks++)
                {
                    // Call Generator with rand.
                    int foo = method.GetParameters().Count();

                    if (!method.IsStatic && arbitrary != null)
                    {
                        arbitraryValue = arbitrary.Generator.Invoke(rand);
                    }
                    else
                    {  
                        arbitraryValue = (TInput) method.Invoke(null, new object[] {rand});
                    }
                    // Call the Arbitrary method and cast to TInput, tossing the function a discardable null as parameter.
                    // Test the function against the arbitrary value.
                    if (!Test(arbitraryValue, propertyFunc, verbose))
                    {
                        // Check failed, start shrinking.
                        break;
                    }
                }

                if (checks == Tests)
                {
                    Console.WriteLine("OK, passed " + Tests + " tests.");
                }

                else
                {
                    // Try shrinking.
                    int numShrinks = 0;
                    TInput lastShrunk = arbitraryValue;
                    if (shrink)
                    {
                        MethodInfo shrinker = shrinkerMethods.First();
                        // Shrink the elements starting from the failed value.
                        var shrinks = (IEnumerable<TInput>) shrinker.Invoke(null, new object[] {arbitraryValue});
                        // Loop while the shrinking still holds.
                        IEnumerator<TInput> shrinkIter = shrinks.GetEnumerator();
                        if (verbose)
                            Console.WriteLine("Starting shrinking from value " + Util.Repr(arbitraryValue));
                        foreach (TInput shrunk in shrinks)
                        {
                            // Stop if the counterexample becomes true, i.e. we've shrunk far enough.
                            if (Test(shrunk, propertyFunc, verbose))
                                break;
                            lastShrunk = shrunk;
                            numShrinks++;
                        }
                        if (verbose)
                            Console.WriteLine("Stopped shrinking after " + numShrinks + " shrinks.");
                    }
                    Console.Write("FAILED, falsifiable after " + checks + " tests, with input ");
                    if (numShrinks > 0)
                        Console.Write("(" + numShrinks + " shrinks)");
                    Console.WriteLine(": `" + Util.Repr(lastShrunk) + "'");
                }
            }
            else
            {
                throw new ArgumentException("The provided type parameter `" + typeof (TInput) +
                                            "' has no Arbitrary extension method implemented for it.");
            }
        }

        private static bool Test<TInput>(TInput arbitraryValue, Func<TInput, bool> func)
        {
            return Test(arbitraryValue, func, false);
        }

        private static bool Test<TInput>(TInput arbitraryValue, Func<TInput, bool> func, bool verbose)
        {
            bool result = func(arbitraryValue);
            if (verbose)
            {
                Console.WriteLine("Testing with input `" + Util.Repr(arbitraryValue) + "': " +
                                  ((result) ? "OK" : "FAIL"));
            }
            return result;
        }

        #region Type-based default template aliases

        public static void Quick(this Func<int, bool> func)
        {
            Quick(func, CommonExtensions.ArbitraryInt, false);
        }

        public static void Quick(this Func<int, bool> func, bool verbose)
        {
            Quick(func, CommonExtensions.ArbitraryInt, verbose);
        }

        #endregion
    }
}