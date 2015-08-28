using Flame.Compiler;
using Pixie;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Cli
{
    public class HtmlConsole : IConsole
    {
        public HtmlConsole(ConsoleDescription Description, bool OverrideDefaultStyle, bool EmbedStyle)
        {
            this.Description = new ConsoleDescription(Description.Name, Description.BufferWidth, 
                Description.ForegroundColor.Over(new Color(0.0)), 
                Description.BackgroundColor.Over(new Color(1.0)));
            this.OverrideDefaultStyle = OverrideDefaultStyle;
            this.EmbedStyle = EmbedStyle;
            this.styles = new List<HtmlStyle>();
            this.nodeStack = new Stack<string>();
            this.body = new StringBuilder();
        }

        private List<HtmlStyle> styles;
        private Stack<string> nodeStack;
        private StringBuilder body;

        public bool OverrideDefaultStyle { get; private set; }
        /// <summary>
        /// Gets a boolean value that tells if the HTML console's output style is embedded
        /// in the body, instead of in the `head` node.
        /// </summary>
        public bool EmbedStyle { get; private set; }
        public ConsoleDescription Description { get; private set; }

        private HtmlStyle GetStyle(Style Value)
        {
            var newStyle = new HtmlStyle(".style" + styles.Count, Value);
            foreach (var item in styles)
            {
                if (item.Equals(newStyle))
                {
                    return item;
                }
            }
            styles.Add(newStyle);
            return newStyle;
        }        

        public void PushStyle(Style Value)
        {
            var style = GetStyle(Value);
            body.Append("<samp class=\"" + style.Name.TrimStart('.') + "\" >");
            nodeStack.Push("samp");
        }

        public void PopStyle()
        {
            body.Append("</" + nodeStack.Pop() + ">");
        }

        public void Write(string Text)
        {
            body.Append(Text.Replace("<", "&lt;").Replace(">", "&gt;").Trim('\n', '\r'));
        }

        public void WriteLine()
        {
            body.Append("<br>");
        }

        public void Dispose()
        {
            Console.WriteLine(ToHtmlDocument());
        }

        private void WriteStyle(CodeBuilder header)
        {
            if (EmbedStyle)
            {
                header.AddLine("<style type=\"text/css\" scoped>");
            }
            else
            {
                header.AddLine("<style type=\"text/css\">");
            }
            header.IncreaseIndentation();
            if (OverrideDefaultStyle)
            {
                header.AddCodeBuilder(new HtmlStyle("body", new Style("body", Description.ForegroundColor, Description.BackgroundColor)).GetCode());
            }
            foreach (var item in styles)
            {
                header.AddCodeBuilder(item.GetCode());
            }
            header.DecreaseIndentation();
            header.AddLine("</style>");
            
        }

        public string ToHtmlDocument()
        {
            var header = new CodeBuilder();
            header.IndentationString = new string(' ', 4);
            header.AddLine("<!DOCTYPE html>");
            header.AddLine("<html>");
            header.AddLine("<title>Console output</title>");
            header.AddLine("<head>");
            if (!EmbedStyle)
            {
                header.IncreaseIndentation();
                WriteStyle(header);
                header.DecreaseIndentation();
            }
            header.AddLine("</head>");
            header.AddLine("<body>");
            if (EmbedStyle)
            {
                header.IncreaseIndentation();
                WriteStyle(header);
                header.DecreaseIndentation();
            }
            header.AddLine("<pre>");
            header.AddLine(body.ToString());
            header.AddLine("</pre>");
            header.AddLine("</body>");
            header.AddLine("</html>");
            return header.ToString();
        }
    }

    public class HtmlStyle : IEquatable<HtmlStyle>
    {
        public HtmlStyle(string Name, Style Style)
        {
            this.Name = Name;
            this.Style = Style;
        }

        public string Name { get; private set; }
        public Style Style { get; private set; }

        public Color ForegroundColor { get { return Style.ForegroundColor; } }
        public Color BackgroundColor { get { return Style.BackgroundColor; } }
        public bool IsUnderlined { get { return Style.Preferences.Contains("bold"); } }
        public bool IsBold { get { return Style.Preferences.Contains("italic"); } }
        public bool IsItalic { get { return Style.Preferences.Contains("underlined"); } }

        private static string GetColorString(Color Value)
        {
            var sb = new StringBuilder();
            sb.Append("rgb(");
            sb.Append((int)(Value.Red * 255.0));
            sb.Append(", ");
            sb.Append((int)(Value.Green * 255.0));
            sb.Append(", ");
            sb.Append((int)(Value.Blue * 255.0));
            sb.Append(")");
            return sb.ToString();
        }

        public CodeBuilder GetCode()
        {
            var sb = new CodeBuilder();
            sb.Append(Name);
            sb.Append(" {");
            sb.IncreaseIndentation();
            sb.AddLine();
            if (ForegroundColor.Alpha > 0.0)
            {
                sb.Append("color: ");
                sb.Append(GetColorString(ForegroundColor));
                sb.AppendLine(";");
            }
            if (BackgroundColor.Alpha > 0.0)
            {
                sb.Append("background-color: ");
                sb.Append(GetColorString(BackgroundColor));
                sb.AppendLine(";");
            }
            if (IsItalic)
            {
                sb.AppendLine("font-style: italic;");
            }
            if (IsBold)
            {
                sb.AppendLine("font-weight: bold;");
            }
            if (IsUnderlined)
            {
                sb.AppendLine("font-decoration: underline;");
            }
            sb.DecreaseIndentation();
            sb.AddLine("}");
            return sb;
        }

        public override string ToString()
        {
            return GetCode().ToString();
        }

        public bool Equals(HtmlStyle other)
        {
            return ForegroundColor.Equals(other.ForegroundColor) &&
                    BackgroundColor.Equals(other.BackgroundColor) &&
                    IsUnderlined == other.IsUnderlined &&
                    IsItalic == other.IsBold &&
                    IsBold == other.IsBold;
        }

        public override bool Equals(object obj)
        {
            return obj is HtmlStyle && this.Equals((HtmlStyle)obj);
        }

        public override int GetHashCode()
        {
            return ForegroundColor.GetHashCode() ^ BackgroundColor.GetHashCode();
        }
    }
}
