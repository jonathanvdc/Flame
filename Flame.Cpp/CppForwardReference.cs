using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public class CppForwardReference
    {
        public CppForwardReference(CppType Type)
        {
            this.Type = Type;
        }

        public CppType Type { get; private set; }

        public string Namespace
        {
            get { return Type.DeclaringNamespace.FullName; }
        }

        public bool IsStruct
        {
            get { return Type.IsStruct; }
        }

        public CppTemplateDefinition TemplateDefinition
        {
            get
            {
                return Type.Templates;
            }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.IndentationString = new string(' ', 4);
            if (!TemplateDefinition.IsEmpty)
            {
                cb.AddCodeBuilder(TemplateDefinition.GetHeaderCode());
                cb.AppendLine();
            }
            if (IsStruct)
            {
                cb.Append("struct ");
            }
            else
            {
                cb.Append("class ");
            }
            cb.Append(Type.GetGenericFreeName().Replace(".", "::"));
            cb.Append(";");
            return cb;
        }

        public override string ToString()
        {
            return GetCode().ToString();
        }
    }
}
