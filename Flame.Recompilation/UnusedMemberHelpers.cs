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
        /// A warning group for members that are never used.
        /// </summary>
        public static readonly WarningDescription UnusedMemberWarningGroup = new WarningDescription("unused", Warnings.Instance.Extra);

        /// <summary>
        /// A warning for types that are never used.
        /// </summary>
        public static readonly WarningDescription UnusedTypeWarning = new WarningDescription("unused-type", UnusedMemberWarningGroup);

        /// <summary>
        /// A warning for methods that are never used.
        /// </summary>
        public static readonly WarningDescription UnusedMethodWarning = new WarningDescription("unused-method", UnusedMemberWarningGroup);

        /// <summary>
        /// A warning for fields that are never used.
        /// </summary>
        public static readonly WarningDescription UnusedFieldWarning = new WarningDescription("unused-field", UnusedMemberWarningGroup);

        private static void WarnUnused(IMember Member, string MemberType, WarningDescription Warning, ICompilerLog Log)
        {
            var srcLoc = Member.GetSourceLocation();
            if (srcLoc != null)
            {
                // Don't issue warnings for members that don't
                // have source locations, because they could have been
                // be compiler-generated. We don't want to 
                // confuse people with false positives like that.

                string upperMemberType = MemberType.Length > 0 ? MemberType.Substring(0, 1).ToUpper() + MemberType.Substring(1) : MemberType;

                Log.LogWarning(new LogEntry(
                    "Unused " + MemberType,
                    Warning.CreateMessage(upperMemberType + " '" + Member.Name + "' is never used. "),
                    srcLoc));
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
            if (UnusedTypeWarning.UseWarning(Recompiler.Log.Options))
            {
                foreach (var item in Recompiler.FilterEliminatedTypes(SourceTypes))
                {
                    WarnUnused(item, "type", UnusedTypeWarning, Recompiler.Log);                    
                }
            }
            bool warnFields = UnusedFieldWarning.UseWarning(Recompiler.Log.Options);
            bool warnMethods = UnusedMethodWarning.UseWarning(Recompiler.Log.Options);
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
                            WarnUnused(item, "field", UnusedFieldWarning, Recompiler.Log);
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
                        WarnUnused(item, methodType, UnusedMethodWarning, Recompiler.Log);
                    }
                }
            }
        }
    }
}
