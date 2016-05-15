using Flame.MIPS.Emit;
using Flame.MIPS.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS
{
    public interface IAssemblerState
    {
        StaticDataSection DataSection { get; }
        LabelManager Labels { get; }
        IStaticDataItem GetStaticStorage(IAssemblerType Type);
    }

    public static class AssemblerStateExtensions
    {
        public static IStaticDataItem Allocate(this IAssemblerState State, IType Type)
        {
            var item = new StaticInstanceItem(State.Labels.DeclareLabel(Type.Name.ToString(), true), Type);
            State.DataSection.AddItem(item);
            return item;
        }
    }

    public class GlobalAssemblerState : IAssemblerState
    {
        public GlobalAssemblerState(LabelManager Labels)
        {
            this.DataSection = new StaticDataSection();
            this.Labels = Labels;
            this.staticTypeStorage = new Dictionary<IAssemblerType, IStaticDataItem>();
        }
        public GlobalAssemblerState()
            : this(new LabelManager())
        { }

        public StaticDataSection DataSection { get; private set; }
        public LabelManager Labels { get; private set; }
        private Dictionary<IAssemblerType, IStaticDataItem> staticTypeStorage;

        public IStaticDataItem GetStaticStorage(IAssemblerType Type)
        {
            if (!staticTypeStorage.ContainsKey(Type))
            {
                staticTypeStorage[Type] = new StaticTypeItem(Labels.DeclareLabel(Type.Name + "_Static"), Type);
            }
            return staticTypeStorage[Type];
        }
    }

    public class LocalAssemblerState : IAssemblerState
    {
        public LocalAssemblerState(IAssemblerState ParentState)
        {
            this.ParentState = ParentState;
            this.DataSection = DataSection;
        }

        public IAssemblerState ParentState { get; private set; }
        public StaticDataSection DataSection { get; private set; }

        public LabelManager Labels
        {
            get { return ParentState.Labels; }
        }

        public IStaticDataItem GetStaticStorage(IAssemblerType Type)
        {
            return ParentState.GetStaticStorage(Type);
        }
    }
}
