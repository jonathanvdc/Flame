using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    #region ICallConvention

    /// <summary>
    /// Describes a calling convention for a MIPS method.
    /// </summary>
    public interface ICallConvention
    {
        int StackDelta { get; }
        bool UsesStaticLink { get; }
        IMethod Method { get; }
        IEnumerable<IStorageLocation> GetReturnValues(IAssemblerEmitContext Context);
        IEnumerable<IStorageLocation> GetArguments(IAssemblerEmitContext Context);

        bool PreservesTemporaries { get; }
    }

    #endregion

    #region AutoCallConvention

    /// <summary>
    /// An automatic implementation of a MIPS call convention.
    /// </summary>
    public class AutoCallConvention : ICallConvention
    {
        public AutoCallConvention(IAssemblerMethod Method, bool UsesStaticLink, bool PreservesTemporaries)
        {
            this.Method = Method;
            this.UsesStaticLink = UsesStaticLink;
            this.PreservesTemporaries = PreservesTemporaries;
            this.delta = -1;
        }
        public AutoCallConvention(IAssemblerMethod Method)
            : this(Method, true, false)
        { }

        public IAssemblerMethod Method { get; private set; }
        public bool UsesStaticLink { get; private set; }
        public bool PreservesTemporaries { get; private set; }

        IMethod ICallConvention.Method
        {
            get
            {
                return Method;
            }
        }

        private int delta;
        public int StackDelta
        {
            get
            {
                if (delta == -1)
                {
                    delta = 0;
                    int retTypeSize = Method.ReturnType.GetSize();
                    if (retTypeSize > 4)
                    {
                        delta += retTypeSize;
                    }
                    foreach (var item in Method.GetParameters())
                    {
                        int tSize = item.ParameterType.GetSize();
                        if (tSize > 4)
                        {
                            delta += tSize;
                        }
                    }
                }
                return delta;
            }
        }

        public IEnumerable<IStorageLocation> GetReturnValues(IAssemblerEmitContext Context)
        {
            int tsize = Method.ReturnType.GetSize();
            if (tsize <= 4) // $v0 - $v1
            {
                return new IStorageLocation[] { Context.AcquireRegister(RegisterType.ReturnValue, 0, Method.ReturnType) };
            }
            else // Stack-allocate
            {
                throw new NotImplementedException();
            }
        }

        public IEnumerable<IStorageLocation> GetArguments(IAssemblerEmitContext Context)
        {
            int argRegIndex = 0;
            var args = new List<IStorageLocation>();
            foreach (var item in Method.GetParameters())
            {
                int tsize = item.ParameterType.GetSize();
                if (argRegIndex < 4 && tsize <= 4) // $a0 - $a3
                {
                    args.Add(Context.AcquireRegister(RegisterType.Argument, argRegIndex, Method.ReturnType));
                    argRegIndex++;
                }
                else // Stack-allocate
                {
                    throw new NotImplementedException();
                }
            }
            return args;
        }
    }

    #endregion
}
