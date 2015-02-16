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
            return Recompiler.GetMethods(SourceMethod.GetBaseMethods());
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
    }
}
