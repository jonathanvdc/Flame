using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python
{
    public interface IPythonProperty : IProperty
    {
        /// <summary>
        /// Gets a boolean value that indicates if the property can be accessed through property syntax, rather than standard method syntax.
        /// </summary>
        bool UsesPropertySyntax { get; }
    }
}
