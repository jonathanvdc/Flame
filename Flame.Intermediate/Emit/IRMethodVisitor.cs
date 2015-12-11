using Flame.Intermediate.Parsing;
using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate.Emit
{
    public class IRMethodVisitor : IConverter<IMethod, LNode>
    {
        public IRMethodVisitor(IRAssemblyBuilder Assembly)
        {
            this.Assembly = Assembly;
        }

        public IRAssemblyBuilder Assembly { get; private set; }

        private LNode ConvertDefinitionReference(IMethod Value)
        {
            // Format:
            //
            // #method_reference(declaring_type, name, is_static, { generic_parameters_names... }, return_type, { parameter_types... })
            //
            // --OR--
            //
            // #ctor_reference(...)

            var declType = Assembly.TypeTable.GetReference(Value.DeclaringType);
            var genParamNames = NodeFactory.Block(Value.GenericParameters.Select(item => NodeFactory.Literal(item.Name)));
            var visitor = new IRGenericMemberTypeVisitor(Assembly, Value);
            var retType = visitor.Convert(Value.ReturnType);
            var paramTypes = NodeFactory.Block(Value.Parameters.GetTypes().Select(visitor.GetTypeReference));

            return NodeFactory.Call(Value.IsConstructor ? IRParser.ConstructorReferenceName : IRParser.MethodReferenceName, new LNode[]
            {
                declType,
                NodeFactory.IdOrLiteral(Value.Name),
                NodeFactory.Literal(Value.IsStatic),
                genParamNames,
                retType,
                paramTypes
            });
        }

        private LNode ConvertAccessorDefinitionReference(IAccessor Value)
        {
            // Format:
            //
            // #accessor_reference(declaring_type, property_name, property_is_static, property_type, { indexer_parameter_types... }, accessor_type)

            var declType = Assembly.TypeTable.GetReference(Value.DeclaringType);
            var propType = Assembly.TypeTable.GetReference(Value.DeclaringProperty.PropertyType);
            var paramTypes = NodeFactory.Block(Value.DeclaringProperty.IndexerParameters.GetTypes().Select(Assembly.TypeTable.GetReference));
            var accType = NodeFactory.IdOrLiteral(Value.AccessorType.Name);

            return NodeFactory.Call(IRParser.AccessorReferenceName, new LNode[]
            {
                declType,
                NodeFactory.IdOrLiteral(Value.DeclaringProperty.Name),
                NodeFactory.Literal(Value.DeclaringProperty.IsStatic),
                propType,
                paramTypes,
                accType
            });
        }

        public LNode Convert(IMethod Value)
        {
            if (!Value.DeclaringType.GetIsContainerType())
            {
                if (Value is GenericMethod)
                {
                    var genInst = (GenericMethod)Value;
                    return NodeFactory.Call(IRParser.GenericInstanceName, new LNode[]
                    { 
                        Assembly.MethodTable.GetReference(genInst.Declaration)
                    }.Concat(genInst.GenericArguments.Select(Assembly.TypeTable.GetReference)));
                }
                else if (Value is GenericInstanceMethod)
                {
                    return NodeFactory.Call(IRParser.GenericInstanceMemberName, new LNode[]
                    { 
                        Assembly.TypeTable.GetReference(Value.DeclaringType), 
                        Assembly.MethodTable.GetReference(((GenericInstanceMethod)Value).Declaration)
                    });
                }
            }
            if (Value is IAccessor)
            {
                return ConvertAccessorDefinitionReference((IAccessor)Value);
            }
            else
            {
                return ConvertDefinitionReference(Value);
            }
        }
    }
}
