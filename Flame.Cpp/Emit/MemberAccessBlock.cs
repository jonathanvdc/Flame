using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class MemberAccessBlock : IOpBlock
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
        public int Precedence { get { return 2; } }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Target.Dependencies.MergeDependencies(Member.DeclaringType.GetDependencies()).MergeDependencies(Type.GetDependencies()); }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Target.LocalsUsed; }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Target.CodeGenerator; }
        }

        /// <summary>
        /// Gets a boolean value that tells if the method is to be applied to a slice of this type.
        /// </summary>
        public bool IsSliceMethod
        {
            get
            {
                if (Member is IMethod)
                {
                    if (Target.GetCode().ToString() == "this" && Member.DeclaringType != null && CodeGenerator.Method.DeclaringType != null && CodeGenerator.Method.DeclaringType.Is(Member.DeclaringType))
                    {
                        var method = (IMethod)Member;
                        if (method.IsConstructor)
                        {
                            return true;
                        }
                        else
                        {
                            var impl = method.GetImplementation(CodeGenerator.Method.DeclaringType);
                            return impl != null && !impl.Equals(Member);
                        }
                    }
                }
                return false;
            }
        }

        private CodeBuilder GetMemberCode()
        {
            if (Member is IMethod)
            {
                return ((IMethod)Member).CreateMemberBlock(CodeGenerator).GetCode();
            }
            else if (Member is IField)
            {
                return ((IField)Member).CreateMemberBlock(CodeGenerator).GetCode();
            }
            else
            {
                return new CodeBuilder(Member.Name);
            }
        }

        public CodeBuilder GetCode()
        {
            if (Target.Type.GetIsSingleton() && Target.Type.IsGlobalType())
            {
                if (Member is IMethod)
                {
                    return ((IMethod)Member).CreateBlock(CodeGenerator).GetCode();
                }
                else if (Member is IField)
                {
                    return ((IField)Member).CreateBlock(CodeGenerator).GetCode();
                }
            }

            CodeBuilder cb = new CodeBuilder();
            if (Target is IPointerBlock)
            {
                cb.Append(((IPointerBlock)Target).StaticDereference().GetOperandCode(this));
                cb.Append('.');
            }
            else
            {
                if (Target is DereferenceBlock)
                {
                    cb.Append(((DereferenceBlock)Target).Value.GetOperandCode(this));
                    cb.Append("->");
                }
                else
                {
                    cb.Append(Target.GetOperandCode(this));
                    if (Target.Type.IsExplicitPointer())
                    {
                        cb.Append("->");
                    }
                    else
                    {
                        cb.Append('.');
                    }
                }
            }

            if (IsSliceMethod)
            {
                cb.Append(Member.DeclaringType.CreateBlock(CodeGenerator).GetCode());
                cb.Append("::");
            }
            cb.Append(GetMemberCode());
            return cb;
        }

        public override string ToString()
        {
            return GetCode().ToString();
        }
    }
}
