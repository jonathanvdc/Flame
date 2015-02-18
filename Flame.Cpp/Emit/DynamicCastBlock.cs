using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class DynamicCastBlock : ICppBlock
    {
        public DynamicCastBlock(ICppBlock Value, IType Type)
        {
            this.Value = Value;
            this.Type = Type;
        }

        public ICppBlock Value { get; private set; }
        public IType Type { get; private set; }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Value.Dependencies.MergeDependencies(Type.GetDependencies()); }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Value.LocalsUsed; }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Value.CodeGenerator; }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            if (Type.AsContainerType().AsPointerType().PointerKind.Equals(PointerKind.ReferencePointer))
            {
                cb.Append("std::dynamic_pointer_cast<");
                cb.Append(Type.AsContainerType().GetElementType().CreateBlock(CodeGenerator).GetCode());
                cb.Append(">(");
                cb.Append(Value.GetCode());
                cb.Append(")");
            }
            else
            {
                cb.Append("dynamic_cast<");
                cb.Append(Type.CreateBlock(CodeGenerator).GetCode());
                cb.Append(">(");
                cb.Append(Value.GetCode());
                cb.Append(")");
            }
            return cb;
        }
    }
}
