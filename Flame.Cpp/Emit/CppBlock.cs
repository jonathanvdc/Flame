using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class CppBlock : ICppLocalDeclaringBlock, IMultiBlock
    {
        public CppBlock(ICodeGenerator CodeGenerator, IReadOnlyList<ICppBlock> Blocks)
        {
            this.CodeGenerator = CodeGenerator;
            this.Blocks = Blocks;
            this.usedLocals = new Lazy<CppLocal[]>(() => Blocks.Aggregate(Enumerable.Empty<CppLocal>(), (acc, item) => acc.Union(item.LocalsUsed)).ToArray());
            this.depends = new Lazy<IHeaderDependency[]>(() => Blocks.Aggregate(Enumerable.Empty<IHeaderDependency>(), (acc, item) => acc.MergeDependencies(item.Dependencies)).ToArray());
            this.type = new Lazy<IType>(() => Blocks.Select(item => item.Type).Where(item => !item.Equals(PrimitiveTypes.Void)).LastOrDefault() ?? PrimitiveTypes.Void);
            this.localDecls = new Lazy<LocalDeclaration[]>(() => Blocks.GetLocalDeclarations().ToArray());
            this.spilledDecls = new Lazy<LocalDeclaration[]>(() => Blocks.GetSpilledLocals().ToArray());
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IReadOnlyList<ICppBlock> Blocks { get; private set; }

        #region Declarations

        public static IReadOnlyList<ICppBlock> InsertSequenceDeclarations(params ICppBlock[] Blocks)
        {
            return InsertSequenceDeclarations((IEnumerable<ICppBlock>)Blocks);
        }

        public static IReadOnlyList<ICppBlock> InsertSequenceDeclarations(IEnumerable<ICppBlock> Blocks)
        {
            return InsertSequenceDeclarations(Blocks, true);
        }

        private static IReadOnlyList<ICppBlock> InsertSequenceDeclarations(IEnumerable<ICppBlock> Blocks, bool IncludeBlocks)
        {
            var results = new List<ICppBlock>();
            var declLocals = new HashSet<CppLocal>(); // Locals that are definitely declared
            var externalDecls = new Dictionary<CppLocal, LocalDeclarationReference>(); // Local declarations that have not been applied yet
            foreach (var item in Blocks)
            {
                var spilledLocals = item.GetSpilledLocals().ToArray();

                foreach (var declLocal in spilledLocals)
                {
                    if (declLocals.Contains(declLocal.Local))
                    {
                        declLocal.DeclareVariable = false;
                    }
                    else if (externalDecls.ContainsKey(declLocal.Local))
                    {
                        externalDecls[declLocal.Local].Acquire();
                        declLocal.DeclareVariable = false;
                        declLocals.Add(declLocal.Local);
                    }
                    else
                    {
                        declLocals.Add(declLocal.Local);
                    }
                }
                
                var internalLocals = item.GetLocalDeclarations().Distinct(DeclarationLocalComparer.Instance).ToArray();

                foreach (var declLocal in internalLocals.Except(spilledLocals, DeclarationLocalComparer.Instance))
                {
                    if (declLocals.Contains(declLocal.Local))
                    {
                        declLocal.DeclareVariable = false;
                    }
                    else if (externalDecls.ContainsKey(declLocal.Local))
                    {
                        externalDecls[declLocal.Local].Acquire();
                        declLocal.DeclareVariable = false;
                        declLocals.Add(declLocal.Local);
                    }
                    else
                    {
                        var declRef = new LocalDeclarationReference(declLocal);
                        externalDecls[declLocal.Local] = declRef;
                        results.Add(declRef);
                    }
                }

                foreach (var local in item.LocalsUsed.Except(internalLocals.Select(decl => decl.Local)))
                {
                    if (!declLocals.Contains(local))
                    {
                        var newRef = externalDecls.ContainsKey(local) ? externalDecls[local] : new LocalDeclarationReference(local);
                        newRef.Acquire();
                        results.Add(newRef);
                        declLocals.Add(local);
                    }
                }
                if (IncludeBlocks)
                {
                    results.Add(item);
                }
            }
            return results;
        }

        public static IReadOnlyList<ICppBlock> HoistSequenceDeclarations(params ICppBlock[] Blocks)
        {
            return HoistSequenceDeclarations((IEnumerable<ICppBlock>)Blocks);
        }

        public static IReadOnlyList<ICppBlock> HoistSequenceDeclarations(IEnumerable<ICppBlock> Blocks)
        {
            return InsertSequenceDeclarations(Blocks, false);
        }

        public static ISet<CppLocal> GetCommonVariables(IEnumerable<ICppBlock> Blocks)
        {
            var localOccurences = new Dictionary<CppLocal, int>();
            foreach (var block in Blocks)
            {
                foreach (var item in block.GetLocalDeclarations().Select(item => item.Local).Concat(block.LocalsUsed).Distinct())
                {
                    if (localOccurences.ContainsKey(item))
                    {
                        localOccurences[item]++;
                    }
                    else
                    {
                        localOccurences[item] = 1;
                    }
                }
            }
            return new HashSet<CppLocal>(localOccurences.Where(item => item.Value > 1).Select(item => item.Key));
        }

        public static IReadOnlyList<LocalDeclarationReference> HoistUnionDeclarations(params ICppBlock[] Blocks)
        {
            return HoistUnionDeclarations((IEnumerable<ICppBlock>)Blocks);
        }

        public static IReadOnlyList<LocalDeclarationReference> HoistUnionDeclarations(IEnumerable<ICppBlock> Blocks)
        {
            var results = new List<LocalDeclarationReference>();
            var commonVariables = GetCommonVariables(Blocks);
            foreach (var item in commonVariables)
            {
                results.Add(new LocalDeclarationReference(item));
            }
            foreach (var item in Blocks)
            {
                foreach (var declLocal in item.GetLocalDeclarations())
                {
                    if (commonVariables.Contains(declLocal.Local))
                    {
                        declLocal.DeclareVariable = false;
                    }
                }
            }
            return results;
        }

        #endregion

        private Lazy<LocalDeclaration[]> localDecls;
        public IEnumerable<LocalDeclaration> LocalDeclarations
        {
            get { return localDecls.Value; }
        }

        private Lazy<LocalDeclaration[]> spilledDecls;
        public IEnumerable<LocalDeclaration> SpilledDeclarations
        {
            get { return spilledDecls.Value; }
        }

        private Lazy<IType> type;
        public IType Type
        {
            get
            {
                return type.Value;
            }
        }

        private Lazy<IHeaderDependency[]> depends;
        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return depends.Value; }
        }

        private Lazy<CppLocal[]> usedLocals;
        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return usedLocals.Value; }
        }

        public CodeBuilder GetCode()
        {
            var codes = this.Flatten()
                              .Select(item => item.GetCode())
                              .Where(item => !item.IsWhitespace)
                              .ToArray();

            if (codes.Length == 0)
            {
                return new CodeBuilder(";");
            }
            else if (codes.Length == 1)
            {
                return codes[0];
            }
            else
            {
                CodeBuilder cb = new CodeBuilder();
                cb.AddLine("{");
                cb.IncreaseIndentation();
                foreach (var item in codes)
                {
                    cb.AddCodeBuilder(item);
                }
                cb.DecreaseIndentation();
                cb.AddLine("}");
                return cb;
            }
        }

        public IEnumerable<ICppBlock> GetBlocks()
        {
            return Blocks;
        }

        public override string ToString()
        {
            return GetCode().ToString();
        }
    }
}
