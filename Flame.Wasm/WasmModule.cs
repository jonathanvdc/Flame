using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Compiler.Build;
using Flame.Compiler;
using System.IO;
using Wasm;
using Wasm.Instructions;
using WasmMemorySection = Wasm.MemorySection;

namespace Flame.Wasm
{
    public class WasmModule : IAssembly, IAssemblyBuilder
    {
        public WasmModule(
            string Name, Version AssemblyVersion,
            IEnvironment Environment, IWasmAbi Abi,
            ICompilerOptions Options)
        {
            this.Name = new SimpleName(Name);
            this.AssemblyVersion = AssemblyVersion;
            this.Environment = Environment;
            this.Options = Options;
            this.entryPoint = null;
            this.moduleNs = new WasmModuleNamespace(this, new WasmModuleData(Abi));
            Abi.InitializeMemory(this);
        }

        /// <summary>
        /// Gets this wasm module's name.
        /// </summary>
        public UnqualifiedName Name { get; private set; }
        public IEnvironment Environment { get; private set; }
        public Version AssemblyVersion { get; private set; }
        public ICompilerOptions Options { get; private set; }

        private WasmModuleNamespace moduleNs;
        private IMethod entryPoint;

        public WasmModuleData Data { get { return moduleNs.Data; } }

        public QualifiedName FullName { get { return new QualifiedName(Name); } }
        public AttributeMap Attributes { get { return AttributeMap.Empty; } }

        public IMethod GetEntryPoint() { return entryPoint; }

        public IBinder CreateBinder()
        {
            return new Flame.Binding.NamespaceTreeBinder(Environment, moduleNs);
        }

        public INamespaceBuilder DeclareNamespace(string Name)
        {
            return moduleNs.DeclareNamespace(Name);
        }

        public void Save(IOutputProvider OutputProvider)
        {
            var file = ToWasmFile();
            using (var stream = OutputProvider.Create().OpenOutput())
            {
                file.WriteBinaryTo(stream);
            }
        }

        public void SetEntryPoint(IMethod Method)
        {
            entryPoint = Method;
        }

        public void Initialize()
        { }

        public IAssembly Build()
        {
            return this;
        }

        /// <summary>
        /// Creates a data segment for the given memory section if it initializes
        /// a region of memory; otherwise, returns null.
        /// </summary>
        /// <param name="Memory">The memory section.</param>
        private DataSegment GetDataSegmentOrNull(MemorySegment Memory)
        {
            var initData = Memory.InitialData;

            // Strip leading zero bytes. (Wasm memory is zero-initialized by default)
            int firstNonzero = 0;
            while (firstNonzero < initData.Count && initData[firstNonzero] == 0)
                firstNonzero++;

            if (firstNonzero == initData.Count)
                // Nothing to initialize.
                return null;

            // Strip trailing zero bytes. (Wasm memory is zero-initialized by default)
            int lastNonzero = initData.Count - 1;
            while (lastNonzero >= 0 && initData[lastNonzero] == 0)
                lastNonzero--;

            return new DataSegment(
                0,
                new InitializerExpression(new Instruction[]
                {
                    Operators.Int32Const.Create(Memory.Offset + firstNonzero)
                }),
                initData.Skip(firstNonzero).Take(lastNonzero - firstNonzero).ToArray());
        }

        /// <summary>
        /// Creates the memory section for this WebAssembly module.
        /// </summary>
        /// <returns>The memory section.</returns>
        private WasmMemorySection CreateMemorySection()
        {
            // The initial and maximum memory size are required to
            // be a multiple of the WebAssembly page size,
            // which is 64KiB on all engines.
            int rem;
            int pageCount = Math.DivRem(Data.Memory.Size, (int)MemoryType.PageSize, out rem);
            if (rem > 0)
                pageCount++;

            return new WasmMemorySection(new MemoryType[]
            {
                new MemoryType(new ResizableLimits((uint)pageCount))
            });
        }

        /// <summary>
        /// Creates the data section for this WebAssembly module.
        /// </summary>
        /// <returns>The data section.</returns>
        private DataSection CreateDataSection()
        {
            var section = new DataSection();
            foreach (var sec in Data.Memory.Segments)
            {
                if (sec.IsInitialized)
                {
                    var segment = GetDataSegmentOrNull(sec);
                    if (segment != null)
                    {
                        section.Segments.Add(segment);
                    }
                }
            }
            return section;
        }

        public WasmFile ToWasmFile()
        {
            var file = new WasmFile();
            file.Sections.Add(CreateMemorySection());
            file.Sections.Add(CreateDataSection());

            var fileBuilder = WasmFileBuilder.Create(file);

            var allMethods = moduleNs.GetAllMethodDefinitions();
            foreach (var item in allMethods)
            {
                item.Declare(fileBuilder);
            }
            foreach (var item in allMethods)
            {
                item.Define(fileBuilder);
            }

            Data.Abi.SetupEntryPoint(this, fileBuilder);

            return file;
        }
    }
}

