using System;
using System.Collections.Generic;
using Flame.Compiler.Native;
using Flame.Compiler;

namespace Flame.Wasm
{
    /// <summary>
    /// A data structure that represents a named section of linear memory.
    /// </summary>
    public sealed class MemorySection
    {
        public MemorySection(
            int Offset, DataLayout Layout, 
            IReadOnlyList<byte> InitialData)
        {
            this.Offset = Offset;
            this.Layout = Layout;
            this.InitialData = InitialData;
        }

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
        /// Gets this section's initial data, 
        /// as a read-only sequence of bytes.
        /// This can be null if the memory section
        /// is left uninitialized.
        /// </summary>
        /// <value>The section's initial data.</value>
        public IReadOnlyList<byte> InitialData { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this memory section's data is initialized.
        /// </summary>
        /// <value><c>true</c> if this memory section's data is initialized; otherwise, <c>false</c>.</value>
        public bool IsInitialized { get { return InitialData != null; } }

        public override string ToString()
        {
            return string.Format("memory-section({0}, {1}, {2})", Offset, Size, IsInitialized ? "initialized" : "uninitialized");
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
            this.memSecs = new List<MemorySection>();
            this.offset = 0;
        }

        private List<MemorySection> memSecs;
        private int offset;

        /// <summary>
        /// Gets the total size of this memory layout.
        /// </summary>
        public int Size { get { return offset; } }

        /// <summary>
        /// Gets this memory layout's section list.
        /// </summary>
        public IReadOnlyList<MemorySection> Sections { get { return memSecs; } }

        /// <summary>
        /// Adds the given memory section to this memory
        /// layout structure.
        /// </summary>
        public MemorySection DeclareSection(MemorySection Section)
        {
            memSecs.Add(Section);
            return Section;
        }

        public MemorySection DeclareSection(DataLayout Layout, IReadOnlyList<byte> InitialData)
        {
            var result = new MemorySection(offset, Layout, InitialData);
            offset += Layout.Size;
            return DeclareSection(result);
        }
        public MemorySection DeclareSection(DataLayout Layout)
        { 
            return DeclareSection(Layout, null);
        }
        public MemorySection DeclareSection(IReadOnlyList<byte> InitialData)
        { 
            return DeclareSection(new DataLayout(InitialData.Count), InitialData);
        }
        public MemorySection DeclareSection(int Size)
        { 
            return DeclareSection(new DataLayout(Size));
        }
    }
}

