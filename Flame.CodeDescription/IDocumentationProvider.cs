using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.CodeDescription
{
    public interface IDocumentationProvider
    {
        /// <summary>
        /// Gets all description attributes this documentation provider has for the given member.
        /// </summary>
        /// <param name="Member"></param>
        /// <returns></returns>
        IEnumerable<DescriptionAttribute> GetDescriptionAttributes(IMember Member);
    }

    public interface IDocumentationBuilder : IDocumentationProvider
    {
        /// <summary>
        /// Adds documentation for a description attribute applied to the given member.
        /// </summary>
        /// <param name="Member"></param>
        /// <param name="Attribute"></param>
        void AddDescriptionAttribute(IMember Member, DescriptionAttribute Attribute);
        /// <summary>
        /// Gets the documentation builder's output extension.
        /// </summary>
        string Extension { get; }
        /// <summary>
        /// Saves the documentation builder to a stream.
        /// </summary>
        /// <param name="Target"></param>
        void Save(IOutputProvider Target);
    }
}
