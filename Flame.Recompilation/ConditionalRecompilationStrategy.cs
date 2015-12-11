using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    /// <summary>
    /// A recompilation strategy where all members in
    /// the assembly for whom a predicate function evaluates to true
    /// are marked as root.
    /// </summary>
    public sealed class ConditionalRecompilationStrategy : IRecompilationStrategy
    {
        /// <summary>
        /// Creates a new condional compilation strategy based on the given function.
        /// </summary>
        /// <param name="IsRoot"></param>
        public ConditionalRecompilationStrategy(Func<IMember, bool> IsRoot)
        {
            this.IsRoot = IsRoot;
        }

        /// <summary>
        /// Tells if a given member in the input assembly is a recompilation root.
        /// </summary>
        public Func<IMember, bool> IsRoot { get; private set; }

        #region Static

        /// <summary>
        /// A recompilation strategy where absolutely every member in the 
        /// input assembly is marked as a root.
        /// </summary>
        public static ConditionalRecompilationStrategy TotalRecompilationStrategy
        {
            get
            {
                return new ConditionalRecompilationStrategy(_ => true);
            }
        }

        /// <summary>
        /// A recompilation strategy where all externally visible members, i.e. public and 
        /// protected members, are recompiled.
        /// </summary>
        public static ConditionalRecompilationStrategy ExternallyVisibleRecompilationStrategy
        {
            get
            {
                return new ConditionalRecompilationStrategy(item =>
                {
                    switch (item.GetAccess())
                    {
                        case AccessModifier.Assembly:
                        case AccessModifier.Private:
                        case AccessModifier.ProtectedAndAssembly:
                            return false;
                        case AccessModifier.Protected:
                        case AccessModifier.ProtectedOrAssembly:
                        case AccessModifier.Public:
                        default:
                            return true; // Err on the safe side.
                    }
                });
            }
        }

        #endregion

        private IEnumerable<IMember> GetAllMembers(IEnumerable<IType> Types)
        {
            return Types.SelectMany(GetAllMembers);
        }

        private IEnumerable<IMember> GetAllMembers(IType Type)
        {
            if (IsRoot(Type))
            {
                yield return Type;
                foreach (var field in Type.Fields.Where(IsRoot))
                {
                    yield return field;
                }
                foreach (var method in Type.Methods.Where(IsRoot))
                {
                    yield return method;
                }
                foreach (var prop in Type.Properties.Where(item => IsRoot(item)))
                {
                    yield return prop;
                    foreach (var accessor in prop.Accessors.Where(IsRoot))
                    {
                        yield return accessor;
                    }
                }
                if (Type is INamespace)
                {
                    foreach (var item in GetAllMembers(((INamespace)Type).Types))
                    {
                        yield return item;
                    }
                }
            }
        }

        public IEnumerable<IMember> GetRoots(IAssembly Assembly)
        {
            return GetAllMembers(Assembly.CreateBinder().GetTypes());
        }
    }
}
