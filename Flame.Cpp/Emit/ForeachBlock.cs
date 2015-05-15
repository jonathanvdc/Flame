using Flame.Compiler;
using Flame.Compiler.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class ForeachHeader : IForeachBlockHeader
    {
        public ForeachHeader(CppCodeGenerator CodeGenerator, CollectionBlock Collection)
        {
            this.Collection = Collection;
            DeclareElement(CodeGenerator);
        }

        public LocalDeclarationReference Element { get; private set; }
        public CollectionBlock Collection { get; private set; }

        private void DeclareElement(CppCodeGenerator CodeGenerator)
        {
            var cg = (CppCodeGenerator)CodeGenerator;
            var elemVar = cg.DeclareOwnedVariable(Collection.Member);
            this.Element = new LocalDeclarationReference(elemVar);
        }

        public IReadOnlyList<IEmitVariable> Elements
        {
            get { return new IEmitVariable[] { Element.Declaration.Local }; }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get
            {
                return Element.Dependencies.MergeDependencies(Collection.Dependencies);
            }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get
            {
                return Element.LocalsUsed.Union(Collection.LocalsUsed);
            }
        }

        public IEnumerable<LocalDeclaration> LocalDeclarations
        {
            get { return Element.GetLocalDeclarations().Concat(Collection.GetLocalDeclarations()); }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.Append("for (");
            cb.Append(Element.GetExpressionCode(true, true));
            cb.Append(" : ");
            cb.Append(Collection.GetCode());
            cb.Append(")");
            return cb;
        }
    }

    public class ForeachBlock : ICppLocalDeclaringBlock
    {
        public ForeachBlock(ForeachHeader Header, ICppBlock Body)
        {
            this.Header = Header;
            this.Body = Body;
        }

        public ForeachHeader Header { get; private set; }
        public ICppBlock Body { get; private set; }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get
            {
                return Header.Dependencies.MergeDependencies(Body.Dependencies);
            }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get
            {
                return Header.LocalsUsed.Union(Body.LocalsUsed);
            }
        }

        public IEnumerable<LocalDeclaration> LocalDeclarations
        {
            get { return Header.LocalDeclarations.Union(Body.GetLocalDeclarations()); }
        }

        public IEnumerable<LocalDeclaration> SpilledDeclarations
        {
            get { return Enumerable.Empty<LocalDeclaration>(); }
        }

        public IType Type
        {
            get { return PrimitiveTypes.Void; }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Body.CodeGenerator; }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = Header.GetCode();
            cb.AddBodyCodeBuilder(Body.GetCode());
            return cb;
        }
    }
}
