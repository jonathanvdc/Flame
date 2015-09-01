using Flame.Compiler;
using Flame.Compiler.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pixie;
using Flame.Syntax;

namespace Flame.Verification
{
    public abstract class MethodVerifierBase<TMethod> : MemberVerifierBase<TMethod>
        where TMethod : IMethod
    {
        public MethodVerifierBase()
            : base()
        {
            this.duplicates = new HashSet<TMethod>();
        }
        public MethodVerifierBase(IEnumerable<IAttributeVerifier<TMethod>> Verifiers)
            : base(Verifiers)
        {
            this.duplicates = new HashSet<TMethod>();
        }
        public MethodVerifierBase(MethodVerifierBase<TMethod> Verifier)
            : base(Verifier.AttributeVerifiers)
        {
            this.duplicates = Verifier.duplicates;
        }

        private HashSet<TMethod> duplicates;

        private bool LogDuplicates(TMethod Member, ICompilerLog Log)
        {
            bool isDuplicate = duplicates.Contains(Member);

            if (!isDuplicate)
            {
                var dupls = GetDuplicates(Member, Log).ToArray();

                if (dupls.Length == 0)
                {
                    return true;
                }

                duplicates.UnionWith(dupls);

                var message = new MarkupNode(NodeConstants.TextNodeType, GetDescription(Member) + " has a duplicate.");
                IMarkupNode mainNode = new MarkupNode("entry", new IMarkupNode[] { message, Member.GetSourceLocation().CreateDiagnosticsNode() });
                int count = 1;
                foreach (var item in dupls)
                {
                    var loc = item.GetSourceLocation();
                    if (loc != null)
                    {
                        mainNode = RedefinitionHelpers.Instance.AppendDiagnosticsRemark(mainNode, dupls.Length == 1 ? "Duplicate definition: " : "Duplicate #" + count + ": ", loc);
                    }
                    else
                    {
                        mainNode = new MarkupNode("entry", new IMarkupNode[] { mainNode, new MarkupNode(NodeConstants.RemarksNodeType, "Could not get duplicate #" + count + "'s source location.") });
                    }
                    count++;
                }

                Log.LogError(new LogEntry(dupls.Length == 1 ? "Duplicate " + SingularMemberKindName : "Duplicate " + PluralMemberKindName, mainNode));

                return false;
            }
            return isDuplicate;
        }

        protected abstract IEnumerable<TMethod> GetDuplicates(TMethod Member, ICompilerLog Log);

        protected virtual string GetDescription(TMethod Method)
        {
            return "Method '" + Method.Name + "' of '" + Method.DeclaringType.FullName + "'";
        }

        protected virtual string SingularMemberKindName { get { return "method"; } }
        protected virtual string PluralMemberKindName { get { return "methods"; } }

        protected override bool VerifyMemberCore(TMethod Member, ICompilerLog Log)
        {
            foreach (var item in Member.GetParameters())
            {
                if (item.ParameterType == null)
                {
                    Log.LogError(new LogEntry("Invalid (unresolved?) parameter type",
                        "Parameter '" + item.Name + "' of " + SingularMemberKindName + " '" + Member.FullName + "' has a null parameter type.",
                        Member.GetSourceLocation()));
                    return false;
                }
            }

            bool success = true;
            foreach (var item in Member.BaseMethods)
            {
                if (!item.get_IsVirtual() && !item.get_IsAbstract() && !item.DeclaringType.get_IsInterface())
                {
                    Log.LogError(new LogEntry("Invalid base method",
                        GetDescription(Member) + " has a non-virtual, non-abstract and non-interface base method, declared in '" + item.DeclaringType.FullName + "'",
                        Member.GetSourceLocation()));
                    success = false;
                }

                if (item.get_Access() != Member.get_Access())
                {
                    Log.LogError(new LogEntry("Access modifier mismatch",
                        GetDescription(Member) + " was marked '" + Member.get_Access().GetAccessModifierName() +
                        "' but its base method in '" + item.DeclaringType.FullName + "' was declared '" + item.get_Access().GetAccessModifierName() + "'. " + 
                        "Access modifiers should remain the same within a method inheritance tree.",
                        Member.GetSourceLocation()));
                    success = false;
                }
            }
            if (!LogDuplicates(Member, Log))
            {
                success = false;
            }
            if (Member.get_IsAbstract() && !Member.DeclaringType.get_IsAbstract() && !Member.DeclaringType.get_IsInterface())
            {
                Log.LogError(new LogEntry("Abstract method in non-abstract type",
                    GetDescription(Member) + " has been marked abstract, but its declaring type is neither abstract nor an interface.",
                    Member.GetSourceLocation()));
                success = false;
            }
            return success;
        }
    }
}
