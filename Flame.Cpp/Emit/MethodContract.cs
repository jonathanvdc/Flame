using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class MethodContract
    {
        public MethodContract()
        {
            this.Preconditions = new List<PreconditionBlock>();
            this.Postconditions = new List<PostconditionBlock>();
        }

        public IList<PreconditionBlock> Preconditions { get; private set; }
        public IList<PostconditionBlock> Postconditions { get; private set; }

        /// <summary>
        /// Gets a boolean flag that indicates if this method contract has preconditions. 
        /// </summary>
        public bool HasPreconditions
        {
            get
            {
                return Preconditions.Count > 0;
            }
        }

        /// <summary>
        /// Gets a boolean flag that indicates if this method contract has postconditions. 
        /// </summary>
        public bool HasPostconditions
        {
            get
            {
                return Postconditions.Count > 0;
            }
        }

        /// <summary>
        /// Gets a sequence of precondition/postcondition description attributes.
        /// </summary>
        public IEnumerable<DescriptionAttribute> DescriptionAttributes
        {
            get
            {
                List<DescriptionAttribute> attrs = new List<DescriptionAttribute>();
                foreach (var item in Preconditions)
                {
                    attrs.Add(new DescriptionAttribute("pre", item.GetCode().ToString()));
                }
                foreach (var item in Postconditions)
                {
                    attrs.Add(new DescriptionAttribute("post", item.GetCode().ToString()));
                }
                return attrs;
            }
        }
    }
}
