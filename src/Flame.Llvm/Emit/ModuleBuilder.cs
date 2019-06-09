using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Compiler;
using Flame.TypeSystem;
using LLVMSharp;

namespace Flame.Llvm.Emit
{
    internal sealed class ModuleBuilder
    {
        public ModuleBuilder(LLVMModuleRef module, TypeEnvironment typeSystem)
        {
            this.Module = module;
            this.TypeSystem = typeSystem;
            this.methodDecls = new Dictionary<IMethod, LLVMValueRef>();
        }

        public LLVMModuleRef Module { get; private set; }

        public TypeEnvironment TypeSystem { get; private set; }

        public LLVMContextRef Context => LLVM.GetModuleContext(Module);

        private Dictionary<IMethod, LLVMValueRef> methodDecls;

        public LLVMValueRef DeclareMethod(IMethod method)
        {
            LLVMValueRef result;
            if (methodDecls.TryGetValue(method, out result))
            {
                return result;
            }

            var funType = LLVM.FunctionType(
                ImportType(method.ReturnParameter.Type),
                method.Parameters.Select(p => ImportType(p.Type)).ToArray(),
                false);

            var fun = LLVM.AddFunction(Module, method.Name.ToString(), funType);
            methodDecls[method] = fun;
            return result;
        }

        public void DefineMethod(IMethod method, MethodBody body)
        {
            var fun = DeclareMethod(method);
            var emitter = new MethodBodyEmitter(this, fun);
            emitter.Emit(body);
        }

        public LLVMTypeRef ImportType(IType type)
        {
            var intSpec = type.GetIntegerSpecOrNull();
            if (intSpec != null)
            {
                return LLVM.IntTypeInContext(Context, (uint)intSpec.Size);
            }
            else if (type is PointerType)
            {
                var elemType = ((PointerType)type).ElementType;
                if (elemType == TypeSystem.Void)
                {
                    return LLVM.PointerType(LLVM.Int8TypeInContext(Context), 0);
                }
                else
                {
                    return LLVM.PointerType(ImportType(elemType), 0);
                }
            }
            else if (type == TypeSystem.Void)
            {
                return LLVM.VoidTypeInContext(Context);
            }
            else
            {
                throw new NotSupportedException($"Cannot import type '{type.FullName}'.");
            }
        }
    }
}
