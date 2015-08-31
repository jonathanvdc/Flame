using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class CppThis : CppVariableBase
    {
        public CppThis(ICodeGenerator CodeGenerator)
            : base(CodeGenerator)
        { }

        public override ICppBlock CreateBlock()
        {
            return new ThisBlock(CodeGenerator);
        }

        public override IType Type
        {
            get
            {
                var declType = CodeGenerator.Method.DeclaringType;
                return declType.MakeGenericType(declType.GenericParameters).MakePointerType(PointerKind.TransientPointer);
            }
        }
    }

    public class ThisBlock : ICppBlock
    {
        public ThisBlock(ICodeGenerator CodeGenerator)
        {
            this.CodeGenerator = CodeGenerator;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IType Type
        {
            get { return CodeGenerator.Method.DeclaringType.MakeGenericType(CodeGenerator.Method.DeclaringType.GenericParameters).MakePointerType(PointerKind.TransientPointer); }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Type.GetDependencies(); }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return new CppLocal[0]; }
        }

        public CodeBuilder GetCode()
        {
            return new CodeBuilder("this");
        }
    }
}
