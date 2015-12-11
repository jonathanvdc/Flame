using Flame.Build;
using Flame.Compiler;
using Flame.Compiler.Build;
using Flame.Compiler.Expressions;
using Flame.Compiler.Statements;
using Flame.Compiler.Variables;
using Flame.Compiler.Visitors;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Passes
{
    /// <summary>
    /// Defines a root pass that generates static methods
    /// that forward calls to static singleton methods.
    /// The purpose of this pass is to make interop
    /// with other languages easier, as they may not
    /// have built-in support for static singleton classes.
    /// </summary>
    public sealed class GenerateStaticPass : IPass<BodyPassArgument, IEnumerable<IMember>>
    {
        private GenerateStaticPass() { }

        /// <summary>
        /// Stores this pass' one and only instance.
        /// </summary>
        public static readonly GenerateStaticPass Instance = new GenerateStaticPass();

        /// <summary>
        /// Defines a string constant that represents this pass' name.
        /// </summary>
        public const string GenerateStaticPassName = "generate-static";

        /// <summary>
        /// Defines a key that is used to cache the owning associated
        /// singleton for types.
        /// </summary>
        private const string AssociatedSingletonOwnerKey = GenerateStaticPassName + "-associated-singleton-owner";

        /// <summary>
        /// Defines a key that is used to store forwarding properties
        /// in the pass metadata.
        /// </summary>
        private const string ForwardingPropertiesKey = GenerateStaticPassName + "-forwarding-properties";

        public IEnumerable<IMember> Apply(BodyPassArgument Value)
        {
            var declMethod = Value.DeclaringMethod;

            // Don't even consider static, non-public or generic methods.
            if (declMethod.IsStatic ||
                declMethod.GetAccess() != AccessModifier.Public ||
                declMethod.GetIsGeneric())
                return Enumerable.Empty<IMember>();

            var singletonTy = Value.DeclaringType;
            var ownerTy = GetAssociatedSingletonOwner(Value.Metadata, singletonTy);

            // Only proceed if the declaring type actually
            // is an associated singleton type.
            if (ownerTy == null)
                return Enumerable.Empty<IMember>();

            var getSingletonExpr = new SingletonVariable(singletonTy).CreateGetExpression();

            if (declMethod is IAccessor)
            {
                return new IMember[] { CreateForwardingAccessor(Value.Metadata, getSingletonExpr, ownerTy, (IAccessor)declMethod) };
            }
            else
            {
                return new IMember[] { CreateForwardingMethod(getSingletonExpr, ownerTy, declMethod) };
            }
        }

        /// <summary>
        /// Forwards a call from the given calling
        /// method to the given target method.
        /// </summary>
        /// <param name="ThisExpression"></param>
        /// <param name="CallingMethod"></param>
        /// <param name="TargetMethod"></param>
        /// <returns></returns>
        public static IExpression CreateForwardingCall(IExpression ThisExpression, IMethod CallingMethod, IMethod TargetMethod)
        {
            var parameters = CallingMethod.GetParameters();
            var args = new IExpression[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                args[i] = new ArgumentVariable(parameters[i], i).CreateGetExpression();
            }
            return new InvocationExpression(TargetMethod, ThisExpression, args);
        }

        /// <summary>
        /// Creates a static forwarding method from the given owner
        /// type and associated singleton method.
        /// </summary>
        /// <param name="GetSingletonExpression"></param>
        /// <param name="OwnerType"></param>
        /// <param name="Method"></param>
        /// <returns></returns>
        private static IMethod CreateForwardingMethod(IExpression GetSingletonExpression, IType OwnerType, IMethod Method)
        {
            var descMethod = new DescribedBodyMethod(Method.Name, OwnerType, Method.ReturnType, true);
            foreach (var attr in Method.Attributes)
            {
                descMethod.AddAttribute(attr);
            }
            foreach (var param in Method.GetParameters())
            {
                descMethod.AddParameter(param);
            }

            descMethod.Body = new ReturnStatement(CreateForwardingCall(GetSingletonExpression, descMethod, Method));

            return descMethod;
        }

        /// <summary>
        /// Creates a static forwarding accessor from the given owner type
        /// and associated singleton accessor.
        /// </summary>
        /// <param name="GetSingletonExpression"></param>
        /// <param name="OwnerType"></param>
        /// <param name="Accessor"></param>
        /// <returns></returns>
        private static IAccessor CreateForwardingAccessor(PassMetadata Metadata, IExpression GetSingletonExpression, IType OwnerType, IAccessor Accessor)
        {
            var ownerProp = GetForwardingProperty(Metadata, OwnerType, Accessor.DeclaringProperty);

            var descMethod = new DescribedBodyAccessor(Accessor.AccessorType, ownerProp, Accessor.ReturnType);
            descMethod.IsStatic = true;
            foreach (var attr in Accessor.Attributes)
            {
                descMethod.AddAttribute(attr);
            }
            foreach (var param in Accessor.GetParameters())
            {
                descMethod.AddParameter(param);
            }

            descMethod.Body = new ReturnStatement(CreateForwardingCall(GetSingletonExpression, descMethod, Accessor));

            return descMethod;
        }

        /// <summary>
        /// Gets the owner of this associated singleton type.
        /// </summary>
        /// <param name="Metadata"></param>
        /// <param name="AssociatedSingleton"></param>
        /// <returns></returns>
        private static IType GetAssociatedSingletonOwner(PassMetadata Metadata, IType AssociatedSingleton)
        {
            var tyMetadata = Metadata.TypeMetadata;
            // Be careful when it comes to
            // thread-safety.
            lock (tyMetadata)
            {
                if (!tyMetadata.HasOption(ForwardingPropertiesKey))
                {
                    // Seems like this is the first time we're here for this
                    // type. Let's find out what this type's associated singleton
                    // owner is.
                    tyMetadata.SetOption(ForwardingPropertiesKey, AssociatedSingleton.GetAssociatedSingletonOwner());
                }

                return tyMetadata.GetOption<IType>(
                    ForwardingPropertiesKey, null);
            }
        }

        /// <summary>
        /// Gets a dictionary that maps static singleton properties
        /// to forwarding properties.
        /// </summary>
        /// <param name="Metadata"></param>
        /// <returns></returns>
        private static ConcurrentDictionary<IProperty, IProperty> GetForwardingPropertyDictionary(PassMetadata Metadata)
        {
            var tyMetadata = Metadata.TypeMetadata;
            // Be careful when it comes to
            // thread-safety.
            lock (tyMetadata)
            {
                var newDict = new ConcurrentDictionary<IProperty, IProperty>();
                if (!tyMetadata.HasOption(ForwardingPropertiesKey))
                {
                    // If no such dictionary exists right now, then
                    // create a new one.
                    tyMetadata.SetOption(ForwardingPropertiesKey, newDict);
                }

                return tyMetadata.GetOption<ConcurrentDictionary<IProperty, IProperty>>(
                    ForwardingPropertiesKey, newDict);
            }
        }

        /// <summary>
        /// Gets the static forwarding property for the given owner type
        /// and associated singleton property.
        /// </summary>
        private static IProperty GetForwardingProperty(PassMetadata Metadata, IType OwnerType, IProperty Property)
        {
            var dict = GetForwardingPropertyDictionary(Metadata);

            return dict.GetOrAdd(Property, item => CreateForwardingProperty(OwnerType, item));
        }

        /// <summary>
        /// Creates a static forwarding property from the given owner type
        /// and associated singleton property.
        /// </summary>
        /// <param name="GetSingletonExpression"></param>
        /// <param name="OwnerType"></param>
        /// <param name="Property"></param>
        /// <returns></returns>
        private static IProperty CreateForwardingProperty(IType OwnerType, IProperty Property)
        {
            var descProp = new DescribedProperty(Property.Name, OwnerType, Property.PropertyType, true);
            foreach (var attr in Property.Attributes)
            {
                descProp.AddAttribute(attr);
            }
            foreach (var param in Property.IndexerParameters)
            {
                descProp.AddIndexerParameter(param);
            }

            return descProp;
        }
    }
}
