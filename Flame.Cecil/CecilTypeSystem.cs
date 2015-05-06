using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilTypeSystem
    {
        public CecilTypeSystem(CecilModule Module)
        {
            this.Module = Module;
            this.multicastDelegate = new Lazy<ICecilType>(() => CecilType.ImportCecil<MulticastDelegate>(Module));
            this.funcDelegates = new Dictionary<int, ICecilType>();
            this.actionDelegates = new Dictionary<int, ICecilType>();
            this.canonicalDelegates = new Dictionary<DelegateSignature, ICecilType>();
        }

        public CecilModule Module { get; private set; }

        private Lazy<ICecilType> multicastDelegate;
        public ICecilType MulticastDelegate { get { return multicastDelegate.Value; } }

        private Dictionary<int, ICecilType> funcDelegates;
        private Dictionary<int, ICecilType> actionDelegates;
        private Dictionary<DelegateSignature, ICecilType> canonicalDelegates;

        public ICecilType GetCanonicalDelegate(IMethod Signature)
        {
            var sig = new DelegateSignature(Signature.ReturnType, Signature.GetParameters().GetTypes());
            if (!canonicalDelegates.ContainsKey(sig))
            {
                var deleg = GetCanonicalDelegateCore(sig);
                canonicalDelegates[sig] = deleg;
                return deleg;
            }
            return canonicalDelegates[sig];
        }

        private ICecilType GetCanonicalDelegateCore(DelegateSignature Signature)
        {
            if (Signature.ReturnType == null || Signature.ReturnType.Equals(PrimitiveTypes.Void))
            {
                return (ICecilType)GetActionDelegate(Signature.ParameterTypes.Length).MakeGenericType(Signature.ParameterTypes);
            }
            else
            {
                return (ICecilType)GetFuncDelegate(Signature.ParameterTypes.Length).MakeGenericType(Signature.ParameterTypes.Concat(new IType[] { Signature.ReturnType }));
            }
        }

        #region GetActionDelegateType

        private Type GetActionDelegateType(int ParameterCount)
        {
            switch (ParameterCount)
            {
                case 0:
                    return typeof(Action);
                case 1:
                    return typeof(Action<>);
                case 2:
                    return typeof(Action<,>);
                case 3:
                    return typeof(Action<,,>);
                case 4:
                    return typeof(Action<,,,>);
                case 5:
                    return typeof(Action<,,,,>);
                case 6:
                    return typeof(Action<,,,,,>);
                case 7:
                    return typeof(Action<,,,,,,>);
                case 8:
                    return typeof(Action<,,,,,,,>);
                case 9:
                    return typeof(Action<,,,,,,,,>);
                case 10:
                    return typeof(Action<,,,,,,,,,>);
                case 11:
                    return typeof(Action<,,,,,,,,,,>);
                case 12:
                    return typeof(Action<,,,,,,,,,,,>);
                case 13:
                    return typeof(Action<,,,,,,,,,,,,>);
                case 14:
                    return typeof(Action<,,,,,,,,,,,,,>);
                case 15:
                    return typeof(Action<,,,,,,,,,,,,,,>);
                case 16:
                    return typeof(Action<,,,,,,,,,,,,,,,>);
                default:
                    throw new NotSupportedException("CLR delegate types for more than 16 parameters are not supported.");
            }
        }

        #endregion

        #region GetFunctionDelegateType

        private Type GetFunctionDelegateType(int ParameterCount)
        {
            switch (ParameterCount)
            {
                case 0:
                    return typeof(Func<>);
                case 1:
                    return typeof(Func<,>);
                case 2:
                    return typeof(Func<,,>);
                case 3:
                    return typeof(Func<,,,>);
                case 4:
                    return typeof(Func<,,,,>);
                case 5:
                    return typeof(Func<,,,,,>);
                case 6:
                    return typeof(Func<,,,,,,>);
                case 7:
                    return typeof(Func<,,,,,,,>);
                case 8:
                    return typeof(Func<,,,,,,,,>);
                case 9:
                    return typeof(Func<,,,,,,,,,>);
                case 10:
                    return typeof(Func<,,,,,,,,,,>);
                case 11:
                    return typeof(Func<,,,,,,,,,,,>);
                case 12:
                    return typeof(Func<,,,,,,,,,,,,>);
                case 13:
                    return typeof(Func<,,,,,,,,,,,,,>);
                case 14:
                    return typeof(Func<,,,,,,,,,,,,,,>);
                case 15:
                    return typeof(Func<,,,,,,,,,,,,,,,>);
                case 16:
                    return typeof(Func<,,,,,,,,,,,,,,,,>);
                default:
                    throw new NotSupportedException("CLR delegate types for more than 16 parameters are not supported.");
            }
        }

        #endregion

        private ICecilType GetActionDelegate(int ParameterCount)
        {
            if (actionDelegates.ContainsKey(ParameterCount))
            {
                return actionDelegates[ParameterCount];
            }
            else
            {
                var type = CecilType.ImportCecil(GetActionDelegateType(ParameterCount), Module);
                actionDelegates[ParameterCount] = type;
                return type;
            }
        }

        private ICecilType GetFuncDelegate(int ParameterCount)
        {
            if (funcDelegates.ContainsKey(ParameterCount))
            {
                return funcDelegates[ParameterCount];
            }
            else
            {
                var type = CecilType.ImportCecil(GetFunctionDelegateType(ParameterCount), Module);
                funcDelegates[ParameterCount] = type;
                return type;
            }
        }
    
        private struct DelegateSignature : IEquatable<DelegateSignature>
        {
            public DelegateSignature(IType ReturnType, IType[] ParameterTypes)
            {
                this.ReturnType = ReturnType;
                this.ParameterTypes = ParameterTypes;
            }

            public IType ReturnType;
            public IType[] ParameterTypes;

            public bool Equals(DelegateSignature other)
            {
                return ReturnType == other.ReturnType && ParameterTypes.SequenceEqual(other.ParameterTypes);
            }

            public override int GetHashCode()
            {
                int result = ParameterTypes.Aggregate(0, (aggr, item) => item.GetHashCode() ^ aggr);
                if (ReturnType != null)
                {
                    result ^= ReturnType.GetHashCode();
                }
                return result;
            }

            public override bool Equals(object obj)
            {
                if (obj is DelegateSignature)
                {
                    return Equals((DelegateSignature)obj);
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
