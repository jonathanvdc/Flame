using Pixie;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Cli
{
    /// <summary>
    /// A style for console output.
    /// </summary>
    public class Style
    {
        public Style(string Name, Color ForegroundColor, Color BackgroundColor, params string[] Preferences)
        {
            this.Name = Name;
            this.ForegroundColor = ForegroundColor;
            this.BackgroundColor = BackgroundColor;
            this.Preferences = Preferences;
        }

        public string Name { get; private set; }
        public Color ForegroundColor { get; private set; }
        public Color BackgroundColor { get; private set; }
        public string[] Preferences { get; private set; }
    }
}
