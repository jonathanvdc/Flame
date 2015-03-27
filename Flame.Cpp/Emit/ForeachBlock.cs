using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class ForeachBlock : ICppLocalDeclaringBlock
    {
        public ForeachBlock(LocalDeclarationReference Element,ICppBlock Collection, ICppBlock Body)
        {
            this.Element = Element;
            this.Collection = Collection;
            this.Body = Body;
        }

        public LocalDeclarationReference Element { get; private set; }
        public ICppBlock Collection { get; private set; }
        public ICppBlock Body { get; private set; }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get
            {
                return Element.Dependencies.MergeDependencies(Collection.Dependencies).MergeDependencies(Body.Dependencies);
            }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get
            {
                return Element.LocalsUsed.Union(Collection.LocalsUsed).Union(Body.LocalsUsed);
            }
        }

        public IEnumerable<LocalDeclaration> LocalDeclarations
        {
            get { return Element.GetLocalDeclarations().Concat(Collection.GetLocalDeclarations()).Union(Body.GetLocalDeclarations()); }
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
            CodeBuilder cb = new CodeBuilder();
            cb.Append("for (");
            cb.Append(Element.GetExpressionCode(true, true));
            cb.Append(" : ");
            cb.Append(Collection.GetCode());
            cb.Append(")");
            cb.AddBodyCodeBuilder(Body.GetCode());
            return cb;
        }
    }
}
