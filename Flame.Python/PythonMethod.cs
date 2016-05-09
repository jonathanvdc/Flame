using Flame.Build;
using Flame.Compiler;
using Flame.Compiler.Build;
using Flame.Python.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python
{
    public class PythonMethod : IPythonMethod, IMethodBuilder, IDependencyNode
    {
        public PythonMethod(IType DeclaringType, IMethodSignatureTemplate Template)
        {
            this.DeclaringType = DeclaringType;
            this.Template = new MethodSignatureInstance(Template, this);
        }

        public MethodSignatureInstance Template { get; private set; }
        public IType DeclaringType { get; private set; }

        protected IMemberNamer GetMemberNamer()
        {
            return DeclaringType.DeclaringNamespace.GetMemberNamer();
        }

        public virtual string Name
        {
            get
            {
                if (Template.IsConstructor)
                {
                    return "__init__";
                }
                else if (this.GetIsCast() && Template.ReturnType.Equals(PrimitiveTypes.String))
                {
                    return "__str__";
                }
                else if (this.GetIsOperator())
                {
                    var op = this.GetOperator();
                    if (op.Equals(Operator.Add))
                    {
                        return "__add__";
                    }
                    else if (op.Equals(Operator.Subtract))
                    {
                        return "__sub__";
                    }
                    else if (op.Equals(Operator.Multiply))
                    {
                        return "__mul__";
                    }
                    else if (op.Equals(Operator.Divide))
                    {
                        return "__div__";
                    }
                    else if (op.Equals(Operator.Or))
                    {
                        return "__or__";
                    }
                    else if (op.Equals(Operator.And))
                    {
                        return "__and__";
                    }
                    else if (op.Equals(Operator.Xor))
                    {
                        return "__xor__";
                    }
                    else if (op.Equals(Operator.Not))
                    {
                        return "__not__";
                    }
                    else if (op.Equals(Operator.CheckEquality))
                    {
                        return "__eq__";
                    }
                    else if (op.Equals(Operator.CheckInequality))
                    {
                        return "__ne__";
                    }
                    else if (op.Equals(Operator.CheckGreaterThan))
                    {
                        return "__gt__";
                    }
                    else if (op.Equals(Operator.CheckGreaterThanOrEqual))
                    {
                        return "__ge__";
                    }
                    else if (op.Equals(Operator.CheckLessThan))
                    {
                        return "__lt__";
                    }
                    else if (op.Equals(Operator.CheckLessThanOrEqual))
                    {
                        return "__le__";
                    }
                    else if (op.Equals(Operator.Hash))
                    {
                        return "__hash__";
                    }
                }
                var descMethod = new DescribedMethod(Template.Name, DeclaringType);
                return GetMemberNamer().Name(descMethod);
            }
        }

        #region Body

        private ICodeGenerator codeGen;
        public ICodeGenerator CodeGenerator
        {
            get
            {
                if (codeGen == null)
                {
                    codeGen = new PythonCodeGenerator(this);
                }
                return codeGen;
            }
        }

        public IPythonBlock Body { get; private set; }

        public void SetMethodBody(ICodeBlock Body)
        {
            this.Body = (IPythonBlock)Body;
        }

        #endregion

        #region IMethodBuilder Implementation

        public ICodeGenerator GetBodyGenerator()
        {
            return CodeGenerator;
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

        #region IMethod Implementation

        public IEnumerable<IMethod> BaseMethods
        {
            get { return Template.BaseMethods.Value; }
        }

        public IEnumerable<IParameter> Parameters
        {
            get { return Template.Parameters.Value; }
        }

        public IParameter[] GetPythonParameters()
        {
            if (IsStatic)
            {
                return this.GetParameters();
            }
            else
            {
                return MemberSelection.Concat(new DescribedParameter("self", DeclaringType), this.GetParameters());
            }
        }

        public bool IsConstructor
        {
            get { return Name == "__init__"; }
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

        public AttributeMap Attributes
        {
            get { return Template.Attributes.Value; }
        }

        public IEnumerable<IGenericParameter> GenericParameters
        {
            get { return Template.GenericParameters.Value; }
        }

        #endregion

        #region Decorators

        /// <summary>
        /// Gets all decorators associated with this method.
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<PythonDecorator> GetDecorators()
        {
            return new PythonDecorator[0];
        }

        #endregion

        public CodeBuilder GetHeaderCode()
        {
            CodeBuilder cb = new CodeBuilder();
            foreach (var item in GetDecorators())
            {
                cb.Append(item.GetCode());
                cb.AppendLine();
            }
            cb.Append("def ");
            cb.Append(Name);
            cb.Append("(");
            bool first = true;
            foreach (var item in GetPythonParameters())
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    cb.Append(", ");
                }
                cb.Append(item.Name);
            }
            cb.Append(")");
            return cb;
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.IndentationString = "    ";
            cb.Append(GetHeaderCode());
            cb.Append(":");
            cb.IncreaseIndentation();
            cb.AddCodeBuilder(this.GetDocCode());
            cb.AddCodeBuilder(GetBodyCode());
            cb.DecreaseIndentation();
            return cb;
        }

        /// <summary>
        /// Gets the description that gets thrown if this method is abstract and not implemented in a base class.
        /// </summary>
        public virtual string NotImplementedDescription
        {
            get
            {
                return "Method \'" + FullName + "\' was not implemented.";
            }
        }

        public IPythonBlock GetCompleteBody()
        {
            var bodyBlock = Body ?? new EmptyBlock(CodeGenerator);
            if (IsConstructor)
            {
                return (IPythonBlock)CodeGenerator.EmitSequence(DeclaringType.CreatePythonFieldInitBlock(CodeGenerator), bodyBlock);
            }
            return bodyBlock;
        }

        public CodeBuilder GetBodyCode()
        {
            var bodyCode = GetCompleteBody().GetCode();
            var lastLine = bodyCode.LastCodeLine;
            if (lastLine.Indentation == 0 && lastLine.Text.TrimEnd() == "return")
            {
                bodyCode.TrimEnd();
                bodyCode[bodyCode.LineCount - 1] = new CodeLine("");
            }
            if (bodyCode.IsWhitespace)
            {
                if (DeclaringType.GetIsInterface() || this.GetIsAbstract())
                {
                    bodyCode.AddLine("raise NotImplementedError(\"" + NotImplementedDescription + "\")");
                }
                else
                {
                    bodyCode.AddLine("pass");
                }
            }
            return bodyCode;
        }

        public override string ToString()
        {
            return GetHeaderCode().ToString();
        }

        public IEnumerable<ModuleDependency> GetDependencies()
        {
            return GetCompleteBody().GetDependencies();
        }
    }
}
