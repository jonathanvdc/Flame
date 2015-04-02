using Pixie;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Cli
{
    public class ExtendedPalette : IStylePalette
    {
        public ExtendedPalette(IStylePalette Palette, IEnumerable<Style> ExtendedStyles)
        {
            this.Palette = Palette;
            this.ExtendedStyles = ExtendedStyles.ToDictionary(item => item.Name, item => item);
        }
        public ExtendedPalette(IStylePalette Palette, IReadOnlyDictionary<string, Style> ExtendedStyles)
        {
            this.Palette = Palette;
            this.ExtendedStyles = ExtendedStyles;
        }

        public IStylePalette Palette { get; private set; }
        public IReadOnlyDictionary<string, Style> ExtendedStyles { get; private set; }

        public Style GetNamedStyle(string Name)
        {
            if (ExtendedStyles.ContainsKey(Name))
            {
                return ExtendedStyles[Name];
            }
            else
            {
                return Palette.GetNamedStyle(Name);
            }
        }

        public bool IsNamedStyle(string Name)
        {
            return ExtendedStyles.ContainsKey(Name) || Palette.IsNamedStyle(Name);
        }

        public Color MakeBrightColor(Color Value)
        {
            return Palette.MakeBrightColor(Value);
        }

        public Color MakeDimColor(Color Value)
        {
            return Palette.MakeDimColor(Value);
        }
    }
}
