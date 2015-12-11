using Flame.Compiler;
using Flame.Compiler.Build;
using Flame.MIPS.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS
{
    public class AssemblerMethod : IAssemblerMethod, IMethodBuilder
    {
        public AssemblerMethod(IType DeclaringType, IMethodSignatureTemplate Template, IAssemblerState GlobalState)
        {
            this.DeclaringType = DeclaringType;
            this.Template = new MethodSignatureInstance(Template, this);
            this.GlobalState = GlobalState;
            this.Label = GlobalState.Labels.DeclareLabel(this);
            this.CallConvention = new AutoCallConvention(this);

            this.codeGen = new AssemblerCodeGenerator(this);            
        }

        public IType DeclaringType { get; private set; }
        public MethodSignatureInstance Template { get; private set; }
        public IAssemblerState GlobalState { get; private set; }

        public IAssemblerLabel Label { get; private set; }
        public ICallConvention CallConvention { get; private set; }

        public IAssemblerBlock CreateCallBlock(ICodeGenerator CodeGenerator)
        {
            return new CallLabelBlock(CodeGenerator, Label, Name);
        }

        private AssemblerEmitContext bodyContext;
        private AssemblerCodeGenerator codeGen;

        #region IAssemblerMethod Implementation

        public bool IsGlobal
        {
            get { return this.GetAccess() == AccessModifier.Public; }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.Append("# ");
            cb.Append(ReturnType.Name);
            cb.Append(" ");
            cb.Append(Name);
            cb.Append("(");
            var parameters = this.GetParameters();
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i > 0)
                {
                    cb.Append(", ");
                }
                cb.Append(parameters[i].ParameterType.Name);
                cb.Append(" "); 
                cb.Append(parameters[i].Name);
            }
            cb.Append(")");
            cb.AddCodeBuilder(bodyContext.GetCode());
            return cb;
        }

        #endregion

        #region IMethod Implementation

        public IEnumerable<IMethod> BaseMethods
        {
            get { return Template.BaseMethods.Value; }
        }

        public IEnumerable<IParameter> Parameters
        {
            get { return Template.Parameters.Value; }
        }

        public IBoundObject Invoke(IBoundObject Caller, IEnumerable<IBoundObject> Arguments)
        {
            return null;
        }

        public bool IsConstructor
        {
            get { return Template.IsConstructor; }
        }

        public IType ReturnType
        {
            get { return Template.ReturnType.Value; }
        }

        public bool IsStatic
        {
            get { return Template.Template.IsStatic; }
        }

        public string FullName
        {
            get { return MemberExtensions.CombineNames(DeclaringType.FullName, Name); }
        }

        public IEnumerable<IAttribute> Attributes
        {
            get { return Template.Attributes.Value; }
        }

        public string Name
        {
            get { return Template.Name; }
        }

        public IEnumerable<IGenericParameter> GenericParameters
        {
            get { return Template.GenericParameters.Value; }
        }

        #endregion

        #region IMethodBuilder

        public ICodeGenerator GetBodyGenerator()
        {
            return codeGen;
        }

        public void SetMethodBody(ICodeBlock Body)
        {
            this.bodyContext = new AssemblerEmitContext(codeGen, Label, GlobalState);
            ((IAssemblerBlock)Body).Emit(bodyContext);
            bodyContext.Build();
        }

        public IMethod Build()
        {            
            return this;
        }

        public void Initialize()
        {
            // Do nothing. This back-end does not need `Initialize` to get things done.
        }

        #endregion
    }
}
