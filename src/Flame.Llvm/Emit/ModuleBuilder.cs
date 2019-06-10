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
        public ModuleBuilder(
            LLVMModuleRef module,
            TypeEnvironment typeSystem,
            NameMangler mangler)
        {
            this.Module = module;
            this.TypeSystem = typeSystem;
            this.Mangler = mangler;
            this.methodDecls = new Dictionary<IMethod, LLVMValueRef>();
            this.importCache = new Dictionary<IType, LLVMTypeRef>();
            this.fieldIndices = new Dictionary<IType, Dictionary<IField, int>>();
            this.baseIndices = new Dictionary<IType, Dictionary<IType, int>>();
        }

        public LLVMModuleRef Module { get; private set; }

        public TypeEnvironment TypeSystem { get; private set; }

        public NameMangler Mangler { get; private set; }

        public LLVMContextRef Context => LLVM.GetModuleContext(Module);

        private Dictionary<IMethod, LLVMValueRef> methodDecls;
        private Dictionary<IType, LLVMTypeRef> importCache;
        private Dictionary<IType, Dictionary<IField, int>> fieldIndices;
        private Dictionary<IType, Dictionary<IType, int>> baseIndices;

        public LLVMValueRef DeclareMethod(IMethod method)
        {
            LLVMValueRef result;
            if (methodDecls.TryGetValue(method, out result))
            {
                return result;
            }

            var externAttr = method.Attributes.GetOrNull(ExternAttribute.AttributeType);
            if (externAttr == null)
            {
                result = DeclareLocal(method);
            }
            else
            {
                result = DeclareExtern(method, (ExternAttribute)externAttr);
            }

            methodDecls[method] = result;
            return result;
        }

        private LLVMValueRef DeclareLocal(IMethod method)
        {
            var paramTypes = new List<LLVMTypeRef>();
            if (!method.IsStatic)
            {
                paramTypes.Add(ImportType(method.ParentType.MakePointerType(PointerKind.Reference)));
            }
            paramTypes.AddRange(method.Parameters.Select(p => ImportType(p.Type)));
            var funType = LLVM.FunctionType(
                ImportType(method.ReturnParameter.Type),
                paramTypes.ToArray(),
                false);
            return LLVM.AddFunction(Module, Mangler.Mangle(method, true), funType);
        }

        private LLVMValueRef DeclareExtern(IMethod method, ExternAttribute externAttribute)
        {
            var funType = LLVM.FunctionType(
                ImportType(method.ReturnParameter.Type),
                method.Parameters.Select(p => ImportType(p.Type)).ToArray(),
                false);
            return LLVM.AddFunction(
                Module,
                externAttribute.ImportNameOrNull ?? CMangler.Instance.Mangle(method, false),
                funType);
        }

        public void DefineMethod(IMethod method, MethodBody body)
        {
            var fun = DeclareMethod(method);
            var emitter = new MethodBodyEmitter(this, fun);
            emitter.Emit(body);
        }

        public void SynthesizeMain(IMethod entryPoint)
        {
            var retType = entryPoint.ReturnParameter.Type;
            bool syntheticRet = retType == TypeSystem.Void;
            if (retType == TypeSystem.Void)
            {
                retType = TypeSystem.Int32;
            }

            var mainSignature = LLVM.FunctionType(
                ImportType(retType),
                new[]
                {
                    LLVM.Int32TypeInContext(Context),
                    LLVM.PointerType(LLVM.PointerType(LLVM.Int8TypeInContext(Context), 0), 0)
                },
                false);

            var mainFunc = LLVM.AddFunction(Module, "main", mainSignature);
            using (var builder = new IRBuilder(Context))
            {
                builder.PositionBuilderAtEnd(mainFunc.AppendBasicBlock("entry"));
                var call = builder.CreateCall(DeclareMethod(entryPoint), new LLVMValueRef[] { }, "");
                if (syntheticRet)
                {
                    builder.CreateRet(LLVM.ConstInt(ImportType(retType), 0, false));
                }
                else
                {
                    builder.CreateRet(call);
                }
            }
        }

        public LLVMTypeRef ImportType(IType type)
        {
            LLVMTypeRef result;
            if (importCache.TryGetValue(type, out result))
            {
                return result;
            }
            else
            {
                return importCache[type] = ImportTypeImpl(type);
            }
        }

        public int GetFieldIndex(IField field)
        {
            ImportType(field.ParentType);
            return fieldIndices[field.ParentType][field];
        }

        private LLVMTypeRef ImportTypeImpl(IType type)
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
                var result = LLVM.StructCreateNamed(Context, Mangler.Mangle(type, true));
                importCache[type] = result;
                var fieldTypes = new List<LLVMTypeRef>();
                var fieldNumbering = new Dictionary<IField, int>();
                var baseNumbering = new Dictionary<IType, int>();
                foreach (var baseType in type.BaseTypes)
                {
                    var importedBase = ImportType(baseType);
                    // TODO: eliminate interface types from field list.
                    baseNumbering[baseType] = fieldTypes.Count;
                    fieldTypes.Add(importedBase);
                }
                foreach (var field in type.Fields.Where(f => !f.IsStatic))
                {
                    fieldNumbering[field] = fieldTypes.Count;
                    fieldTypes.Add(ImportType(field.FieldType));
                }
                fieldIndices[type] = fieldNumbering;
                baseIndices[type] = baseNumbering;
                LLVM.StructSetBody(result, fieldTypes.ToArray(), false);
                return result;
            }
        }
    }
}
