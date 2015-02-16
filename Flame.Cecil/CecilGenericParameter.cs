using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilGenericParameter : CecilTypeBase, IGenericParameter, IEquatable<IGenericParameter>
    {
        public CecilGenericParameter(GenericParameter GenericParameter)
        {
            this.genericParam = GenericParameter;
        }
        public CecilGenericParameter(GenericParameter GenericParameter, AncestryGraph Graph)
            : base(Graph)
        {
            this.genericParam = GenericParameter;
        }
        public CecilGenericParameter(GenericParameter GenericParameter, AncestryGraph Graph, IGenericMember DeclaringMember)
            : base(Graph)
        {
            this.genericParam = GenericParameter;
            this.declMember = DeclaringMember;
        }

        private GenericParameter genericParam;
        public override TypeReference GetTypeReference()
        {
            return genericParam;
        }

        public override bool IsComplete
        {
            get { return true; }
        }

        #region IGenericParameter Implementation

        private IGenericMember declMember;
        public IGenericMember DeclaringMember
        {
            get
            {
                if (declMember == null)
                {
                    var declType = genericParam.DeclaringType;
                    if (declType != null)
                    {
                        declMember = CecilTypeBase.Create(declType);
                    }
                    else
                    {
                        declMember = CecilMethodBase.Create(genericParam.DeclaringMethod);
                    }
                }
                return declMember;
            }
        }

        private MemberReference resolvedDeclRef;
        protected MemberReference ResolvedDeclaringReference
        {
            get
            {
                if (resolvedDeclRef == null)
                {
                    var declType = genericParam.DeclaringType;
                    if (declType != null)
                    {
                        resolvedDeclRef = declType.Resolve();
                    }
                    else
                    {
                        resolvedDeclRef = genericParam.DeclaringMethod.Resolve();
                    }
                }
                return resolvedDeclRef;
            }
        }

        public IType ParameterType
        {
            get { return null; }
        }

        public bool IsAssignable(IType Type)
        {
            return Constraint.Satisfies(Type);
        }

        #region Constraints

        public bool IsCovariant
        {
            get
            {
                return genericParam.IsCovariant;
            }
        }
        public bool IsContravariant
        {
            get
            {
                return genericParam.IsContravariant;
            }
        }

        public bool HasReferenceTypeConstraint
        {
            get
            {
                return genericParam.HasReferenceTypeConstraint;
            }
        }

        public bool HasValueTypeConstraint
        {
            get
            {
                return genericParam.HasNotNullableValueTypeConstraint;
            }
        }

        private IEnumerable<IGenericConstraint> GetConstraints()
        {
            var constraints = genericParam.Constraints;
            var results = new List<IGenericConstraint>(constraints.Count);
            for (int i = 0; i < constraints.Count; i++)
            {
                results.Add(new TypeConstraint(CecilTypeBase.Create(constraints[i])));
            }
            if (HasReferenceTypeConstraint)
            {
                results.Add(new ReferenceTypeConstraint());
            }
            if (HasValueTypeConstraint)
            {
                results.Add(new ValueTypeConstraint());
            }
            return results;
        }

        private IGenericConstraint cachedConstraint;
        public IGenericConstraint Constraint
        {
            get
            {
                if (cachedConstraint == null)
                {
                    cachedConstraint = new AndConstraint(GetConstraints());
                }
                return cachedConstraint;
            }
        }

        #endregion

        #endregion

        #region CecilTypeBase Implementation

        public override IType ResolveTypeParameter(IGenericParameter TypeParameter)
        {
            return null;
        }

        private IType[] cachedBaseTypes;
        // Note that these "base types" are really the type constraints.
        public override IType[] GetBaseTypes()
        {
            if (cachedBaseTypes == null)
            {
                cachedBaseTypes = Constraint.ExtractBaseTypes().ToArray();
            }
            return cachedBaseTypes;
        }

        public override ICecilType GetCecilGenericDeclaration()
        {
            return this;
        }

        protected override IEnumerable<IAttribute> GetMemberAttributes()
        {
            List<IAttribute> attrs = new List<IAttribute>();
            if (HasReferenceTypeConstraint)
            {
                attrs.Add(PrimitiveAttributes.Instance.ReferenceTypeAttribute);
            }
            if (HasValueTypeConstraint)
            {
                attrs.Add(PrimitiveAttributes.Instance.ValueTypeAttribute);
            }
            if (IsCovariant)
            {
                attrs.Add(PrimitiveAttributes.Instance.OutAttribute);
            }
            if (IsContravariant)
            {
                attrs.Add(PrimitiveAttributes.Instance.InAttribute);
            }
            return attrs;
        }

        protected override IList<CustomAttribute> GetCustomAttributes()
        {
            return genericParam.CustomAttributes;
        }

        public override bool IsContainerType
        {
            get { return false; }
        }

        public override IContainerType AsContainerType()
        {
            return null;
        }

        #endregion

        public override IEnumerable<IType> GetGenericArguments()
        {
            return new IType[0];
        }

        public override IEnumerable<IType> GetCecilGenericArguments()
        {
            return DeclaringGenericMember.GetCecilGenericArgumentsOrEmpty();
        }

        public override IBoundObject GetDefaultValue()
        {
            return null;
        }

        #region Type Members

        protected override IList<MethodDefinition> GetCecilMethods()
        {
            return new MethodDefinition[0];
        }

        protected override IList<PropertyDefinition> GetCecilProperties()
        {
            return new PropertyDefinition[0];
        }

        protected override IList<FieldDefinition> GetCecilFields()
        {
            return new FieldDefinition[0];
        }

        protected override IList<EventDefinition> GetCecilEvents()
        {
            return new EventDefinition[0];
        }

        #endregion

        #region Static

        private static bool SupportsGenericVariance(IGenericParameterProvider ParameterProvider)
        {
            if (ParameterProvider is TypeReference)
            {
                var typeDef = ((TypeReference)ParameterProvider).Resolve();
                if (typeDef.IsFunctionPointer || typeDef.IsInterface)
                {
                    return true;
                }
                else
                {
                    string delegateName = typeDef.Module.Import(typeof(Delegate)).FullName;
                    var baseType = typeDef.BaseType;
                    while (baseType != null)
                    {
                        if (baseType.FullName == delegateName)
                        {
                            return true;
                        }
                        baseType = baseType.Resolve().BaseType;
                    }
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public static CecilGenericParameter[] DeclareGenericParameters(IGenericParameterProvider ParameterProvider, IGenericParameter[] Templates, AncestryGraph Graph, IGenericMember DeclaringMember)
        {
            return DeclareGenericParameters(ParameterProvider, Templates).Select((item) => new CecilGenericParameter(item, Graph, DeclaringMember)).ToArray();
        }

        public static GenericParameter[] DeclareGenericParameters(IGenericParameterProvider ParameterProvider, IGenericParameter[] Templates)
        {
            GenericParameter[] parameters = new GenericParameter[Templates.Length];
            for (int i = 0; i < Templates.Length; i++)
            {
                var param = new GenericParameter(Templates[i].Name, ParameterProvider);
                parameters[i] = param;
                ParameterProvider.GenericParameters.Add(param);
            }
            var module = ParameterProvider.Module;
            for (int i = 0; i < parameters.Length; i++)
            {
                foreach (var item in Templates[i].GetBaseTypes())
                {
                    parameters[i].Constraints.Add(item.GetImportedReference(module, ParameterProvider));
                }

                GenericParameterAttributes genericParamAttrs = default(GenericParameterAttributes);

                if (Templates[i].Constraint.HasConstraint<ReferenceTypeConstraint>())
                {
                    genericParamAttrs |= GenericParameterAttributes.ReferenceTypeConstraint;
                }
                else if (Templates[i].Constraint.HasConstraint<ValueTypeConstraint>())
                {
                    genericParamAttrs |= GenericParameterAttributes.NotNullableValueTypeConstraint;
                }

                foreach (var item in Templates[i].GetAttributes())
                {
                    if (item.AttributeType.Equals(PrimitiveAttributes.Instance.OutAttribute.AttributeType) && SupportsGenericVariance(ParameterProvider))
                    {
                        genericParamAttrs |= GenericParameterAttributes.Covariant;
                    }
                    else if (item.AttributeType.Equals(PrimitiveAttributes.Instance.InAttribute.AttributeType) && SupportsGenericVariance(ParameterProvider))
                    {
                        genericParamAttrs |= GenericParameterAttributes.Contravariant;
                    }
                }

                parameters[i].Attributes = genericParamAttrs;
            }
            return parameters;
        }

        #endregion

        #region Equality

        public override bool Equals(object obj)
        {
            if (obj is IGenericParameter)
            {
                return Equals((IGenericParameter)obj);
            }
            return base.Equals(obj);
        }

        public override bool Equals(ICecilType other)
        {
            if (other is IGenericParameter)
            {
                return Equals((IGenericParameter)other);
            }
            return base.Equals(other);
        }
        public bool Equals(IGenericParameter other)
        {
            var thisParam = this.genericParam;
            var otherParam = other.GetTypeReference(thisParam.Module) as GenericParameter;

            if (otherParam == null)
            {
                return false;
            }

            //return thisParam.Owner == otherParam.Owner && thisParam.Position == otherParam.Position;

            if (other is CecilGenericParameter)
            {
                return thisParam.Position == otherParam.Position && this.ResolvedDeclaringReference.Equals(((CecilGenericParameter)other).ResolvedDeclaringReference);
            }
            else
            {
                return thisParam.Position == otherParam.Position && this.DeclaringMember.Equals(other.DeclaringMember);
            }
        }

        public override int GetHashCode()
        {
            return this.genericParam.GetHashCode();
        }

        #endregion
    }
}
