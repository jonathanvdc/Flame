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
            this.locals = new List<CppLocal>();
            this.freedLocals = new List<CppLocal>();
        }

        /// <summary>
        /// Gets the code generator associated with this local variable manager.
        /// </summary>
        public ICodeGenerator CodeGenerator { get; private set; }

        private List<CppLocal> locals;
        private List<CppLocal> freedLocals;

        /// <summary>
        /// Gets all locals declared by this local variable manager.
        /// </summary>
        public IReadOnlyList<CppLocal> Locals { get { return locals; } }

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
        public CppLocal Reuse(IType VariableType)
        {
            for (int i = 0; i < freedLocals.Count; i++)
            {
                if (freedLocals[i].Type.Equals(VariableType))
                {
                    var item = freedLocals[i];
                    freedLocals.RemoveAt(i);
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
        public bool CanDeclare(string Name)
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                return false;
            }
            else
            {
                return !Locals.Any((item) => item.Member.Name == Name) && !CodeGenerator.Method.GetParameters().Any((item) => item.Name == Name);
            }
        }

        #region Identifier Generation

        private string GenerateIdentifier(IVariableMember Member)
        {
            if (string.IsNullOrWhiteSpace(Member.Name))
            {
                return GenerateIdentifier(Member.VariableType);
            }
            else
            {
                return GenerateIdentifier(Member.Name);
            }
        }

        private string GenerateIdentifier(IType Type)
        {
            if (Type.get_IsBit())
            {
                return GenerateIdentifier("data", "bits");
            }
            else if (Type.Equals(PrimitiveTypes.Boolean))
            {
                return GenerateIdentifier("flag");
            }
            else if (Type.get_IsFloatingPoint())
            {
                return GenerateIdentifier("x", "y", "z", "num");
            }
            else if (Type.get_IsInteger())
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
            else if (Type.get_IsArray())
            {
                return GenerateIdentifier("arr", "temp");
            }
            else if (Type.get_IsVector())
            {
                return GenerateIdentifier("vec", "temp");
            }
            else if (Type.get_IsPointer())
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
                if (CanDeclare(item))
                {
                    return item;
                }
            }
            int index = 0;
            while (!CanDeclare(Names[Names.Length - 1] + index))
            {
                index++;
            }
            return Names[Names.Length - 1] + index;
        }

        #endregion

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
                foreach (var item in VariableMember.GetAttributes())
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
        public CppLocal DeclareNew(IVariableMember VariableMember)
        {
            var varMember = GenerateVariableMember(VariableMember);
            var local = new CppLocal(CodeGenerator, locals.Count, varMember);
            locals.Add(local);
            return local;
        }

        /// <summary>
        /// Declares an owned local variable, i.e. a variable that cannot be released by a call to EmitRelease.
        /// </summary>
        /// <param name="VariableMember"></param>
        /// <returns></returns>
        public OwnedCppLocal DeclareOwned(IVariableMember VariableMember)
        {
            var varMember = GenerateVariableMember(VariableMember);
            var local = new OwnedCppLocal(CodeGenerator, locals.Count, varMember);
            locals.Add(local);
            return local;
        }

        /// <summary>
        /// Declares a local variable, optionally reusing a released variable.
        /// </summary>
        /// <param name="VariableMember"></param>
        /// <returns></returns>
        public CppLocal Declare(IVariableMember VariableMember)
        {
            var reused = Reuse(VariableMember.VariableType);
            if (reused != null)
            {
                return reused;
            }
            else
            {
                return DeclareNew(VariableMember);
            }
        }
    }
}