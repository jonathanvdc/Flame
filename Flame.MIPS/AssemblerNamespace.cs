﻿using Flame.Compiler;
using Flame.Compiler.Build;
using Flame.MIPS.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS
{
    public class AssemblerNamespace : INamespaceBuilder, INamespaceBranch
    {
        public AssemblerNamespace(IAssembly DeclaringAssembly, string Name, IAssemblerState GlobalState)
        {
            this.DeclaringAssembly = DeclaringAssembly;
            this.Name = Name;
            this.GlobalState = GlobalState;

            this.ns = new List<AssemblerNamespace>();
            this.types = new List<AssemblerType>();
        }

        public IAssembly DeclaringAssembly { get; private set; }
        public IAssemblerState GlobalState { get; private set; }
        public string Name { get; private set; }

        private List<AssemblerNamespace> ns;
        private List<AssemblerType> types;

        public INamespaceBuilder DeclareNamespace(string Name)
        {
            var asmNs = new AssemblerNamespace(DeclaringAssembly, MemberExtensions.CombineNames(this.FullName, Name), GlobalState);
            ns.Add(asmNs);
            return asmNs;
        }

        public ITypeBuilder DeclareType(ITypeSignatureTemplate Template)
        {
            var asmType = new AssemblerType(this, Template, GlobalState);
            types.Add(asmType);
            return asmType;
        }

        public INamespace Build()
        {
            return this;
        }

        public void Initialize()
        {
            // Do nothing. This back-end does not need `Initialize` to get things done.
        }

        public string FullName
        {
            get { return Name; }
        }

        public AttributeMap Attributes
        {
            get { return AttributeMap.Empty; }
        }

        public IEnumerable<IType> Types
        {
            get { return types; }
        }

        public IEnumerable<INamespaceBranch> Namespaces
        {
            get { return ns; }
        }
    }
}
