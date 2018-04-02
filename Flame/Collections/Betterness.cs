using System;
using System.Collections.Generic;
using System.Linq;

namespace Flame.Collections
{
    /// <summary>
    /// Specifies if one of two items is better than the other.
    /// </summary>
    public enum Betterness
    {
        /// <summary>
        /// Neither item is better than the other.
        /// </summary>
        Neither,

        /// <summary>
        /// Both items are equal.
        /// </summary>
        Equal,

        /// <summary>
        /// The first item is better than the second.
        /// </summary>
        First,

        /// <summary>
        /// The second item is better than the second.
        /// </summary>
        Second
    }

    /// <summary>
    /// Extension methods related to the BetterResult enum.
    /// </summary>
    public static class BetternessExtensions
    {
        /// <summary>
        /// "Flips" the given betterness: if the first
        /// element was better, then this method returns
        /// a betterness for the second element, and
        /// vice-versa.
        /// </summary>
        /// <param name="Value">The beterness to flip.</param>
        public static Betterness Flip(this Betterness Value)
        {
            switch (Value)
            {
                case Betterness.First:
                    return Betterness.Second;
                case Betterness.Second:
                    return Betterness.First;
                case Betterness.Equal:
                case Betterness.Neither:
                default:
                    return Value;
            }
        }

        /// <summary>
        /// Tries the get best element in the sequence:
        /// the element that is better than every other element.
        /// </summary>
        /// <returns><c>true</c>, if a best element was found, <c>false</c> otherwise.</returns>
        /// <param name="Elements">The sequence of elements to search through.</param>
        /// <param name="Compare">A function that compares to elements for betterness.</param>
        /// <param name="BestElement">The best element in the sequence.</param>
        /// <typeparam name="T">The type of elements in the sequence.</typeparam>
        public static bool TryGetBestElement<T>(
            this IEnumerable<T> Elements,
            Func<T, T, Betterness> Compare,
            out T BestElement)
        {
            var elemArray = Elements as T[] ?? Elements.ToArray();

            if (elemArray.Length == 0)
            {
                // Hopeless. Return early.
                BestElement = default(T);
                return false;
            }

            // First, try to find an element that is better
            // than every element that comes after it in the
            // element array.
            var bestItem = elemArray[0];
            int bestIndex = 0;

            for (int i = 1; i < elemArray.Length; i++)
            {
                var item = elemArray[i];
                if (bestIndex >= 0)
                {
                    switch (Compare(bestItem, item))
                    {
                        case Betterness.Neither:
                            // Neither element is better than the
                            // other.
                            bestIndex = -1;
                            break;

                        case Betterness.Second:
                            // The second element is better than
                            // the first.
                            bestItem = item;
                            bestIndex = i;
                            break;

                        case Betterness.Equal:
                        case Betterness.First:
                        default:
                            // Do nothing. The candidate item
                            // is better than the item we just
                            // threw at it.
                            break;
                    }
                }
                else
                {
                    // Take the current element as an an
                    // arbitrary candidate for best element.
                    bestItem = item;
                    bestIndex = i;
                }
            }

            if (bestIndex < 0)
            {
                // We couldn't find an element that
                // was better than every element that
                // comes after it.
                BestElement = default(T);
                return false;
            }

            // We know that bestItem is better than every item
            // that comes after it. Now we have to check that
            // it's better than every element that comes before
            // it in the element array.
            for (int i = 0; i < bestIndex; i++)
            {
                switch (Compare(bestItem, elemArray[i]))
                {
                    case Betterness.Neither:
                    case Betterness.Second:
                        // The candidate element is not
                        // better than every other element
                        // in the sequence.
                        BestElement = default(T);
                        return false;

                    case Betterness.Equal:
                    case Betterness.First:
                    default:
                        // Do nothing. The candidate item
                        // is better than the item we just
                        // threw at it.
                        break;
                }
            }

            // We have an element that is better than everything
            // that comes after it and everything that comes before
            // it. It is therefore the best element in the sequence.
            BestElement = bestItem;
            return true;
        }

        /// <summary>
        /// Tries the get best element in the sequence:
        /// the element that is better than every other element.
        /// If no such elemet is found, then the default value
        /// for its type is returned.
        /// </summary>
        /// <returns>The best element or the default value for its type.</returns>
        /// <param name="Elements">The sequence of elements to search through.</param>
        /// <param name="Compare">A function that compares to elements for betterness.</param>
        /// <typeparam name="T">The type of elements in the sequence.</typeparam>
        public static T GetBestElementOrDefault<T>(
            this IEnumerable<T> Elements,
            Func<T, T, Betterness> Compare)
        {
            T result;
            TryGetBestElement(Elements, Compare, out result);
            return result;
        }
    }
}
