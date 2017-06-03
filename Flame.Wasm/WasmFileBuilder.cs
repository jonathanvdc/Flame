using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Compiler.Build;
using Flame.Compiler;
using System.IO;
using Wasm;
using Wasm.Instructions;

namespace Flame.Wasm
{
    /// <summary>
    /// A WasmFile wrapper that makes it easy to define functions.
    /// </summary>
    public sealed class WasmFileBuilder
    {
        /// <summary>
        /// Creates a file builder from the given file.
        /// </summary>
        /// <param name="File">The file.</param>
        private WasmFileBuilder(WasmFile File)
        {
            this.file = File;
            this.importSection = File.GetFirstSectionOrNull<ImportSection>();
            this.typeSection = File.GetFirstSectionOrNull<TypeSection>();
            this.funcSection = File.GetFirstSectionOrNull<FunctionSection>();
            this.codeSection = File.GetFirstSectionOrNull<CodeSection>();
            this.funcImportCount = (uint)this.importSection.Imports.OfType<ImportedFunction>().Count();
            this.methodIndices = new Dictionary<WasmMethod, uint>();
            this.importIndices = new Dictionary<WasmMethod, uint>();
        }

        /// <summary>
        /// The WebAssembly file that is populated by this builder.
        /// </summary>
        private WasmFile file;

        /// <summary>
        /// The type section for this WebAssembly file builder.
        /// </summary>
        private TypeSection typeSection;

        /// <summary>
        /// The function section for this WebAssembly file builder.
        /// </summary>
        private FunctionSection funcSection;

        /// <summary>
        /// The code section for this WebAssembly file builder.
        /// </summary>
        private CodeSection codeSection;

        /// <summary>
        /// The import section for this WebAssmebly file builder.
        /// </summary>
        private ImportSection importSection;

        /// <summary>
        /// The number of function imports.
        /// </summary>
        private uint funcImportCount;

        private Dictionary<WasmMethod, uint> methodIndices;
        private Dictionary<WasmMethod, uint> importIndices;

        /// <summary>
        /// Gets the type index for the given function signature.
        /// </summary>
        /// <param name="Signature">A function signature.</param>
        /// <returns>An index into the type table.</returns>
        private uint GetTypeIndex(FunctionType Signature)
        {
            typeSection.FunctionTypes.Add(Signature);
            return (uint)typeSection.FunctionTypes.Count - 1;
        }

        /// <summary>
        /// Declares the given method definition or import.
        /// </summary>
        /// <param name="Method">The method to declare.</param>
        public void DeclareMethod(WasmMethod Method)
        {
            uint index;
            if (Method.IsImport)
            {
                var typeIndex = GetTypeIndex(Method.ModuleData.Abi.ImportAbi.GetSignature(Method));
                importSection.Imports.Add(
                    new ImportedFunction(
                        Method.DeclaringType.Name.ToString(),
                        Method.Name.ToString(),
                        typeIndex));
                index = funcImportCount;
                importIndices.Add(Method, index);
                funcImportCount++;
            }
            else
            {
                var typeIndex = GetTypeIndex(Method.ModuleData.Abi.GetSignature(Method));
                funcSection.FunctionTypes.Add(typeIndex);
                codeSection.Bodies.Add(null);
                index = (uint)codeSection.Bodies.Count - 1;
                methodIndices.Add(Method, index);
            }
        }

        /// <summary>
        /// Gets the index of the given WebAssembly method in the function section.
        /// </summary>
        /// <param name="Method">The WebAssembly method.</param>
        /// <returns>The method's index in the function section.</returns>
        public uint GetMethodIndex(WasmMethod Method)
        {
            if (Method.IsImport)
            {
                return importIndices[Method];
            }
            else
            {
                return funcImportCount + methodIndices[Method];
            }
        }

        /// <summary>
        /// Defines the given method's body and returns the method's index.
        /// </summary>
        /// <param name="Method">The method to define.</param>
        /// <param name="Body">The method's body.</param>
        public uint DefineMethod(WasmMethod Method, FunctionBody Body)
        {
            var index = GetMethodIndex(Method) - funcImportCount;
            codeSection.Bodies[(int)index] = Body;
            return funcImportCount + index;
        }

        /// <summary>
        /// Defines a method with the given signature and body, and returns its index.
        /// </summary>
        /// <param name="Signature">The method's signature.</param>
        /// <param name="Body">The method's body.</param>
        public uint DefineMethod(FunctionType Signature, FunctionBody Body)
        {
            var typeIndex = GetTypeIndex(Signature);
            funcSection.FunctionTypes.Add(typeIndex);
            codeSection.Bodies.Add(Body);
            return funcImportCount + (uint)codeSection.Bodies.Count - 1;
        }

        /// <summary>
        /// Sets the entry point method to the method with the given index.
        /// </summary>
        /// <param name="MethodIndex">The index of the entry point method.</param>
        public void SetStartMethod(uint MethodIndex)
        {
            var startSection = file.GetFirstSectionOrNull<StartSection>();
            if (startSection == null)
            {
                file.Sections.Add(new StartSection(MethodIndex));
            }
            else
            {
                // TODO: spell `startSection.StartFunctionIndex = MethodIndex;` when the Wasm package
                // is updated.
                file.Sections[file.Sections.IndexOf(startSection)] =
                    new StartSection(MethodIndex, startSection.ExtraPayload);
            }
        }

        /// <summary>
        /// Creates a file builder from the given file.
        /// </summary>
        /// <param name="File">The file.</param>
        public static WasmFileBuilder Create(WasmFile File)
        {
            // Initialize import, type, function and code sections.
            if (File.GetFirstSectionOrNull<ImportSection>() == null)
            {
                File.Sections.Add(new ImportSection());
            }

            if (File.GetFirstSectionOrNull<TypeSection>() == null)
            {
                File.Sections.Add(new TypeSection());
            }

            if (File.GetFirstSectionOrNull<FunctionSection>() == null)
            {
                File.Sections.Add(new FunctionSection(Enumerable.Empty<uint>()));
            }

            if (File.GetFirstSectionOrNull<CodeSection>() == null)
            {
                File.Sections.Add(new CodeSection());
            }

            return new WasmFileBuilder(File);
        }
    }
}

