using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class LocalDeclarationBlock : ICppBlock
    {
        public LocalDeclarationBlock(ICodeGenerator CodeGenerator)
        {
            this.CodeGenerator = CodeGenerator;
            this.freelocals = new List<CppLocal>();
            this.assignments = new List<VariableAssignmentBlock>();
        }
        public LocalDeclarationBlock(ICodeGenerator CodeGenerator, CppLocal Local)
            : this(CodeGenerator)
        {
            Declare(Local);
        }

        private List<CppLocal> freelocals;
        private List<VariableAssignmentBlock> assignments;

        public bool IsDeclared(CppLocal Local)
        {
            for (int i = 0; i < freelocals.Count; i++)
            {
                if (Local == freelocals[i])
                {
                    return true;
                }
            }
            return false;
        }

        public void Declare(CppLocal Local)
        {
            if (!IsDeclared(Local))
            {
                this.freelocals.Add(Local);
            }
        }
        public void Assign(CppLocal Local, ICppBlock Value)
        {
            Assign(new VariableAssignmentBlock(new LocalBlock(Local), Value));
        }
        public void Assign(VariableAssignmentBlock AssignmentBlock)
        {
            if (AssignmentBlock.Target is LocalBlock)
            {
                var local = (LocalBlock)AssignmentBlock.Target;
                for (int i = 0; i < freelocals.Count; i++)
                {
                    if (local.Local == freelocals[i])
                    {
                        freelocals.RemoveAt(i);
                        break;
                    }
                }
            }
            this.assignments.Add(AssignmentBlock);
        }

        public ICodeGenerator CodeGenerator { get; private set; }

        public IType LocalType
        {
            get
            {
                if (freelocals.Count > 0)
                {
                    return freelocals[0].Type;
                }
                else
                {
                    return assignments.First().Target.Type;
                }
            }
        }

        public IType Type
        {
            get { return PrimitiveTypes.Void; }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return LocalType.GetDependencies().MergeDependencies(assignments.SelectMany((item) => item.Dependencies)); }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return freelocals.Concat(assignments.SelectMany((item) => item.LocalsUsed)).Distinct(); }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.Append(CodeGenerator.GetTypeNamer().Name(LocalType, CodeGenerator));
            cb.Append(" ");
            bool first = true;
            foreach (var item in freelocals)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    cb.Append(", ");
                }
                cb.Append(item.Member.Name);
            }
            foreach (var item in assignments)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    cb.Append(", ");
                }
                if (item.Value is INewObjectBlock)
                {
                    var initBlock = (INewObjectBlock)item.Value;
                    if (initBlock.Kind == AllocationKind.Stack || initBlock.Kind == AllocationKind.MakeManaged)
                    {
                        cb.Append(item.Target.GetCode());
                        cb.Append(initBlock.GetArgumentListCode());
                        continue;
                    }
                }
                cb.Append(item.GetCode());
            }
            cb.Append(';');
            return cb;
        }
    }
}
