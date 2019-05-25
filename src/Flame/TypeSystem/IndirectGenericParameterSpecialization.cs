using System;
using System.Collections.Generic;
using Flame.Collections;

namespace Flame.TypeSystem
{
    /// <summary>
    /// A generic specialization of a generic parameter that is obtained
    /// by specializing the declaring member of a generic parameter
    /// definition.
    /// </summary>
    public sealed class IndirectGenericParameterSpecialization : TypeSpecialization, IGenericParameter
    {
        private IndirectGenericParameterSpecialization(
            IGenericParameter declaration, IGenericMember parentMember)
            : base(declaration)
        {
            this.ParentMember = parentMember;
        }

        private static IndirectGenericParameterSpecialization InitializeInstance(
            IndirectGenericParameterSpecialization instance)
        {
            instance.qualName = instance.Declaration.Name.Qualify(instance.ParentMember.FullName);
            instance.genericParameterCache = new Lazy<IReadOnlyList<IGenericParameter>>(
                instance.CreateGenericParameters);

            return instance;
        }

        private QualifiedName qualName;
        private Lazy<IReadOnlyList<IGenericParameter>> genericParameterCache;

        /// <inheritdoc/>
        public IGenericMember ParentMember { get; private set; }

        /// <inheritdoc/>
        public override TypeParent Parent
        {
            get
            {
                if (ParentMember is IMethod)
                {
                    return new TypeParent((IMethod)ParentMember);
                }
                else
                {
                    return new TypeParent((IType)ParentMember);
                }
            }
        }

        /// <inheritdoc/>
        public override UnqualifiedName Name => qualName.FullyUnqualifiedName;

        /// <inheritdoc/>
        public override QualifiedName FullName => qualName;

        /// <inheritdoc/>
        public override IReadOnlyList<IGenericParameter> GenericParameters => genericParameterCache.Value;

        private IReadOnlyList<IGenericParameter> CreateGenericParameters()
        {
            return CreateAll(Declaration, this);
        }

        private static InterningCache<IndirectGenericParameterSpecialization> instanceCache =
            new InterningCache<IndirectGenericParameterSpecialization>(
                new StructuralGenericParameterSpecializationComparer(),
                InitializeInstance);

        /// <summary>
        /// Creates a generic parameter specialization from a generic parameter
        /// and a parent type that is itself an (indirect) generic type.
        /// </summary>
        /// <param name="declaration">
        /// The generic parameter to specialize.
        /// </param>
        /// <param name="parentMember">
        /// A specialization of the generic declaration's parent member.
        /// </param>
        /// <returns>A specialization of the generic declaration.</returns>
        internal static IndirectGenericParameterSpecialization Create(
            IGenericParameter declaration,
            IGenericMember parentMember)
        {
            return instanceCache.Intern(
                new IndirectGenericParameterSpecialization(declaration, parentMember));
        }

        /// <summary>
        /// Specializes the generic parameter list of a generic member to
        /// an indirectly specialized generic parameter list for a
        /// specialization of the aforementioned generic member.
        /// </summary>
        /// <param name="genericMember">
        /// The original generic member whose generic parameters are specialized.
        /// </param>
        /// <param name="specializedMember">
        /// The proud parent of the indirectly specialized generic parameters
        /// produced by this method.
        /// </param>
        /// <returns>
        /// A list of indirectly specialized generic parameters.
        /// </returns>
        internal static IReadOnlyList<IndirectGenericParameterSpecialization> CreateAll(
            IGenericMember genericMember,
            IGenericMember specializedMember)
        {
            var originalTypeParams = genericMember.GenericParameters;
            var results = new IndirectGenericParameterSpecialization[originalTypeParams.Count];
            for (int i = 0; i < results.Length; i++)
            {
                results[i] = Create(originalTypeParams[i], specializedMember);
            }
            return results;
        }
    }

    internal sealed class StructuralGenericParameterSpecializationComparer : IEqualityComparer<IndirectGenericParameterSpecialization>
    {
        public bool Equals(IndirectGenericParameterSpecialization x, IndirectGenericParameterSpecialization y)
        {
            return object.Equals(x.Declaration, y.Declaration)
                && object.Equals(x.ParentMember, y.ParentMember);
        }

        public int GetHashCode(IndirectGenericParameterSpecialization obj)
        {
            return (((object)obj.ParentMember).GetHashCode() << 4)
                ^ ((object)obj.Declaration).GetHashCode();
        }
    }
}