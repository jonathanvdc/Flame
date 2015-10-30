using Flame.Compiler;
using Pixie;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public static class UnusedMemberHelpers
    {
        /// <summary>
        /// A warning name for types that are never used.
        /// </summary>
        public const string UnusedTypeWarningName = "unused-type";

        /// <summary>
        /// A warning name for methods that are never used.
        /// </summary>
        public const string UnusedMethodWarningName = "unused-method";

        /// <summary>
        /// A warning name for fields that are never used.
        /// </summary>
        public const string UnusedFieldWarningName = "unused-field";

        private static void WarnUnused(IMember Member, string MemberType, string WarningName, ICompilerLog Log)
        {
            var srcLoc = Member.GetSourceLocation();
            if (srcLoc != null)
            {
                // Don't issue warnings for members that don't
                // have source locations, because they could be
                // be compiler-generated. We don't want to 
                // confuse people with false positives like those.

                string upperMemberType = MemberType.Length > 0 ? MemberType.Substring(0, 1).ToUpper() + MemberType.Substring(1) : MemberType;

                var desc = new MarkupNode(NodeConstants.TextNodeType, upperMemberType + " '" + Member.Name + "' is never used. ");
                var cause = new MarkupNode(NodeConstants.CauseNodeType, Warnings.Instance.GetWarningName(WarningName));
                var entry = new LogEntry("Unused " + MemberType, new IMarkupNode[] { desc, cause }, srcLoc);
                Log.LogWarning(entry);
            }
        }

        /// <summary>
        /// Warns about unused members in the given assembly recompiler.
        /// Only members belonging to the given set of types are warned about.
        /// </summary>
        /// <param name="Recompiler"></param>
        /// <param name="SourceTypes"></param>
        public static void WarnUnusedMembers(AssemblyRecompiler Recompiler, IEnumerable<IType> SourceTypes)
        {
            if (Recompiler.Log.UseDefaultWarnings(UnusedTypeWarningName))
            {
                foreach (var item in Recompiler.FilterEliminatedTypes(SourceTypes))
                {
                    WarnUnused(item, "type", UnusedTypeWarningName, Recompiler.Log);                    
                }
            }
            bool warnFields = Recompiler.Log.UseDefaultWarnings(UnusedFieldWarningName);
            bool warnMethods = Recompiler.Log.UseDefaultWarnings(UnusedMethodWarningName);
            if (warnFields || warnMethods)
            {
                var recompSourceTypes = Recompiler.FilterRecompiledTypes(SourceTypes);
                if (warnFields)
                {
                    foreach (var item in Recompiler.FilterEliminatedFields(recompSourceTypes.SelectMany(item => item.Fields)))
                    {
                        // Don't issue warnings for static constant fields, because
                        // their values could have just been inlined: we can't
                        // tell if they haven't been used or not.
                        if (!(item.IsStatic && item.get_IsConstant()))
                        {
                            WarnUnused(item, "field", UnusedFieldWarningName, Recompiler.Log);
                        }
                    }
                }
                if (warnMethods)
                {
                    foreach (var item in Recompiler.FilterEliminatedMethods(
                        recompSourceTypes.SelectMany(item => 
                            item.Methods.Concat(item.Properties.SelectMany(prop => prop.Accessors)))))
                    {
                        string methodType = item.IsConstructor ? "constructor" : item is IAccessor ? "accessor" : "method";
                        WarnUnused(item, methodType, UnusedMethodWarningName, Recompiler.Log);
                    }
                }
            }
        }
    }
}
