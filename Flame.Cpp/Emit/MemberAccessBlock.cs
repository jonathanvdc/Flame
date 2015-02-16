using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class MemberAccessBlock : ICppBlock
    {
        public MemberAccessBlock(ICppBlock Target, ITypeMember Member, IType Type)
        {
            this.Target = Target;
            this.Member = Member;
            this.Type = Type;
        }

        public ICppBlock Target { get; private set; }
        public ITypeMember Member { get; private set; }
        public IType Type { get; private set; }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Target.Dependencies.MergeDependencies(Type.GetDependencies()); }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Target.LocalsUsed; }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Target.CodeGenerator; }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            if (Target is IPointerBlock)
            {
                cb.Append(((IPointerBlock)Target).StaticDereference().GetCode());
                cb.Append('.');
            }
            else
            {
                if (Target is DereferenceBlock)
                {
                    cb.Append(((DereferenceBlock)Target).Value.GetCode());
                    cb.Append("->");
                }
                else
                {
                    cb.Append(Target.GetCode()); 
                    if (Target.Type.get_IsPointer())
                    {
                        cb.Append("->");
                    }
                    else
                    {
                        cb.Append('.');
                    }
                }
            }
            cb.Append(Member.Name);
            return cb;
        }

        public override string ToString()
        {
            return GetCode().ToString();
        }
    }
}
