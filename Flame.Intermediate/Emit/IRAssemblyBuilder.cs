using Flame.Compiler.Build;
using Flame.Intermediate.Parsing;
using Loyc.Binary;
using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate.Emit
{
    public enum IRAssemblyEncoding
    {
        Textual,
        Binary
    }

    public class IRAssemblyBuilder : IRAssembly, IAssemblyBuilder
    {
        public IRAssemblyBuilder(IRSignature Signature, IEnvironment Environment, IRAssemblyEncoding Encoding)
            : this(Signature, Environment, Encoding, new Version(1, 0, 0, 0))
        { }
        public IRAssemblyBuilder(IRSignature Signature, IEnvironment Environment, IRAssemblyEncoding Encoding, Version Version)
            : base(Signature, Environment, Version)
        {
            this.Encoding = Encoding;

            this.Dependencies = new IRDependencyBuilder(this);
            this.TypeTable = new IRTableBuilder<IType>(
                IRParser.TypeTableName,
                createElementNode<IType>(new IRTypeVisitor(this).Convert), 
                index => NodeFactory.Call(IRParser.TypeTableReferenceName, new LNode[] { NodeFactory.Literal(index) }));
            this.MethodTable = new IRTableBuilder<IMethod>(
                IRParser.MethodTableName,
                createElementNode<IMethod>(new IRMethodVisitor(this).Convert), 
                index => NodeFactory.Call(IRParser.MethodTableReferenceName, new LNode[] { NodeFactory.Literal(index) }));
            this.FieldTable = new IRTableBuilder<IField>(
                IRParser.FieldTableName, 
                createElementNode<IField>(new IRFieldVisitor(this).Convert),
                index => NodeFactory.Call(IRParser.FieldTableReferenceName, new LNode[] { NodeFactory.Literal(index) }));
        }

        public IRDependencyBuilder Dependencies { get; private set; }
        public IRTableBuilder<IType> TypeTable { get; private set; }
        public IRTableBuilder<IMethod> MethodTable { get; private set; }
        public IRTableBuilder<IField> FieldTable { get; private set; }

        public IRAssemblyEncoding Encoding { get; private set; }

        private Func<T, int, LNode> createElementNode<T>(Func<T, LNode> Converter)
            where T : IMember
        {
            if (Encoding == IRAssemblyEncoding.Textual)
            {
                return (input, index) => 
                    Converter(input).PlusAttr(
                        LNode.Trivia(CodeSymbols.TriviaSLCommentBefore, " " + index + ": " + input.FullName));
            }
            else
            {
                return (input, index) => Converter(input);
            }
        }

        public void Save(IOutputProvider OutputProvider)
        {
            // Create the assembly's node *first* such that
            // any lazily generated nodes will be computed,
            // resulting in the type, method and field tables
            // getting updated just in time.
            var asmNode = this.Node;

            var nodes = Dependencies.DependencyNodes.Concat(new LNode[]
            {
                TypeTable.Node, 
                MethodTable.Node,
                FieldTable.Node, 
                asmNode 
            }).ToArray();
            using (var fs = OutputProvider.Create().OpenOutput())
            {
                if (Encoding == IRAssemblyEncoding.Binary)
                {
                    LoycBinaryHelpers.WriteFile(fs, nodes);
                }
                else
                {
                    using (var writer = new StreamWriter(fs))
                    {
                        writer.Write(Loyc.Syntax.Les.LesLanguageService.Value.PrintMultiple(nodes));
                    }
                }
            }
        }

        private IRRootNamespaceBuilder GetRootNamespaceBuilder()
        {
            if (!(this.RootNamespace is IRRootNamespaceBuilder))
            {
                this.RootNamespace = new IRRootNamespaceBuilder(this, this.RootNamespace);
            }
            return (IRRootNamespaceBuilder)this.RootNamespace;
        }

        public INamespaceBuilder DeclareNamespace(string Name)
        {
            var rootNs = GetRootNamespaceBuilder();
            if (string.IsNullOrEmpty(Name))
            {
                return rootNs;
            }
            else
            {
                return rootNs.DeclareNamespace(Name);
            }
        }

        public void SetEntryPoint(IMethod Method)
        {
            this.EntryPointNode = new LazyNodeStructure<IMethod>(Method, MethodTable.GetReference);
        }

        public IAssembly Build()
        {
            return this;
        }

        public void Initialize()
        {
            // Nothing to do here.
        }
    }
}
