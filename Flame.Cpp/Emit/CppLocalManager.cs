using Flame.Compiler;
using Flame.Compiler.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    /// <summary>
    /// A class that manages C++ local variables.
    /// </summary>
    public class CppLocalManager
    {
        public CppLocalManager(ICodeGenerator CodeGenerator)
        {
            this.CodeGenerator = CodeGenerator;
            this.locals = new Dictionary<UniqueTag, CppLocal>();
            this.freedLocals = new List<CppLocal>();
        }

        /// <summary>
        /// Gets the code generator associated with this local variable manager.
        /// </summary>
        public ICodeGenerator CodeGenerator { get; private set; }

        private Dictionary<UniqueTag, CppLocal> locals;
        private List<CppLocal> freedLocals;

        /// <summary>
        /// Gets all locals declared by this local variable manager.
        /// </summary>
        public IEnumerable<CppLocal> Locals { get { return locals.Values; } }

        /// <summary>
        /// Releases a local variable.
        /// </summary>
        /// <param name="Local"></param>
        public void Release(CppLocal Local)
        {
            this.freedLocals.Add(Local);
        }

        /// <summary>
        /// Tries to recycle a released variable with the specified type. If successful, the variable is returned and removed from the released variables list. Otherwise null.
        /// </summary>
        /// <param name="VariableType"></param>
        /// <returns></returns>
        public CppLocal Reuse(UniqueTag Tag, IType VariableType)
        {
            for (int i = 0; i < freedLocals.Count; i++)
            {
                if (freedLocals[i].Type.Equals(VariableType))
                {
                    var item = freedLocals[i];
                    freedLocals.RemoveAt(i);
                    locals[Tag] = item;
                    return item;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets a boolean value that indicates whether a variable with the given name can be declared.
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public bool CanDeclare(UnqualifiedName Name)
        {
            if (string.IsNullOrWhiteSpace(Name.ToString()))
            {
                return false;
            }
            else
            {
                return !Locals.Any((item) => item.Member.Name.Equals(Name)) && !CodeGenerator.Method.GetParameters().Any((item) => item.Name.Equals(Name));
            }
        }

        #region Identifier Generation

        private string GenerateIdentifier(IVariableMember Member)
        {
            if (string.IsNullOrWhiteSpace(Member.Name.ToString()))
            {
                return GenerateIdentifier(Member.VariableType);
            }
            else
            {
                return GenerateIdentifier(Member.Name.ToString());
            }
        }

        private string GenerateIdentifier(IType Type)
        {
            if (Type.GetIsBit())
            {
                return GenerateIdentifier("data", "bits");
            }
            else if (Type.Equals(PrimitiveTypes.Boolean))
            {
                return GenerateIdentifier("flag");
            }
            else if (Type.GetIsFloatingPoint())
            {
                return GenerateIdentifier("x", "y", "z", "num");
            }
            else if (Type.GetIsInteger())
            {
                return GenerateIdentifier("i", "j", "k", "num");
            }
            else if (Type.Equals(PrimitiveTypes.String))
            {
                return GenerateIdentifier("s", "str", "str");
            }
            else if (Type.Equals(PrimitiveTypes.Char))
            {
                return GenerateIdentifier("ch", "c", "ch");
            }
            else if (Type.GetIsArray())
            {
                return GenerateIdentifier("arr", "temp");
            }
            else if (Type.GetIsVector())
            {
                return GenerateIdentifier("vec", "temp");
            }
            else if (Type.GetIsPointer())
            {
                return GenerateIdentifier("ptr");
            }
            else
            {
                return GenerateIdentifier("temp");
            }
        }

        private string GenerateIdentifier(params string[] Names)
        {
            foreach (var item in Names)
            {
                if (CanDeclare(new SimpleName(item)))
                {
                    return item;
                }
            }
            int index = 0;
            while (!CanDeclare(new SimpleName(Names[Names.Length - 1] + index)))
            {
                index++;
            }
            return Names[Names.Length - 1] + index;
        }

        #endregion

        #region Declare

        /// <summary>
        /// Generates a valid variable member based on the given variable member.
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public IVariableMember GenerateVariableMember(IVariableMember VariableMember)
        {
            if (CanDeclare(VariableMember.Name))
            {
                return VariableMember;
            }
            else
            {
                var descVarMember = new DescribedVariableMember(GenerateIdentifier(VariableMember), VariableMember.VariableType);
                foreach (var item in VariableMember.Attributes)
                {
                    descVarMember.AddAttribute(item);
                }
                return descVarMember;
            }
        }

        /// <summary>
        /// Declares a new local variable.
        /// </summary>
        /// <param name="VariableMember"></param>
        /// <returns></returns>
        public CppLocal DeclareNew(UniqueTag Tag, IVariableMember VariableMember)
        {
            var varMember = GenerateVariableMember(VariableMember);
            var local = new CppLocal(CodeGenerator, locals.Count, varMember);
            locals.Add(Tag, local);
            return local;
        }

        /// <summary>
        /// Declares an owned local variable, i.e. a variable that cannot be released by a call to EmitRelease.
        /// </summary>
        /// <param name="VariableMember"></param>
        /// <returns></returns>
        public OwnedCppLocal DeclareOwned(UniqueTag Tag, IVariableMember VariableMember)
        {
            var varMember = GenerateVariableMember(VariableMember);
            var local = new OwnedCppLocal(CodeGenerator, locals.Count, varMember);
            locals.Add(Tag, local);
            return local;
        }

        /// <summary>
        /// Declares a local variable, optionally reusing a released variable.
        /// </summary>
        /// <param name="VariableMember"></param>
        /// <returns></returns>
        public CppLocal Declare(UniqueTag Tag, IVariableMember VariableMember)
        {
            var reused = Reuse(Tag, VariableMember.VariableType);
            if (reused != null)
            {
                return reused;
            }
            else
            {
                return DeclareNew(Tag, VariableMember);
            }
        }

        #endregion

        #region Get

        /// <summary>
        /// Gets the C++ local variable identified by the
        /// given unique tag.
        /// </summary>
        /// <param name="Tag"></param>
        /// <returns></returns>
        public CppLocal Get(UniqueTag Tag)
        {
            CppLocal result;
            if (locals.TryGetValue(Tag, out result))
            {
                return result;
            }
            else
            {
                return null;
            }
        }

        #endregion
    }
}