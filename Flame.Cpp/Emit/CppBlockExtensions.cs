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

        public static IEnumerable<ICppBlock> Flatten(this IMultiBlock Block)
        {
            var items = Block.GetBlocks();
            return items.SelectMany(item => item is IMultiBlock ? Flatten((IMultiBlock)item) : new IMultiBlock[] { Block });
        }
             
        /// <summary>
        /// Gets a boolean value that identifies the block as either a simple or complex statement.
        /// </summary>
        /// <param name="Block"></param>
        /// <returns></returns>
        public static bool IsSimple(this ICppBlock Block)
        {
            if (Block is IMultiBlock)
            {
                var items = Flatten((IMultiBlock)Block);
                return !items.Skip(1).Any();
            }
            return true;
        }

        #endregion

        #region GetDeclarations

        public static IEnumerable<LocalDeclaration> GetLocalDeclarations(this ICppBlock Block)
        {
            if (Block is LocalDeclarationReference)
            {
                var block = (LocalDeclarationReference)Block;
                return block.DeclaresVariable ? new LocalDeclaration[] { block.Declaration } : Enumerable.Empty<LocalDeclaration>();
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

        public static IEnumerable<CppLocal> GetDeclaredLocals(this ICppBlock Block)
        {
            return Block.GetLocalDeclarations().Select(item => item.Local).Distinct();
        }

        public static IEnumerable<CppLocal> GetDeclaredLocals(this IEnumerable<ICppBlock> Blocks)
        {
            return Blocks.Aggregate(Enumerable.Empty<CppLocal>(), (acc, item) => acc.Union(item.GetDeclaredLocals()));
        }

        #endregion

        #region IntersectLocalDeclarations

        public static IEnumerable<LocalDeclaration> GetCommonLocalDeclarations(this IEnumerable<ICppBlock> Blocks)
        {
            var localDecls = Blocks.Select(GetLocalDeclarations).ToArray();
            foreach (var declGroup in localDecls)
            {
                foreach (var item in declGroup)
                {
                    if (localDecls.Any(group => group != declGroup && group.Any(value => value.Local == item.Local)))
                    {
                        yield return item;
                    }
                }
            }
        }

        #endregion

        #region GetUsedLocals

        public static IEnumerable<CppLocal> GetUsedLocals(this IEnumerable<ICppBlock> Blocks)
        {
            return Blocks.Aggregate(Enumerable.Empty<CppLocal>(), (acc, block) => acc.Union(block.LocalsUsed));
        }

        #endregion

        #region GetDependencies

        public static IEnumerable<IHeaderDependency> GetDependencies(this IEnumerable<ICppBlock> Blocks)
        {
            return Blocks.Aggregate(Enumerable.Empty<IHeaderDependency>(), (acc, block) => acc.Union(block.Dependencies));
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
