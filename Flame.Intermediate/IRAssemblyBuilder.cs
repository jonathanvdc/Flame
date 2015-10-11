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

namespace Flame.Intermediate
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

            this.Dependencies = new IRDependencyBuilder();
            this.TypeTable = new IRTableBuilder<IType>(IRParser.TypeTableName);
            this.MethodTable = new IRTableBuilder<IMethod>(IRParser.MethodTableName);
            this.FieldTable = new IRTableBuilder<IField>(IRParser.FieldTableName);
        }

        public IRDependencyBuilder Dependencies { get; private set; }
        public IRTableBuilder<IType> TypeTable { get; private set; }
        public IRTableBuilder<IMethod> MethodTable { get; private set; }
        public IRTableBuilder<IField> FieldTable { get; private set; }

        public IRAssemblyEncoding Encoding { get; private set; }

        public void Save(IOutputProvider OutputProvider)
        {
            var nodes = Dependencies.DependencyNodes.Concat(new LNode[]
            { 
                TypeTable.Node, 
                MethodTable.Node,
                FieldTable.Node, 
                this.Node 
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
                        Loyc.Syntax.Les.LesLanguageService.Value.PrintMultiple(nodes);
                    }
                }
            }
        }

        public INamespaceBuilder DeclareNamespace(string Name)
        {
            // TODO: implement this!
            throw new NotImplementedException();
        }

        public void SetEntryPoint(IMethod Method)
        {
            // TODO: implement this!
            throw new NotImplementedException();
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
