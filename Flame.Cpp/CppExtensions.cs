using Flame.Compiler;
using Flame.Cpp.Emit;
using Flame.RT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public static class CppExtensions
    {
        #region GetHeaderAttributes

        public static IEnumerable<HeaderAttribute> GetHeaderAttributes(this IMember Member)
        {
            foreach (var item in Member.GetAttributes())
            {
                if (item != null && item.AttributeType.FullName == typeof(HeaderAttribute).FullName)
                {
                    var eval = item.Value.GetObjectValue();
                    if (eval is HeaderAttribute)
                    {
                        yield return (HeaderAttribute)eval;
                    }
                    else if (item is IConstructedAttribute)
                    {
                        yield return new HeaderAttribute(((IConstructedAttribute)item).GetArguments().First().GetValue<string>());
                    }
                }
            }
        }

        #endregion

        #region IsGlobalType

        public static bool IsGlobalType(this IType Member)
        {
            foreach (var item in Member.GetAttributes())
            {
                if (item != null && item.AttributeType.FullName == typeof(GlobalTypeAttribute).FullName)
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Enumerable Extensions

        public static IEnumerable<T> With<T>(this IEnumerable<T> Enumerable, T Value)
        {
            foreach (var item in Enumerable)
            {
                yield return item;
            }
            yield return Value;
        }

        #endregion

        #region Dependencies

        public static IEnumerable<IHeaderDependency> GetDependencies(this IMember Member, params IMember[] Exclude)
        {
            return GetDependencies(Member, (IEnumerable<IMember>)Exclude);
        }
        public static IEnumerable<IHeaderDependency> GetDependencies(this IMember Member, IEnumerable<IMember> Exclude)
        {
            if (Exclude.Contains(Member))
            {
                return new IHeaderDependency[0];
            }
            if (Member is IType)
            {
                return GetDependencies((IType)Member, Exclude);
            }
            else if (Member is ICppMember)
            {
                return ((ICppMember)Member).Dependencies;
            }
            else if (Member is IParameter)
            {
                return ((IParameter)Member).ParameterType.GetDependencies();
            }
            return new IHeaderDependency[0];
        }

        public static IEnumerable<IHeaderDependency> GetDependencies(this IType Type, IEnumerable<IMember> Exclude)
        {
            if (Exclude.Contains(Type) || Type.get_IsGenericParameter())
            {
                return new IHeaderDependency[0];
            }
            else if (Type.get_IsPointer())
            {
                var depends = Type.AsContainerType().GetElementType().GetDependencies();
                if (Type.AsContainerType().AsPointerType().PointerKind.Equals(PointerKind.ReferencePointer))
                {
                    return depends.MergeDependencies(new IHeaderDependency[] { StandardDependency.Memory });
                }
                else
                {
                    return depends;
                }
            }
            else if (Type.get_IsGenericInstance())
            {
                return Type.GetGenericDeclaration().GetDependencies(Exclude).MergeDependencies(Type.GetGenericArguments().SelectMany((item) => item.GetDependencies(Exclude)));
            }
            else if (Type is ICppMember)
            {
                //return new IHeaderDependency[] { new TypeHeaderDependency(Type) };
                return new IHeaderDependency[] { new CppFile((ICppMember)Type) };
            }
            else if (Type.Equals(PrimitiveTypes.String))
            {
                return new IHeaderDependency[] { StandardDependency.String };
            }
            else
            {
                return Type.GetHeaderAttributes().Select((item) => new StandardDependency(item.HeaderName));
            }
        }

        public static IEnumerable<IHeaderDependency> GetDependencies(this IEnumerable<IMember> Members, params IMember[] Exclude)
        {
            return GetDependencies(Members, (IEnumerable<IMember>)Exclude);
        }

        public static IEnumerable<IHeaderDependency> GetDependencies(this IEnumerable<IMember> Members, IEnumerable<IMember> Exclude)
        {
            var depends = Members.Select((item) => item.GetDependencies(Exclude));
            if (!depends.Any())
            {
                return new IHeaderDependency[0];
            }
            else
            {
                return depends.Aggregate((first, second) => first.MergeDependencies(second));
            }
        }

        public static IEnumerable<IHeaderDependency> MergeDependencies(this IEnumerable<IHeaderDependency> Dependencies, IEnumerable<IHeaderDependency> Others)
        {
            List<IHeaderDependency> results = new List<IHeaderDependency>();
            foreach (var item in Dependencies)
            {
                results.Add(item);
            }
            foreach (var item in Others)
            {
                bool success = true;
                foreach (var pre in Dependencies)
                {
                    if (pre.HeaderName == item.HeaderName && pre.IsStandard == item.IsStandard)
                    {
                        success = false;
                        break;
                    }
                }
                if (success)
                {
                    results.Add(item);
                }
            }
            return results;
        }

        public static IEnumerable<IHeaderDependency> SortDependencies(this IEnumerable<IHeaderDependency> Dependencies)
        {
            return Dependencies.Where((item) => item.IsStandard).Concat(Dependencies.Where((item) => !item.IsStandard));
        }

        public static IEnumerable<IHeaderDependency> ExcludeDependencies(this IEnumerable<IHeaderDependency> Dependencies, IEnumerable<IHeaderDependency> Exclude)
        {
            foreach (var item in Dependencies)
            {
                bool success = true;
                foreach (var excl in Exclude)
                {
                    if (excl.HeaderName == item.HeaderName && excl.IsStandard == item.IsStandard)
                    {
                        success = false;
                        break;
                    }
                }
                if (success)
                {
                    yield return item;
                }
            }
        }

        #endregion

        #region Scoping Blocks

        public static ICppBlock BlockFromName(ICodeGenerator CodeGenerator, string Name)
        {
            return BlockFromSplitName(CodeGenerator, CppNameExtensions.SplitScope(Name));
        }

        public static ICppBlock BlockFromSplitName(ICodeGenerator CodeGenerator, string[] Name)
        {
            ICppBlock block = new LiteralBlock(CodeGenerator, Name[0], PrimitiveTypes.Void);
            for (int i = 1; i < Name.Length; i++)
            {
                block = new ScopeOperatorBlock(block, new LiteralBlock(CodeGenerator, Name[i], PrimitiveTypes.Void));
            }
            return block;
        }

        public static ICppBlock CreateBlock(this IType Type, ICodeGenerator CodeGenerator)
        {
            return new ImplicitDependencyBlock(BlockFromName(CodeGenerator, CodeGenerator.GetTypeNamer().Name(Type, CodeGenerator)), Type.GetDependencies());
        }

        public static ICppBlock CreateConstructorBlock(this IMethod Member, ICodeGenerator CodeGenerator)
        {
            return new RetypedBlock(CodeGenerator.ConvertValueType(Member.DeclaringType).CreateBlock(CodeGenerator), MethodType.Create(Member));
        }

        private static ICppBlock CreateGenericFreeBlock(IMethod Member, ICodeGenerator CodeGenerator)
        {
            var declType = Member.DeclaringType;
            if (declType == null)
            {
                return new RetypedBlock(BlockFromName(CodeGenerator, Member.GetGenericFreeName()), MethodType.Create(Member));
            }
            else
            {
                return new ScopeOperatorBlock(declType.CreateBlock(CodeGenerator), new LiteralBlock(CodeGenerator, Member.GetGenericFreeName(), MethodType.Create(Member)));
            }
        }

        private static ICppBlock CreateGenericBlock(ICppBlock Block, IGenericMember Member)
        {
            if (Member.get_IsGenericInstance())
            {
                var cg = Block.CodeGenerator;
                return new TypeArgumentBlock(Block, Member.GetGenericArguments().Select((item) => item.CreateBlock(cg)));
            }
            else
            {
                return Block;
            }
        }

        public static ICppBlock CreateBlock(this IMethod Member, ICodeGenerator CodeGenerator)
        {
            return CreateGenericBlock(CreateGenericFreeBlock(Member, CodeGenerator), Member);
        }

        public static ICppBlock CreateBlock(this IField Member, ICodeGenerator CodeGenerator)
        {
            var declType = Member.DeclaringType;
            if (declType == null)
            {
                return new RetypedBlock(BlockFromName(CodeGenerator, Member.Name), Member.FieldType);
            }
            else
            {
                return new ScopeOperatorBlock(declType.CreateBlock(CodeGenerator), new LiteralBlock(CodeGenerator, Member.Name, Member.FieldType));
            }
        }

        #endregion

        #region GetEnvironment

        public static ICppEnvironment GetEnvironment(this ICodeGenerator CodeGenerator)
        {
            if (CodeGenerator is CppCodeGenerator)
            {
                return ((CppCodeGenerator)CodeGenerator).Environment;
            }
            return CodeGenerator.Method.GetEnvironment();
        }

        public static ICppEnvironment GetEnvironment(this IMember Member)
        {
            if (Member is ICppMember)
            {
                return ((ICppMember)Member).Environment;
            }
            else
            {
                return new CppEnvironment();
            }
        }

        #endregion

        #region GetTypeNamer

        public static Func<INamespace, IConverter<IType, string>> GetTypeNamer(this ICodeGenerator CodeGenerator)
        {
            return CodeGenerator.GetEnvironment().TypeNamer;
        }

        public static Func<INamespace, IConverter<IType, string>> GetTypeNamer(this IMember Member)
        {
            return Member.GetEnvironment().TypeNamer;
        }

        #endregion

        #region ConvertType

        public static IType ConvertType(this ICodeGenerator CodeGenerator, IType Type)
        {
            return CodeGenerator.GetEnvironment().TypeConverter.Convert(Type);
        }

        public static IType ConvertValueType(this ICodeGenerator CodeGenerator, IType Type)
        {
            return CodeGenerator.GetEnvironment().TypeConverter.ConvertWithValueSemantics(Type);
        }

        public static IType ConvertType(this IMember Member, IType Type)
        {
            return Member.GetEnvironment().TypeConverter.Convert(Type);
        }

        public static IType ConvertValueType(this IMember Member, IType Type)
        {
            return Member.GetEnvironment().TypeConverter.ConvertWithValueSemantics(Type);
        }

        #endregion

        #region Local Usage

        public static bool UsesLocal(this ICppBlock Block, CppLocal Local)
        {
            return Block.LocalsUsed.Contains(Local);
        }

        #endregion

        #region Code Builder Extensions

        private static string GetFirstLine(CodeBuilder Body)
        {
            int i;
            for (i = 0; i < Body.LineCount - 1 && Body[i].IsWhitespace; i++) ;

            return Body[i].Text;
        }

        public static BodyStatementType AddBodyCodeBuilder(this CodeBuilder CodeBuilder, CodeBuilder Body)
        {
            string firstLine = GetFirstLine(Body).Trim();
            if (firstLine == ";")
            {
                CodeBuilder.Append(Body);
                return BodyStatementType.Empty;
            }
            else if (!firstLine.StartsWith("{"))
            {
                CodeBuilder.IncreaseIndentation();
                CodeBuilder.AddCodeBuilder(Body);
                CodeBuilder.DecreaseIndentation();
                return BodyStatementType.Single;
            }
            else
            {
                CodeBuilder.AddCodeBuilder(Body);
                return BodyStatementType.Block;
            }
        }

        public static BodyStatementType AddEmbracedBodyCodeBuilder(this CodeBuilder CodeBuilder, CodeBuilder Body)
        {
            string firstLine = GetFirstLine(Body).Trim();
            if (firstLine == ";")
            {
                CodeBuilder.AddLine("{ }");
                return BodyStatementType.Empty;
            }
            else if (!firstLine.StartsWith("{"))
            {
                CodeBuilder.AddLine("{");
                CodeBuilder.IncreaseIndentation();
                CodeBuilder.AddCodeBuilder(Body);
                CodeBuilder.DecreaseIndentation();
                CodeBuilder.AddLine("}");
                return BodyStatementType.Single;
            }
            else
            {
                CodeBuilder.AddCodeBuilder(Body);
                return BodyStatementType.Block;
            }
        }

        #endregion

        #region Pointer Metrics

        public static int GetPointerDepth(this IType Type)
        {
            int depth = 0;
            var t = Type;
            while (t.get_IsPointer())
            {
                depth++;
                t = t.AsContainerType().GetElementType();
            }
            return depth;
        }

        #endregion

        #region ResolveGenericInstance

        /*public static IType ResolveGenericInstance(this IType Type, IGenericMember GenericMember)
        {
            if (Type is IGenericParameter && GenericMember.get_IsGenericInstance())
            {
                var seq = GenericMember.GetGenericParameters().Zip(GenericMember.GetGenericArguments(), (a, b) => new KeyValuePair<IGenericParameter, IType>(a, b));
                foreach (var item in seq)
                {
                    if (Type.Equals(item.Key))
                    {
                        return item.Value;
                    }
                }
            }
            if (Type.get_IsArray())
            {
                var tArr = Type.AsContainerType().AsArrayType();
                return tArr.GetElementType().ResolveGenericInstance(GenericMember).MakeArrayType(tArr.ArrayRank);
            }
            else if (Type.get_IsPointer())
            {
                var tPtr = Type.AsContainerType().AsPointerType();
                return tPtr.GetElementType().ResolveGenericInstance(GenericMember).MakePointerType(tPtr.PointerKind);
            }
            else if (Type.get_IsVector())
            {
                var tPtr = Type.AsContainerType().AsVectorType();
                return tPtr.GetElementType().ResolveGenericInstance(GenericMember).MakeVectorType(tPtr.GetDimensions());
            }
            else if (Type.get_IsGenericInstance())
            {
                var genericDecl = Type.GetGenericDeclaration();
                var genericArgs = Type.GetGenericArguments().ResolveGenericInstances(GenericMember);
                return genericDecl.MakeGenericType(genericArgs);
            }
            return Type;
        }*/

        /*public static IEnumerable<IType> ResolveGenericInstances(this IEnumerable<IType> Types, IGenericMember GenericMember)
        {
            return Types.Select((item) => item.ResolveGenericInstance(GenericMember));
        }

        public static IType[] ResolveGenericInstances(this IType[] Types, IGenericMember GenericMember)
        {
            IType[] results = new IType[Types.Length];
            for (int i = 0; i < Types.Length; i++)
            {
                results[i] = Types[i].ResolveGenericInstance(GenericMember);
            }
            return results;
        }*/

        #endregion

        #region GetTemplateDefinition

        public static CppTemplateDefinition GetTemplateDefinition(this IGenericMember Member)
        {
            if (Member is ICppTemplateMember)
            {
                return ((ICppTemplateMember)Member).Templates;
            }
            else
            {
                return null;
            }
        }

        #endregion

        #region GetCopyConstructor

        public static IMethod GetCopyConstructor(this IType Type)
        {
            var method = Type.GetConstructor(new IType[] { Type.MakePointerType(CppPointerExtensions.AtAddressPointer) }, false);
            if (method == null)
            {
                method = new ImplicitCopyConstructor(Type);
            }
            return method;
        }

        #endregion

        #region ContainsTemplates

        public static bool ContainsTemplates(this IMember Member)
        {
            if (Member is IGenericMember)
            {
                if (((IGenericMember)Member).get_IsGeneric()) return true;
            }
            if (Member is IType)
            {
                if (((IType)Member).GetMethods().Any(ContainsTemplates)) return true;
            }
            if (Member is INamespace)
            {
                if (((INamespace)Member).GetTypes().Any(ContainsTemplates)) return true;
            }
            return false;
        }

        #endregion
    }
}
