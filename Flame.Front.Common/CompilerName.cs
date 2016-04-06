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
        public CompilerName(string Name, Version CurrentVersion, string Title, string ReleaseUrl)
        {
            this.Name = Name;
            this.CurrentVersion = CurrentVersion;
            this.Title = Title;
            this.ReleaseUrl = ReleaseUrl;
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
        public Version CurrentVersion { get; private set; }

        /// <summary>
        /// Creates a compiler version for this compiler assembly.
        /// The compiler version number is extracted from the assembly
        /// metadata.
        /// </summary>
        public static CompilerName Create(string Name, string Title, string ReleaseUrl)
        {
            var version = Assembly.GetEntryAssembly().GetName().Version;
            return new CompilerName(Name, version, Title, ReleaseUrl);
        }

        /// <summary>
        /// Prints this compiler version to the given log.
        /// </summary>
        public void Print(ConsoleLog Log)
        {
            var nodes = new List<MarkupNode>();

            nodes.Add(new MarkupNode("#group", new MarkupNode[]
            {
                new MarkupNode(NodeConstants.BrightNodeType, Name),
                new MarkupNode(NodeConstants.TextNodeType, " version " + CurrentVersion.ToString())
            }));
            WriteVariable("Platform", ConsoleEnvironment.OSVersionString, nodes);
            WriteVariable("Console", ConsoleEnvironment.TerminalIdentifier, nodes);
            if (!string.IsNullOrWhiteSpace(ReleaseUrl))
                nodes.Add(new MarkupNode(NodeConstants.TextNodeType, "You can check for new releases at " + ReleaseUrl));
            nodes.Add(new MarkupNode(NodeConstants.TextNodeType, "Thanks for using " + Title + "! Have fun writing code."));

            foreach (var item in nodes)
            {
                Log.WriteEntry(new LogEntry("", item));
            }
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
