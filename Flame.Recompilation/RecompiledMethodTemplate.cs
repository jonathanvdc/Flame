using Flame.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public class RecompiledMethodTemplate : RecompiledTypeMemberTemplate, IMethod
    {
        public RecompiledMethodTemplate(AssemblyRecompiler Recompiler, IMethod SourceMethod)
            : base(Recompiler)
        {
            this.SourceMethod = SourceMethod;
        }

        public static IMethod GetRecompilerTemplate(AssemblyRecompiler Recompiler, IMethod SourceMethod)
        {
            if (Recompiler.IsExternal(SourceMethod))
            {
                return SourceMethod;
            }
            else if (SourceMethod.get_IsGenericInstance())
            {
                return Recompiler.GetMethod(SourceMethod);
            }
            else
            {
                return new RecompiledMethodTemplate(Recompiler, SourceMethod);
            }
        }

        public IMethod SourceMethod { get; private set; }
        public override ITypeMember GetSourceTypeMember()
        {
            return SourceMethod;
        }

        public IParameter[] GetParameters()
        {
            return RecompiledParameterTemplate.GetParameterTemplates(Recompiler, SourceMethod.GetParameters(), SourceMethod);
        }

        public IType ReturnType
        {
            get { return RecompiledTypeTemplate.GetWeakRecompiledType(SourceMethod.ReturnType, Recompiler, SourceMethod); }
        }

        public IMethod[] GetBaseMethods()
        {
            return GetWeakRecompiledMethods(SourceMethod.GetBaseMethods(), Recompiler, SourceMethod);
        }

        public IMethod GetGenericDeclaration()
        {
            return this;
        }

        public IBoundObject Invoke(IBoundObject Caller, IEnumerable<IBoundObject> Arguments)
        {
            return SourceMethod.Invoke(Caller, Arguments);
        }

        public bool IsConstructor
        {
            get { return SourceMethod.IsConstructor; }
        }

        public IMethod MakeGenericMethod(IEnumerable<IType> TypeArguments)
        {
            return Recompiler.GetMethod(SourceMethod).MakeGenericMethod(TypeArguments);
        }

        public IEnumerable<IType> GetGenericArguments()
        {
            return Recompiler.GetTypes(SourceMethod.GetGenericArguments());
        }

        public IEnumerable<IGenericParameter> GetGenericParameters()
        {
            return RecompiledGenericParameterTemplate.GetRecompilerTemplates(Recompiler, this, SourceMethod.GetGenericParameters());
        }

        public override IEnumerable<IAttribute> GetAttributes()
        {
            return base.GetAttributes().Concat(Recompiler.Optimizer.InferAttributes(SourceMethod));
        }

        #region Static

        public static IMethod[] GetWeakRecompiledMethods(IMethod[] SourceMethods, AssemblyRecompiler Recompiler, IGenericMember DeclaringMember)
        {
            return SourceMethods.Select(item => GetWeakRecompiledMethod(item, Recompiler, DeclaringMember)).ToArray();
        }

        public static IMethod GetWeakRecompiledMethod(IMethod SourceMethod, AssemblyRecompiler Recompiler, IGenericMember DeclaringMember)
        {
            if (SourceMethod.get_IsGenericInstance())
            {
                var genDecl = Recompiler.GetMethod(SourceMethod.GetGenericDeclaration());
                var genArgs = SourceMethod.GetGenericArguments().Select(item => RecompiledTypeTemplate.GetWeakRecompiledType(item, Recompiler, DeclaringMember));
                return genDecl.MakeGenericMethod(genArgs.ToArray());
            }
            else if (SourceMethod.get_IsAnonymous())
            {
                var recompRetType = RecompiledTypeTemplate.GetWeakRecompiledType(SourceMethod.ReturnType, Recompiler, DeclaringMember);
                var recompParams = RecompiledParameterTemplate.GetParameterTemplates(Recompiler, SourceMethod.GetParameters(), DeclaringMember);
                var descMethod = new DescribedMethod(SourceMethod.Name, SourceMethod.DeclaringType, recompRetType, SourceMethod.IsStatic);
                foreach (var item in recompParams)
                {
                    descMethod.AddParameter(item);
                }
                foreach (var item in SourceMethod.GetAttributes().Select(Recompiler.GetAttribute))
                {
                    descMethod.AddAttribute(item);
                }
                return descMethod;
            }
            else
            {
                return Recompiler.GetMethod(SourceMethod);
            }
        }

        #endregion
    }
}
