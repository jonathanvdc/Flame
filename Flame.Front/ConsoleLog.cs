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
        public ConsoleLog(IConsole Console, ICompilerOptions Options)
            : this(Console, Options, CreateDefaultPalette(Console.Description), CreateDefaultNodeWriter(Console.Description.BufferWidth))
        {
        }

        public ConsoleLog(IConsole Console, ICompilerOptions Options, IStylePalette Palette)
            : this(Console, Options, Palette, CreateDefaultNodeWriter(Console.Description.BufferWidth))
        {
        }

        public ConsoleLog(IConsole Console, ICompilerOptions Options, INodeWriter NodeWriter)
            : this(Console, Options, CreateDefaultPalette(Console.Description), NodeWriter)
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

        private void WriteEntryInternal(Color PrimaryColor, Color SecondaryColor, LogEntry Entry)
        {
            string name = Entry.Name;
            if (!string.IsNullOrWhiteSpace(name))
            {
                Console.Write(name, ContrastForegroundColor);
                Console.Write(": ");
            }
            WriteNode(Entry.Contents, PrimaryColor, SecondaryColor);
            Console.WriteLine();
        }

        public void WriteEntry(string Header, Color HeaderColor, Color PrimaryColor, Color SecondaryColor, LogEntry Entry)
        {
            lock (writeLock)
            {
                if (!string.IsNullOrWhiteSpace(Header))
                {
                    Console.Write(Header + ": ", HeaderColor);
                }
                WriteEntryInternal(PrimaryColor, SecondaryColor, Entry);
            }
        }
        public void WriteEntry(string Header, Color PrimaryColor, Color SecondaryColor, LogEntry Entry)
        {
            WriteEntry(Header, PrimaryColor, PrimaryColor, SecondaryColor, Entry);
        }
        public void WriteEntry(string Header, Color HeaderColor, string Entry)
        {
            WriteEntry(Header, HeaderColor, BrightGreen, DimGreen, new LogEntry("", Entry));
        }
        public void WriteEntry(string Header, LogEntry Entry)
        {
            WriteEntry(Header, new Color(), BrightGreen, DimGreen, Entry);
        }
        public void WriteEntry(LogEntry Entry)
        {
            WriteEntry("", BrightGreen, DimGreen, Entry);
        }

        public void WriteBlockEntry(string Header, Color HeaderColor, Color PrimaryColor, Color SecondaryColor, LogEntry Entry)
        {
            lock (writeLock)
            {
                Console.WriteSeparator(2);
                WriteEntry(Header, HeaderColor, PrimaryColor, SecondaryColor, Entry);
                Console.WriteSeparator(2);
            }
        }
        public void WriteBlockEntry(string Header, Color PrimaryColor, Color SecondaryColor, LogEntry Entry)
        {
            WriteBlockEntry(Header, PrimaryColor, PrimaryColor, SecondaryColor, Entry);
        }
        public void WriteBlockEntry(LogEntry Entry)
        {
            WriteBlockEntry("", BrightGreen, DimGreen, Entry);
        }

        #region Defaults

        #region Node Writer

        public static INodeWriter CreateDefaultNodeWriter(int BufferWidth)
        {
            var writer = new NodeWriter();
            writer.Writers[NodeConstants.RemarksNodeType] = new RemarksNodeWriter(writer);
            writer.Writers[NodeConstants.SourceNodeType] = new SourceNodeWriter(new string(' ', 4), BufferWidth - 8);
            writer.Writers["list"] = new ListNodeWriter(writer);
            writer.Writers[NodeConstants.HighlightNodeType] = new HighlightingNodeWriter(writer);
            return writer;
        }

        #endregion

        #region Palette

        public static IStylePalette CreateDefaultPalette(ConsoleDescription Description)
        {
            return CreateDefaultPalette(Description.ForegroundColor, Description.BackgroundColor);
        }

        public static IStylePalette CreateDefaultPalette(Color ForegroundColor, Color BackgroundColor)
        {
            var palette = new StylePalette(ForegroundColor, BackgroundColor);
            palette.RegisterStyle(RemarksNodeWriter.GetRemarksStyle(palette));
            palette.RegisterStyle(HighlightingNodeWriter.GetHighlightStyle(palette));
            palette.RegisterStyle(HighlightingNodeWriter.GetHighlightExtraStyle(palette));
            palette.RegisterStyle(HighlightingNodeWriter.GetHighlightMissingStyle(palette));
            return palette;
        }

        #endregion

        #endregion

        #region Palette

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

        public Color BrightRed
        {
            get
            {
                return Palette.MakeBrightColor(DefaultConsole.ToPixieColor(ConsoleColor.Red));
            }
        }

        public Color DimRed
        {
            get
            {
                return Palette.MakeDimColor(DefaultConsole.ToPixieColor(ConsoleColor.Red));
            }
        }

        public Color BrightYellow
        {
            get
            {
                return Palette.MakeBrightColor(DefaultConsole.ToPixieColor(ConsoleColor.Yellow));
            }
        }

        public Color DimYellow
        {
            get
            {
                return Palette.MakeDimColor(DefaultConsole.ToPixieColor(ConsoleColor.Yellow));
            }
        }

        public Color BrightBlue
        {
            get
            {
                return Palette.MakeBrightColor(DefaultConsole.ToPixieColor(ConsoleColor.Blue));
            }
        }

        public Color DimBlue
        {
            get
            {
                return Palette.MakeDimColor(DefaultConsole.ToPixieColor(ConsoleColor.Blue));
            }
        }

        public Color BrightCyan
        {
            get
            {
                return Palette.MakeBrightColor(DefaultConsole.ToPixieColor(ConsoleColor.Cyan));
            }
        }

        public Color DimCyan
        {
            get
            {
                return Palette.MakeDimColor(DefaultConsole.ToPixieColor(ConsoleColor.Cyan));
            }
        }

        public Color BrightMagenta
        {
            get
            {
                return Palette.MakeBrightColor(DefaultConsole.ToPixieColor(ConsoleColor.Magenta));
            }
        }

        public Color DimMagenta
        {
            get
            {
                return Palette.MakeDimColor(DefaultConsole.ToPixieColor(ConsoleColor.Magenta));
            }
        }

        public Color BrightGreen
        {
            get
            {
                return Palette.MakeBrightColor(DefaultConsole.ToPixieColor(ConsoleColor.Green));
            }
        }

        public Color DimGreen
        {
            get
            {
                return Palette.MakeDimColor(DefaultConsole.ToPixieColor(ConsoleColor.Green));
            }
        }

        public Color BrightGray
        {
            get
            {
                return Palette.MakeBrightColor(DefaultConsole.ToPixieColor(ConsoleColor.Gray));
            }
        }

        public Color DimGray
        {
            get
            {
                return Palette.MakeDimColor(DefaultConsole.ToPixieColor(ConsoleColor.Gray));
            }
        }

        #endregion

        #region Write*Node

        public void WriteNode(IMarkupNode Node, Color CaretColor, Color HighlightColor)
        {
            lock (writeLock)
            {
                WriteNodeCore(Node, CaretColor, HighlightColor);
            }
        }

        private void WriteNodeDefault(IMarkupNode Node, Color CaretColor, Color HighlightColor)
        {
            Console.Write(Node.GetText());
            foreach (var item in Node.Children)
            {
                WriteNodeCore(item, CaretColor, HighlightColor);
            }
        }

        private void WriteNodeCore(IMarkupNode Node, Color CaretColor, Color HighlightColor)
        {
            var dependentStyles = new List<Style>();
            dependentStyles.Add(new Style(StyleConstants.CaretMarkerStyleName, CaretColor, new Color()));
            dependentStyles.Add(new Style(StyleConstants.CaretHighlightStyleName, HighlightColor, new Color()));

            var extPalette = new ExtendedPalette(Palette, dependentStyles);

            NodeWriter.Write(Node, Console, extPalette);
        }

        #endregion

        public void LogError(LogEntry Entry)
        {
            WriteBlockEntry("Error", BrightRed, DimRed, Entry);
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
            WriteBlockEntry("Warning", BrightYellow, DimYellow, Entry);
        }

        public void Dispose()
        {
            Console.Dispose();
        }
    }
}
