using System.Collections.Generic;
using System.Linq;

namespace wcg.CodeGeneration.Extensions
{
    internal static class EnumerableExtensions
    {
        public static IEnumerable<T> AllButLast<T>(this IEnumerable<T> source)
        {
            return source.Reverse().Skip(1).Reverse();
        }
    }
}
