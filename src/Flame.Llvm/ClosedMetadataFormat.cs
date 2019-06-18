using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Collections;
using Flame.Llvm.Emit;
using Flame.TypeSystem;
using LLVMSharp;

namespace Flame.Llvm
{
    /// <summary>
    /// A VTable-based metadata format based on the closed-world assumption,
    /// that is, the set of all members is known at compile time and will not change.
    /// </summary>
    public sealed class ClosedMetadataFormat : MetadataFormat
    {
        /// <summary>
        /// Creates a metadata format description.
        /// </summary>
        /// <param name="typeMembers">
        /// All type members that are to be included in the metadata.
        /// </param>
        public ClosedMetadataFormat(IEnumerable<ITypeMember> typeMembers)
        {
            this.methods = new Dictionary<IType, List<IMethod>>();
            this.slotIndices = new Dictionary<IMethod, int>();
            this.vtableLayouts = new Dictionary<IType, IReadOnlyList<IMethod>>();
            this.primeGenerator = new PrimeNumberGenerator();
            this.typePrimes = new Dictionary<IType, ulong>();
            this.typeTags = new Dictionary<IType, ulong>();
            foreach (var member in typeMembers)
            {
                Register(member);
            }
        }

        private void Register(ITypeMember member)
        {
            if (member is IMethod)
            {
                Register((IMethod)member);
            }
        }

        private void Register(IMethod method)
        {
            var parent = method.ParentType;
            if (parent == null)
            {
                return;
            }

            List<IMethod> implList;
            if (!methods.TryGetValue(parent, out implList))
            {
                methods[parent] = implList = new List<IMethod>();
            }
            implList.Add(method);
        }

        private Dictionary<IType, List<IMethod>> methods;
        private Dictionary<IMethod, int> slotIndices;
        private Dictionary<IType, IReadOnlyList<IMethod>> vtableLayouts;

        private PrimeNumberGenerator primeGenerator;
        private Dictionary<IType, ulong> typePrimes;
        private Dictionary<IType, ulong> typeTags;

        private const uint virtualFunctionOffset = 1;

        private IReadOnlyList<IMethod> GetVTableLayout(IType type)
        {
            IReadOnlyList<IMethod> result;
            if (vtableLayouts.TryGetValue(type, out result))
            {
                return result;
            }

            if (type.IsInterfaceType())
            {
                result = EmptyArray<IMethod>.Value;
            }
            else
            {
                var layout = type.BaseTypes.SelectMany(GetVTableLayout).ToList();
                List<IMethod> typeMethods;
                if (methods.TryGetValue(type, out typeMethods))
                {
                    foreach (var method in typeMethods)
                    {
                        bool isOverride = false;
                        foreach (var baseMethod in method.BaseMethods)
                        {
                            int index;
                            if (slotIndices.TryGetValue(baseMethod, out index))
                            {
                                layout[index] = method;
                                slotIndices[method] = index;
                                isOverride = true;
                            }
                        }
                        if (!isOverride && method.IsVirtual())
                        {
                            slotIndices[method] = layout.Count;
                            layout.Add(method);
                        }
                    }
                }
                result = layout;
            }
            vtableLayouts[type] = result;
            return result;
        }

        private ulong GetTypeTag(IType type, out ulong prime)
        {
            if (typePrimes.TryGetValue(type, out prime))
            {
                return typeTags[type];
            }
            else
            {
                // TODO: reuse primes.
                typePrimes[type] = prime = primeGenerator.Next();
                var tag = type.BaseTypes.Select(GetTypeTag).Aggregate(prime, (x, y) => x * y);
                typeTags[type] = tag;
                return tag;
            }
        }

        private ulong GetTypeTag(IType type)
        {
            ulong prime;
            return GetTypeTag(type, out prime);
        }

        private LLVMTypeRef GetTypeTagType(ModuleBuilder module)
        {
            return LLVM.Int64TypeInContext(module.Context);
        }

        private LLVMValueRef GetTypeTagValue(IType type, ModuleBuilder module)
        {
            return LLVM.ConstInt(GetTypeTagType(module), GetTypeTag(type), false);
        }

        private LLVMValueRef GetTypeMetadataTable(IType type, ModuleBuilder module)
        {
            var name = module.Mangler.Mangle(type, true) + ".vtable";
            var result = LLVM.GetNamedGlobal(module.Module, name);
            if (result.Pointer != IntPtr.Zero)
            {
                return result;
            }

            // Compose the vtable's contents.
            var entries = new List<LLVMValueRef>();
            // A unique type tag.
            entries.Add(GetTypeTagValue(type, module));
            // Virtual function addresses.
            foreach (var method in GetVTableLayout(type))
            {
                if (method.IsAbstract())
                {
                    entries.Add(LLVM.ConstNull(LLVM.PointerType(module.GetFunctionPrototype(method), 0)));
                }
                else
                {
                    entries.Add(module.DeclareMethod(method));
                }
            }

            var metadataTableContents = LLVM.ConstStructInContext(
                module.Context,
                entries.ToArray(),
                false);
            var metadataTable = LLVM.AddGlobal(module.Module, metadataTableContents.TypeOf(), name);
            metadataTable.SetInitializer(metadataTableContents);
            metadataTable.SetLinkage(LLVMLinkage.LLVMInternalLinkage);
            metadataTable.SetGlobalConstant(true);
            return metadataTable;
        }

        /// <inheritdoc/>
        public override LLVMTypeRef GetMetadataType(ModuleBuilder module)
        {
            return LLVM.PointerType(LLVM.Int8TypeInContext(module.Context), 0);
        }

        /// <inheritdoc/>
        public override LLVMValueRef GetMetadata(
            IType type,
            ModuleBuilder module,
            IRBuilder builder,
            string name)
        {
            return builder.CreateBitCast(
                GetTypeMetadataTable(type, module),
                GetMetadataType(module),
                name);
        }

        /// <inheritdoc/>
        public override LLVMValueRef EmitMethodAddress(
            IMethod callee,
            LLVMValueRef metadataPointer,
            ModuleBuilder module,
            IRBuilder builder,
            string name)
        {
            if (callee.ParentType.IsInterfaceType())
            {
                throw new NotImplementedException($"Interface method lookup is not supported yet.");
            }
            else if (callee.IsVirtual())
            {
                var functionProto = module.GetFunctionPrototype(callee);
                var typedMetadataPointer = builder.CreateBitCast(
                    metadataPointer,
                    GetTypeMetadataTable(callee.ParentType, module).TypeOf(),
                    "vtable.ptr");
                var index = slotIndices[callee];
                return builder.CreateLoad(
                    builder.CreateStructGEP(
                        typedMetadataPointer,
                        virtualFunctionOffset + (uint)index,
                        "vfptr.address"),
                    name);
            }
            else
            {
                return module.DeclareMethod(callee);
            }
        }

        /// <inheritdoc/>
        public override LLVMValueRef EmitIsSubtype(
            LLVMValueRef subtypeMetadata,
            IType supertype,
            ModuleBuilder module,
            IRBuilder builder,
            string name)
        {
            // Here's how we do isinstance tests: every type has a unique integer tag.
            // That tag is designed such that `subtype.tag % supertype.tag == 0` iff
            // `subtype isa supertype`. We compute the former to determine the latter.

            var tagType = GetTypeTagType(module);
            var tag = builder.CreateLoad(
                builder.CreateBitCast(subtypeMetadata, LLVM.PointerType(tagType, 0), "tag.ptr"),
                "tag");
            return builder.CreateICmp(
                LLVMIntPredicate.LLVMIntEQ,
                builder.CreateURem(
                    tag,
                    GetTypeTagValue(supertype, module),
                    "tag.rem"),
                LLVM.ConstNull(tagType),
                name);
        }
    }
}
