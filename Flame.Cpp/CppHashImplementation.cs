using Flame.Build;
using Flame.Compiler;
using Flame.Compiler.Variables;
using Flame.Cpp.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public class CppHashImplementation : ICppMember
    {
        public CppHashImplementation(CppType ArgumentType, IMethod HashMethod)
        {
            this.ArgumentType = ArgumentType;
            this.HashMethod = HashMethod;
        }

        public CppTemplateDefinition Templates
        {
            get
            {
                return ArgumentType.Templates;
            }
        }

        public CppType ArgumentType { get; private set; }
        public IMethod HashMethod { get; private set; }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return new IHeaderDependency[] { StandardDependency.Functional }; }
        }

        public ICppEnvironment Environment
        {
            get { return ArgumentType.Environment; }
        }

        public CodeBuilder GetHeaderCode()
        {
            var argType = Environment.TypeConverter.Convert(ArgumentType.get_IsGeneric() ? ArgumentType.MakeGenericType(ArgumentType.GenericParameters) : ArgumentType);

            var cb = new CodeBuilder();
            cb.AddLine("namespace std");
            cb.AddLine("{");
            cb.IncreaseIndentation();
            cb.AddCodeBuilder(Templates.GetImplementationCode());
            cb.AddLine();
            string tName = Environment.TypeNamer.Name(argType, Plugs.StdNamespace.Instance);
            cb.AddLine("struct hash<");
            cb.Append(tName);
            cb.Append(">");
            cb.AddLine("{");
            cb.IncreaseIndentation();
            cb.AddLine("std::size_t operator()(const " + tName + "& Arg) const");
            cb.AddLine("{");
            cb.IncreaseIndentation();
            if (HashMethod.IsStatic)
            {
                cb.AddLine("return " + tName + "::" + HashMethod.Name + "(Arg);");
            }
            else
            {
                cb.AddLine("return Arg." + HashMethod.Name + "();");
            }            
            cb.DecreaseIndentation();
            cb.AddLine("}");
            cb.DecreaseIndentation();
            cb.AddLine("};");
            cb.DecreaseIndentation();
            cb.AddLine("}");
            return cb;
        }

        public bool HasSourceCode
        {
            get { return false; }
        }

        public CodeBuilder GetSourceCode()
        {
            return new CodeBuilder();
        }

        public string FullName
        {
            get { return GenericNameExtensions.ChangeTypeArguments("std.hash", new string[] { ArgumentType.FullName }); }
        }

        public IEnumerable<IAttribute> Attributes
        {
            get { return new IAttribute[] { PrimitiveAttributes.Instance.ValueTypeAttribute }; }
        }

        public string Name
        {
            get { return GenericNameExtensions.ChangeTypeArguments("hash", new string[] { ArgumentType.Name }); }
        }
    }
}
