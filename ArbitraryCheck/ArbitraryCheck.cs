using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace ArbitraryCheck
{
    static class ArbitraryCheck
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
            var methods = typeof(ArbitraryCheck).Assembly.GetTypes().Where(t => t.IsSealed && !t.IsGenericType && !t.IsNested)
                          .SelectMany(type => type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));
            var extensionMethods = methods.Where(method => method.IsDefined(typeof(ExtensionAttribute), false)
                                                 && method.Name == "Arbitrary");
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
                var arbitraryMethods = genericExtensionMethods.Where(method => method.ReturnType.Name == typeName.Name
                                                                            && method.GetGenericArguments().Count() == typeName.GetGenericArguments().Count());
                // Turn the methods into generic ones.
                arbitraryMethods = arbitraryMethods.Select(method => method.MakeGenericMethod(typeName.GetGenericArguments()));
                return arbitraryMethods;
            }
            else
            {
                var arbitraryMethods = extensionMethods.Where(method =>
                                                              method.GetParameters().Count() == 1 &&                  // Has only one parameter
                                                              method.ReturnType == typeName);                    // Return type matches that of Input
                return arbitraryMethods;
            }
        }

        public static bool HasArbitrary(this Type typeName)
        {
            return GetArbitraryMethods(typeName).Any();
        }

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

        public static void Check<Input>(this Func<Input, bool> func)
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
                    Console.WriteLine("Failed after " + checks + " tests, with input `" + arbitraryValue.ToString() + "'" );
                }
            }
            else
            {
                throw new ArgumentException("The provided type parameter `" + typeof(Input) + "' has no Arbitrary extension method implemented for it.");
            }
        }
    }
}
