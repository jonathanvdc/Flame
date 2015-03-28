using dsc.Target;
using Flame.Compiler;
using Flame.Compiler.Projects;
using Flame.Recompilation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsc.Options
{
    public class BuildArguments
    {
        public BuildArguments()
        {
            this.args = new Dictionary<IBuildParameter, string[]>();
        }

        #region Options as properties

        public string SourcePath
        {
            get
            {
                return GetBuildArgumentOrDefault<string>("source");
            }
        }
        public string TargetPath
        {
            get
            {
                return GetBuildArgumentOrDefault<string>("target");
            }
        }
        public bool? CompileSingleFile
        {
            get
            {
                bool? singleFile = GetBuildArgumentOrNull<bool>("file");
                if (singleFile.HasValue)
                {
                    return singleFile;
                }
                else
                {
                    return !GetBuildArgumentOrNull<bool>("project");
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
        public string TargetPlatform
        {
            get
            {
                return GetBuildArgumentOrDefault<string>("platform") ?? "";
            }
        }
        public bool CompileAll
        {
            get
            {
                return GetBuildArgumentOrNull<bool>("compileall").GetValueOrDefault(true);
            }
        }
        public bool MakeProject
        {
            get
            {
                return GetBuildArgumentOrNull<bool>("make-project").GetValueOrDefault(false);
            }
        }
        public bool VerifyAssembly
        {
            get
            {
                return GetBuildArgumentOrNull<bool>("verify").GetValueOrDefault(true);
            }
        }

        public IMethodOptimizer Optimizer
        {
            get
            {
                return GetBuildArgumentOrDefault<IMethodOptimizer>("optimize") ?? new DefaultOptimizer();
            }
        }

        public ILogFilter LogFilter
        {
            get
            {
                return GetBuildArgumentOrDefault<ILogFilter>("chat") ?? new ChatLogFilter(ChatLevel.Silent);
            }
        }

        /// <summary>
        /// Gets a boolean value that tells if the compiler should print its version number.
        /// </summary>
        public bool PrintVersion
        {
            get
            {
                return GetBuildArgumentOrDefault<bool>("version");
            }
        }

        /// <summary>
        /// Gets a boolean value that tells if the compiler has anything to compile.
        /// </summary>
        public bool CanCompile
        {
            get
            {
                return !string.IsNullOrWhiteSpace(SourcePath);
            }
        }

        #endregion

        #region Helper Methods

        public bool InSingleFileMode(string Path)
        {
            if (CompileSingleFile.HasValue)
            {
                return CompileSingleFile.Value;
            }
            else
            {
                return Path.EndsWith(".ds");
            }
        }

        public string GetTargetPathWithoutExtension(string CurrentPath, IProject Project)
        {
            Uri relUri;
            if (!string.IsNullOrWhiteSpace(TargetPath))
            {
                relUri = new Uri(TargetPath, UriKind.RelativeOrAbsolute);
            }
            else
            {
                string path = "bin/" + Project.Name;
                relUri = new Uri(path, UriKind.RelativeOrAbsolute);
            }
            return System.IO.Path.ChangeExtension(new Uri(new Uri(CurrentPath), relUri).AbsolutePath, null);
        }

        public string GetTargetPath(string CurrentPath, IProject Project, BuildTarget Target)
        {
            Uri relUri;
            if (!string.IsNullOrWhiteSpace(TargetPath))
            {
                relUri = new Uri(TargetPath, UriKind.RelativeOrAbsolute);
            }
            else
            {
                string path = "bin/" + Project.Name + "." + Target.Extension;
                relUri = new Uri(path, UriKind.RelativeOrAbsolute);
            }
            return new Uri(new Uri(CurrentPath), relUri).AbsolutePath;
        }

        public string GetTargetPlatform(IProject Project)
        {
            string platform = TargetPlatform;
            if (string.IsNullOrWhiteSpace(platform))
            {
                return Project.BuildTargetIdentifier;
            }
            else
            {
                return platform;
            }
        }

        #endregion

        #region Build Arguments

        private Dictionary<IBuildParameter, string[]> args;

        public bool HasBuildArgument(string Name)
        {
            foreach (var item in args)
            {
                if (item.Key.Key == Name)
                {
                    return true;
                }
            }
            return false;
        }

        public T GetBuildArgument<T>(string Name)
        {
            foreach (var item in args)
            {
                if (item.Key.Key == Name)
                {
                    return ((IBuildOption<T>)item.Key).GetValue(item.Value);
                }
            }
            return default(T);
        }

        public T? GetBuildArgumentOrNull<T>(string Name)
            where T : struct
        {
            foreach (var item in args)
            {
                if (item.Key.Key == Name)
                {
                    return ((IBuildOption<T>)item.Key).GetValue(item.Value);
                }
            }
            return null;
        }
        public T GetBuildArgumentOrDefault<T>(string Name)
        {
            foreach (var item in args)
            {
                if (item.Key.Key == Name)
                {
                    return ((IBuildOption<T>)item.Key).GetValue(item.Value);
                }
            }
            return default(T);
        }

        public void AddBuildArgument(IBuildParameter Parameter, params string[] Value)
        {
            args[Parameter] = Value;
        }

        /// <summary>
        /// Gets all foreign/compiler options.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, string> GetCompilerOptions()
        {
            Dictionary<string, string> results = new Dictionary<string, string>();
            foreach (var item in args)
            {
                if (IsCompilerParameter(item.Key.Key))
                {
                    if (item.Key is IBuildOption<bool>)
                    {
                        results[item.Key.Key] = ((IBuildOption<bool>)item.Key).GetValue(item.Value) ? "true" : "false";
                    }
                    else if (item.Key is IBuildOption<string>)
                    {
                        results[item.Key.Key] = ((IBuildOption<string>)item.Key).GetValue(item.Value);
                    }
                    else if (item.Key is IBuildOption<string[]>)
                    {
                        var stringArgs = ((IBuildOption<string[]>)item.Key).GetValue(item.Value);
                        if (stringArgs.Length == 0)
                        {
                            results[item.Key.Key] = "true";
                        }
                        else
                        {
                            results[item.Key.Key] = stringArgs[0];
                        }
                    }
                }
            }
            return results;
        }

        #endregion

        #region Static

        static BuildArguments()
        {
            parameters = new Dictionary<string, IBuildParameter>();
            AddBuildParameter(new SingleFileBuildOption());
            AddBuildParameter(new ProjectBuildOption());
            AddBuildParameter(new SourcePathOption());
            AddBuildParameter(new TargetPathOption());
            AddBuildParameter(new TargetPlatformOption());
            AddBuildParameter(new CompileAllOption());
            AddBuildParameter(new MakeProjectBuildOption());
            AddBuildParameter(new VerifyOption());
            AddBuildParameter(new OptimizerOption());
            AddBuildParameter(new ChatOption());
            AddBuildParameter(new VersionOption());
        }

        private static Dictionary<string, IBuildParameter> parameters;
        public static void AddBuildParameter(IBuildParameter Parameter)
        {
            parameters[Parameter.Key] = Parameter;
        }
        public static IBuildParameter GetBuildParameter(string Key)
        {
            if (parameters.ContainsKey(Key))
            {
                return parameters[Key];
            }
            else
            {
                return null;
            }
        }
        public static bool IsBuildParameter(string Key)
        {
            return parameters.ContainsKey(Key);
        }
        public static bool IsCompilerParameter(string Key)
        {
            return !IsBuildParameter(Key);
        }

        #region Parsing

        private static bool IsOption(string Argument)
        {
            return !string.IsNullOrEmpty(Argument) && Argument[0] == '-';
        }

        private static string GetOptionParameterName(string Argument)
        {
            return Argument.TrimStart('-');
        }

        private static IBuildParameter GetOptionParameter(string Argument)
        {
            return GetBuildParameter(GetOptionParameterName(Argument));
        }

        private static string[] ParseArguments(ArgumentStream<string> ArgStream, int Count)
        {
            List<string> results = new List<string>();
            string peek = ArgStream.Peek();
            while ((Count < 0 || results.Count < Count) && peek != null && !IsOption(peek))
            {
                ArgStream.MoveNext();
                results.Add(ArgStream.Current);
                peek = ArgStream.Peek();
            }
            return results.ToArray();
        }

        public static BuildArguments Parse(string[] Arguments)
        {
            BuildArguments result = new BuildArguments();
            IBuildParameter[] defaultParameters = new IBuildParameter[]
            {
                new SourcePathOption(),
                new TargetPathOption(),
                new TargetPlatformOption()
            };

            int defaultIndex = 0;
            var argStream = new ArgumentStream<string>(Arguments);
            while (argStream.MoveNext())
            {
                string item = argStream.Current;
                IBuildParameter param;
                if (IsOption(item)) // Parse known or unknown option. 'param' will become null in the latter case.
                {
                    param = GetOptionParameter(item);
                }
                else if (defaultIndex < defaultParameters.Length)
                {
                    param = defaultParameters[defaultIndex];
                    defaultIndex++;
                    argStream.Move(-1);
                }
                else
                {
                    ConsoleLog.Instance.LogWarning(new LogEntry("Build parameter mismatch", "Could not guess default build parameter #" + defaultIndex + "."));
                    param = null;
                    continue;
                }

                // Parse arguments
                string[] args = ParseArguments(argStream, param == null ? -1 : param.ArgumentsCount);
                if (param != null)
	            {
                    if (args.Length != param.ArgumentsCount)
                    {
                        ConsoleLog.Instance.LogWarning(new LogEntry("Build parameter mismatch", "Too " + (args.Length < param.ArgumentsCount ? "few" : "many") + " arguments were provided for build parameter '" + param.Key + "'. Expected " + param.ArgumentsCount + ", got " + args.Length + "."));
                    }
                    result.AddBuildArgument(param, args);
	            }
                else
                {
                    result.AddBuildArgument(new ForeignBuildOption(GetOptionParameterName(item), args.Length), args);
                }
            }

            return result;
        }

        #endregion

        #endregion
    }
}
