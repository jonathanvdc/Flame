using Flame.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Plugs
{
    public class StdSharedPointer : StdTemplatedTypeBase
    {
        private StdSharedPointer()
        {
            ElementType = new DescribedGenericParameter("T", this);
        }

        public IGenericParameter ElementType { get; private set; }

        public override IEnumerable<IGenericParameter> GetGenericParameters()
        {
            return new IGenericParameter[] { ElementType };
        }

        public override string Name
        {
            get { return "shared_ptr<>"; }
        }

        public override IEnumerable<IAttribute> GetAttributes()
        {
            return new IAttribute[] { new AccessAttribute(AccessModifier.Public) };
        }

        public override IMethod[] GetConstructors()
        {
            return new IMethod[] { CreateSharedPointer };
        }

        public override IField[] GetFields()
        {
            return new IField[0];
        }

        public override IMethod[] GetMethods()
        {
            return new IMethod[] { GetSharedPointer };
        }

        public override IProperty[] GetProperties()
        {
            return new IProperty[0];
        }

        #region Static

        private static StdSharedPointer inst;
        public static StdSharedPointer Instance
        {
            get
            {
                if (inst == null)
                {
                    inst = new StdSharedPointer();
                }
                return inst;
            }
        }

        #region Members

        private IMethod createSharedPtr;
        public IMethod CreateSharedPointer
        {
            get
            {
                if (createSharedPtr == null)
                {
                    // C++ signature: 
                    // template<typename T>
                    // std::shared_ptr<T>::shared_ptr(T* Instance)
                    // D# signature:
                    // public const this(T* Instance);

                    var descMethod = new DescribedMethod("shared_ptr", this);
                    descMethod.IsStatic = false;
                    descMethod.IsConstructor = true;
                    descMethod.AddParameter(new DescribedParameter("Instance", ElementType.MakePointerType(PointerKind.TransientPointer)));
                    descMethod.ReturnType = PrimitiveTypes.Void;
                    createSharedPtr = descMethod;
                }
                return createSharedPtr;
            }
        }

        private IMethod getSharedPtr;
        public IMethod GetSharedPointer
        {
            get
            {
                if (getSharedPtr == null)
                {
                    // C++ signature: 
                    // template<typename T>
                    // T* std::shared_ptr<T>::get()
                    // D# signature:
                    // T* get();

                    var descMethod = new DescribedMethod("get", this);
                    descMethod.IsStatic = false;
                    descMethod.IsConstructor = false;
                    descMethod.ReturnType = ElementType.MakePointerType(PointerKind.TransientPointer);
                    getSharedPtr = descMethod;
                }
                return getSharedPtr;
            }
        }

        #endregion

        #endregion
    }

    public class StdSharedPointerInstance : DescribedGenericTypeInstance, IPointerType
    {
        public StdSharedPointerInstance(IType ElementType)
            : base(StdSharedPointer.Instance, new IType[] { ElementType })
        {

        }

        public override bool IsContainerType
        {
            get
            {
                return true;
            }
        }

        public override IContainerType AsContainerType()
        {
            return this;
        }

        public PointerKind PointerKind
        {
            get { return PointerKind.ReferencePointer; }
        }

        public IArrayType AsArrayType()
        {
            return null;
        }

        public IPointerType AsPointerType()
        {
            return this;
        }

        public IVectorType AsVectorType()
        {
            return null;
        }

        public ContainerTypeKind ContainerKind
        {
            get { return ContainerTypeKind.Pointer; }
        }

        public IType GetElementType()
        {
            return this.TypeArguments.First();
        }
    }
}
