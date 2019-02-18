using System;
using System.Collections.Generic;
using Flame.Collections;
using Flame.TypeSystem;

namespace Flame.Compiler.Instructions.Fused
{
    /// <summary>
    /// An instruction prototype that stores a field. It is a
    /// fused instruction prototype that is equivalent to a
    /// get-field-pointer followed by a store.
    /// </summary>
    public sealed class StoreFieldPrototype : FusedInstructionPrototype
    {
        private StoreFieldPrototype(IField field)
        {
            this.Field = field;
        }

        /// <summary>
        /// Gets the field that is loaded.
        /// </summary>
        /// <value>The field that is loaded.</value>
        public IField Field { get; private set; }

        /// <inheritdoc/>
        public override int ParameterCount => 2;

        /// <inheritdoc/>
        public override InstructionPrototype Map(MemberMapping mapping)
        {
            return Create(mapping.MapField(Field));
        }

        /// <inheritdoc/>
        public override void Expand(InstructionBuilder instance)
        {
            var insn = instance.Instruction;
            AssertIsPrototypeOf(insn);

            var gfp = instance.InsertBefore(
                Instruction.CreateGetFieldPointer(
                    Field, insn.Arguments[0]));
            instance.Instruction = Instruction.CreateStore(
                Field.FieldType,
                gfp,
                insn.Arguments[1]);
        }

        private static readonly InterningCache<StoreFieldPrototype> instanceCache
            = new InterningCache<StoreFieldPrototype>(
                new MappedComparer<StoreFieldPrototype, IField>(proto => proto.Field));

        /// <summary>
        /// Gets or creates an instruction prototype for instructions
        /// that store a value in a particular field.
        /// </summary>
        /// <param name="field">
        /// The field that is to be updated.
        /// </param>
        /// <returns>
        /// A store-field instruction prototype.
        /// </returns>
        public static StoreFieldPrototype Create(IField field)
        {
            return instanceCache.Intern(new StoreFieldPrototype(field));
        }
    }
}
