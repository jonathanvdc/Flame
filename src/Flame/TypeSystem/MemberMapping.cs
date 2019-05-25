using System;

namespace Flame.TypeSystem
{
    /// <summary>
    /// A (type mapping, method mapping, field mapping) triple.
    /// </summary>
    public sealed class MemberMapping
    {
        /// <summary>
        /// Creates a member mapping.
        /// </summary>
        /// <param name="mapType">
        /// A type-to-type mapping.
        /// </param>
        /// <param name="mapMethod">
        /// A method-to-method mapping.
        /// </param>
        /// <param name="mapField">
        /// A field-to-field mapping.
        /// </param>
        public MemberMapping(
            Func<IType, IType> mapType,
            Func<IMethod, IMethod> mapMethod,
            Func<IField, IField> mapField)
        {
            this.MapType = mapType;
            this.MapMethod = mapMethod;
            this.MapField = mapField;
        }

        /// <summary>
        /// Creates a member mapping from a type mapping.
        /// </summary>
        /// <param name="mapType">
        /// A type-to-type mapping.
        /// </param>
        public MemberMapping(Func<IType, IType> mapType)
        {
            this.MapType = mapType;
            var visitor = new TypeFuncVisitor(mapType);
            MapMethod = visitor.Visit;
            MapField = visitor.Visit;
        }

        /// <summary>
        /// Gets a type-to-type mapping.
        /// </summary>
        /// <returns>A type-to-type mapping.</returns>
        public Func<IType, IType> MapType { get; private set; }

        /// <summary>
        /// Gets a method-to-method mapping.
        /// </summary>
        /// <returns>A method-to-method mapping.</returns>
        public Func<IMethod, IMethod> MapMethod { get; private set; }

        /// <summary>
        /// Gets a field-to-field mapping.
        /// </summary>
        /// <returns>A field-to-field mapping.</returns>
        public Func<IField, IField> MapField { get; private set; }
    }
}