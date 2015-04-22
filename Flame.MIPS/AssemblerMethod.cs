using Flame.Compiler;
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
        public AssemblerMethod(IType DeclaringType, IMethod Template, IAssemblerState GlobalState)
        {
            this.DeclaringType = DeclaringType;
            this.Template = Template;
            this.GlobalState = GlobalState;
            this.Label = GlobalState.Labels.DeclareLabel(this);
            this.CallConvention = new AutoCallConvention(this);

            this.codeGen = new AssemblerCodeGenerator(this);            
        }

        public IType DeclaringType { get; private set; }
        public IMethod Template { get; private set; }
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
            get { return this.get_Access() == AccessModifier.Public; }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.Append("# ");
            cb.Append(ReturnType.Name);
            cb.Append(" ");
            cb.Append(Name);
            cb.Append("(");
            var parameters = GetParameters();
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

        public IMethod[] GetBaseMethods()
        {
            return Template.GetBaseMethods();
        }

        public IMethod GetGenericDeclaration()
        {
            return this;
        }

        public IParameter[] GetParameters()
        {
            return Template.GetParameters();
        }

        public IBoundObject Invoke(IBoundObject Caller, IEnumerable<IBoundObject> Arguments)
        {
            throw new NotImplementedException();
        }

        public bool IsConstructor
        {
            get { return Template.IsConstructor; }
        }

        public IMethod MakeGenericMethod(IEnumerable<IType> TypeArguments)
        {
            throw new InvalidOperationException();
        }

        public IType ReturnType
        {
            get { return Template.ReturnType; }
        }

        public bool IsStatic
        {
            get { return Template.IsStatic; }
        }

        public string FullName
        {
            get { return MemberExtensions.CombineNames(DeclaringType.FullName, Name); }
        }

        public IEnumerable<IAttribute> GetAttributes()
        {
            return Template.GetAttributes();
        }

        public string Name
        {
            get { return Template.Name; }
        }

        public IEnumerable<IType> GetGenericArguments()
        {
            return new IType[0];
        }

        public IEnumerable<IGenericParameter> GetGenericParameters()
        {
            return new IGenericParameter[0];
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

        #endregion
    }
}
