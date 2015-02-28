using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public static class CppBlockExtensions
    {
        #region IsSimple

        /// <summary>
        /// Gets a boolean value that identifies the block as either a simple or complex statement.
        /// </summary>
        /// <param name="Block"></param>
        /// <returns></returns>
        public static bool IsSimple(this ICppBlock Block)
        {
            if (Block is CppBlockGeneratorBase)
            {
                return Block is CppBlockGenerator && ((CppBlockGenerator)Block).StatementCount <= 1;
            }
            return true;
        }

        #endregion

        #region GetDeclarations

        public static IEnumerable<LocalDeclaration> GetLocalDeclarations(this ICppBlock Block)
        {
            if (Block is LocalDeclarationReference)
            {
                return new LocalDeclaration[] { ((LocalDeclarationReference)Block).Declaration };
            }
            else if (Block is ICppLocalDeclaringBlock)
            {
                return ((ICppLocalDeclaringBlock)Block).LocalDeclarations;
            }
            else
            {
                return Enumerable.Empty<LocalDeclaration>();
            }
        }

        public static IEnumerable<LocalDeclaration> GetLocalDeclarations(this IEnumerable<ICppBlock> Blocks)
        {
            return Blocks.SelectMany((item) => item.GetLocalDeclarations());
        }

        #endregion

        #region DeclaresLocal

        public static bool DeclaresLocal(this ICppBlock Block, CppLocal Local)
        {
            return Block.GetLocalDeclarations().Any(item => item.Local == Local);
        }

        #endregion
    }
}
