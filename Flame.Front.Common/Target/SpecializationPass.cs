using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Flame.Compiler;
using Flame.Compiler.Expressions;
using Flame.Compiler.Statements;
using Flame.Compiler.Variables;
using Flame.Compiler.Visitors;
using Flame.Optimization;

namespace Flame.Front.Target
{
    /// <summary>
    /// A partial function specialization pass implementation.
    /// </summary>
    public class SpecializationPass : SpecializationPassBase
    {
        private SpecializationPass()
        { }

        /// <summary>
        /// Gets this pass' only instance.
        /// </summary>
        public static readonly SpecializationPass Instance = new SpecializationPass();

        private const double SingletonScore = 2.0;
        private const double FullImplementationScore = 10.0;

        public override Func<DissectedCall, bool> GetSpecializationCriteria(BodyPassArgument Argument)
        {
            double minPoints = Argument.PassEnvironment.Log.Options.GetOption<double>("specialize-threshold", FullImplementationScore);
            return call => ShouldSpecialize(call, minPoints, Argument.Metadata.GlobalMetadata);
        }

        private static bool ShouldSpecialize(DissectedCall Call, double Threshold, IRandomAccessOptions GlobalMetadata)
        {
            // This is a heuristic for partial function specialization.
            // At this point, we're facing the following trade-off:
            // on one hand, we would like to perform partial function 
            // specialization, because it gets rid of abstraction, 
            // and allows us to optimize more aggressively in the 
            // specialized version, but on the other hand it increases
            // code size, which may be bad for JIT compile times and
            // the processor cache.
            // Just placing a hard limit on the size of the method body
            // (like we did with inlining) doesn't work, though. After all,
            // specialization is recursive: this optimization will also
            // be applied to specialized methods, which will in turn
            // result in more specialization. Ideally, we'd base this
            // heuristic on the percentage of specialized operations
            // vs unspecialized operations. Unfortunately, estimating
            // run-time performance at compile-time is hard.
            // We'll do the following instead:
            //   * Don't specialize constructors, because constructors
            //     aren't supposed to do any work (and typically don't)
            //   * Invent a specialization threshold. Once this is exceeded,
            //     specialization occurs. Derived parameter types result
            //     in points toward this specialization threshold, depending
            //     on their number of overrides and base types.

            if (Call.Method.IsConstructor)
                return false;

            var specCall = new SpecializedCall(Call);
            double result = 0;
            foreach (var item in specCall.ArgumentTypes.Zip(specCall.Method.Parameters, Tuple.Create))
            {
                result += RateMatch(item.Item2.ParameterType, item.Item1, GlobalMetadata);
                if (result >= Threshold)
                    return true;
            }

            return false;
        }

        private static ConcurrentDictionary<TKey, TValue> GetCache<TKey, TValue>(string Key, IRandomAccessOptions GlobalMetadata)
        {
            if (GlobalMetadata.HasOption(Key))
            {
                return GlobalMetadata.GetOption<ConcurrentDictionary<TKey, TValue>>(Key, null);
            }
            else
            {
                var cache = new ConcurrentDictionary<TKey, TValue>();
                GlobalMetadata.SetOption<ConcurrentDictionary<TKey, TValue>>(Key, cache);
                return cache;
            }
        }

        private static int GetImplementedMethodCount(IType ImplementationType, IType BaseType, IRandomAccessOptions GlobalMetadata)
        {
            var cache = GetCache<Tuple<IType, IType>, int>("implemented-method-count-cache", GlobalMetadata);
            var pair = Tuple.Create(ImplementationType, BaseType);
            return cache.GetOrAdd(pair, item => GetImplementedMethodCountImpl(
                ImplementationType, 
                GetVirtualMethods(BaseType, GlobalMetadata)));
        }

        private static int GetImplementedMethodCountImpl(IType ImplementationType, IReadOnlyList<IMethod> VirtualMethods)
        {
            int result = 0;
            foreach (var item in VirtualMethods)
            {
                if (item.GetImplementation(ImplementationType) != null)
                    result++;
            }
            return result;
        }

        private static List<IMethod> GetVirtualMethods(IType BaseType, IRandomAccessOptions GlobalMetadata)
        {
            var cache = GetCache<IType, List<IMethod>>("virtual-method-cache", GlobalMetadata);
            return cache.GetOrAdd(BaseType, GetVirtualMethodsImpl);
        }

        private static List<IMethod> GetVirtualMethodsImpl(IType BaseType)
        {
            // Ignore non-virtual base types.
            if (!BaseType.GetIsVirtual() && !BaseType.GetIsAbstract() && !BaseType.GetIsInterface())
                return new List<IMethod>();

            var results = new List<IMethod>();
            results.AddRange(
                BaseType.Methods.Concat(
                    BaseType.Properties.SelectMany(prop => prop.Accessors))
                .Where(MemberExtensions.GetIsVirtual));

            var baseMethods = 
                BaseType.BaseTypes.SelectMany(item => item.GetAllMethods()).Concat(
                    BaseType.BaseTypes.SelectMany(item => item.GetAllProperties().SelectMany(prop => prop.Accessors)));
            
            foreach (var item in baseMethods)
            {
                if (item.GetIsVirtual() && item.GetImplementation(BaseType) == null)
                {
                    results.Add(item);
                }
            }

            return results;
        }

        private static double RateMatch(IType ParameterType, IType ArgumentType, IRandomAccessOptions GlobalMetadata)
        {
            if (ParameterType.Equals(ArgumentType))
                return 0;

            double result = 0;

            if (ArgumentType.GetIsSingleton())
                // Singletons get extra points, because they need not
                // be passed as arguments: the specialization can retrieve them
                // itself.
                result += SingletonScore;

            int baseMethodCount = GetVirtualMethods(ParameterType, GlobalMetadata).Count;
            int implCount = GetImplementedMethodCount(ArgumentType, ParameterType, GlobalMetadata);
            double implPercentage = (double)implCount / (double)baseMethodCount;

            // Penalize low implementation percentages. These correspond to virtual
            // classes with a comparatively small number of virtual methods.
            // Specializing for them is probably not worth it.
            result += implPercentage * implPercentage * FullImplementationScore;

            return result;
        }
    }
}

