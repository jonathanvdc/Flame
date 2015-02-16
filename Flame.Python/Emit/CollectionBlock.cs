using Flame.Compiler;
using Flame.Compiler.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python.Emit
{
    public interface IPythonCollectionBlock : ICollectionBlock, IPythonBlock
    {
        IPythonBlock Collection { get; }
        IVariable GetElementVariable();
    }

    public interface IPythonIndexedCollectionBlock : IPythonCollectionBlock
    {
        IVariable GetElementVariable(IVariable Index);
        IVariable GetIndexVariable();
        IPythonBlock GetLengthExpression();
    }

    public class CollectionBlock : IPythonCollectionBlock
    {
        public CollectionBlock(ICodeGenerator CodeGenerator, IVariableMember Member, IPythonBlock Collection)
        {
            this.CodeGenerator = CodeGenerator;
            this.Member = Member;
            this.Collection = Collection;
        }

        public IVariableMember Member { get; private set; }
        public IPythonBlock Collection { get; private set; }
        public ICodeGenerator CodeGenerator { get; private set; }

        public IType Type
        {
            get { return Collection.Type.GetEnumerableElementType(); }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.Append(Member.Name);
            cb.Append(" in ");
            cb.Append(Collection.GetCode());
            return cb;
        }

        public IVariable GetElementVariable()
        {
            return CodeGenerator.DeclareVariable(this.Member);
        }

        public IEnumerable<ModuleDependency> GetDependencies()
        {
            return Collection.GetDependencies();
        }
    }

    public class ListCollectionBlock : IPythonIndexedCollectionBlock
    {
        public ListCollectionBlock(ICodeGenerator CodeGenerator, IPythonBlock List)
        {
            this.CodeGenerator = CodeGenerator;
            this.List = List;
        }

        private PythonVariableBase indexVariable;
        public PythonVariableBase IndexVariable
        {
            get
            {
                if (indexVariable == null)
                {
                    indexVariable = (PythonVariableBase)CodeGenerator.DeclareVariable(PrimitiveTypes.Int32);
                }
                return indexVariable;
            }
        }
        public IVariableMember Member
        {
            get
            {
                return new DescribedVariableMember(indexVariable.GetCode().ToString(), PrimitiveTypes.Int32);
            }
        }
        public IPythonBlock List { get; private set; }
        public IPythonBlock Collection
        {
            get
            {
                // range(len(<list>))
                var range = new PythonIdentifierBlock(CodeGenerator, "range", PythonObjectType.Instance);
                var rangeCall = new InvocationBlock(CodeGenerator, range, 
                    new IPythonBlock[] { GetLengthExpression() }, 
                    PythonIterableType.Instance);
                return rangeCall;
            }
        }
        public ICodeGenerator CodeGenerator { get; private set; }

        public IType Type
        {
            get { return Collection.Type.GetEnumerableElementType(); }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.Append(Member.Name);
            cb.Append(" in ");
            cb.Append(Collection.GetCode());
            return cb;
        }

        public IVariable GetElementVariable()
        {
            return GetElementVariable(null);
        }

        public IVariable GetElementVariable(IVariable Index)
        {
            if (Index != null)
            {
                indexVariable = (PythonVariableBase)Index;
            }
            return new PythonIndexedReleaseVariable(CodeGenerator, List,
                new IPythonBlock[] { (IPythonBlock)IndexVariable.CreateGetExpression().Emit(CodeGenerator) },
                IndexVariable.CreateReleaseStatement());
        }

        public IPythonBlock GetLengthExpression()
        {
            var len = new PythonIdentifierBlock(CodeGenerator, "len", PythonObjectType.Instance);
            var lenCall = new InvocationBlock(CodeGenerator, len, new IPythonBlock[] { List }, PrimitiveTypes.Int32);
            return lenCall;
        }

        public IVariable GetIndexVariable()
        {
            return IndexVariable;
        }

        public IEnumerable<ModuleDependency> GetDependencies()
        {
            return List.GetDependencies();
        }
    }
}
