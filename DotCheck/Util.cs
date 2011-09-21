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