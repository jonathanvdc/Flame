using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class InvocationBlock : IAssemblerBlock
    {
        public InvocationBlock(ICodeGenerator CodeGenerator, IAssemblerBlock Target, params IAssemblerBlock[] Arguments)
        {
            this.CodeGenerator = CodeGenerator;
            this.Target = Target; 
            this.Arguments = Arguments;
        }
        public InvocationBlock(ICodeGenerator CodeGenerator, IAssemblerBlock Target, IEnumerable<IAssemblerBlock> Arguments)
        {
            this.CodeGenerator = CodeGenerator; 
            this.Target = Target; 
            this.Arguments = Arguments;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IAssemblerBlock Target { get; private set; }
        public IEnumerable<IAssemblerBlock> Arguments { get; private set; }

        public IMethod Method
        {
            get
            {
                return MethodType.GetMethod(Target.Type);
            }
        }

        public IType Type
        {
            get 
            {
                return Method.ReturnType;
            }
        }

        public IEnumerable<IStorageLocation> Emit(IAssemblerEmitContext Context)
        {
            List<IStorageLocation> locations = new List<IStorageLocation>();
            foreach (var item in Arguments)
            {
                locations.Add(item.EmitAndSpill(Context));
            }
            if (Target is MethodBlock)
            {
                return Context.EmitInvoke(Method, locations);
            }
            else
            {
                var jumpTarget = Target.EmitToRegister(Context);
                return Context.EmitInvoke(jumpTarget, ((IAssemblerMethod)Method).CallConvention, locations);
            }
        }
    }
}
