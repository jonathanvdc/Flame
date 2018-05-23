using System.Globalization;

namespace Flame.Clr
{
    /// <summary>
    /// A collection of functions that help convert back and forth
    /// between Flame names and IL names.
    /// </summary>
    public static class NameConversion
    {
        /// <summary>
        /// Parses an IL name as a Flame simple name.
        /// </summary>
        /// <param name="name">The name to parse.</param>
        /// <returns>A simple name.</returns>
        public static SimpleName ParseSimpleName(string name)
        {
            int backtickIndex = name.LastIndexOf('`');
            int genericParamCount;
            if (backtickIndex >= 0
                && int.TryParse(
                    name.Substring(backtickIndex + 1),
                    NumberStyles.None,
                    CultureInfo.InstalledUICulture,
                    out genericParamCount))
            {
                return new SimpleName(name, genericParamCount);
            }
            else
            {
                return new SimpleName(name);
            }
        }
    }
}