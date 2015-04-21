using Flame.Build;
using Flame.Compiler;
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
        public PythonMethod(IMethod Template)
        {
            this.DeclaringType = Template.DeclaringType;
            this.Template = Template;
        }
        public PythonMethod(IType DeclaringType, IMethod Template)
        {
            this.DeclaringType = DeclaringType;
            this.Template = Template;
        }

        public IMethod Template { get; private set; }
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
                else if (Template.get_IsCast() && Template.ReturnType.Equals(PrimitiveTypes.String))
                {
                    return "__str__";
                }
                else if (Template.get_IsOperator())
                {
                    var op = Template.GetOperator();
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
                return GetMemberNamer().Name(Template);
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
            Body = (IPythonBlock)Body;
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

        public IParameter[] GetPythonParameters()
        {
            if (IsStatic)
            {
                return Template.GetParameters();
            }
            else
            {
                return MemberSelection.Concat(new DescribedParameter("self", DeclaringType), Template.GetParameters());
            }
        }

        public IBoundObject Invoke(IBoundObject Caller, IEnumerable<IBoundObject> Arguments)
        {
            return Template.Invoke(Caller, Arguments);
        }

        public bool IsConstructor
        {
            get { return Name == "__init__"; }
        }

        public IMethod MakeGenericMethod(IEnumerable<IType> TypeArguments)
        {
            return this;
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

        public IEnumerable<IType> GetGenericArguments()
        {
            return Template.GetGenericArguments();
        }

        public IEnumerable<IGenericParameter> GetGenericParameters()
        {
            return Template.GetGenericParameters();
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
            var bodyBlock = new CodeBuilder();
            var bodyCode = Body.GetCode();
            if (IsConstructor)
            {
                bodyBlock.AddCodeBuilder(DeclaringType.CreatePythonFieldInitBlock(CodeGenerator).GetCode());
            }
            var lastLine = bodyCode.LastCodeLine;
            if (lastLine.Indentation == 0 && lastLine.Text.TrimEnd() == "return")
            {
                bodyCode.TrimEnd();
                bodyCode[bodyCode.LineCount - 1] = new CodeLine("");
            }
            bodyBlock.AddCodeBuilder(bodyCode);
            if (bodyBlock.IsWhitespace)
            {
                if (DeclaringType.get_IsInterface() || this.get_IsAbstract())
                {
                    bodyBlock.AddLine("raise NotImplementedError(\"" + NotImplementedDescription + "\")");
                }
                else
                {
                    bodyBlock.AddLine("pass");
                }
            }
            return bodyBlock;
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
