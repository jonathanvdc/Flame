using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Flame.Collections;
using Flame.TypeSystem;
using Mono.Cecil;

namespace Flame.Clr
{
    /// <summary>
    /// A data structure that represents the parts of an IL method signature
    /// that are relevant to method reference resolution.
    /// </summary>
    internal struct ClrMethodSignature : IEquatable<ClrMethodSignature>
    {
        /// <summary>
        /// Gets the name of the method signature.
        /// </summary>
        /// <returns>The method signature's name.</returns>
        public UnqualifiedName Name { get; private set; }

        /// <summary>
        /// Gets the number of generic parameters in this signature.
        /// </summary>
        /// <returns>The number of generic parameters.</returns>
        public int GenericParameterCount { get; private set; }

        /// <summary>
        /// Gets the return type of the method signature.
        /// </summary>
        /// <returns>The return type.</returns>
        public IType ReturnType { get; private set; }

        /// <summary>
        /// Gets the parameter types of the method signature.
        /// </summary>
        /// <returns>The parameter types.</returns>
        public ImmutableArray<IType> ParameterTypes { get; private set; }

        /// <summary>
        /// Checks if this method signature equals another method
        /// signature.
        /// </summary>
        /// <param name="other">
        /// The method signature to compare this signature to.
        /// </param>
        /// <returns>
        /// <c>true</c> if the method signatures are equal; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(ClrMethodSignature other)
        {
            return Name.Equals(other.Name)
                && GenericParameterCount == other.GenericParameterCount
                && ParameterTypes.Length == other.ParameterTypes.Length
                && object.Equals(ReturnType, other.ReturnType)
                && ParameterTypes.SequenceEqual(other.ParameterTypes);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int hashCode = EnumerableComparer.EmptyHash;

            hashCode = EnumerableComparer.FoldIntoHashCode(
                hashCode,
                Name.GetHashCode());

            hashCode = EnumerableComparer.FoldIntoHashCode(
                hashCode,
                GenericParameterCount);

            hashCode = EnumerableComparer.FoldIntoHashCode(
                hashCode,
                ReturnType.GetHashCode());

            foreach (var type in ParameterTypes)
            {
                hashCode = EnumerableComparer.FoldIntoHashCode(
                    hashCode,
                    type.GetHashCode());
            }
            return hashCode;
        }

        /// <inheritdoc/>
        public override bool Equals(object other)
        {
            return other is ClrMethodSignature && Equals((ClrMethodSignature)other);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{{ Name: {Name}, GenericParameterCount: {GenericParameterCount}, " +
                $"ReturnType: {ReturnType}, ParameterTypes: [{string.Join(", ", ParameterTypes)}] }}";
        }

        /// <summary>
        /// Creates a method signature from a method's name,
        /// the number of generic parameters the method
        /// takes, its return type and its parameter types.
        /// </summary>
        /// <param name="name">
        /// The name of the method signature.
        /// </param>
        /// <param name="genericParameterCount">
        /// The number of generic parameters in the method
        /// signature.
        /// </param>
        /// <param name="returnType">
        /// The return type of the method signature.
        /// </param>
        /// <param name="parameterTypes">
        /// The types of the method signature's parameters.
        /// </param>
        /// <returns>A method signature.</returns>
        public static ClrMethodSignature Create(
            UnqualifiedName name,
            int genericParameterCount,
            IType returnType,
            IReadOnlyList<IType> parameterTypes)
        {
            var result = new ClrMethodSignature();
            result.Name = name;
            result.GenericParameterCount = genericParameterCount;

            // Replace generic parameters with standins.
            var visitor = new GenericParameterToStandinVisitor();
            result.ReturnType = visitor.Visit(returnType);
            result.ParameterTypes = visitor
                .VisitAll(parameterTypes)
                .ToImmutableArray();
            return result;
        }

        /// <summary>
        /// Creates a method signature for a method.
        /// </summary>
        /// <param name="method">
        /// The method to describe using a signature.
        /// </param>
        /// <returns>
        /// A method signature.
        /// </returns>
        public static ClrMethodSignature Create(IMethod method)
        {
            return Create(
                method.Name,
                method.GenericParameters.Count,
                method.ReturnParameter.Type,
                method.Parameters
                    .Select(param => param.Type)
                    .ToArray());
        }
    }

    /// <summary>
    /// A stand-in for a generic parameter as used during the
    /// method reference resolution process.
    /// </summary>
    internal sealed class ClrGenericParameterStandin : IType
    {
        private ClrGenericParameterStandin(
            GenericParameterType kind,
            int position)
        {
            this.Kind = kind;
            this.Position = position;
        }

        /// <summary>
        /// Gets the kind of generic type represented by this
        /// standin.
        /// </summary>
        /// <returns>The generic parameter's kind.</returns>
        public GenericParameterType Kind { get; private set; }

        /// <summary>
        /// Gets the position of the generic parameter in the list
        /// of generic parameters that defines it.
        /// </summary>
        /// <returns>The generic parameter's position.</returns>
        public int Position { get; private set; }

        /// <inheritdoc/>
        public AttributeMap Attributes { get; private set; }

        /// <inheritdoc/>
        public QualifiedName FullName { get; private set; }

        /// <inheritdoc/>
        public UnqualifiedName Name => FullName.FullyUnqualifiedName;

        // This cache interns all generic parameter standins.
        private static InterningCache<ClrGenericParameterStandin> instanceCache
            = new InterningCache<ClrGenericParameterStandin>(
                new ClrGenericParameterStandinComparer(),
                InitializeInstance);

        private static ClrGenericParameterStandin InitializeInstance(
            ClrGenericParameterStandin standin)
        {
            standin.Attributes = AttributeMap.Empty;
            standin.FullName = new SimpleName(
                (standin.Kind == GenericParameterType.Method
                    ? "!!"
                    : "!")
                + standin.Position)
                .Qualify();
            return standin;
        }

        /// <summary>
        /// Creates a generic parameter stand-in.
        /// </summary>
        /// <param name="kind">
        /// The kind of generic parameter to create a stand-in for.
        /// </param>
        /// <param name="position">
        /// The position of the generic parameter in the generic
        /// parameter list.
        /// </param>
        /// <param name="isReferenceType">
        /// Tells if the generic parameter is always a reference type.
        /// </param>
        /// <returns>A generic parameter stand-in.</returns>
        internal static ClrGenericParameterStandin Create(
            GenericParameterType kind,
            int position)
        {
            return instanceCache.Intern(
                new ClrGenericParameterStandin(
                    kind,
                    position));
        }

        public TypeParent Parent
        {
            get
            {
                throw new InvalidOperationException();
            }
        }

        public IReadOnlyList<IType> BaseTypes
        {
            get
            {
                throw new InvalidOperationException();
            }
        }

        public IReadOnlyList<IField> Fields
        {
            get
            {
                throw new InvalidOperationException();
            }
        }

        public IReadOnlyList<IMethod> Methods
        {
            get
            {
                throw new InvalidOperationException();
            }
        }

        public IReadOnlyList<IProperty> Properties
        {
            get
            {
                throw new InvalidOperationException();
            }
        }

        public IReadOnlyList<IType> NestedTypes
        {
            get
            {
                throw new InvalidOperationException();
            }
        }

        public IReadOnlyList<IGenericParameter> GenericParameters
        {
            get
            {
                throw new InvalidOperationException();
            }
        }
    }

    internal sealed class ClrGenericParameterStandinComparer : IEqualityComparer<ClrGenericParameterStandin>
    {
        public bool Equals(ClrGenericParameterStandin x, ClrGenericParameterStandin y)
        {
            return x.Kind == y.Kind
                && x.Position == y.Position;
        }

        public int GetHashCode(ClrGenericParameterStandin obj)
        {
            return (obj.Position << 2)
                ^ (obj.Kind.GetHashCode() << 1);
        }
    }

    internal sealed class GenericParameterToStandinVisitor : TypeVisitor
    {
        public GenericParameterToStandinVisitor()
        {
            this.memoizedStandins =
                new Dictionary<IGenericParameter, ClrGenericParameterStandin>();
            this.processedParents =
                new HashSet<IGenericMember>();
        }

        private Dictionary<IGenericParameter, ClrGenericParameterStandin> memoizedStandins;
        private HashSet<IGenericMember> processedParents;

        protected override bool IsOfInterest(IType type)
        {
            return type is IGenericParameter;
        }

        protected override IType VisitInteresting(IType type)
        {
            var genericParameter = (IGenericParameter)type;
            var parent = genericParameter.ParentMember;
            if (parent is IMethod)
            {
                ProcessParent((IMethod)parent);
            }
            else
            {
                ProcessParent((IType)parent);
            }
            return memoizedStandins[genericParameter];
        }

        private void ProcessParent(IType type)
        {
            if (processedParents.Add(type))
            {
                var genParams = type.GetRecursiveGenericParameters();
                int genParamsCount = genParams.Count;
                for (int i = 0; i < genParamsCount; i++)
                {
                    memoizedStandins[genParams[i]] =
                        ClrGenericParameterStandin.Create(
                            GenericParameterType.Type,
                            i);
                }
            }
        }

        private void ProcessParent(IMethod method)
        {
            if (processedParents.Add(method))
            {
                var genParams = method.GenericParameters;
                int genParamsCount = genParams.Count;
                for (int i = 0; i < genParamsCount; i++)
                {
                    memoizedStandins[genParams[i]] =
                        ClrGenericParameterStandin.Create(
                            GenericParameterType.Method,
                            i);
                }
            }
        }
    }
}
