using Pixie;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Cli
{
    public class NeutralDiagnosticsWriter : INodeWriter
    {
        public NeutralDiagnosticsWriter(INodeWriter MainWriter)
        {
            this.MainWriter = MainWriter;
        }

        public INodeWriter MainWriter { get; private set; }

        public static Style GetNeutralCaretHighlightStyle(IStylePalette Palette)
        {
            if (Palette.IsNamedStyle(StyleConstants.NeutralCaretHighlightStyleName))
            {
                return Palette.GetNamedStyle(StyleConstants.NeutralCaretHighlightStyleName);
            }
            else
            {
                return new Style(StyleConstants.NeutralCaretHighlightStyleName, Palette.MakeDimColor(new Color(0.0, 1.0, 0.0)), new Color());
            }
        }

        public static Style GetNeutralCaretMarkerStyle(IStylePalette Palette)
        {
            if (Palette.IsNamedStyle(StyleConstants.NeutralCaretMarkerStyleName))
            {
                return Palette.GetNamedStyle(StyleConstants.NeutralCaretMarkerStyleName);
            }
            else
            {
                return new Style(StyleConstants.NeutralCaretMarkerStyleName, Palette.MakeBrightColor(new Color(0.0, 1.0, 0.0)), new Color());
            }
        }

        public void Write(IMarkupNode Node, IConsole Console, IStylePalette Palette)
        {
            var neutralStyle = GetNeutralCaretMarkerStyle(Palette);
            var neutralHighlightStyle = GetNeutralCaretHighlightStyle(Palette);

            var newPalette = new ExtendedPalette(Palette, new Style[] 
            {
                new Style(StyleConstants.CaretMarkerStyleName, neutralStyle.ForegroundColor, neutralStyle.BackgroundColor, neutralStyle.Preferences),
                new Style(StyleConstants.CaretHighlightStyleName, neutralHighlightStyle.ForegroundColor, neutralHighlightStyle.BackgroundColor, neutralHighlightStyle.Preferences)
            });
            NodeWriter.WriteDefault(Node, Console, newPalette, MainWriter);
        }
    }
}
