using Flame.Compiler;
using Flame.Compiler.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python.Emit
{
    public class ForeachBlockGenerator : BlockGenerator, IForeachBlockGenerator
    {
        public ForeachBlockGenerator(ICodeGenerator CodeGenerator, IEnumerable<IPythonCollectionBlock> CollectionBlocks)
            : base(CodeGenerator)
        {
            var collBlocks = CollectionBlocks.ToArray();
            var elemDict = new Dictionary<IPythonCollectionBlock, IVariable>();
            var elems = new List<IVariable>();
            this.indexedBlocks = new List<IPythonIndexedCollectionBlock>();
            for (int i = 0; i < collBlocks.Length; i++)
            {
                if (collBlocks[i] is IPythonIndexedCollectionBlock)
                {
                    var indexedCollection = (IPythonIndexedCollectionBlock)collBlocks[i];
                    if (indexVariable == null)
                    {
                        indexVariable = (PythonVariableBase)indexedCollection.GetIndexVariable();
                    }
                    indexedBlocks.Add(indexedCollection);
                    elems.Add(indexedCollection.GetElementVariable(indexVariable));
                }
                else
                {
                    var elemVar = collBlocks[i].GetElementVariable();
                    elemDict[collBlocks[i]] = elemVar;
                    elems.Add(elemVar);
                }
            }
            this.Elements = elems;
            this.collectionMap = elemDict;
        }

        private List<IPythonIndexedCollectionBlock> indexedBlocks;
        private PythonVariableBase indexVariable;
        private IReadOnlyDictionary<IPythonCollectionBlock, IVariable> collectionMap;

        public IReadOnlyList<IVariable> Elements { get; private set; }

        public IReadOnlyList<IVariable> IndexVariables
        {
            get { return indexVariable != null ? new IVariable[] { indexVariable } : new IVariable[0]; }
        }
        public IEnumerable<IPythonCollectionBlock> CollectionBlocks
        {
            get { return indexedBlocks.Concat(collectionMap.Keys); }
        }
        public IEnumerable<IPythonBlock> GetCollections()
        {
            var normalCollections = collectionMap.Keys.Select((item) => item.Collection);
            if (indexedBlocks.Count == 0)
            {
                return normalCollections;
            }
            else
            {
                return new IPythonBlock[] { GetMinRange(indexedBlocks.Select((item) => item.GetLengthExpression())) }.Concat(normalCollections);
            }
        }
        public IEnumerable<string> LoopVariableNames
        {
            get
            {
                var regularNames = collectionMap.Keys.Select((item) => item.Member.Name);
                var indexNames = IndexVariables.Select((item) => ((PythonVariableBase)item).GetCode().ToString());
                return indexNames.Concat(regularNames);
            }
        }

        public static IPythonBlock ZipCollections(IEnumerable<IPythonBlock> Collections)
        {
            var first = Collections.FirstOrDefault();
            if (first == null)
            {
                return null;
            }
            else if (!Collections.Skip(1).Any())
            {
                return first;
            }
            else
            {
                var zipMethod = new PythonIdentifierBlock(first.CodeGenerator, "zip", PythonObjectType.Instance);
                return new InvocationBlock(first.CodeGenerator, zipMethod, Collections.ToArray(), PythonIterableType.Instance);
            }
        }

        public static IPythonBlock GetMin(IEnumerable<IPythonBlock> Values)
        {
            var first = Values.FirstOrDefault();
            if (first == null)
            {
                return null;
            }
            else if (!Values.Skip(1).Any())
            {
                return first;
            }
            else
            {
                var minMethod = new PythonIdentifierBlock(first.CodeGenerator, "min", PythonObjectType.Instance);
                return new InvocationBlock(first.CodeGenerator, minMethod, Values.ToArray(), PrimitiveTypes.Int32);
            }
        }

        public static IPythonBlock GetMinRange(IEnumerable<IPythonBlock> Values)
        {
            var first = Values.FirstOrDefault();
            if (first == null)
            {
                return null;
            }
            var rangeMethod = new PythonIdentifierBlock(first.CodeGenerator, "range", PythonObjectType.Instance);
            return new InvocationBlock(first.CodeGenerator, rangeMethod, new IPythonBlock[] { GetMin(Values) }, PythonIterableType.Instance);
        }

        public override CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.Append("for ");
            bool first = true;
            foreach (string name in LoopVariableNames)
            {
                if (!first)
                {
                    cb.Append(", ");
                }
                else
                {
                    first = false;
                }

                cb.Append(name);
            }
            cb.Append(" in ");

            var zipped = ZipCollections(GetCollections());
            cb.Append(zipped.GetCode());
            cb.Append(":");
            cb.AppendLine();
            cb.IncreaseIndentation();
            cb.AddCodeBuilder(GetBlockCode(true));
            cb.DecreaseIndentation();
            return cb;
        }

        public override IEnumerable<ModuleDependency> GetDependencies()
        {
            return CollectionBlocks.GetDependencies().Union(base.GetDependencies());
        }
    }
}
