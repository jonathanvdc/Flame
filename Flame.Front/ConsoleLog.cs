using Flame.Compiler;
using Flame.Front.Cli;
using Flame.Front.Options;
using Pixie;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front
{
    public sealed class ConsoleLog : ICompilerLog
    {
        public ConsoleLog()
            : this(ConsoleEnvironment.AcquireConsole(), new StringCompilerOptions())
        {
        }

        public ConsoleLog(ICompilerOptions Options)
            : this(ConsoleEnvironment.AcquireConsole(), Options)
        {
        }

        public ConsoleLog(IConsole Console, ICompilerOptions Options)
            : this(Console, Options, CreateDefaultPalette(Console.Description, Options))
        {
        }

        public ConsoleLog(IConsole Console, ICompilerOptions Options, IStylePalette Palette)
            : this(Console, Options, Palette, CreateDefaultNodeWriter(Console.Description, Palette))
        {
        }

        public ConsoleLog(IConsole Console, ICompilerOptions Options, INodeWriter NodeWriter)
            : this(Console, Options, CreateDefaultPalette(Console.Description, Options), NodeWriter)
        {
        }

        public ConsoleLog(IConsole Console, ICompilerOptions Options, IStylePalette Palette, INodeWriter NodeWriter)
        {
            this.Options = Options;
            this.Console = new ParagraphConsole(Console, 1);
            this.Palette = Palette;
            this.NodeWriter = NodeWriter;
            this.writeLock = new object();
        }

        public ParagraphConsole Console { get; private set; }
        public ICompilerOptions Options { get; private set; }
        public IStylePalette Palette { get; private set; }
        public INodeWriter NodeWriter { get; private set; }
        private object writeLock;

        public int BufferWidth
		{
			get
			{
				return Console.Description.BufferWidth;
			}
		}

        #region Defaults

        #region Node Writer

        public static INodeWriter CreateDefaultNodeWriter(ConsoleDescription Description, IStylePalette Palette)
        {
            var writer = new NodeWriter();
            writer.Writers[NodeConstants.RemarksNodeType] = new RemarksNodeWriter(writer);
            writer.Writers[NodeConstants.SourceNodeType] = new SourceNodeWriter(new string(' ', 4), Description.BufferWidth - 8);
            writer.Writers[NodeConstants.ListNodeType] = new ListNodeWriter(writer);
            writer.Writers[NodeConstants.HighlightNodeType] = new HighlightingNodeWriter(writer);
            writer.Writers[NodeConstants.SourceQuoteNodeType] = new SourceQuoteNodeWriter(writer);
			writer.Writers[NodeConstants.SourceLocationNodeType] = new SourceLocationWriter(writer);
            writer.Writers[NodeConstants.ParagraphNodeType] = new ParagraphWriter(writer);
            writer.Writers[NodeConstants.CauseNodeType] = new CauseNodeWriter(writer);
            var contrastColor = StylePalette.MakeContrastColor(Description.ForegroundColor, Description.BackgroundColor);
            writer.Writers[NodeConstants.BrightNodeType] = new StyleWriter(writer, new Style("bright", contrastColor, new Color()));
            writer.Writers[NodeConstants.DimNodeType] = new StyleWriter(writer, new Style("dim", Palette.MakeDimColor(contrastColor), new Color()));
            writer.Writers["neutral-diagnostics"] = new NeutralDiagnosticsWriter(writer);
            return writer;
        }

        #endregion

        #region Style customization

        /// <summary>
        /// The internal style name for warnings.
        /// </summary>
        public const string WarningStyleName = "warning-header";

        /// <summary>
        /// The input option prefix for warning styles.
        /// </summary>
        public const string WarningStyleOptionPrefix = "warning";

        /// <summary>
        /// The internal style name for errors.
        /// </summary>
        public const string ErrorStyleName = "error-header";

        /// <summary>
        /// The input option prefix for error styles.
        /// </summary>
        public const string ErrorStyleOptionPrefix = "error";

        /// <summary>
        /// The internal style name for messages and notes.
        /// </summary>
        public const string MessageStyleName = "message-header";

        /// <summary>
        /// The input option prefix for message/note styles.
        /// </summary>
        public const string MessageStyleOptionPrefix = "message";
        
        /// <summary>
        /// The suffix for foreground color options.
        /// </summary>
        public const string ForegroundColorOptionSuffix = "-fg-color";       

        /// <summary>
        /// The suffix for background color options.
        /// </summary>
        public const string BackgroundColorOptionSuffix = "-bg-color";

        /// <summary>
        /// The suffix for style preference options.
        /// </summary>
        public const string StylePreferencesOptionSuffix = "-style-prefs";

        /// <summary>
        /// Creates a style from the given default arguments, 
        /// and input stored in the set of compiler options.
        /// </summary>
        /// <param name="Options"></param>
        /// <param name="OptionPrefix"></param>
        /// <param name="Name"></param>
        /// <param name="DefaultForegroundColor"></param>
        /// <param name="DefaultBackgroundColor"></param>
        /// <param name="DefaultPreferences"></param>
        /// <returns></returns>
        public static Style GetStyle(ICompilerOptions Options, string OptionPrefix,
            string Name, Color DefaultForegroundColor, Color DefaultBackgroundColor, params string[] DefaultPreferences)
        {
            Color fgColor = Options.GetOption<Color>(OptionPrefix + ForegroundColorOptionSuffix, DefaultForegroundColor);
            Color bgColor = Options.GetOption<Color>(OptionPrefix + BackgroundColorOptionSuffix, DefaultBackgroundColor);
            string[] prefs = Options.GetOption<string[]>(OptionPrefix + StylePreferencesOptionSuffix, DefaultPreferences);

            return new Style(Name, fgColor, bgColor, prefs);
        }

        /// <summary>
        /// Creates a style from the given default arguments, 
        /// and input stored in the set of compiler options.
        /// </summary>
        /// <param name="Options"></param>
        /// <param name="OptionPrefix"></param>
        /// <param name="Name"></param>
        /// <param name="DefaultForegroundColor"></param>
        /// <returns></returns>
        public static Style GetStyle(ICompilerOptions Options, string OptionPrefix,
            string Name, Color DefaultForegroundColor)
        {
            return GetStyle(Options, OptionPrefix, Name, DefaultForegroundColor, new Color());
        }

        #endregion

        #region Palette

        public static IStylePalette CreateDefaultPalette(ConsoleDescription Description, ICompilerOptions Options)
        {
            return CreateDefaultPalette(Description.ForegroundColor, Description.BackgroundColor, Options);
        }

        public static IStylePalette CreateDefaultPalette(Color ForegroundColor, Color BackgroundColor, ICompilerOptions Options)
        {
            var palette = new StylePalette(ForegroundColor, BackgroundColor);
            palette.RegisterStyle(RemarksNodeWriter.GetRemarksStyle(palette));
            palette.RegisterStyle(HighlightingNodeWriter.GetHighlightStyle(palette));
            palette.RegisterStyle(HighlightingNodeWriter.GetHighlightExtraStyle(palette));
            palette.RegisterStyle(HighlightingNodeWriter.GetHighlightMissingStyle(palette));
            palette.RegisterStyle(GetStyle(Options, ErrorStyleOptionPrefix, ErrorStyleName, DefaultConsole.ToPixieColor(ConsoleColor.Red)));
            palette.RegisterStyle(GetStyle(Options, WarningStyleOptionPrefix, WarningStyleName, DefaultConsole.ToPixieColor(ConsoleColor.Yellow)));
            palette.RegisterStyle(GetStyle(Options, MessageStyleOptionPrefix, MessageStyleName, DefaultConsole.ToPixieColor(ConsoleColor.Green)));
            return palette;
        }

        #endregion

        #endregion

        #region Palette

        public Style MakeBrightStyle(Style Input)
        {
            return new Style(Input.Name, Palette.MakeBrightColor(Input.ForegroundColor), Input.BackgroundColor, Input.Preferences);
        }

        public Style MakeDimStyle(Style Input)
        {
            return new Style(Input.Name, Palette.MakeDimColor(Input.ForegroundColor), Input.BackgroundColor, Input.Preferences);
        }

        public Color ContrastForegroundColor
        {
            get
            {
                return StylePalette.MakeContrastColor(Console.Description.ForegroundColor, Console.Description.BackgroundColor);
            }
        }

        public Color ForegroundColor
        {
            get
            {
                return Console.Description.ForegroundColor;
            }
        }

        public Color BackgroundColor
        {
            get
            {
                return Console.Description.BackgroundColor;
            }
        }

        public Style ErrorStyle
        {
            get
            {
                return Palette.GetNamedStyle(ErrorStyleName);
            }
        }

        public Style WarningStyle
        {
            get
            {
                return Palette.GetNamedStyle(WarningStyleName);
            }
        }

        public Style MessageStyle
        {
            get
            {
                return Palette.GetNamedStyle(MessageStyleName);
            }
        }

        #endregion

		#region WriteEntry

		public void WriteEntry(string Header, Style HeaderStyle, Style PrimaryStyle, Style SecondaryStyle, LogEntry Entry)
		{
			lock (writeLock)
			{
				var srcLocFinder = new SourceLocationFinder();
				var node = srcLocFinder.Visit(Entry.Contents);

				// Write the first diagnostic source location,
				// if at all possible.
				if (srcLocFinder.FirstSourceLocation != null)
				{
					NodeWriter.Write(
						CompilerLogExtensions.CreateLineNumberNode(srcLocFinder.FirstSourceLocation),
						Console, Palette);
					Console.Write(": ");
				}

				// Write the header, if there is a header.
				if (!string.IsNullOrWhiteSpace(Header))
				{
					Console.Write(Header + ": ", HeaderStyle);
				}

				// Write the entry's name, provided it has a name.
				string name = Entry.Name;
				if (!string.IsNullOrWhiteSpace(name))
				{
					Console.Write(name, ContrastForegroundColor);
					Console.Write(": ");
				}

				// Write the node itself.
				WriteNodeCore(node, PrimaryStyle, SecondaryStyle);
				Console.WriteLine();
			}
		}
		public void WriteEntry(string Header, Style PrimaryStyle, Style SecondaryStyle, LogEntry Entry)
		{
			WriteEntry(Header, PrimaryStyle, PrimaryStyle, SecondaryStyle, Entry);
		}
		public void WriteEntry(string Header, Style EntryStyle, LogEntry Entry)
		{
			WriteEntry(Header, MakeBrightStyle(EntryStyle), MakeDimStyle(EntryStyle), Entry);
		}
		public void WriteEntry(string Header, Style EntryStyle, string Entry)
		{
			WriteEntry(Header, EntryStyle, new LogEntry("", Entry));
		}
		public void WriteEntry(LogEntry Entry)
		{
			WriteEntry("", MessageStyle, Entry);
		}

		public void WriteBlockEntry(string Header, Style HeaderStyle, Style PrimaryStyle, Style SecondaryStyle, LogEntry Entry)
		{
			lock (writeLock)
			{
				Console.WriteSeparator(2);
				WriteEntry(Header, HeaderStyle, PrimaryStyle, SecondaryStyle, Entry);
				Console.WriteSeparator(2);
			}
		}
		public void WriteBlockEntry(string Header, Style PrimaryStyle, Style SecondaryStyle, LogEntry Entry)
		{
			WriteBlockEntry(Header, PrimaryStyle, PrimaryStyle, SecondaryStyle, Entry);
		}
		public void WriteBlockEntry(string Header, Style EntryStyle, LogEntry Entry)
		{
			WriteBlockEntry(Header, MakeBrightStyle(EntryStyle), MakeDimStyle(EntryStyle), Entry);
		}
		public void WriteBlockEntry(LogEntry Entry)
		{
			WriteBlockEntry("", MessageStyle, Entry);
		}

		#endregion

        #region WriteNode

        public void WriteNode(MarkupNode Node, Style CaretStyle, Style HighlightStyle)
        {
            lock (writeLock)
            {
                WriteNodeCore(Node, CaretStyle, HighlightStyle);
            }
        }

        private void WriteNodeCore(MarkupNode Node, Style CaretStyle, Style HighlightStyle)
        {
            var dependentStyles = new List<Style>();
            dependentStyles.Add(new Style(StyleConstants.CaretMarkerStyleName, CaretStyle.ForegroundColor, CaretStyle.BackgroundColor, GetDiagnosticsCharacterPreferences("caret-character")));
            dependentStyles.Add(new Style(StyleConstants.CaretHighlightStyleName, HighlightStyle.ForegroundColor, HighlightStyle.BackgroundColor, GetDiagnosticsCharacterPreferences("highlight-character")));

            var extPalette = new ExtendedPalette(Palette, dependentStyles);

            NodeWriter.Write(Node, Console, extPalette);
        }

        private string[] GetDiagnosticsCharacterPreferences(string Name)
        {
            string option = Options.GetOption<string>("diagnostics-" + Name, "default");

            switch (option)
            {
                case "dash":
                    return new string[] { Name + ":-" };
                case "caret":
                    return new string[] { Name + ":^" };
                case "tilde":
                    return new string[] { Name + ":~" };
                case "slash":
                    return new string[] { Name + ":/" };
                case "backslash":
                    return new string[] { Name + ":\\" };
                default:
                    if (option != null && option.Length == 1)
                    {
                        return new string[] { Name + ":" + option[0] };
                    }
                    else
                    {
                        return new string[0];
                    }
            }
        }

        #endregion

        public void LogError(LogEntry Entry)
        {
            WriteBlockEntry("error", ErrorStyle, Entry);
        }

        public void LogEvent(LogEntry Entry)
        {
            WriteEntry(Entry);
        }

        public void LogMessage(LogEntry Entry)
        {
            WriteBlockEntry(Entry);
        }

        public void LogWarning(LogEntry Entry)
        {
            WriteBlockEntry("warning", WarningStyle, Entry);
        }

        public void Dispose()
        {
            Console.Dispose();
        }
    }
}
