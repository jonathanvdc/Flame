using System.Globalization;
using System.Linq;

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
                return new SimpleName(
                    name.Substring(0, backtickIndex),
                    genericParamCount);
            }
            else
            {
                return new SimpleName(name);
            }
        }

        /// <summary>
        /// Parses an IL namespace as a Flame qualified name.
        /// </summary>
        /// <param name="ns">The namespace to parse.</param>
        /// <returns>A qualified name.</returns>
        public static QualifiedName ParseNamespace(string ns)
        {
            if (string.IsNullOrEmpty(ns))
            {
                return new QualifiedName();
            }
            else
            {
                return new QualifiedName(
                    ns
                        .Split('.')
                        .Select(ParseSimpleName)
                        .ToArray());
            }
        }
    }
}