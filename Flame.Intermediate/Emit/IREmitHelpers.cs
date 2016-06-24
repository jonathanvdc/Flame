using Flame.Build;
using Flame.Compiler;
using Flame.Intermediate.Parsing;
using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate.Emit
{
    public static class IREmitHelpers
    {
        public static IRSignature CreateSignature(IRAssemblyBuilder Assembly, UnqualifiedName Name, IEnumerable<IAttribute> Attributes)
        {
            return new IRSignature(Name, ConvertAttributes(Assembly, Attributes));
        }

        #region Attributes

        private static readonly Dictionary<AccessModifier, string> accessAttributeNames = new Dictionary<AccessModifier, string>()
        {
            { AccessModifier.Public, AttributeParsers.PublicNodeName },
            { AccessModifier.Private, AttributeParsers.PrivateNodeName },
            { AccessModifier.Protected, AttributeParsers.ProtectedNodeName },
            { AccessModifier.ProtectedOrAssembly, AttributeParsers.ProtectedOrInternal },
            { AccessModifier.ProtectedAndAssembly, AttributeParsers.ProtectedAndInternal },
            { AccessModifier.Assembly, AttributeParsers.InternalNodeName },
        };

        private static readonly Dictionary<IAttribute, string> constantAttributeNames = new Dictionary<IAttribute, string>()
        {
            // Miscellaneous attributes
            { PrimitiveAttributes.Instance.ConstantAttribute, AttributeParsers.ConstantNodeName },
            { PrimitiveAttributes.Instance.ExtensionAttribute, AttributeParsers.ExtensionNodeName },
            { PrimitiveAttributes.Instance.HiddenAttribute, AttributeParsers.HiddenNodeName },
            { PrimitiveAttributes.Instance.IndexerAttribute, AttributeParsers.IndexerNodeName },
            { PrimitiveAttributes.Instance.InAttribute, AttributeParsers.InNodeName },
            { PrimitiveAttributes.Instance.OutAttribute, AttributeParsers.OutNodeName },
            { PrimitiveAttributes.Instance.ImportAttribute, AttributeParsers.ImportNodeName },
            { PrimitiveAttributes.Instance.TotalInitializationAttribute, AttributeParsers.TotalInitializationNodeName },

            // Inheritance attributes
            { PrimitiveAttributes.Instance.AbstractAttribute, AttributeParsers.AbstractNodeName },
            { PrimitiveAttributes.Instance.VirtualAttribute, AttributeParsers.VirtualNodeName },

            // Type attributes
            { PrimitiveAttributes.Instance.StaticTypeAttribute, AttributeParsers.StaticTypeNodeName },
            { PrimitiveAttributes.Instance.ReferenceTypeAttribute, AttributeParsers.ReferenceTypeNodeName },
            { PrimitiveAttributes.Instance.ValueTypeAttribute, AttributeParsers.ValueTypeNodeName },
            { PrimitiveAttributes.Instance.InterfaceAttribute, AttributeParsers.InterfaceTypeNodeName },
            { PrimitiveAttributes.Instance.EnumAttribute, AttributeParsers.EnumTypeNodeName }
        };

        public static INodeStructure<IAttribute> ConvertAttribute(IRAssemblyBuilder Assembly, IAttribute Attribute)
        {
            if (Attribute is AccessAttribute)
            {
                var mod = ((AccessAttribute)Attribute).Access;
                return new ConstantNodeStructure<IAttribute>(NodeFactory.Id(accessAttributeNames[mod]), Attribute);
            }
            else if (Attribute is IConstructedAttribute)
            {
                // Format:
                //
                // #attribute(<attribute_constructor>, <argument_expressions...>)

                var ctedAttr = (IConstructedAttribute)Attribute;
                var attrCtor = Assembly.MethodTable.GetReference(ctedAttr.Constructor);
                var args = ConvertExpressions(Assembly, ctedAttr.GetArguments().Select(PrimitiveExpressionExtensions.ToExpression));
                return new LazyNodeStructure<IAttribute>(Attribute, attr => 
                    NodeFactory.Call(AttributeParsers.ConstructedAttributeNodeName, new[] { attrCtor }.Concat(args)));
            }
            else
            {
                string attrName;
                if (constantAttributeNames.TryGetValue(Attribute, out attrName))
                {
                    return new ConstantNodeStructure<IAttribute>(NodeFactory.Id(attrName), Attribute);
                }
                else if (Attribute is SingletonAttribute)
                {
                    var singletonAttr = (SingletonAttribute)Attribute;
                    return new ConstantNodeStructure<IAttribute>(
                        NodeFactory.Call(AttributeParsers.SingletonNodeName, new LNode[]
                        { 
                            NodeFactory.IdOrLiteral(singletonAttr.InstanceMemberName) 
                        }), singletonAttr);
                }
                else if (Attribute is AssociatedTypeAttribute)
                {
                    var type = ((AssociatedTypeAttribute)Attribute).AssociatedType;
                    return new LazyNodeStructure<IAttribute>(Attribute, () =>
                        NodeFactory.Call(AttributeParsers.AssociatedTypeNodeName, new LNode[]
                    { 
                        Assembly.TypeTable.GetReference(type)
                    }));
                }
                else if (Attribute is OperatorAttribute)
                {
                    var op = ((OperatorAttribute)Attribute).Operator;
                    return new LazyNodeStructure<IAttribute>(Attribute, () =>
                        NodeFactory.Call(AttributeParsers.OperatorNodeName, new LNode[]
                    { 
                        NodeFactory.IdOrLiteral(op.Name)
                    }));
                }
                else
                {
                    return null;
                }
            }
        }

        public static IEnumerable<INodeStructure<IAttribute>> ConvertAttributes(IRAssemblyBuilder Assembly, IEnumerable<IAttribute> Attributes)
        {
            return Attributes.Select(item => ConvertAttribute(Assembly, item))
                             .Where(item => item != null);
        }

        #endregion

        #region Generics and constraints

        private static readonly Dictionary<IGenericConstraint, INodeStructure<IGenericConstraint>> constantConstraints = new Dictionary<IGenericConstraint, INodeStructure<IGenericConstraint>>()
        {
            { ReferenceTypeConstraint.Instance, 
              new ConstantNodeStructure<IGenericConstraint>(NodeFactory.Id(AttributeParsers.ReferenceTypeNodeName), ReferenceTypeConstraint.Instance) },
        
            { ValueTypeConstraint.Instance, 
              new ConstantNodeStructure<IGenericConstraint>(NodeFactory.Id(AttributeParsers.ValueTypeNodeName), ValueTypeConstraint.Instance) },
        
            { EnumConstraint.Instance,
              new ConstantNodeStructure<IGenericConstraint>(NodeFactory.Id(AttributeParsers.EnumTypeNodeName), EnumConstraint.Instance) }
        };

        public static IEnumerable<INodeStructure<IGenericConstraint>> ConvertConstraint(IRAssemblyBuilder Assembly, IGenericConstraint Constraint)
        {
            if (Constraint is AndConstraint)
            {
                return ((AndConstraint)Constraint).Constraints.SelectMany(item => ConvertConstraint(Assembly, item));
            }
            else if (Constraint is TypeConstraint)
            {
                var result = new LazyNodeStructure<IGenericConstraint>(Constraint, item => 
                    NodeFactory.Call(IRParser.TypeConstraintName, new LNode[] { Assembly.TypeTable.GetReference(((TypeConstraint)item).Type) }));
                return new INodeStructure<IGenericConstraint>[] { result };
            }
            else
            {
                INodeStructure<IGenericConstraint> constConstraint;
                if (constantConstraints.TryGetValue(Constraint, out constConstraint))
                {
                    return new INodeStructure<IGenericConstraint>[] { constConstraint };
                }
                else
                {
                    return Enumerable.Empty<INodeStructure<IGenericConstraint>>();
                }
            }
        }

        public static INodeStructure<IGenericParameter> ConvertGenericParameter(IRAssemblyBuilder Assembly, IGenericMember DeclaringMember, IGenericParameter GenericParameter)
        {
            var genParam = new IRGenericParameter(DeclaringMember, CreateSignature(Assembly, GenericParameter.Name, GenericParameter.Attributes));

            genParam.GenericParameterNodes = ConvertGenericParameters(Assembly, DeclaringMember, GenericParameter.GenericParameters);
            genParam.ConstraintNodes = new NodeList<IGenericConstraint>(ConvertConstraint(Assembly, GenericParameter.Constraint).ToArray());

            return genParam;
        }

        public static INodeStructure<IEnumerable<IGenericParameter>> ConvertGenericParameters(IRAssemblyBuilder Assembly, IGenericMember DeclaringMember, IEnumerable<IGenericParameter> GenericParameters)
        {
            return new NodeList<IGenericParameter>(GenericParameters.Select(item => ConvertGenericParameter(Assembly, DeclaringMember, item)).ToArray());
        }

        #endregion

        #region Parameters

        public static INodeStructure<IParameter> ConvertParameter(IRAssemblyBuilder Assembly, Func<IType, LNode> GetReference, IParameter Parameter)
        {
            return new IRParameter(
                CreateSignature(Assembly, Parameter.Name, Parameter.Attributes),
                new LazyNodeStructure<IType>(Parameter.ParameterType, GetReference));
        }

        public static INodeStructure<IParameter> ConvertParameter(IRAssemblyBuilder Assembly, IParameter Parameter)
        {
            return ConvertParameter(Assembly, Assembly.TypeTable.GetReference, Parameter);
        }

        public static INodeStructure<IEnumerable<IParameter>> ConvertParameters(IRAssemblyBuilder Assembly, Func<IType, LNode> GetReference, IEnumerable<IParameter> Parameter)
        {
            return new NodeList<IParameter>(
                Parameter.Select(item => ConvertParameter(Assembly, GetReference, item)).ToArray());
        }

        public static INodeStructure<IEnumerable<IParameter>> ConvertParameters(IRAssemblyBuilder Assembly, IEnumerable<IParameter> Parameter)
        {
            return new NodeList<IParameter>(
                Parameter.Select(item => ConvertParameter(Assembly, item)).ToArray());
        }

        #endregion

        #region Expressions

        public static LNode ConvertExpression(IRAssemblyBuilder Assembly, IExpression Expression)
        {
            return ConvertExpression(Assembly, Expression, (IType)null);
        }

        public static IEnumerable<LNode> ConvertExpressions(IRAssemblyBuilder Assembly, IEnumerable<IExpression> Expressions)
        {
            return ConvertExpressions(Assembly, Expressions, (IType)null);
        }

        public static LNode ConvertExpression(IRAssemblyBuilder Assembly, IExpression Expression, IType DeclaringType)
        {
            var descMethod = new DescribedMethod("", DeclaringType, PrimitiveTypes.Void, true);

            return ConvertExpression(Assembly, Expression, descMethod);
        }

        public static IEnumerable<LNode> ConvertExpressions(IRAssemblyBuilder Assembly, IEnumerable<IExpression> Expressions, IType DeclaringType)
        {
            var descMethod = new DescribedMethod("", DeclaringType, PrimitiveTypes.Void, true);

            return Expressions.Select(item => ConvertExpression(Assembly, item, descMethod));
        }

        public static LNode ConvertExpression(IRAssemblyBuilder Assembly, IExpression Expression, IMethod DeclaringMethod)
        {
            var codeGen = new IRCodeGenerator(Assembly, DeclaringMethod);

            return NodeBlock.ToNode(codeGen.Postprocess(Expression.Emit(codeGen)));
        }

        #endregion
    }
}
