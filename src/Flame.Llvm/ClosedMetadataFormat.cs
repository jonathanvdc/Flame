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
            this.metadata = new Dictionary<IType, LLVMValueRef>();
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
        private Dictionary<IType, LLVMValueRef> metadata;

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
                                layout[index] = baseMethod;
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

        /// <inheritdoc/>
        public override LLVMValueRef GetMetadataPointer(IType type, ModuleBuilder module)
        {
            LLVMValueRef result;
            if (metadata.TryGetValue(type, out result))
            {
                return result;
            }

            var entries = new List<LLVMValueRef>();
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
            var metadataTable = LLVM.AddGlobal(
                module.Module,
                metadataTableContents.TypeOf(),
                module.Mangler.Mangle(type, true) + ".vtable");
            metadataTable.SetInitializer(metadataTableContents);
            metadataTable.SetLinkage(LLVMLinkage.LLVMInternalLinkage);
            metadataTable.SetGlobalConstant(true);
            metadata[type] = metadataTable;
            return metadataTable;
        }
    }
}
