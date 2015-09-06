using Flame.Compiler;
using Flame.MIPS.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS
{
    public class SyscallMethod : ISyscallMethod
    {
        public SyscallMethod(IType DeclaringType, IMethod Template, int ServiceIndex, ICallConvention CallConvention)
        {
            this.DeclaringType = DeclaringType;
            this.Template = Template;
            this.ServiceIndex = ServiceIndex;
            this.CallConvention = CallConvention;
        }
        public SyscallMethod(IType DeclaringType, IMethod Template, int ServiceIndex)
            : this(DeclaringType, Template, ServiceIndex, null)
        {
            this.CallConvention = new AutoCallConvention(this);
        }

        public IType DeclaringType { get; private set; }
        public IMethod Template { get; private set; }
        public int ServiceIndex { get; private set; }
        public ICallConvention CallConvention { get; private set; }

        public virtual IAssemblerBlock CreateCallBlock(ICodeGenerator CodeGenerator)
        {
            return new SyscallBlock(CodeGenerator, this);
        }

        public IEnumerable<IMethod> BaseMethods
        {
            get { return Template.BaseMethods; }
        }

        public IEnumerable<IParameter> Parameters
        {
            get { return Template.Parameters; }
        }

        public IBoundObject Invoke(IBoundObject Caller, IEnumerable<IBoundObject> Arguments)
        {
            throw new NotImplementedException();
        }

        public bool IsConstructor
        {
            get { return Template.IsConstructor; }
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

        public IEnumerable<IAttribute> Attributes
        {
            get { return Template.Attributes; }
        }

        public string Name
        {
            get { return Template.Name; }
        }

        public IEnumerable<IGenericParameter> GenericParameters
        {
            get { return Template.GenericParameters; }
        }
    }
}
