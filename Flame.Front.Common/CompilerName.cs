using Flame.Compiler;
using Flame.Front;
using Flame.Front.Cli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Pixie;

namespace Flame.Front
{
    /// <summary>
    /// A data structure that describes a compiler. It includes
    /// the compiler's name, version, title and releases URL.
    /// </summary>
    public class CompilerName
    {
        public CompilerName(
            string Name, Lazy<Version> CurrentVersion, 
            string Title, string ReleaseUrl)
            : this(
                Name, CurrentVersion, Title, ReleaseUrl, 
                new Lazy<IEnumerable<MarkupNode>>(
                    () => Enumerable.Empty<MarkupNode>()))
        { }

        public CompilerName(
            string Name, Lazy<Version> CurrentVersion, 
            string Title, string ReleaseUrl,
            Lazy<IEnumerable<MarkupNode>> ExtraVersionInfo)
        {
            this.Name = Name;
            this.CurrentVersion = CurrentVersion;
            this.Title = Title;
            this.ReleaseUrl = ReleaseUrl;
            this.ExtraVersionInfo = ExtraVersionInfo;
        }

        /// <summary>
        /// Gets the name of the compiler.
        /// </summary>
        /// <value>The name of the compiler.</value>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the compiler's title.
        /// </summary>
        /// <value>The compiler's title.</value>
        public string Title { get; private set; }

        /// <summary>
        /// Gets a URL from where releases can be downloaded.
        /// </summary>
        /// <value>The URL from where releases can be downloaded.</value>
        public string ReleaseUrl { get; private set; }

        /// <summary>
        /// Gets the compiler's version.
        /// </summary>
        /// <value>The compiler's version.</value>
        public Lazy<Version> CurrentVersion { get; private set; }

        /// <summary>
        /// Gets formatted extra version information. 
        /// Each node is printed on a separate line.
        /// </summary>
        public Lazy<IEnumerable<MarkupNode>> ExtraVersionInfo { get; private set; }

        /// <summary>
        /// Creates a compiler version for this compiler assembly.
        /// The compiler version number is extracted from the assembly
        /// metadata.
        /// </summary>
        public static CompilerName Create(string Name, string Title, string ReleaseUrl)
        {
            return Create(Name, Title, ReleaseUrl, new Lazy<IEnumerable<MarkupNode>>(
                () => Enumerable.Empty<MarkupNode>()));
        }

        /// <summary>
        /// Creates a compiler version for this compiler assembly.
        /// The compiler version number is extracted from the assembly
        /// metadata. Extra version information can also be provided.
        /// </summary>
        public static CompilerName Create(
            string Name, string Title, string ReleaseUrl, 
            Lazy<IEnumerable<MarkupNode>> ExtraVersionInfo)
        {
            var version = new Lazy<Version>(() => Assembly.GetEntryAssembly().GetName().Version);
            return new CompilerName(Name, version, Title, ReleaseUrl, ExtraVersionInfo);
        }

        /// <summary>
        /// Gets the version number of the Flame libraries. 
        /// </summary>
        /// <returns>
        /// The version of Flame.Front is assumed to be
        /// representative of all Flame libraries.
        /// </returns>
        public static Version GetFlameVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version;
        }

        /// <summary>
        /// Gets a sequence of markup nodes which represent formatted info
        /// that pertains to the version number. 
        /// Each node is to be printed on a separate line.
        /// </summary>
        public IEnumerable<MarkupNode> FormattedVersion
        {
            get
            {
                var nodes = new List<MarkupNode>();

                nodes.Add(new MarkupNode("#group", new MarkupNode[]
                {
                    new MarkupNode(NodeConstants.BrightNodeType, Name),
                    new MarkupNode(
                        NodeConstants.TextNodeType, 
                        " version " + CurrentVersion.Value.ToString(3) +
                        " (based on Flame " + GetFlameVersion().ToString(3) + ")")
                }));

                nodes.AddRange(ExtraVersionInfo.Value);

                return nodes;
            }
        }

        /// <summary>
        /// Gets a sequence of markup nodes which represent formatted info
        /// that pertains to the version number, as well as some 
        /// information about the current environment.
        /// Each node is to be printed on a separate line.
        /// </summary>
        public IEnumerable<MarkupNode> FormattedInfo
        {
            get
            {
                var nodes = new List<MarkupNode>();

                nodes.AddRange(FormattedVersion);
                WriteVariable("Platform", ConsoleEnvironment.OSVersionString, nodes);
                WriteVariable("Console", ConsoleEnvironment.TerminalIdentifier, nodes);
                if (!string.IsNullOrWhiteSpace(ReleaseUrl))
                    nodes.Add(new MarkupNode(NodeConstants.TextNodeType, "You can check for new releases at " + ReleaseUrl));
                nodes.Add(new MarkupNode(NodeConstants.TextNodeType, "Thanks for using " + Title + "! Have fun writing code."));

                return nodes;
            }
        }

        /// <summary>
        /// Prints each node in the given sequence of nodes
        /// on a separate line.
        /// </summary>
        public static void PrintLines(
            IEnumerable<MarkupNode> Lines, ConsoleLog Log)
        {
            foreach (var item in Lines)
            {
                Log.WriteEntry(new LogEntry("", item));
            }
        }

        public void PrintVersion(ConsoleLog Log)
        {
            PrintLines(FormattedVersion, Log);
        }

        /// <summary>
        /// Prints this compiler version to the given log.
        /// </summary>
        public void PrintInfo(ConsoleLog Log)
        {
            PrintLines(FormattedInfo, Log);
        }

        private static void WriteVariable(string Name, string Value, List<MarkupNode> Target)
        {
            if (!string.IsNullOrWhiteSpace(Value))
            {
                Target.Add(new MarkupNode(NodeConstants.TextNodeType, Name + ": " + Value));
            }
        }

        /// <summary>
        /// Prints information pertaining to the current color scheme.
        /// </summary>
        public static void PrintColorScheme(ConsoleLog Log)
        {
            var nodes = new List<MarkupNode>();
            nodes.Add(new MarkupNode("#group", new MarkupNode[]
            {
                new MarkupNode(NodeConstants.TextNodeType, "foreground color: "),
                new MarkupNode(NodeConstants.TextNodeType, Log.ForegroundColor.ToString())
            }));
            nodes.Add(new MarkupNode("#group", new MarkupNode[]
            {
                new MarkupNode(NodeConstants.TextNodeType, "background color: "),
                new MarkupNode(NodeConstants.TextNodeType, Log.BackgroundColor.ToString())
            }));

            foreach (var item in nodes)
            {
                Log.WriteEntry(new LogEntry("", item));
            }
        }
    }
}
