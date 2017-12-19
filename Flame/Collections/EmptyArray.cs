namespace Flame.Collections
{
    internal static class EmptyArray<T>
    {
        /// <summary>
        /// Gets an empty array.
        /// </summary>
        public static readonly T[] Value = new T[0];
    }
}