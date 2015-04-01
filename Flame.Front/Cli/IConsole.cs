using Pixie;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Cli
{
    /// <summary>
    /// Defines common low-level functionality for consoles.
    /// </summary>
    public interface IConsole : IDisposable
    {
        /// <summary>
        /// Gets the console's description.
        /// </summary>
        ConsoleDescription Description { get; }

        /// <summary>
        /// Applies the given style to the console output.
        /// </summary>
        /// <param name="Value"></param>
        void PushStyle(Style Value);
        /// <summary>
        /// Undoes the last style from the console output.
        /// </summary>
        void PopStyle();

        /// <summary>
        /// Writes a string of text to the console.
        /// </summary>
        /// <param name="Text"></param>
        void Write(string Text);

        /// <summary>
        /// Writes a newline to the console.
        /// </summary>
        void WriteLine();
    }

    public static class ConsoleExtensions
    {
        /// <summary>
        /// Writes a string of text to the console, followed by a newline.
        /// </summary>
        /// <param name="Target"></param>
        /// <param name="Text"></param>
        public static void WriteLine(this IConsole Target, string Text)
        {
            Target.Write(Text);
            Target.WriteLine();
        }
        /// <summary>
        /// Writes the text representation of the given object to the console.
        /// </summary>
        /// <param name="Target"></param>
        /// <param name="Value"></param>
        public static void Write(this IConsole Target, object Value)
        {
            Target.Write(Value.ToString());
        }
        /// <summary>
        /// Writes the text representation of the given object to the console, followed by a newline.
        /// </summary>
        /// <param name="Target"></param>
        /// <param name="Value"></param>
        public static void WriteLine(this IConsole Target, object Value)
        {
            Target.WriteLine(Target.ToString());
        }

        public static void WriteLine(this IConsole Target, string Text, Style Style)
        {
            Target.PushStyle(Style);
            Target.WriteLine(Text);
            Target.PopStyle();
        }

        public static void WriteLine(this IConsole Target, string Text, Color Foreground)
        {
            Target.WriteLine(Text, new Style("custom", Foreground, new Color()));
        }

        public static void Write(this IConsole Target, string Text, Style Style)
        {
            Target.PushStyle(Style);
            Target.Write(Text);
            Target.PopStyle();
        }

        public static void Write(this IConsole Target, string Text, Color Foreground)
        {
            Target.Write(Text, new Style("custom", Foreground, new Color()));
        }
    }
}
