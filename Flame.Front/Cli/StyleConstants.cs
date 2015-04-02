using Pixie;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Cli
{
    public static class StyleConstants
    {
        public const string RemarksStyleName = "remarks";
        public const string CaretMarkerStyleName = "caret-marker";
        public const string CaretHighlightStyleName = "caret-highlight";
        public const string HighlightStyleName = "highlight";
        public const string HighlightMissingStyleName = "highlight-missing";
        public const string HighlightExtraStyleName = "highlight-extra";

        public static Style GetBrightStyle(IStylePalette Palette, string Name, Color ForegroundColor, params string[] Preferences)
        {
            if (Palette.IsNamedStyle(Name))
            {
                return Palette.GetNamedStyle(Name);
            }
            else
            {
                return new Style(Name, Palette.MakeBrightColor(ForegroundColor), new Color(), Preferences);
            }
        }
        public static Style GetDimStyle(IStylePalette Palette, string Name, Color ForegroundColor, params string[] Preferences)
        {
            if (Palette.IsNamedStyle(Name))
            {
                return Palette.GetNamedStyle(Name);
            }
            else
            {
                return new Style(Name, Palette.MakeDimColor(ForegroundColor), new Color(), Preferences);
            }
        }
    }
}
