using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public class CppFile : IHeaderDependency
    {
        public CppFile(ICppMember Member)
        {
            this.Name = Member.Name;
            this.Members = new ICppMember[] { Member };
            this.HeaderDirectives = new PreprocessorDirective[] { PreprocessorDirective.PragmaOnce };
            this.SourceDirectives = new PreprocessorDirective[] { };
        }
        public CppFile(string Name, IEnumerable<ICppMember> Members)
        {
            this.Name = Name;
            this.Members = Members;
            this.HeaderDirectives = new PreprocessorDirective[] { PreprocessorDirective.PragmaOnce };
            this.SourceDirectives = new PreprocessorDirective[] { };
        }
        public CppFile(string Name, IEnumerable<ICppMember> Members, IEnumerable<PreprocessorDirective> Directives)
        {
            this.Name = Name;
            this.Members = Members;
            this.HeaderDirectives = new PreprocessorDirective[] { PreprocessorDirective.PragmaOnce }.Concat(Directives);
            this.SourceDirectives = Directives;
        }
        public CppFile(string Name, IEnumerable<ICppMember> Members, IEnumerable<PreprocessorDirective> HeaderDirectives, IEnumerable<PreprocessorDirective> SourceDirectives)
        {
            this.Name = Name;
            this.Members = Members;
            this.HeaderDirectives = HeaderDirectives;
            this.SourceDirectives = SourceDirectives;
        }

        public string Name { get; private set; }

        public IEnumerable<PreprocessorDirective> HeaderDirectives { get; private set; }
        public IEnumerable<PreprocessorDirective> SourceDirectives { get; private set; }
        public IEnumerable<ICppMember> Members { get; private set; }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get
            {
                IEnumerable<IHeaderDependency> depends = new IHeaderDependency[0];
                foreach (var item in Members)
                {
                    depends = depends.MergeDependencies(item.Dependencies);
                }
                return depends.ExcludeDependencies(new IHeaderDependency[] { this }).SortDependencies();
            }
        }

        public string HeaderName { get { return Name + "." + HeaderExtension; } }
        public string SourceName { get { return Name + "." + SourceExtension; } }

        public bool IsStandard
        {
            get { return false; }
        }

        public bool HasSourceCode
        {
            get { return Members.Any((item) => item.HasSourceCode); }
        }

        private bool? containsTempls;
        public bool ContainsTemplates
        {
            get
            {
                if (containsTempls == null)
                {
                    containsTempls = Members.Any((item) => item.ContainsTemplates());
                }
                return containsTempls.Value;
            }
        }

        public string HeaderExtension
        {
            get
            {
                return "h";
            }
        }

        public string SourceExtension
        {
            get
            {
                return ContainsTemplates ? "hxx" : "cpp";
            }
        }

        public IHeaderDependency SourceDependency
        {
            get
            {
                return new UserDependency(SourceName);
            }
        }

        public CodeBuilder GetIncludeCode(bool IsHeader)
        {
            CodeBuilder cb = new CodeBuilder();
            cb.IndentationString = new string(' ', 4);
            foreach (var item in IsHeader ? HeaderDirectives : SourceDirectives)
            {
                cb.AddCodeBuilder(item.GetCode());
            }
            foreach (var item in Dependencies)
            {
                cb.AddCodeBuilder(PreprocessorDirective.CreateIncludeDirective(item).GetCode());
            }
            if (!IsHeader)
            {
                cb.AddCodeBuilder(PreprocessorDirective.CreateIncludeDirective(this).GetCode());
            }
            return cb;
        }

        public CodeBuilder GetSourceCode()
        {
            CodeBuilder cb = GetIncludeCode(false);
            cb.AddEmptyLine();
            var usings = GetNamespaces();
            if (usings.Any())
            {
                foreach (var item in usings)
                {
                    cb.AddLine("using namespace " + item.Replace(".", "::") + ";");
                }
                cb.AddEmptyLine();
            }
            foreach (var item in Members)
            {
                cb.AddCodeBuilder(item.GetSourceCode());
            }
            return cb;
        }

        public CodeBuilder GetHeaderCode()
        {
            CodeBuilder cb = GetIncludeCode(true);
            cb.AddEmptyLine();
            foreach (var item in Members)
            {
                cb.AddCodeBuilder(GetHeaderCode(item));
            }
            if (ContainsTemplates && HasSourceCode) // Include .hxx
            {
                cb.AddEmptyLine();
                cb.AddCodeBuilder(PreprocessorDirective.CreateIncludeDirective(SourceDependency).GetCode());
            }
            return cb;
        }

        private void IncludeDependencies(IOutputProvider OutputProvider)
        {
            foreach (var item in Dependencies)
            {
                item.Include(OutputProvider);
            }
        }

        public void Include(IOutputProvider OutputProvider)
        {
            if (OutputProvider.Exists(Name, HeaderExtension))
            {
                return;
            }

            var headerFile = OutputProvider.Create(Name, HeaderExtension);

            IncludeDependencies(OutputProvider);

            using (var headerStream = headerFile.OpenOutput())
            using (var writer = new StreamWriter(headerStream))
            {
                writer.Write(GetHeaderCode().ToString());
            }
            if (HasSourceCode)
            {
                using (var sourceStream = OutputProvider.Create(Name, SourceExtension).OpenOutput())
                using (var writer = new StreamWriter(sourceStream))
                {
                    writer.Write(GetSourceCode().ToString());
                }
            }
        }

        public override string ToString()
        {
            return HeaderName;
        }

        #region Helpers

        private IEnumerable<string> GetNamespaces()
        {
            return Members.Select(GetNamespace).Where((item) => !string.IsNullOrWhiteSpace(item));
        }

        private static string GetNamespace(ICppMember Member)
        {
            if (Member is IType)
            {
                return ((IType)Member).DeclaringNamespace.FullName;
            }
            else
            {
                return "";
            }
        }

        private static string[] GetSplitNamespace(ICppMember Member)
        {
            return GetNamespace(Member).Split(new string[] { "::", "." }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static CodeBuilder GetHeaderCode(ICppMember Member)
        {
            string[] ns = GetSplitNamespace(Member);
            return WrapNamespaces(Member.GetHeaderCode(), ns);
        }

        private static CodeBuilder WrapNamespaces(CodeBuilder Code, IEnumerable<string> Namespaces)
        {
            if (!Namespaces.Any())
            {
                return Code;
            }
            else
            {
                CodeBuilder cb = new CodeBuilder();
                cb.IndentationString = Code.IndentationString;
                cb.AddLine("namespace " + Namespaces.First());
                cb.AddLine("{");
                cb.IncreaseIndentation();
                cb.AddCodeBuilder(WrapNamespaces(Code, Namespaces.Skip(1)));
                cb.DecreaseIndentation();
                cb.AddLine("}");
                return cb;
            }
        }

        #endregion
    }
}
