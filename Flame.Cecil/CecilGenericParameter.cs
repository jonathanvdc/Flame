using Flame.Build;
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
        public CecilGenericParameter(GenericParameter GenericParameter, CecilModule Module)
            : base(Module)
        {
            this.genericParam = GenericParameter;
        }
        public CecilGenericParameter(GenericParameter GenericParameter, CecilModule Module, IGenericMember DeclaringMember)
            : base(Module)
        {
            this.genericParam = GenericParameter;
            this.declMember = DeclaringMember;
        }

        private GenericParameter genericParam;
        public override TypeReference GetTypeReference()
        {
            return genericParam;
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
                        declMember = Module.Convert(declType);
                    }
                    else
                    {
                        declMember = Module.Convert(genericParam.DeclaringMethod);
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
                results.Add(new TypeConstraint(Module.Convert(constraints[i])));
            }
            if (HasReferenceTypeConstraint)
            {
                results.Add(ReferenceTypeConstraint.Instance);
            }
            if (HasValueTypeConstraint)
            {
                results.Add(ValueTypeConstraint.Instance);
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

        private IType[] cachedBaseTypes;
        // Note that these "base types" are really the type constraints.
        public override IEnumerable<IType> BaseTypes
        {
            get
            {
                if (cachedBaseTypes == null)
                {
                    cachedBaseTypes = Constraint.ExtractBaseTypes().ToArray();
                }
                return cachedBaseTypes;
            }
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

        #endregion

        public override IEnumerable<IGenericParameter> GenericParameters
        {
            get { return Enumerable.Empty<IGenericParameter>(); }
        }

        public override IAncestryRules AncestryRules
        {
            get { return DefinitionAncestryRules.Instance; }
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

        public static CecilGenericParameter[] DeclareGenericParameters(
            IGenericParameterProvider ParameterProvider, 
            IGenericParameter[] Templates, CecilModule Module,
            IGenericMember DeclaringMember)
        {
            GenericParameter[] parameters = new GenericParameter[Templates.Length];
            CecilGenericParameter[] results = new CecilGenericParameter[Templates.Length];
            var mapping = new Dictionary<IType, IType>();
            for (int i = 0; i < Templates.Length; i++)
            {
                var paramTemplate = Templates[i];
                var param = new GenericParameter(paramTemplate.Name.ToString(), ParameterProvider);
                ParameterProvider.GenericParameters.Add(param);
                parameters[i] = param;
                var resultParam = new CecilGenericParameter(param, Module, DeclaringMember);
                results[i] = resultParam;
                mapping[paramTemplate] = resultParam;
            }

            var conv = new TypeMappingConverter(mapping);
            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                foreach (var item in Templates[i].BaseTypes)
                {
                    param.Constraints.Add(
                        conv.Convert(item)
                        .GetImportedReference(Module, ParameterProvider));
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

                foreach (var item in Templates[i].Attributes)
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

                param.Attributes = genericParamAttrs;
            }
            return results;
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
            var otherParam = other.GetImportedReference(Module) as GenericParameter;

            if (otherParam == null)
            {
                return false;
            }

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
