using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public struct AnalysisToken
    {
        public AnalysisToken(int Index)
        {
            this = default(AnalysisToken);
            this.Index = Index;
        }

        public int Index { get; private set; }

        public override int GetHashCode()
        {
            return Index;
        }

        public override bool Equals(object obj)
        {
            if (obj is AnalysisToken)
            {
                return Index == ((AnalysisToken)obj).Index;
            }
            else
            {
                return false;
            }
        }

        public static bool operator ==(AnalysisToken Left, AnalysisToken Right)
        {
            return Left.Index == Right.Index;
        }
        public static bool operator !=(AnalysisToken Left, AnalysisToken Right)
        {
            return Left.Index != Right.Index;
        }
    }
}
