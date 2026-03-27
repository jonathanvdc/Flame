using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Flame;
using Flame.Collections;
using Flame.Compiler;
using Flame.Compiler.Flow;
using Flame.Compiler.Instructions;
using Flame.Compiler.Pipeline;
using Flame.Constants;
using Flame.Llvm;
using Flame.Llvm.Emit;
using Flame.TypeSystem;
using LLVMSharp.Interop;
using UnitTests.Flame.Clr;

namespace UnitTests.Flame.Llvm
{
    internal static class LlvmBackendSupport
    {
        public static MethodBody CreateConstantBody(global::Flame.IType type, IntegerConstant value)
        {
            var graph = new FlowGraphBuilder();
            var result = graph.EntryPoint.AppendInstruction(
                Instruction.CreateConstant(value, type));
            graph.EntryPoint.Flow = new ReturnFlow(Instruction.CreateCopy(type, result));
            return new MethodBody(
                new Parameter(type),
                default(Parameter),
                EmptyArray<Parameter>.Value,
                graph.ToImmutable());
        }

        public static MethodBody CreateLocalRoundTripBody(global::Flame.IType type, IntegerConstant value)
        {
            var graph = new FlowGraphBuilder();
            var local = graph.EntryPoint.AppendInstruction(Instruction.CreateAlloca(type), "local");
            var constant = graph.EntryPoint.AppendInstruction(Instruction.CreateConstant(value, type), "value");
            graph.EntryPoint.AppendInstruction(Instruction.CreateStore(type, local, constant), "store");
            var loaded = graph.EntryPoint.AppendInstruction(Instruction.CreateLoad(type, local), "loaded");
            graph.EntryPoint.Flow = new ReturnFlow(Instruction.CreateCopy(type, loaded));
            return new MethodBody(
                new Parameter(type),
                default(Parameter),
                EmptyArray<Parameter>.Value,
                graph.ToImmutable());
        }

        public static MethodBody CreateSelectBody(global::Flame.IType type)
        {
            var graph = new FlowGraphBuilder();
            var flag = graph.EntryPoint.AppendParameter(type, "flag");
            var thenBlock = graph.AddBasicBlock("then");
            var elseBlock = graph.AddBasicBlock("else");
            var mergeBlock = graph.AddBasicBlock("merge");
            var mergeValue = mergeBlock.AppendParameter(type, "selected");

            graph.EntryPoint.Flow = SwitchFlow.CreateIfElse(
                Instruction.CreateCopy(type, flag.Tag),
                new Branch(thenBlock.Tag),
                new Branch(elseBlock.Tag));

            var thenConstant = thenBlock.AppendInstruction(
                Instruction.CreateConstant(new IntegerConstant(11), type),
                "then_value");
            thenBlock.Flow = new JumpFlow(mergeBlock.Tag, new[] { thenConstant.Tag });

            var elseConstant = elseBlock.AppendInstruction(
                Instruction.CreateConstant(new IntegerConstant(22), type),
                "else_value");
            elseBlock.Flow = new JumpFlow(mergeBlock.Tag, new[] { elseConstant.Tag });

            mergeBlock.Flow = new ReturnFlow(Instruction.CreateCopy(type, mergeValue.Tag));
            return new MethodBody(
                new Parameter(type),
                default(Parameter),
                new[] { new Parameter(type, "flag") },
                graph.ToImmutable());
        }

        public static MethodBody CreateFieldLoadBody(global::Flame.IType parentType, IField field, global::Flame.IType resultType)
        {
            var graph = new FlowGraphBuilder();
            var thisParam = graph.EntryPoint.AppendParameter(parentType.MakePointerType(PointerKind.Reference), "this");
            var fieldPtr = graph.EntryPoint.AppendInstruction(Instruction.CreateGetFieldPointer(field, thisParam.Tag), "field_ptr");
            var loaded = graph.EntryPoint.AppendInstruction(Instruction.CreateLoad(resultType, fieldPtr), "loaded");
            graph.EntryPoint.Flow = new ReturnFlow(Instruction.CreateCopy(resultType, loaded));
            return new MethodBody(
                new Parameter(resultType),
                new Parameter(parentType.MakePointerType(PointerKind.Reference), "this"),
                EmptyArray<Parameter>.Value,
                graph.ToImmutable());
        }

        public static MethodBody CreateStaticCallBody(IMethod callee)
        {
            var graph = new FlowGraphBuilder();
            var call = graph.EntryPoint.AppendInstruction(
                Instruction.CreateCall(callee, MethodLookup.Static, EmptyArray<ValueTag>.Value),
                "call");
            graph.EntryPoint.Flow = new ReturnFlow(Instruction.CreateCopy(callee.ReturnParameter.Type, call));
            return new MethodBody(
                new Parameter(callee.ReturnParameter.Type),
                default(Parameter),
                EmptyArray<Parameter>.Value,
                graph.ToImmutable());
        }

        public static MethodBody CreateIdentityBody(global::Flame.IType type)
        {
            var graph = new FlowGraphBuilder();
            var value = graph.EntryPoint.AppendParameter(type, "value");
            graph.EntryPoint.Flow = new ReturnFlow(Instruction.CreateCopy(type, value.Tag));
            return new MethodBody(
                new Parameter(type),
                default(Parameter),
                new[] { new Parameter(type, "value") },
                graph.ToImmutable());
        }

        public static MethodBody CreateVoidBody(global::Flame.IType declaringType, global::Flame.IType voidType)
        {
            var graph = new FlowGraphBuilder();
            graph.EntryPoint.Flow = new ReturnFlow(Instruction.CreateDefaultConstant(voidType));
            return new MethodBody(
                new Parameter(voidType),
                new Parameter(declaringType.MakePointerType(PointerKind.Reference), "this"),
                EmptyArray<Parameter>.Value,
                graph.ToImmutable());
        }

        public static MethodBody CreateDelegateCallBody(global::Flame.IType delegateType, IMethod callee, global::Flame.IType argumentType)
        {
            var graph = new FlowGraphBuilder();
            var value = graph.EntryPoint.AppendParameter(argumentType, "value");
            var delegateValue = graph.EntryPoint.AppendInstruction(
                Instruction.CreateNewDelegate(delegateType.MakePointerType(PointerKind.Box), callee, null, MethodLookup.Static),
                "delegate");
            var call = graph.EntryPoint.AppendInstruction(
                Instruction.CreateIndirectCall(
                    callee.ReturnParameter.Type,
                    new[] { argumentType },
                    delegateValue,
                    new[] { value.Tag }),
                "call");
            graph.EntryPoint.Flow = new ReturnFlow(Instruction.CreateCopy(callee.ReturnParameter.Type, call));
            return new MethodBody(
                new Parameter(callee.ReturnParameter.Type),
                default(Parameter),
                new[] { new Parameter(argumentType, "value") },
                graph.ToImmutable());
        }

        public static MethodBody CreateVirtualCallBody(IMethod constructor, IMethod callee, global::Flame.IType receiverType)
        {
            var graph = new FlowGraphBuilder();
            var instance = graph.EntryPoint.AppendInstruction(
                Instruction.CreateNewObject(constructor, EmptyArray<ValueTag>.Value),
                "instance");
            var receiver = graph.EntryPoint.AppendInstruction(
                Instruction.CreateReinterpretCast(receiverType.MakePointerType(PointerKind.Box), instance),
                "receiver");
            var call = graph.EntryPoint.AppendInstruction(
                Instruction.CreateCall(callee, MethodLookup.Virtual, receiver, EmptyArray<ValueTag>.Value),
                "call");
            graph.EntryPoint.Flow = new ReturnFlow(Instruction.CreateCopy(callee.ReturnParameter.Type, call));
            return new MethodBody(
                new Parameter(callee.ReturnParameter.Type),
                default(Parameter),
                EmptyArray<Parameter>.Value,
                graph.ToImmutable());
        }

        public static MethodBody CreateIntegerSwitchBody(global::Flame.IType type)
        {
            var graph = new FlowGraphBuilder();
            var value = graph.EntryPoint.AppendParameter(type, "value");
            var caseOne = graph.AddBasicBlock("case.one");
            var caseTwo = graph.AddBasicBlock("case.two");
            var caseDefault = graph.AddBasicBlock("case.default");

            graph.EntryPoint.Flow = new SwitchFlow(
                Instruction.CreateCopy(type, value.Tag),
                new[]
                {
                    new SwitchCase(new IntegerConstant(1), new Branch(caseOne.Tag)),
                    new SwitchCase(new IntegerConstant(2), new Branch(caseTwo.Tag))
                },
                new Branch(caseDefault.Tag));

            caseOne.Flow = new ReturnFlow(Instruction.CreateConstant(new IntegerConstant(10), type));
            caseTwo.Flow = new ReturnFlow(Instruction.CreateConstant(new IntegerConstant(20), type));
            caseDefault.Flow = new ReturnFlow(Instruction.CreateConstant(new IntegerConstant(30), type));

            return new MethodBody(
                new Parameter(type),
                default(Parameter),
                new[] { new Parameter(type, "value") },
                graph.ToImmutable());
        }

        public static LLVMValueRef CompileMethod(
            IAssembly assembly,
            global::Flame.IType declaringType,
            DescribedBodyMethod method,
            IEnumerable<global::Flame.IType> extraTypes = null,
            IEnumerable<ITypeMember> extraMembers = null,
            IDictionary<IMethod, MethodBody> extraBodies = null)
        {
            var mangler = new ItaniumMangler(LocalTypeResolutionTests.Corlib.Resolver.TypeEnvironment);
            var types = new List<global::Flame.IType> { declaringType };
            if (extraTypes != null)
            {
                types.AddRange(extraTypes);
            }
            var members = new List<ITypeMember> { method };
            if (extraMembers != null)
            {
                members.AddRange(extraMembers);
            }
            var bodies = new Dictionary<IMethod, MethodBody> { { method, method.Body } };
            if (extraBodies != null)
            {
                foreach (var pair in extraBodies)
                {
                    bodies[pair.Key] = pair.Value;
                }
            }
            var moduleBuilder = CompileModule(assembly, types, members, bodies, null);
            return moduleBuilder.Module.GetNamedFunction(mangler.Mangle(method, true));
        }

        public static ModuleBuilder CompileModule(
            IAssembly assembly,
            IEnumerable<global::Flame.IType> types,
            IEnumerable<ITypeMember> members,
            IDictionary<IMethod, MethodBody> bodies,
            IMethod entryPoint)
        {
            var typeSystem = LocalTypeResolutionTests.Corlib.Resolver.TypeEnvironment;
            var mangler = new ItaniumMangler(typeSystem);
            var contents = new AssemblyContentDescription(
                assembly.FullName,
                AttributeMap.Empty,
                ImmutableHashSet.CreateRange(types),
                ImmutableHashSet.CreateRange(members),
                new Dictionary<IMethod, MethodBody>(bodies),
                entryPoint);
            return LlvmBackend.Compile(contents, typeSystem, mangler);
        }

        public static unsafe bool ContainsInstruction(LLVMValueRef function, Func<LLVMValueRef, bool> predicate)
        {
            for (var block = LLVM.GetFirstBasicBlock(function);
                block != null;
                block = LLVM.GetNextBasicBlock(block))
            {
                for (var instruction = LLVM.GetFirstInstruction(block);
                    instruction != null;
                    instruction = LLVM.GetNextInstruction(instruction))
                {
                    if (predicate(new LLVMValueRef((IntPtr)instruction)))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
