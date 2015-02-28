using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    /// <summary>
    /// Describes a potential local variable declaration.
    /// </summary>
    public class LocalDeclaration
    {
        /// <summary>
        /// Creates a new local variable declaration that declares the given local.
        /// </summary>
        /// <param name="Local"></param>
        public LocalDeclaration(CppLocal Local)
        {
            this.Local = Local;
            this.DeclareVariable = true;
        }

        /// <summary>
        /// Gets the local variable to declare.
        /// </summary>
        public CppLocal Local { get; private set; }
        /// <summary>
        /// Gets or sets a boolean value that indicates whether the variable should be declared or not.
        /// </summary>
        public bool DeclareVariable { get; set; }

        public override string ToString()
        {
            return Local.Type.FullName + " " + Local.Member.Name;
        }
    }

    public class LocalDeclarationReference : ICppBlock
    {
        public LocalDeclarationReference(CppLocal Local)
        {
            this.Declaration = new LocalDeclaration(Local);
            this.hasAcquired = true;
        }
        public LocalDeclarationReference(CppLocal Local, ICppBlock Value)
            : this(Local)
        {
            this.Value = Value; 
        }
        public LocalDeclarationReference(LocalDeclaration Declaration)
        {
            this.Declaration = Declaration;
            this.hasAcquired = false;
        }

        public LocalDeclaration Declaration { get; private set; }
        public ICppBlock Value { get; private set; }
        public ICodeGenerator CodeGenerator { get { return Declaration.Local.CodeGenerator; } }

        private bool hasAcquired;

        public bool DeclaresVariable
        {
            get
            {
                return Declaration.DeclareVariable && hasAcquired;
            }
        }

        public bool AssignsValue
        {
            get { return Value != null; }
        }

        public bool IsEmpty
        {
            get { return !DeclaresVariable && !AssignsValue; }
        }

        /// <summary>
        /// Acquires the local declaration.
        /// </summary>
        public void Acquire()
        {
            if (!hasAcquired)
            {
                hasAcquired = true;
                Declaration.DeclareVariable = false;
                Declaration = new LocalDeclaration(Declaration.Local);
            }
        }
        /// <summary>
        /// Acquires the local variable and assigns it the given value.
        /// </summary>
        /// <param name="Value"></param>
        public void Assign(ICppBlock Value)
        {
            Acquire();
            this.Value = Value;
        }

        public IType Type
        {
            get { return PrimitiveTypes.Void; }
        }

        private IEnumerable<IHeaderDependency> TypeDependencies
        {
            get { return DeclaresVariable ? Declaration.Local.Type.GetDependencies() : Enumerable.Empty<IHeaderDependency>(); }
        }
        private IEnumerable<IHeaderDependency> ValueDependencies
        {
            get { return Value != null ? Value.Dependencies : Enumerable.Empty<IHeaderDependency>(); }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return TypeDependencies.MergeDependencies(ValueDependencies); }
        }

        private IEnumerable<CppLocal> TypeLocalsUsed
        {
            get { return DeclaresVariable ? new CppLocal[] { Declaration.Local } : Enumerable.Empty<CppLocal>(); }
        }
        private IEnumerable<CppLocal> ValueLocalsUsed
        {
            get { return Value != null ? Value.LocalsUsed : Enumerable.Empty<CppLocal>(); }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return TypeLocalsUsed.Union(ValueLocalsUsed); }
        }

        protected bool UseAuto
        {
            get
            {
                if (Value is INewObjectBlock)
                {
                    var initBlock = (INewObjectBlock)Value;
                    return initBlock.Kind == AllocationKind.ManagedHeap || initBlock.Kind == AllocationKind.UnmanagedHeap;
                }
                else
                {
                    return false;
                }
            }
        }

        public CodeBuilder GetExpressionCode(bool EmitAuto)
        {
            if (DeclaresVariable)
            {
                CodeBuilder cb = new CodeBuilder();
                if (EmitAuto)
                {
                    cb.Append("auto");
                }
                else
                {
                    cb.Append(Declaration.Local.Type.CreateBlock(CodeGenerator).GetCode());
                }
                cb.Append(' ');
                cb.Append(Declaration.Local.Member.Name);

                if (Value != null)
                {
                    if (DeclaresVariable && !EmitAuto && Value is INewObjectBlock)
                    {
                        var initBlock = (INewObjectBlock)Value;
                        if (initBlock.Kind == AllocationKind.Stack || initBlock.Kind == AllocationKind.MakeManaged)
                        {
                            cb.Append(initBlock.GetArgumentListCode());
                            return cb;
                        }
                    }
                    cb.Append(" = ");
                    cb.Append(Value.GetCode());
                }
                return cb;
            }
            else if (Value != null)
            {
                return new VariableAssignmentBlock(Declaration.Local.CreateBlock(), Value).GetCode();
            }
            else
            {
                return new CodeBuilder();
            }
        }

        public CodeBuilder GetExpressionCode()
        {
            return GetExpressionCode(this.UseAuto);
        }

        public CodeBuilder GetCode()
        {
            var exprCode = GetExpressionCode();
            if (!exprCode.IsWhitespace)
            {
                exprCode.Append(";");
            }
            return exprCode;
        }

        public override string ToString()
        {
            return GetCode().ToString();
        }
    }
}
