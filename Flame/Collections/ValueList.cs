using System;
using System.Collections.Generic;
using System.Linq;

namespace Flame.Collections
{
    /// <summary>
    /// A growable list that is implemented as a value type.
    /// This type is mainly intended for use by other collection types,
    /// and is not a drop-in replacement for System.Collections.Generic.List.
    /// </summary>
    public struct ValueList<T>
    {
        /// <summary>
        /// Creates a list with an initial capacity.
        /// </summary>
        /// <param name="initialCapacity">
        /// The list's initial capacity.
        /// </param>
        public ValueList(int initialCapacity)
        {
            this.Items = new T[initialCapacity];
            this.Count = 0;
        }

        /// <summary>
        /// Creates a list by copying another list's contents.
        /// </summary>
        /// <param name="other">
        /// The list whose contents are to be copied.
        /// </param>
        public ValueList(ValueList<T> other)
        {
            this.Items = new T[other.Count];
            this.Count = other.Count;
            Array.Copy((Array)other.Items, (Array)Items, Count);
        }

        /// <summary>
        /// Creates a list from a sequence of values.
        /// </summary>
        /// <param name="values">The values to put in the list.</param>
        public ValueList(IEnumerable<T> values)
        {
            this.Items = Enumerable.ToArray<T>(values);
            this.Count = this.Items.Length;
        }

        /// <summary>
        /// Gets the backing array for this list.
        /// </summary>
        public T[] Items { get; private set; }

        /// <summary>
        /// Gets the number of items in this list.
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// Tells if this value list has been properly initialized
        /// by a constructor.
        /// </summary>
        public bool IsInitialized => Items != null;

        /// <summary>
        /// Gets the size of this list's backing array.
        /// </summary>
        public int Capacity => Items.Length;

        /// <summary>
        /// Gets the element at the given index in this list.
        /// </summary>
        public T this[int index]
        {
            get
            {
                var itemArr = Items;
                return itemArr[index];
            }
            set
            {
                var itemArr = Items;
                itemArr[index] = value;
            }
        }

        /// <summary>
        /// Appends the given value to this list.
        /// </summary>
        public void Add(T value)
        {
            int newCount = Count + 1;
            Reserve(newCount);
            var itemArr = Items;
            itemArr[newCount - 1] = value;
            Count = newCount;
        }

        /// <summary>
        /// Removes the element at the given index. All values to the right
        /// of the given index are shifted one position to the left.
        /// </summary>
        public void RemoveAt(int index)
        {
            for (int i = index + 1; i < Count; i++)
            {
                this[i - 1] = this[i];
            }
            this.Count--;
        }

        /// <summary>
        /// Appends the given value list to this list.
        /// </summary>
        public void AddRange(ValueList<T> values)
        {
            int oldCount = Count;
            int newCount = oldCount + values.Count;
            Reserve(newCount);
            var itemArr = Items;
            for (int i = 0; i < values.Count; i++)
            {
                itemArr[oldCount + i] = values[i];
            }
            Count = newCount;
        }

        /// <summary>
        /// Minimizes at least the given capacity in this list.
        /// </summary>
        public void Reserve(int minimalCapacity)
        {
            int cap = Capacity;
            if (minimalCapacity <= cap)
                return;

            int newCapacity = cap;

            // Always resize to at least four items.
            if (newCapacity <= 4)
                newCapacity = 4;

            // Grow the new capacity exponentially.
            while (newCapacity < minimalCapacity)
                newCapacity += newCapacity;

            var newArray = new T[newCapacity];
            Array.Copy((Array)Items, (Array)newArray, Count);
            Items = newArray;
        }

        /// <summary>
        /// Copies the contents of this value list to an array.
        /// </summary>
        /// <returns>An array.</returns>
        public T[] ToArray()
        {
            var results = new T[Count];
            Array.Copy((Array)Items, (Array)results, Count);
            return results;
        }
    }
}
