﻿using Flame.Compiler;
using Flame.Compiler.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class IfElseBlockGenerator : MutableCompositeBlockBase, IIfElseBlockGenerator, ICppLocalDeclaringBlock
    {
        public IfElseBlockGenerator(CppCodeGenerator CodeGenerator, ICppBlock Condition)
        {
            this.codeGen = CodeGenerator;
            this.Condition = Condition;
            this.IfBlock = new NotifyingBlockGenerator(CodeGenerator);
            this.ElseBlock = new NotifyingBlockGenerator(CodeGenerator);
            this.commonDecls = new List<LocalDeclarationReference>();

            this.IfBlock.BlockAdded += (sender, args) =>
            {
                RegisterChanged();
            };
            this.ElseBlock.BlockAdded += (sender, args) =>
            {
                RegisterChanged();
            };
            this.IfBlock.LocalDeclared += (sender, args) =>
            {
                if (Condition.DeclaresLocal(args.Local) || ElseBlock.DeclaresLocal(args.Local))
                {
                    Acquire(args.Declaration);
                }
            };
            this.ElseBlock.LocalDeclared += (sender, args) =>
            {
                if (Condition.DeclaresLocal(args.Local) || IfBlock.DeclaresLocal(args.Local))
                {
                    Acquire(args.Declaration);
                }
            };
        }

        private CppCodeGenerator codeGen;
        public ICppBlock Condition { get; private set; }
        public NotifyingBlockGenerator IfBlock { get; private set; }
        public NotifyingBlockGenerator ElseBlock { get; private set; }

        public override ICodeGenerator CodeGenerator
        {
            get
            {
                return codeGen;
            }
        }

        IBlockGenerator IIfElseBlockGenerator.IfBlock
        {
            get { return IfBlock; }
        }

        IBlockGenerator IIfElseBlockGenerator.ElseBlock
        {
            get { return ElseBlock; }
        }

        public IEnumerable<LocalDeclaration> LocalDeclarations
        {
            get 
            {
                return CommonDeclarations.Concat(new object[] { Condition, IfBlock, ElseBlock }.OfType<ICppLocalDeclaringBlock>().SelectMany((item) => item.LocalDeclarations));
            }
        }

        private List<LocalDeclarationReference> commonDecls;
        public IEnumerable<LocalDeclaration> CommonDeclarations
        {
            get { return commonDecls.Select(item => item.Declaration); }
        }

        public void Acquire(LocalDeclaration Declaration)
        {
            var localDecl = new LocalDeclarationReference(Declaration);
            localDecl.Acquire();
            commonDecls.Add(localDecl);
            RegisterChanged();
        }

        public override ICppBlock Simplify()
        {
            var blockGen = new CppBlockGenerator(codeGen);

            foreach (var item in commonDecls)
            {
                blockGen.EmitBlock(item);
            }
            blockGen.EmitBlock(new IfElseBlock(CodeGenerator, Condition, IfBlock, ElseBlock));

            return blockGen;
        }
    }
}
