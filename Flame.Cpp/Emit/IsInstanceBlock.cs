using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class IsInstanceBlock : CompositeBlockBase
    {
        public IsInstanceBlock(CppCodeGenerator CppCodeGenerator, ICppBlock Value, IType TestType)
        {
            this.CppCodeGenerator = CppCodeGenerator;
            this.Value = Value;
            this.TestType = TestType;
        }

        public CppCodeGenerator CppCodeGenerator { get; private set; }
        public ICppBlock Value { get; private set; }
        public IType TestType { get; private set; }

        public PointerKind ValuePointerKind
        {
            get
            {
                return Value.Type.AsContainerType().AsPointerType().PointerKind;
            }
        }
        public IType ValueElementType
        {
            get
            {
                return Value.Type.AsContainerType().AsPointerType().GetElementType();
            }
        }
        public IType TestElementType
        {
            get
            {
                if (TestType.get_IsPointer())
                {
                    return TestType.AsContainerType().AsPointerType().GetElementType();
                }
                else
                {
                    return TestType;
                }
            }
        }

        public bool UseVerboseCheck
        {
            get
            {
                return CppCodeGenerator.Environment.UseVerboseTypeChecks();
            }
        }

        public override IType Type
        {
            get { return PrimitiveTypes.Boolean; }
        }

        public override IEnumerable<IHeaderDependency> Dependencies
        {
            get
            {
                var baseDeps = Value.Dependencies.MergeDependencies(TestElementType.GetDependencies());
                if (!UseVerboseCheck)
                {
                    return baseDeps.MergeDependencies(new IHeaderDependency[] { Plugs.IsInstanceHeader.Instance });
                }
                else
                {
                    return baseDeps;
                }
            }
        }

        public override ICodeGenerator CodeGenerator
        {
            get
            {
                return CppCodeGenerator;
            }
        }

        public override IEnumerable<CppLocal> LocalsUsed
        {
            get { return Value.LocalsUsed; }
        }

        public override ICppBlock Simplify()
        {
            if (Value.Type.Is(TestType))
            {
                return (ICppBlock)CppCodeGenerator.EmitBinary(Value, CppCodeGenerator.EmitNull(), Operator.CheckInequality);
            }
            else if (UseVerboseCheck)
            {
                return (ICppBlock)CppCodeGenerator.EmitBinary(CppCodeGenerator.EmitConversion(Value, TestType), CppCodeGenerator.EmitNull(), Operator.CheckInequality);
            }
            else
            {
                var typeArgBlocks = new ICppBlock[] { TestElementType.CreateBlock(CppCodeGenerator) };
                var isinstMethod = CppPrimitives.GetIsInstanceMethod(ValuePointerKind);
                var genIsInstMethod = isinstMethod.MakeGenericMethod(new IType[] { TestElementType, ValueElementType });
                var methodBlock = new RetypedBlock(new TypeArgumentBlock(isinstMethod.CreateBlock(CppCodeGenerator), typeArgBlocks), MethodType.Create(genIsInstMethod));

                return (ICppBlock)CppCodeGenerator.EmitInvocation(methodBlock, new ICodeBlock[] { Value });
            }
        }
    }
}
