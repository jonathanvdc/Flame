using Flame.Build;
using Flame.Compiler;
using Flame.Compiler.Expressions;
using Flame.Python.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python
{
    public class PythonOverloadedMethod : IPythonMethod
    {
        public PythonOverloadedMethod(params IPythonMethod[] Methods)
        {
            this.Methods = new List<IPythonMethod>(Methods);
        }
        public PythonOverloadedMethod(IEnumerable<IPythonMethod> Methods)
        {
            this.Methods = new List<IPythonMethod>(Methods);
        }

        public List<IPythonMethod> Methods { get; private set; }

        public IEnumerable<IMethod> BaseMethods
        {
            get { return Methods.SelectMany((item) => item.BaseMethods).Distinct(); }
        }

        public IEnumerable<IParameter> Parameters
        {
            get { return GetPythonParameters(); }
        }

        public bool IsConstructor
        {
            get { return Methods.All((item) => item.IsConstructor); }
        }

        public IType ReturnType
        {
            get { return PythonObjectType.Instance; }
        }

        public IType DeclaringType
        {
            get { return Methods.First().DeclaringType; }
        }

        public bool IsStatic
        {
            get { return Methods.All((item) => item.IsStatic); }
        }

        public string FullName
        {
            get { return Methods.First().FullName; }
        }

        public IEnumerable<IAttribute> Attributes
        {
            get { return Methods.SelectMany((item) => item.Attributes).Distinct(); }
        }

        public string Name
        {
            get { return Methods.First().Name; }
        }

        public IEnumerable<IGenericParameter> GenericParameters
        {
            get { throw new NotSupportedException(); }
        }

        #region IPythonMethod Implementation

        #region Biggest/Smallest Methods

        public IMethod BiggestMethod
        {
            get
            {
                IMethod biggest = null;
                int count = -1;
                foreach (var item in Methods)
                {
                    int length = item.GetParameters().Length;
                    if (length > count)
                    {
                        biggest = item;
                        count = length;
                    }
                }
                return biggest;
            }
        }

        public IMethod SmallestMethod
        {
            get
            {
                IMethod smallest = Methods[0];
                int count = smallest.GetParameters().Length;
                foreach (var item in Methods.Skip(1))
                {
                    int length = item.GetParameters().Length;
                    if (length < count)
                    {
                        smallest = item;
                        count = length;
                    }
                }
                return smallest;
            }
        }

        #endregion

        #region Python Parameters

        public PythonParameter[] GetPythonParameters()
        {
            var fattestMethod = this.BiggestMethod;
            var smallestMethod = this.SmallestMethod;
            var fattestParams = fattestMethod.GetParameters();
            var smallestParams = smallestMethod.GetParameters();
            bool isStatic = this.IsStatic;
            PythonParameter[] parameters;
            int offset;
            if (isStatic)
            {
                parameters = new PythonParameter[fattestParams.Length];
                offset = 0;
            }
            else
            {
                parameters = new PythonParameter[fattestParams.Length + 1];
                parameters[0] = new PythonParameter("self", DeclaringType);
                offset = 1;
            }
            for (int i = 0; i < smallestParams.Length; i++)
            {
                parameters[i + offset] = new PythonParameter(smallestParams[i]);
            }
            for (int i = smallestParams.Length; i < fattestParams.Length; i++)
            {
                parameters[i + offset] = new PythonParameter(fattestParams[i], NullExpression.Instance);
            }
            return parameters;
        }

        #endregion

        #region Decorators

        /// <summary>
        /// Gets all decorators associated with this method.
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<PythonDecorator> GetDecorators()
        {
            List<PythonDecorator> decorators = new List<PythonDecorator>();
            var otherDecorators = Methods.Skip(1).Select((item) => item.GetDecorators());
            foreach (var item in Methods[0].GetDecorators())
            {
                bool success = true;
                foreach (var decors in otherDecorators)
                {
                    if (!decors.Contains(item))
                    {
                        success = false;
                        break;
                    }
                }
                if (success)
                {
                    decorators.Add(item);
                }
            }
            return decorators;
        }

        #endregion

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.IndentationString = "    ";
            foreach (var item in GetDecorators())
            {
                cb.AddCodeBuilder(item.GetCode());
            }
            cb.Append("def ");
            cb.Append(Name);
            cb.Append("(");
            bool first = true;
            var codeGenerator = new PythonCodeGenerator(this);
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
                if (item.DefaultValue != null)
                {
                    cb.Append(" = ");
                    cb.Append(((IPythonBlock)item.DefaultValue.Emit(codeGenerator)).GetCode());
                }
            }
            cb.Append("):");
            cb.IncreaseIndentation();
            cb.AddCodeBuilder(this.GetDocCode());
            cb.AddCodeBuilder(GetBodyCode());
            cb.DecreaseIndentation();
            return cb;
        }

        public CodeBuilder GetBodyCode()
        {
            var cb = new CodeBuilder();
            var codeGenerator = new PythonCodeGenerator(this);
            var sortedMethods = Methods.OrderBy((item) => item.GetParameters().Length);
            var pythonParams = GetPythonParameters();
            foreach (var item in sortedMethods.Take(Methods.Count - 1))
            {
                var itemParams = item.GetParameters();
                var firstExtraParam = pythonParams[itemParams.Length + 1];
                var condition = codeGenerator.EmitEquals(new PythonIdentifierBlock(codeGenerator, firstExtraParam.Name, firstExtraParam.ParameterType), firstExtraParam.DefaultValue.Emit(codeGenerator));
                for (int i = itemParams.Length + 2; i < pythonParams.Length; i++)
                {
                    var param = pythonParams[i];
                    var parameterIdentBlock = new PythonIdentifierBlock(codeGenerator, param.Name, param.ParameterType);
                    var ceq = codeGenerator.EmitEquals(parameterIdentBlock, param.DefaultValue.Emit(codeGenerator));
                    condition = codeGenerator.EmitLogicalAnd(condition, ceq);
                }
                ICodeBlock ifBody = new PythonCodeBlock(codeGenerator, PrimitiveTypes.Void, item.GetBodyCode());
                if (!item.GetHasReturnValue())
                {
                    ifBody = codeGenerator.EmitSequence(ifBody, codeGenerator.EmitReturn(null));
                }
                var ifBlock = codeGenerator.EmitIfElse(condition, ifBody, codeGenerator.EmitVoid());
                cb.AddCodeBuilder(((IPythonBlock)ifBlock).GetCode());
            }
            cb.AddCodeBuilder(sortedMethods.Last().GetBodyCode());
            return cb;
        }

        #endregion

        public IEnumerable<ModuleDependency> GetDependencies()
        {
            return Methods.GetDependencies();
        }
    }
}
