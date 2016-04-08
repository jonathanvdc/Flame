using Flame.Build;
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
                createElementNode<IType>(new IRTypeVisitor(this).Convert, CreateTypeNamer().Convert),
                index => NodeFactory.Call(IRParser.TypeTableReferenceName, new LNode[] { NodeFactory.VarLiteral(index) }));
            this.MethodTable = new IRTableBuilder<IMethod>(
                IRParser.MethodTableName,
                createElementNode<IMethod>(new IRMethodVisitor(this).Convert, DescribeMethod),
                index => NodeFactory.Call(IRParser.MethodTableReferenceName, new LNode[] { NodeFactory.VarLiteral(index) }));
            this.FieldTable = new IRTableBuilder<IField>(
                IRParser.FieldTableName,
                createElementNode<IField>(new IRFieldVisitor(this).Convert, DescribeField),
                index => NodeFactory.Call(IRParser.FieldTableReferenceName, new LNode[] { NodeFactory.VarLiteral(index) }));
        }

        public IRDependencyBuilder Dependencies { get; private set; }
        public IRTableBuilder<IType> TypeTable { get; private set; }
        public IRTableBuilder<IMethod> MethodTable { get; private set; }
        public IRTableBuilder<IField> FieldTable { get; private set; }

        public IRAssemblyEncoding Encoding { get; private set; }

        private static IConverter<IType, string> CreateTypeNamer()
        {
            return new TypeNamerBase();
        }

        private static string DescribeField(IField field)
        {
            // format fields like so:
            //
            // (static) field_type full_field_name
            var namer = CreateTypeNamer();

            return (field.IsStatic ? "static " : "") + namer.Convert(field.FieldType) + " " + field.FullName;
        }

        private static string DescribeMethod(IMethod Method)
        {
            // format methods like so:
            //
            // (static) return_type full_method_name(param_types...)

            var namer = CreateTypeNamer();

            var sb = new StringBuilder();
            if (Method.IsStatic)
            {
                sb.Append("static ");
            }
            sb.Append(namer.Convert(Method.ReturnType));
            sb.Append(' ');
            sb.Append(Method.FullName);
            sb.Append('(');
            var parameters = Method.Parameters.ToArray();
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }
                sb.Append(namer.Convert(parameters[i].ParameterType));
            }
            sb.Append(')');
            return sb.ToString();
        }

        private Func<T, int, LNode> createElementNode<T>(Func<T, LNode> Convert, Func<T, string> Describe)
        {
            if (Encoding == IRAssemblyEncoding.Textual)
            {
                return (input, index) =>
                    Convert(input).PlusAttr(
                        LNode.Trivia(CodeSymbols.TriviaSLCommentBefore, " " + index + ": " + Describe(input)));
            }
            else
            {
                return (input, index) => Convert(input);
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
                        writer.Write(Loyc.Syntax.Les.LesLanguageService.Value.Print(nodes));
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
