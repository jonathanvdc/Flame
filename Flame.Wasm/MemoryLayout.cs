using System;
using System.Collections.Generic;
using Flame.Compiler.Native;
using Flame.Compiler;

namespace Flame.Wasm
{
    /// <summary>
    /// A data structure that represents a named segment of linear memory.
    /// </summary>
    public sealed class MemorySegment
    {
        public MemorySegment(
            string Name,
            int Offset,
            DataLayout Layout,
            IReadOnlyList<byte> InitialData)
        {
            this.Name = Name;
            this.Offset = Offset;
            this.Layout = Layout;
            this.InitialData = InitialData;
        }

        public MemorySegment(
            int Offset,
            DataLayout Layout,
            IReadOnlyList<byte> InitialData)
            : this(null, Offset, Layout, InitialData)
        {
        }

        /// <summary>
        /// Gets this section's unique name if it has one.
        /// </summary>
        /// <returns>The section's unique name if it has one; otherwise, <c>null</c>.</returns>
        public string Name { get; private set; }

        /// <summary>
        /// Gets this memory section's offset.
        /// </summary>
        public int Offset { get; private set; }

        /// <summary>
        /// Gets the memory section's size, in bytes.
        /// </summary>
        public int Size { get { return Layout.Size; } }

        /// <summary>
        /// Gets the memory section's layout.
        /// </summary>
        public DataLayout Layout { get; private set; }

        /// <summary>
        /// Gets or sets this section's initial data,
        /// as a read-only sequence of bytes.
        /// This can be null if the memory section
        /// is left uninitialized.
        /// </summary>
        /// <value>The section's initial data.</value>
        public IReadOnlyList<byte> InitialData { get; set; }

        /// <summary>
        /// Gets a value indicating whether this memory section's data is initialized.
        /// </summary>
        /// <value><c>true</c> if this memory section's data is initialized; otherwise, <c>false</c>.</value>
        public bool IsInitialized { get { return InitialData != null; } }

        public override string ToString()
        {
            return string.Format(
                "memory-section({0}{1}, {2}, {3})",
                Name == null ? "" : Name + ", ",
                Offset,
                Size,
                IsInitialized ? "initialized" : "uninitialized");
        }
    }

    /// <summary>
    /// Describes the memory layout of a program, as
    /// a sequence of memory sections.
    /// </summary>
    public sealed class MemoryLayout
    {
        public MemoryLayout()
        {
            this.memSegDict = new Dictionary<string, MemorySegment>();
            this.memSecs = new List<MemorySegment>();
            this.offset = 0;
        }

        private Dictionary<string, MemorySegment> memSegDict;
        private List<MemorySegment> memSecs;
        private int offset;

        /// <summary>
        /// Gets the total size of this memory layout.
        /// </summary>
        public int Size { get { return offset; } }

        /// <summary>
        /// Gets this memory layout's section list.
        /// </summary>
        public IReadOnlyList<MemorySegment> Segments { get { return memSecs; } }

        /// <summary>
        /// Gets the memory segment with the given name.
        /// </summary>
        /// <param name="Name">The memory segment's name.</param>
        /// <returns>The memory segment; <c>null</c> if the segment cannot be found..</returns>
        public MemorySegment GetSegment(string Name)
        {
            MemorySegment result;
            if (memSegDict.TryGetValue(Name, out result))
            {
                return result;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Adds the given memory section to this memory layout.
        /// </summary>
        public MemorySegment DeclareSegment(MemorySegment Section)
        {
            memSecs.Add(Section);
            if (Section.Name != null)
            {
                memSegDict.Add(Section.Name, Section);
            }
            return Section;
        }

        public MemorySegment DeclareSegment(string Name, DataLayout Layout, IReadOnlyList<byte> InitialData)
        {
            var result = new MemorySegment(Name, offset, Layout, InitialData);
            offset += Layout.Size;
            return DeclareSegment(result);
        }

        public MemorySegment DeclareSegment(DataLayout Layout, IReadOnlyList<byte> InitialData)
        {
            return DeclareSegment(null, Layout, InitialData);
        }

        public MemorySegment DeclareSegment(string Name, DataLayout Layout)
        {
            return DeclareSegment(Name, Layout, null);
        }

        public MemorySegment DeclareSegment(DataLayout Layout)
        {
            return DeclareSegment(Layout, null);
        }

        public MemorySegment DeclareSegment(string Name, IReadOnlyList<byte> InitialData)
        {
            return DeclareSegment(Name, new DataLayout(InitialData.Count), InitialData);
        }

        public MemorySegment DeclareSegment(IReadOnlyList<byte> InitialData)
        {
            return DeclareSegment(new DataLayout(InitialData.Count), InitialData);
        }

        public MemorySegment DeclareSegment(string Name, int Size)
        {
            return DeclareSegment(Name, new DataLayout(Size));
        }

        public MemorySegment DeclareSegment(int Size)
        {
            return DeclareSegment(new DataLayout(Size));
        }
    }
}

