namespace Flame.Functional

open Flame
open Flame.Compiler
open Flame.Compiler.Expressions
open System.Linq

/// Defines a set of default conversion rules.
type DefaultConversionRules private() = 
    static let instance = 
        DefaultConversionRules()
    static member Instance = 
        instance

    interface IConversionRules with
        /// Finds out whether a value of the given source type
        /// can be converted implicitly to the given target type.
        member this.HasImplicitConversion sourceType targetType =
            if sourceType.Is(targetType) then
                true

            else if sourceType.get_IsVector() && targetType.get_IsArray() then
                sourceType.AsContainerType().GetElementType().Is(
                    targetType.AsContainerType().GetElementType()) &&
                sourceType.AsContainerType().AsVectorType().GetDimensions().Length =
                    targetType.AsContainerType().AsArrayType().ArrayRank

            else if sourceType.get_IsPointer() && targetType.get_IsPointer() then
                let firstPtrType = sourceType.AsContainerType().AsPointerType()
                let secondPtrType = targetType.AsContainerType().AsPointerType()
                firstPtrType.PointerKind.Equals(PointerKind.ReferencePointer) &&
                    secondPtrType.PointerKind.Equals(PointerKind.TransientPointer)

            else if sourceType.get_IsPrimitive() && targetType.get_IsPrimitive() then

                if sourceType.GetPrimitiveMagnitude() <= targetType.GetPrimitiveMagnitude() then
                    if (sourceType.get_IsSignedInteger()) then
                        targetType.get_IsSignedInteger() || targetType.get_IsFloatingPoint();
                    else if (sourceType.get_IsUnsignedInteger()) then
                        if (targetType.get_IsUnsignedInteger() || targetType.get_IsFloatingPoint()) then
                            true
                        else if (targetType.get_IsSignedInteger()) then
                            sourceType.GetPrimitiveMagnitude() < targetType.GetPrimitiveMagnitude()
                        else
                            false
                    else
                        sourceType.get_IsBit() && targetType.get_IsBit() || 
                            sourceType.get_IsFloatingPoint() && targetType.get_IsFloatingPoint()
                else
                    false

            else if (sourceType.get_IsArray() || sourceType.get_IsVector()) && targetType.GetGenericDeclaration().get_IsEnumerableType() then
                if not(targetType.get_IsGeneric()) then
                    true
                else sourceType.AsContainerType().GetElementType().Is(Enumerable.First<IType>(targetType.GetGenericArguments()));
            else false

        /// Converts the given expression to the given type implicitly.
        member this.ConvertImplicit value targetType =
            let valType = value.Type
            if not(ConversionExpression.RequiresConversion(valType, targetType)) then
                value
            else if (this :> IConversionRules).HasImplicitConversion valType targetType then
                ConversionExpression.Create(value, targetType)
            else
                let message = new LogEntry("Missing implicit conversion",
                                           "An expression of type '" + valType.FullName + "' could not be converted implicitly to an expression of type '" + targetType.FullName + "'")
                ConversionExpression.Create(value, targetType) |> ExpressionBuilder.Error message

        /// Converts the given expression to the given type explicitly.
        member this.ConvertExplicit value targetType =
            let valType = value.Type
            if not(ConversionExpression.RequiresConversion(valType, targetType)) then
                value
            else
                ConversionExpression.Create(value, targetType)
