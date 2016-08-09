using Flame.Build;
using Flame.Compiler;
using Flame.Compiler.Build;
using Flame.Compiler.Expressions;
using Flame.Cpp.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public class CppField : IFieldBuilder, IInitializedField, ICppMember
    {
        public CppField(IType DeclaringType, IFieldSignatureTemplate Template, ICppEnvironment Environment)
        {
            this.DeclaringType = DeclaringType;
            this.Template = new FieldSignatureInstance(Template, this);
            this.Environment = Environment;
            this.IsMutable = false;
        }

        public IType DeclaringType { get; private set; }
        public FieldSignatureInstance Template { get; private set; }
        public ICppEnvironment Environment { get; private set; }
        public IExpression Value { get; private set; }
        public bool IsMutable { get; set; }

        public Func<INamespace, IConverter<IType, string>> TypeNamer { get { return Environment.TypeNamer; } }

        public bool TrySetValue(IExpression Value)
        {
            this.Value = Value;
            return true;
        }

        public IExpression InitialValue
        {
            get { return Value; }
        }

        public IField Build()
        {
            return this;
        }

        public void Initialize()
        {
            // No initialization logic yet.
        }

        public QualifiedName FullName
        {
            get { return Name.Qualify(DeclaringType.FullName); }
        }

        public AttributeMap Attributes
        {
            get { return Template.Attributes.Value; }
        }

        public UnqualifiedName Name
        {
            get { return Template.Name; }
        }

        public IType FieldType
        {
            get { return this.ConvertType(Template.FieldType.Value); }
        }

        public bool IsStatic
        {
            get { return Template.IsStatic; }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return FieldType.GetDependencies(DeclaringType); }
        }

        #region GetCode

        private bool EmitCustomAssignment
        {
            get { return this.Value != null && !(Value is DefaultValueExpression); }
        }

        private bool EmitPrimitiveAssignment
        {
            get { return IsStatic || FieldType.GetIsPrimitive() && !FieldType.Equals(PrimitiveTypes.String) && Environment.Log.Options.GetOption<bool>("initialize-fields", true); }
        }

        private bool EmitAssignment
        {
            get { return EmitCustomAssignment || EmitPrimitiveAssignment; }
        }

        private ICppBlock CreateAssignedValueBlock()
        {
            if (EmitCustomAssignment)
            {
                var cg = new CppCodeGenerator(new DescribedMethod(Name, DeclaringType, FieldType, IsStatic), Environment);
                return (ICppBlock)Value.Emit(cg);
            }
            else if (EmitPrimitiveAssignment)
            {
                var cg = new CppCodeGenerator(new DescribedMethod(Name, DeclaringType, FieldType, IsStatic), Environment);
                return (ICppBlock)cg.EmitDefaultValue(FieldType);
            }
            else
            {
                return null;
            }
        }

        private void AppendAssignment(CodeBuilder cb)
        {
            var block = CreateAssignedValueBlock();
            if (block != null)
            {
                cb.Append(" = ");
                cb.Append(block.GetCode());
            }
        }

        public CodeBuilder GetHeaderCode()
        {
            CodeBuilder cb = this.GetDocumentationComments();
            cb.AppendLine();
            if (this.IsStatic)
            {
                cb.Append("static ");
            }
            if (this.IsMutable)
            {
                cb.Append("mutable ");
            }
            cb.Append(TypeNamer.Name(FieldType, this));
            cb.Append(' ');
            cb.Append(Name.ToString());
            if (!IsStatic)
            {
                AppendAssignment(cb);
            }
            cb.Append(';');
            return cb;
        }

        public CodeBuilder GetSourceCode()
        {
            var cb = new CodeBuilder();

            if (!IsStatic)
            {
                return cb;
            }

            if (DeclaringType.GetIsGeneric())
            {
                cb.Append(DeclaringType.GetFullTemplateDefinition().GetHeaderCode());
                cb.AppendLine();
            }
            cb.Append(TypeNamer.Name(FieldType, this));
            cb.Append(' ');
            var genDeclType = DeclaringType.MakeGenericType(DeclaringType.GenericParameters);
            cb.Append(TypeNamer.Name(genDeclType, this));
            cb.Append("::");
            cb.Append(Name.ToString());
            AppendAssignment(cb);
            cb.Append(';');
            return cb;
        }

        public bool HasSourceCode
        {
            get { return IsStatic; }
        }

        public override string ToString()
        {
            return GetHeaderCode().ToString();
        }

        #endregion
    }
}
