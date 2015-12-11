namespace Flame.Functional

open Flame
open Flame.Compiler
open Flame.Compiler.Expressions
open System
open System.Linq

/// Defines a set of default conversion rules.
type DefaultConversionRules(nameType : IType -> string) =

    static member Create (nameType : Func<IType, string>) =
        new DefaultConversionRules(nameType.Invoke)

    interface IConversionRules with
        /// Finds out whether a value of the given source type
        /// can be converted implicitly to the given target type.
        member this.HasImplicitConversion sourceType targetType =
            if sourceType.Is(targetType) then
                true

            else if sourceType.GetIsVector() && targetType.GetIsArray() then
                sourceType.AsContainerType().ElementType.Is(
                    targetType.AsContainerType().ElementType) &&
                sourceType.AsContainerType().AsVectorType().Dimensions.Count =
                    targetType.AsContainerType().AsArrayType().ArrayRank

            else if sourceType.GetIsPointer() && targetType.GetIsPointer() then
                let firstPtrType = sourceType.AsContainerType().AsPointerType()
                let secondPtrType = targetType.AsContainerType().AsPointerType()
                firstPtrType.PointerKind.Equals(PointerKind.ReferencePointer) &&
                    secondPtrType.PointerKind.Equals(PointerKind.TransientPointer)

            else if sourceType.GetIsPrimitive() && targetType.GetIsPrimitive() then

                if sourceType.GetPrimitiveMagnitude() <= targetType.GetPrimitiveMagnitude() then
                    if (sourceType.GetIsSignedInteger()) then
                        targetType.GetIsSignedInteger() || targetType.GetIsFloatingPoint();
                    else if (sourceType.GetIsUnsignedInteger()) then
                        if (targetType.GetIsUnsignedInteger() || targetType.GetIsFloatingPoint()) then
                            true
                        else if (targetType.GetIsSignedInteger()) then
                            sourceType.GetPrimitiveMagnitude() < targetType.GetPrimitiveMagnitude()
                        else
                            false
                    else
                        sourceType.GetIsBit() && targetType.GetIsBit() ||
                            sourceType.GetIsFloatingPoint() && targetType.GetIsFloatingPoint()
                else
                    false

            else if (sourceType.GetIsArray() || sourceType.GetIsVector()) && targetType.GetGenericDeclaration().GetIsEnumerableType() then
                if not(targetType.GetIsGeneric()) then
                    true
                else sourceType.AsContainerType().ElementType.Is(Enumerable.First<IType>(targetType.GetGenericArguments()));
            else false

        /// Converts the given expression to the given type implicitly.
        member this.ConvertImplicit value targetType =
            let valType = value.Type
            if (this :> IConversionRules).HasImplicitConversion valType targetType then
                ConversionExpression.Instance.Create(value, targetType)
            else
                let message = new LogEntry("Missing implicit conversion",
                                           "An expression of type '" + (nameType valType) + "' could not be converted implicitly to an expression of type '" + (nameType targetType) + "'.")
                ConversionExpression.Instance.Create(value, targetType) |> ExpressionBuilder.Error message

        /// Converts the given expression to the given type explicitly.
        member this.ConvertExplicit value targetType =
            ConversionExpression.Instance.Create(value, targetType)
