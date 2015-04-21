//#define AGGRESSIVE

using Flame.Compiler;
using Flame.CodeDescription;
using Flame.Python.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python
{
    public static class PythonExtensions
    {
        public static CodeBuilder GetDocstring(this IMember Member)
        {
            string desc = Member.GetDocumentation(DescriptionAttributeExtensions.IsSummary);
            string accessDesc = Member.GetAccessDescription();
            string docs = string.IsNullOrEmpty(accessDesc) ? desc : desc + Environment.NewLine + accessDesc;
            return DocumentationExtensions.ToBlockComments(docs, "\"\"\"", "\"\"\"");
        }

        public static CodeBuilder GetDocComments(this IMember Member)
        {
            return Member.GetDocumentationLineComments((item) => !item.IsSummary(), "#");
        }

        public static string GetAccessDescription(this IMember Member)
        {
            var access = Member.get_Access();
            if (access == AccessModifier.Public)
            {
                return "";
            }
            else
            {
                string modifierString = Member.get_IsAbstract() ? "abstract " : string.Empty;
                return "This " + modifierString + Member.GetMemberTypeName() + " is " + access.GetAccessName() + ".";
            }
        }

        public static string GetMemberTypeName(this IMember Member)
        {
            if (Member is IMethod)
            {
                if (Member is IAccessor)
                {
                    return "accessor";
                }
                else if (((IMethod)Member).IsConstructor)
                {
                    return "constructor";
                }
                else if (((IMethod)Member).get_IsOperator())
                {
                    return "operator";
                }
                else
                {
                    return "method";
                }
            }
            else if (Member is IField)
            {
                return "field";
            }
            else if (Member is IProperty)
            {
                return "property";
            }
            else if (Member is IType)
            {
                if (Member is IGenericParameter)
                {
                    return "type parameter";
                }
                else
                {
                    return "class";
                }
            }
            else
            {
                return "member";
            }
        }

        public static string GetAccessName(this AccessModifier Access)
        {
            switch (Access)
            {
                case AccessModifier.Assembly:
                    return "internal";
                case AccessModifier.Private:
                    return "private";
                case AccessModifier.Protected:
                    return "protected";
                case AccessModifier.ProtectedAndAssembly:
                    return "protected and assembly";
                case AccessModifier.ProtectedOrAssembly:
                    return "protected or assembly";
                case AccessModifier.Public:
                default:
                    return "public";
            }
        }

        public static CodeBuilder GetDocCode(this IMember Member)
        {
            CodeBuilder cb = Member.GetDocstring(); 
            cb.AddCodeBuilder(Member.GetDocComments());
            return cb;
        }

        public static bool SetsField(this IMethod Method, IField Field)
        {
            foreach (var item in Method.GetParameters())
                if (item.get_SetsMember())
            {
                var setField = item.GetSetField(Method.DeclaringType, false);
                if (setField != null)
                {
                    if (setField.Name.Equals(Field.Name, StringComparison.InvariantCultureIgnoreCase)) return true;
                }
                else
                {
                    var setProperty = item.GetSetProperty(Method.DeclaringType, false);
                    if (setProperty != null)
                    {
                        if (Field.get_IsHidden() && setProperty.Name.Equals(Field.Name + "_value", StringComparison.InvariantCultureIgnoreCase)) return true;
                    }
                }
#if AGGRESSIVE // This is a really abgressive optimization, and may well get it wrong sometimes
                string lowerParamName = item.Name.ToLower();
                string lowerFieldName = Field.Name.ToLower().Replace("_", "");
                if (lowerParamName == lowerFieldName || lowerParamName + "value" == lowerFieldName)
                    return true;
#endif
            }
            return false;
        }

        public static IPythonBlock CreatePythonFieldInitBlock(this IType Type, ICodeGenerator CodeGenerator)
        {
            var instanceFields = Type.GetFields().OfType<PythonField>().Where((item) => !item.IsStatic);
            var initBlock = CodeGenerator.EmitVoid();
            foreach (var item in instanceFields)
            {
                if (!CodeGenerator.Method.SetsField(item))
                {
                    var setStatement = initBlock.CodeGenerator.GetField(item, initBlock.CodeGenerator.GetThis().EmitGet()).EmitSet(item.AssignedValue.Emit(CodeGenerator));
                    initBlock = CodeGenerator.EmitSequence(initBlock, setStatement);
                }
            }
            return (IPythonBlock)initBlock;
        }
    }
}
