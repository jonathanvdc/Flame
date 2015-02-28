using Flame.Compiler;
using Flame.Compiler.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class FinallyBlock : ICppBlock, ICppLocalDeclaringBlock
    {
        public FinallyBlock(ICppBlock Body)
        {
            this.Body = Body;
            this.FinallyDeclaration = new LocalDeclarationReference((CppLocal)Body.CodeGenerator.DeclareVariable(new DescribedVariableMember("final_action", Plugs.StdxFinally.Instance)));
        }

        public LocalDeclarationReference FinallyDeclaration { get; private set; }
        public ICppBlock Body { get; private set; }

        public bool IsEmpty
        {
            get
            {
                return Body.GetCode().IsWhitespace;
            }
        }

        public IType Type
        {
            get { return PrimitiveTypes.Void; }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return IsEmpty ? Enumerable.Empty<IHeaderDependency>() : Plugs.StdxFinally.Instance.GetDependencies().MergeDependencies(Body.Dependencies); }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Body.LocalsUsed.With(FinallyDeclaration.Declaration.Local); }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Body.CodeGenerator; }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            var bodyCode = Body.GetCode();
            if (!bodyCode.IsWhitespace)
            {
                cb.Append(FinallyDeclaration.GetExpressionCode(false));
                cb.Append("([&]");
                cb.AddEmbracedBodyCodeBuilder(bodyCode);
                cb.Append(");");
            }
            return cb;
        }

        public IEnumerable<LocalDeclaration> LocalDeclarations
        {
            get
            {
                if (Body is ICppLocalDeclaringBlock)
                {
                    return ((ICppLocalDeclaringBlock)Body).LocalDeclarations.With(FinallyDeclaration.Declaration);
                }
                else
                {
                    return new LocalDeclaration[] { FinallyDeclaration.Declaration };
                }
            }
        }
    }
}
