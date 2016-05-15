using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class LabelManager
    {
        public LabelManager()
        {
            this.labels = new List<IAssemblerLabel>();
        }

        private List<IAssemblerLabel> labels;

        #region Declare Label

        public IAssemblerLabel DeclareLabel(string Name)
        {
            return DeclareLabel(Name, false);
        }

        public IAssemblerLabel DeclareLabel(string Name, bool PreferSuffix)
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                return DeclareLabel("label_", PreferSuffix);
            }
            else if (labels.Any((item) => item.Identifier == Name) || PreferSuffix)
            {
                int index = PreferSuffix ? 0 : 1;
                while (labels.Any((item) => item.Identifier == Name + index))
                {
                    index++;
                }
                var lbl = new TextLabel(Name + index);
                labels.Add(lbl);
                return lbl;
            }
            else
            {
                var lbl = new TextLabel(Name);
                labels.Add(lbl);
                return lbl;
            }
        }

        public IAssemblerLabel DeclareLabel(string Prefix, string Name)
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                return DeclareLabel(Prefix, "label_", true);
            }
            else
            {
                return DeclareLabel(Prefix, Name, false);
            }
        }

        public IAssemblerLabel DeclareLabel(string Prefix, string Name, bool PreferSuffix)
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                return DeclareLabel(Prefix, "label_", PreferSuffix);
            }
            else if (string.IsNullOrWhiteSpace(Prefix))
            {
                return DeclareLabel(Name, PreferSuffix);
            }
            else
            {
                return DeclareLabel(Prefix + "_" + Name, PreferSuffix);
            }
        }

        public IAssemblerLabel DeclareLabel(IMethod Method)
        {
            return DeclareLabel(Method.Name.ToString());
        }

        #endregion
    }
}
