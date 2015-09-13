using Flame.Compiler.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python
{
    public class PythonAccessor : PythonMethod, IPythonAccessor
    {
        public PythonAccessor(IPythonProperty DeclaringProperty, AccessorType AccessorType, IMethodSignatureTemplate Template)
            : base(DeclaringProperty.DeclaringType, Template)
        {
            this.AccessorType = AccessorType;
            this.DeclaringProperty = DeclaringProperty;
        }

        public IProperty DeclaringProperty { get; private set; }
        public AccessorType AccessorType { get; private set; }

        public override string Name
        {
            get
            {
                if (((IPythonProperty)DeclaringProperty).UsesPropertySyntax)
                {
                    return DeclaringProperty.Name;
                }
                else
                {
                    return base.Name;
                }
            }
        }

        public override IEnumerable<PythonDecorator> GetDecorators()
        {
            if (((IPythonProperty)DeclaringProperty).UsesPropertySyntax)
            {
                PythonDecorator propDecorator;
                if (this.AccessorType.Equals(AccessorType.GetAccessor))
                {
                    propDecorator = new PythonDecorator("property");
                }
                else if (this.AccessorType.Equals(AccessorType.SetAccessor))
                {
                    propDecorator = new PythonDecorator(DeclaringProperty.Name + ".setter");
                }
                else
                {
                    throw new NotSupportedException();
                }
                return base.GetDecorators().Concat(new PythonDecorator[] { propDecorator });
            }
            else
            {
                return base.GetDecorators();
            }
        }

        public override string NotImplementedDescription
        {
            get
            {
                if (((IPythonProperty)DeclaringProperty).UsesPropertySyntax)
                {
                    string accessorName = this.get_IsGetAccessor() ? "Getter" : this.get_IsSetAccessor() ? "Setter" : null;
                    if (accessorName == null)
                    {
                        return "Property \'" + DeclaringProperty.FullName + "\' was not implemented.";
                    }
                    else
                    {
                        return accessorName + " of property \'" + DeclaringProperty.FullName + "\' was not implemented.";
                    }
                }
                else
                {
                    return base.NotImplementedDescription;
                }
            }
        }
    }
}
