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

        public virtual UnqualifiedName Name
        {
            get
            {
                string result = null;
                if (Template.IsConstructor)
                {
                    result = "__init__";
                }
                else if (this.GetIsCast() && Template.ReturnType.Equals(PrimitiveTypes.String))
                {
                    result = "__str__";
                }
                else if (this.GetIsOperator())
                {
                    var op = this.GetOperator();
                    if (op.Equals(Operator.Add))
                    {
                        result = "__add__";
                    }
                    else if (op.Equals(Operator.Subtract))
                    {
                        result = "__sub__";
                    }
                    else if (op.Equals(Operator.Multiply))
                    {
                        result = "__mul__";
                    }
                    else if (op.Equals(Operator.Divide))
                    {
                        result = "__div__";
                    }
                    else if (op.Equals(Operator.Or))
                    {
                        result = "__or__";
                    }
                    else if (op.Equals(Operator.And))
                    {
                        result = "__and__";
                    }
                    else if (op.Equals(Operator.Xor))
                    {
                        result = "__xor__";
                    }
                    else if (op.Equals(Operator.Not))
                    {
                        result = "__not__";
                    }
                    else if (op.Equals(Operator.CheckEquality))
                    {
                        result = "__eq__";
                    }
                    else if (op.Equals(Operator.CheckInequality))
                    {
                        result = "__ne__";
                    }
                    else if (op.Equals(Operator.CheckGreaterThan))
                    {
                        result = "__gt__";
                    }
                    else if (op.Equals(Operator.CheckGreaterThanOrEqual))
                    {
                        result = "__ge__";
                    }
                    else if (op.Equals(Operator.CheckLessThan))
                    {
                        result = "__lt__";
                    }
                    else if (op.Equals(Operator.CheckLessThanOrEqual))
                    {
                        result = "__le__";
                    }
                    else if (op.Equals(Operator.Hash))
                    {
                        result = "__hash__";
                    }
                }
                if (result == null)
                {
                    var descMethod = new DescribedMethod(Template.Name, DeclaringType);
                    result = GetMemberNamer().Name(descMethod);
                }
                return new SimpleName(result);
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

        public QualifiedName FullName
        {
            get { return Name.Qualify(DeclaringType.FullName); }
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
            cb.Append(Name.ToString());
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
                cb.Append(item.Name.ToString());
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
