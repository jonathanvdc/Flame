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
            string Name, DataLayout Layout, 
            IReadOnlyList<byte> InitialData)
        {
            this.Name = Name;
            this.Layout = Layout;
            this.InitialData = InitialData;
        }
        public MemorySection(string Name, DataLayout Layout)
            : this(Name, Layout, null)
        { }
        public MemorySection(string Name, IReadOnlyList<byte> InitialData)
            : this(Name, new DataLayout(InitialData.Count), InitialData)
        { }
        public MemorySection(string Name, int Size)
            : this(Name, new DataLayout(Size))
        { }

        /// <summary>
        /// Gets this memory section's name.
        /// </summary>
        public string Name { get; private set; }

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
            return string.Format("memory-section({0}, {1}, {2})", Name, Size, IsInitialized ? "initialized" : "uninitialized");
        }
    }

    /// <summary>
    /// Describes the memory layout of a program, as
    /// a sequence of memory sections.
    /// </summary>
    public class MemoryLayout
    {
        public MemoryLayout()
        {
            this.memSecs = new List<MemorySection>();
        }

        private List<MemorySection> memSecs;

        /// <summary>
        /// Gets this memory layout's section list.
        /// </summary>
        public IReadOnlyList<MemorySection> Sections { get { return memSecs; } }

        /// <summary>
        /// Adds the given memory section to this memory
        /// layout structure.
        /// </summary>
        public void DeclareSection(MemorySection Section)
        {
            memSecs.Add(Section);
        }
    }
}

