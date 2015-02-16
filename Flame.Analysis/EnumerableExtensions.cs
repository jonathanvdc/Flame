using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> With<T>(this IEnumerable<T> Enumerable, T Value)
        {
            return Enumerable.Concat(new T[] { Value });
        }
        public static IEnumerable<T> Without<T>(this IEnumerable<T> Enumerable, T Value)
        {
            return Enumerable.Except(new T[] { Value });
        }
    }
}
