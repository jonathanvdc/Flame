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
            this.depends = new Lazy<IHeaderDependency[]>(() => Blocks.Aggregate(Enumerable.Empty<IHeaderDependency>(), (acc, item) => acc.Union(item.Dependencies)).ToArray());
            this.type = new Lazy<IType>(() => Blocks.Select(item => item.Type).Where(item => !item.Equals(PrimitiveTypes.Void)).LastOrDefault() ?? PrimitiveTypes.Void);
            this.localDecls = new Lazy<LocalDeclaration[]>(() => Blocks.GetLocalDeclarations().ToArray());
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
            var results = new List<ICppBlock>();
            var declLocals = new HashSet<CppLocal>();
            foreach (var item in Blocks)
            {
                foreach (var declLocal in item.GetLocalDeclarations())
                {
                    if (declLocals.Contains(declLocal.Local))
                    {
                        declLocal.DeclareVariable = false;
                    }
                    else
                    {
                        declLocals.Add(declLocal.Local);
                    }
                }
                foreach (var local in item.LocalsUsed)
                {
                    if (!declLocals.Contains(local))
                    {
                        var newRef = new LocalDeclarationReference(local);
                        results.Add(newRef);
                        declLocals.Add(local);
                    }
                }
                results.Add(item);
            }
            return results;
        }

        public static IReadOnlyList<LocalDeclarationReference> HoistSequenceDeclarations(params ICppBlock[] Blocks)
        {
            return HoistSequenceDeclarations((IEnumerable<ICppBlock>)Blocks);
        }

        public static IReadOnlyList<LocalDeclarationReference> HoistSequenceDeclarations(IEnumerable<ICppBlock> Blocks)
        {
            var results = new List<LocalDeclarationReference>();
            var declLocals = new HashSet<CppLocal>();
            foreach (var item in Blocks)
            {
                foreach (var declLocal in item.GetLocalDeclarations())
                {
                    if (declLocals.Contains(declLocal.Local))
                    {
                        declLocal.DeclareVariable = false;
                    }
                    else
                    {
                        declLocals.Add(declLocal.Local);
                    }
                }
                foreach (var local in item.LocalsUsed)
                {
                    if (!declLocals.Contains(local))
                    {
                        var newRef = new LocalDeclarationReference(local);
                        results.Add(newRef);
                        declLocals.Add(local);
                    }
                }
            }
            return results;
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
