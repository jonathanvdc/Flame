using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Compiler;
using Flame.Wasm.Emit;

namespace Flame.Wasm
{
    /// <summary>
    /// A wasm-specific direct call expression.
    /// </summary>
    public class DirectCallExpression : IExpression, IMemberNode
    {
        public DirectCallExpression(IMethod Target, IType Type, IEnumerable<IExpression> Arguments)
        {
            this.Target = Target;
            this.Type = Type;
            this.Arguments = Arguments;
        }

        public IMethod Target { get; private set; }
        public IType Type { get; private set; }
        public IEnumerable<IExpression> Arguments { get; private set; }

        public bool IsConstantNode
        {
            get { return Target.GetIsConstant(); }
        }

        public IExpression Accept(INodeVisitor Visitor)
        {
            return new DirectCallExpression(Target, Type, Arguments.Select(Visitor.Visit).ToArray());
        }

        public IMemberNode ConvertMembers(MemberConverter Converter)
        {
            var convMethod = Converter.Convert(Target);
            var convRetType = Converter.Convert(Type);
            if (object.ReferenceEquals(Target, convMethod)
                && object.ReferenceEquals(Type, convRetType))
            {
                return this;
            }
            else
            {
                return new DirectCallExpression(convMethod, convRetType, Arguments);
            }
        }

        public IBoundObject Evaluate()
        {
            return null;
        }

        public IExpression Optimize()
        {
            return new DirectCallExpression(Target, Type, Arguments.OptimizeAll());
        }

        public ICodeBlock Emit(ICodeGenerator CodeGenerator)
        {
            var wasmCg = (WasmCodeGenerator)CodeGenerator;
            return new CallBlock(
                wasmCg,
                (WasmMethod)Target,
                Type,
                Arguments.EmitAll(CodeGenerator).Cast<CodeBlock>().ToArray());
        }
    }
}
