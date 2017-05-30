using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Flame.Compiler;
using Flame.Compiler.Expressions;

namespace Flame.Wasm
{
    /// <summary>
    /// A common interface for wasm expressions.
    /// </summary>
    public abstract class WasmExpr
    {
        /// <summary>
        /// Gets this wasm expression's kind.
        /// </summary>
        public abstract ExprKind Kind { get; }

        /// <summary>
        /// Gets this wasm expression's code.
        /// </summary>
        public abstract CodeBuilder ToCode();

        public override string ToString()
        {
            return ToCode().ToString();
        }
    }

    /// <summary>
    /// A 'call' expression, that applies some opcode 
    /// to a number of arguments, which are themselves
    /// wasm expressions.
    /// </summary>
    public class CallExpr : WasmExpr
    {
        public CallExpr(OpCode Target, params WasmExpr[] Arguments)
            : this(Target, (IReadOnlyList<WasmExpr>)Arguments)
        { }
        public CallExpr(OpCode Target, IReadOnlyList<WasmExpr> Arguments)
        {
            this.Target = Target;
            this.Arguments = Arguments;
        }

        /// <summary>
        /// Gets this 'call' expression's target opcode.
        /// </summary>
        public OpCode Target { get; private set; }

        /// <summary>
        /// Gets this wasm expre.
        /// </summary>
        public IReadOnlyList<WasmExpr> Arguments { get; private set; }

        /// <summary>
        /// Gets this wasm expression's kind.
        /// </summary>
        public override ExprKind Kind { get { return ExprKind.Call; } }

        private static bool UseNewlines(CodeBuilder[] Args, int MaxLength, int IndentationLength)
        {
            int totalLength = 0;
            foreach (var item in Args)
            {
                if (item.GetCodeLineCount(0) > 1)
                    return true;

                totalLength += item.FirstCodeLine.GetTotalLength(IndentationLength);

                if (totalLength > MaxLength)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Gets this S-expression's code.
        /// </summary>
        public override CodeBuilder ToCode()
        {
            var cb = new CodeBuilder();
            cb.Append('(');
            cb.Append(Target.Mnemonic);
            cb.IncreaseIndentation();
            var argsCode = Arguments.Select(item => item.ToCode()).ToArray();
            if (UseNewlines(argsCode, 80, 4))
            {
                foreach (var item in argsCode)
                {
                    cb.AddCodeBuilder(item);
                }
            }
            else
            {
                foreach (var item in argsCode)
                {
                    cb.Append(' ');
                    cb.Append(item);
                }
            }
            cb.DecreaseIndentation();
            cb.Append(")");
            return cb;
        }
    }

    /// <summary>
    /// A wasm i32 literal expression.
    /// </summary>
    public class Int32Expr : WasmExpr
    {
        public Int32Expr(int Value)
        {
            this.Value = Value;
        }

        /// <summary>
        /// Gets this wasm literal's contents.
        /// </summary>
        public int Value { get; private set; }

        /// <summary>
        /// Gets this wasm literal's expression kind.
        /// </summary>
        public override ExprKind Kind { get { return ExprKind.Int32; } }

        public override CodeBuilder ToCode()
        {
            return new CodeBuilder(Value.ToString(CultureInfo.InvariantCulture));
        }
    }

    /// <summary>
    /// A wasm i64 literal expression.
    /// </summary>
    public class Int64Expr : WasmExpr
    {
        public Int64Expr(long Value)
        {
            this.Value = Value;
        }

        /// <summary>
        /// Gets this wasm literal's contents.
        /// </summary>
        public long Value { get; private set; }

        /// <summary>
        /// Gets this wasm literal's expression kind.
        /// </summary>
        public override ExprKind Kind { get { return ExprKind.Int64; } }

        public override CodeBuilder ToCode()
        {
            return new CodeBuilder(Value.ToString(CultureInfo.InvariantCulture));
        }
    }

    /// <summary>
    /// A wasm f32 literal expression.
    /// </summary>
    public class Float32Expr : WasmExpr
    {
        public Float32Expr(float Value)
        {
            this.Value = Value;
        }

        /// <summary>
        /// Gets this wasm literal's contents.
        /// </summary>
        public float Value { get; private set; }

        /// <summary>
        /// Gets this wasm literal's expression kind.
        /// </summary>
        public override ExprKind Kind { get { return ExprKind.Float32; } }

        public override CodeBuilder ToCode()
        {
            return new CodeBuilder(Value.ToString(CultureInfo.InvariantCulture));
        }
    }

    /// <summary>
    /// A wasm f64 literal expression.
    /// </summary>
    public class Float64Expr : WasmExpr
    {
        public Float64Expr(double Value)
        {
            this.Value = Value;
        }

        /// <summary>
        /// Gets this wasm literal's contents.
        /// </summary>
        public double Value { get; private set; }

        /// <summary>
        /// Gets this wasm literal's expression kind.
        /// </summary>
        public override ExprKind Kind { get { return ExprKind.Float64; } }

        public override CodeBuilder ToCode()
        {
            return new CodeBuilder(Value.ToString(CultureInfo.InvariantCulture));
        }
    }

    /// <summary>
    /// A wasm identifier expression.
    /// </summary>
    public class IdentifierExpr : WasmExpr
    {
        public IdentifierExpr(string Identifier)
        {
            this.Identifier = Identifier;
        }

        /// <summary>
        /// Gets this wasm literal's contents.
        /// </summary>
        public string Identifier { get; private set; }

        /// <summary>
        /// Gets this wasm literal's expression kind.
        /// </summary>
        public override ExprKind Kind { get { return ExprKind.Identifier; } }

        public override CodeBuilder ToCode()
        {
            return new CodeBuilder("$" + Identifier);
        }
    }

    /// <summary>
    /// A wasm mnemonic expression.
    /// </summary>
    public class MnemonicExpr : WasmExpr
    {
        public MnemonicExpr(string Mnemonic)
        {
            this.Mnemonic = Mnemonic;
        }

        /// <summary>
        /// Gets this wasm literal's contents.
        /// </summary>
        public string Mnemonic { get; private set; }

        /// <summary>
        /// Gets this wasm literal's expression kind.
        /// </summary>
        public override ExprKind Kind { get { return ExprKind.Mnemonic; } }

        public override CodeBuilder ToCode()
        {
            return new CodeBuilder(Mnemonic);
        }
    }

    /// <summary>
    /// A wasm string expression.
    /// </summary>
    public class StringExpr : WasmExpr
    {
        public StringExpr(string Value)
        {
            this.Value = Value;
        }

        /// <summary>
        /// Gets this wasm literal's contents.
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// Gets this wasm literal's expression kind.
        /// </summary>
        public override ExprKind Kind { get { return ExprKind.String; } }

        public override CodeBuilder ToCode()
        {
            return new CodeBuilder(StringExpression.ToLiteral(Value, "\""));
        }
    }
}

