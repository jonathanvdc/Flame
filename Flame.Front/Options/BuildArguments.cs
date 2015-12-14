using Flame.Front.Target;
using Flame.Compiler;
using Flame.Compiler.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flame.Recompilation;
using Pixie;

namespace Flame.Front.Options
{
    public class BuildArguments : ICompilerOptions, IEnumerable<KeyValuePair<string, string[]>>
    {
        public BuildArguments(IOptionParser<string[]> OptionParser)
        {
            this.OptionParser = OptionParser;
            this.args = new Dictionary<string, string[]>();
            this.accOptions = new HashSet<string>();
        }

        public IOptionParser<string[]> OptionParser { get; private set; }

        /// <summary>
        /// Gets a sequence of all options.
        /// </summary>
        public IEnumerable<string> Options { get { return this.args.Keys; } }

        /// <summary>
        /// Gets all options that have been accessed so far.
        /// </summary>
        public IEnumerable<string> AccessedOptions { get { return accOptions; } }

        /// <summary>
        /// Gets all options that have not been accessed so far.
        /// </summary>
        public IEnumerable<string> UnusedOptions { get { return Options.Except(AccessedOptions); } }

        #region ICompilerOptions Implementation

        public T GetOption<T>(string Key, T Default)
        {
            if (args.ContainsKey(Key) && OptionParser.CanParse<T>())
            {
                accOptions.Add(Key);
                return OptionParser.ParseValue<T>(args[Key]);
            }
            else
            {
                return Default;
            }
        }

        public bool HasOption(string Key)
        {
            return args.ContainsKey(Key);
        }

        #endregion

        #region IEnumerable<KeyValuePair<string, string[]>> Implementation

        public IEnumerator<KeyValuePair<string, string[]>> GetEnumerator()
        {
            return args.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Options as properties

        public bool HasSourcePath
        {
            get
            {
                return HasOption("source");
            }
        }

        public bool HasTargetPath
        {
            get
            {
                return HasOption("target") || HasOption("o");
            }
        }

        public PathIdentifier[] SourcePaths
        {
            get
            {
                return GetOption<PathIdentifier[]>("source", new PathIdentifier[] { });
            }
        }
        public PathIdentifier TargetPath
        {
            get
            {
                if (HasOption("target"))
                {
                    return GetOption<PathIdentifier>("target", new PathIdentifier(""));
                }
                else
                {
                    return GetOption<PathIdentifier>("o", new PathIdentifier(""));
                }
            }
        }
        public bool? CompileSingleFile
        {
            get
            {
                bool? singleFile = GetOptionOrNull<bool>("file");
                if (singleFile.HasValue)
                {
                    return singleFile;
                }
                else
                {
                    return !GetOptionOrNull<bool>("project");
                }
            }
        }
        public bool? CompileProject
        {
            get
            {
                return !CompileSingleFile;
            }
        }
        public bool MakeProject
        {
            get
            {
                return GetOption<bool>("make-project", false);
            }
        }

        /// <summary>
        /// Gets a boolean value that tells if the compiler has anything to compile.
        /// </summary>
        public bool CanCompile
        {
            get
            {
                return SourcePaths.Length > 0;
            }
        }

        #endregion

        #region Helper Methods

        public bool InSingleFileMode(PathIdentifier Path, IEnumerable<string> SingleFileExtensions)
        {
            if (CompileSingleFile.HasValue)
            {
                return CompileSingleFile.Value;
            }
            else
            {
                return SingleFileExtensions.Contains(Path.Extension);
            }
        }

        public PathIdentifier GetTargetPathWithoutExtension(PathIdentifier CurrentPath, IProject Project)
        {
            if (!TargetPath.IsEmpty)
            {
                return TargetPath.ChangeExtension(null).AbsolutePath;
            }
            else
            {
                return CurrentPath.GetAbsolutePath(new PathIdentifier("bin", Project.Name));
            }
        }

        public PathIdentifier GetTargetPath(PathIdentifier CurrentPath, IProject Project, BuildTarget Target)
        {
            if (!TargetPath.IsEmpty)
            {
                return TargetPath.AbsolutePath;
            }
            else
            {
                return CurrentPath.GetAbsolutePath(new PathIdentifier("bin", Project.Name).AppendExtension(Target.Extension));
            }
        }

        #endregion

        #region Build Arguments

        private HashSet<string> accOptions;
        private Dictionary<string, string[]> args;

        public T? GetOptionOrNull<T>(string Name)
            where T : struct
        {
            if (HasOption(Name))
            {
                return GetOption<T>(Name, default(T));
            }
            else
            {
                return null;
            }
        }

        public void AddBuildArgument(string Key, params string[] Value)
        {
            args[Key] = Value;
        }

        #endregion

        #region Static

        private static bool IsOption(string Argument)
        {
            return !string.IsNullOrEmpty(Argument) && (Argument[0] == '-' || Argument[0] == '/');
        }

        private static bool IsSplitOption(string Option)
        {
            return Option.Contains('=') || Option.Contains(':');
        }

        private static string GetOptionParameterName(string Argument)
        {
            return Argument.TrimStart('-', '/');
        }

        private static string[] ParseArguments(ArgumentStream<string> ArgStream)
        {
            var results = new List<string>();
            string peek = ArgStream.Peek();
            while (peek != null && !IsOption(peek))
            {
                ArgStream.MoveNext();
                results.Add(ArgStream.Current);
                peek = ArgStream.Peek();
            }
            return results.ToArray();
        }

        public static BuildArguments Parse(IOptionParser<string> OptionParser, params string[] Arguments)
        {
            return Parse(new StringArrayOptionParser(OptionParser), Arguments);
        }

        public static BuildArguments Parse(IOptionParser<string[]> OptionParser, params string[] Arguments)
        {
            BuildArguments result = new BuildArguments(OptionParser);
            const string defaultParameter = "source";

            var argStream = new ArgumentStream<string>(Arguments);
            while (argStream.MoveNext())
            {
                string item = argStream.Current;
                if (!IsOption(item))
                {
                    // Parse a sequence of raw arguments.
                    argStream.Move(-1);
                    string[] args = ParseArguments(argStream);
                    result.AddBuildArgument(defaultParameter, args);
                }
                else
                {
                    string param = GetOptionParameterName(item);
                    if (IsSplitOption(param))
                    {
                        string[] splitOption = param.Split(new char[] { '=', ':' }, 2, StringSplitOptions.RemoveEmptyEntries);
                        string key = splitOption[0];
                        string value = splitOption[1];
                        result.AddBuildArgument(key, value);
                    }
                    else
                    {
                        // Parse arguments
                        string[] args = ParseArguments(argStream);
                        result.AddBuildArgument(param, args);
                    }
                }
            }

            return result;
        }

        #endregion
    }
}
