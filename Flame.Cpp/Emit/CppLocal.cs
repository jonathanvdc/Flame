﻿using Flame.Compiler;
using Flame.Compiler.Expressions;
using Flame.Compiler.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class CppLocal : CppVariableBase
    {
        public CppLocal(ICodeGenerator CodeGenerator, int Index, IVariableMember Member)
            : base(CodeGenerator)
        {
            this.Index = Index;
            this.Member = Member;
        }

        public int Index { get; private set; }
        public IVariableMember Member { get; private set; }

        public static bool operator ==(CppLocal Left, CppLocal Right)
        {
            if (object.ReferenceEquals(Left, Right))
            {
                return true;
            }
            else if (object.ReferenceEquals(Left, null) || object.ReferenceEquals(Right, null))
            {
                return false;
            }
            return Left.Index == Right.Index;
        }
        public static bool operator !=(CppLocal Left, CppLocal Right)
        {
            return !(Left == Right);
        }

        public override bool Equals(object obj)
        {
            if (obj is CppLocal)
            {
                return this == (CppLocal)obj;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Index;
        }

        public override string ToString()
        {
            return Member.ToString();
        }

        public override IStatement CreateReleaseStatement()
        {
            return new ReleaseLocalStatement(this);
        }

        public override IType Type
        {
            get { return Member.VariableType; }
        }

        public override ICppBlock CreateBlock()
        {
            return new LocalBlock(this);
        }

        private class ReleaseLocalStatement : IStatement
        {
            public ReleaseLocalStatement(CppLocal Local)
            {
                this.Local = Local;
            }

            public CppLocal Local { get; private set; }

            public void Emit(IBlockGenerator Generator)
            {
                ((CppCodeGenerator)Local.CodeGenerator).LocalManager.Release(Local);
            }

            public bool IsEmpty
            {
                get { return false; }
            }

            public IStatement Optimize()
            {
                return this;
            }
        }
    }
}
