using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public class AccessRecord<TAccessed, TIdentifier>
    {
        public AccessRecord()
        {
            this.accessedItems = new HashSet<TAccessed>();
        }
        public AccessRecord(IEnumerable<TAccessed> AccessedItems, TIdentifier Identifier)
        {
            this.Identifier = Identifier;
            this.accessedItems = new HashSet<TAccessed>(AccessedItems);
        }
        public AccessRecord(IEnumerable<TAccessed> AccessedItems, TIdentifier Identifier, AccessRecord<TAccessed, TIdentifier> Previous)
            : this(AccessedItems, Identifier)
        {
            this.Previous = Previous;
        }

        private HashSet<TAccessed> accessedItems;

        /// <summary>
        /// Gets a boolean value that indicates if this access record is first, i.e., has no previous record.
        /// </summary>
        public bool IsFirst { get { return Previous == null; } }

        /// <summary>
        /// Gets a boolean value that indicates whether this access record, as well as all of its parents, are empty.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                if (accessedItems.Count > 0)
                {
                    return false;
                }
                else if (!IsFirst)
                {
                    return Previous.IsEmpty;
                }
                else
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Gets this change record's identifier.
        /// </summary>
        public TIdentifier Identifier { get; private set; }
        /// <summary>
        /// Gets all items that have been accessed in this record.
        /// </summary>
        public IEnumerable<TAccessed> AccessedItems { get { return accessedItems; } }

        /// <summary>
        /// Gets the previous access record.
        /// </summary>
        public AccessRecord<TAccessed, TIdentifier> Previous { get; private set; }

        /// <summary>
        /// Gets a sequence of all accessed items that have been accessed by this access record, or by previous records.
        /// </summary>
        public IEnumerable<TAccessed> AllAccessedItems
        {
            get
            {
                if (IsFirst)
                {
                    return AccessedItems;
                }
                else
                {
                    return Previous.AllAccessedItems.Concat(AccessedItems);
                }
            }
        }

        /// <summary>
        /// Gets a boolean value that indicates whether the given item has been accessed in this specific change record.
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        public bool Accesses(TAccessed Item)
        {
            return accessedItems.Contains(Item);
        }

        /// <summary>
        /// Gets a boolean value that indicates if the given item has been accessed since the given ancestor point.
        /// </summary>
        /// <param name="Item"></param>
        /// <param name="Other"></param>
        /// <returns></returns>
        public bool AccessedSince(TAccessed Item, AccessRecord<TAccessed, TIdentifier> Other)
        {
            if (Accesses(Item))
            {
                return true;
            }
            else if (IsFirst || Other == Previous)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    public static class AccessRecordExtensions
    {
        /// <summary>
        /// Gets the identifier of the last access to the given item, or the default identifier value, if none could be found.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TIdentifier"></typeparam>
        /// <param name="Record"></param>
        /// <returns></returns>
        public static TIdentifier GetAccessIdentifierOrDefault<TAccessed, TIdentifier>(this AccessRecord<TAccessed, TIdentifier> Record, TAccessed Item)
        {
            if (Record.Accesses(Item))
            {
                return Record.Identifier;
            }
            else if (!Record.IsFirst)
            {
                return Record.Previous.GetAccessIdentifierOrDefault(Item);
            }
            else
            {
                return default(TIdentifier);
            }
        }

        /// <summary>
        /// Gets the identifier of the last access to the given item, or null, if none could be found.
        /// </summary>
        /// <typeparam name="TAccessed"></typeparam>
        /// <typeparam name="TIdentifier"></typeparam>
        /// <param name="Record"></param>
        /// <param name="Item"></param>
        /// <returns></returns>
        public static TIdentifier? GetAccessIdentifierOrNull<TAccessed, TIdentifier>(this AccessRecord<TAccessed, TIdentifier> Record, TAccessed Item)
            where TIdentifier : struct
        {
            if (Record.Accesses(Item))
            {
                return Record.Identifier;
            }
            else if (!Record.IsFirst)
            {
                return Record.Previous.GetAccessIdentifierOrNull(Item);
            }
            else
            {
                return null;
            }
        }

        public static IEnumerable<AccessRecord<TAccessed, TIdentifier>> GetTimelineReversed<TAccessed, TIdentifier>(this AccessRecord<TAccessed, TIdentifier> Record)
        {
            while (!Record.IsFirst)
            {
                yield return Record;
                Record = Record.Previous;
            }
            yield return Record;
        }

        public static IEnumerable<AccessRecord<TAccessed, TIdentifier>> GetTimeline<TAccessed, TIdentifier>(this AccessRecord<TAccessed, TIdentifier> Record)
        {
            return Record.GetTimelineReversed().Reverse();
        }

        public static AccessRecord<T, int> PipeAccess<T>(this AccessRecord<T, int> Current, IEnumerable<T> AccessedItems)
        {
            return new AccessRecord<T, int>(AccessedItems, Current.Identifier + 1, Current);
        }

        public static AccessRecord<T, int> PipeEmpty<T>(this AccessRecord<T, int> Current)
        {
            return Current.PipeAccess(Enumerable.Empty<T>());
        }

        public static AccessRecord<T, int> PipeTimeline<T>(this AccessRecord<T, int> Current, AccessRecord<T, int> Other)
        {
            AccessRecord<T, int> currentRecord = Current;
            var timeline = Other.GetTimeline();
            foreach (var item in timeline)
            {
                currentRecord = currentRecord.PipeAccess(item.AccessedItems);
            }
            return currentRecord;
        }
    }
}
