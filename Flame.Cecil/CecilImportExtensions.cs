using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public static class CecilImportExtensions
    {

        #region GetImportedReference

        #region Types

        public static TypeReference GetImportedReference(this IType Method, CecilModule Module)
        {
            return CecilTypeImporter.Import(Module, Method);
        }

        public static TypeReference GetImportedReference(this IType Type, CecilModule Module, IGenericParameterProvider Context)
        {
            return CecilTypeImporter.Import(Module, Context, Type);
        }

        #endregion

        #region Methods

        public static MethodReference GetImportedReference(this IMethod Method, CecilModule Module)
        {
            return CecilMethodImporter.Import(Module, Method);
        }

        public static MethodReference GetImportedReference(this IMethod Method, CecilModule Module, IGenericParameterProvider Context)
        {
            return CecilMethodImporter.Import(Module, Context, Method);
        }

        #endregion

        #endregion
    }
}
