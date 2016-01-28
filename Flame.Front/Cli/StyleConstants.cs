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
        public const string NeutralCaretMarkerStyleName = "neutral-caret-marker";
        public const string NeutralCaretHighlightStyleName = "neutral-caret-highlight";
        public const string HighlightStyleName = "highlight";
        public const string HighlightMissingStyleName = "highlight-missing";
        public const string HighlightExtraStyleName = "highlight-extra";
        public const string SourceQuoteStyleName = "source-quote";

        public const string ColorModifierAttribute = "color-modifier";
        public const string DimColorModifier = "dim";
        public const string BrightColorModifier = "bright";
        public const string NoColorModifier = "none";

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

        public static Color ModifyColor(Color Value, IStylePalette Palette, string Modifier)
        {
            switch (Modifier.ToLower())
            {
                case DimColorModifier:
                    return Palette.MakeDimColor(Value);
                case BrightColorModifier:
                    return Palette.MakeBrightColor(Value);
                case NoColorModifier:
                default:
                    return Value;
            }
        }

        public static Color GetColor(this MarkupNode Node, IStylePalette Palette)
        {
            var color = Node.GetColor();
            string modifier = Node.Attributes.Get<string>(ColorModifierAttribute, NoColorModifier);
            return ModifyColor(color, Palette, modifier);
        }

        public static Style GetStyle(this MarkupNode Node, IStylePalette Palette)
        {
            return new Style("custom", Node.GetColor(Palette), new Color());
        }
    }
}
