using Flame.Compiler;
using Flame.Compiler.Emit;
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
        #region IsForeign

        public static bool IsForeign(this IType Type)
        {
            return ForeignTypePredicate.Instance.Convert(Type);
        }

        #endregion

        #region GetHeaderAttributes

        private static IEnumerable<HeaderAttribute> GetHeaderAttributes(this IMember Member)
        {
            foreach (var item in Member.Attributes)
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
                        var args = ((IConstructedAttribute)item).GetArguments().ToArray();
                        if (args.Length == 1)
                        {
                            yield return new HeaderAttribute(args[0].GetValue<string>());
                        }
                        else
                        {
                            yield return new HeaderAttribute(args[0].GetValue<string>(), args[1].GetValue<bool>());
                        }
                    }
                }
            }
        }

        private static IEnumerable<HeaderDependencyAttribute> GetHeaderDependencyAttributes(this IMember Member)
        {
            return Member.Attributes.OfType<HeaderDependencyAttribute>();
        }

        public static IEnumerable<IHeaderDependency> GetAttributeDependencies(this IMember Member)
        {
            return Member.GetHeaderAttributes().Select(
                item => (item.IsStandardHeader ? 
                            (IHeaderDependency)new StandardDependency(item.HeaderName) : 
                            (IHeaderDependency)new UserDependency(item.HeaderName)))
                .MergeDependencies(Member.GetHeaderDependencyAttributes().Select(item => item.Dependency));
        }

        #endregion

        #region IsGlobalType

        public static bool IsGlobalType(this IType Member)
        {
            foreach (var item in Member.Attributes)
            {
                if (item != null && item.AttributeType.FullName == typeof(GlobalTypeAttribute).FullName)
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region GetReferencePointerAttribute

        private static ReferencePointerAttribute GetReferencePointerAttribute(this IMember Member)
        {
            foreach (var item in Member.Attributes)
            {
                if (item != null && item.AttributeType.FullName == typeof(ReferencePointerAttribute).FullName)
                {
                    var eval = item.Value.GetObjectValue();
                    if (eval is ReferencePointerAttribute)
                    {
                        return (ReferencePointerAttribute)eval;
                    }
                    else if (item is IConstructedAttribute)
                    {
                        return new ReferencePointerAttribute(((IConstructedAttribute)item).GetArguments().First().GetValue<string>());
                    }
                }
            }
            return null;
        }

        public static PointerKind GetReferencePointerKind(this IType Member)
        {
            var attr = Member.GetReferencePointerAttribute();
            if (attr == null)
            {
                return PointerKind.ReferencePointer;
            }
            else
            {
                return PointerKind.Register(attr.PointerType);
            }
        }

        #endregion

        #region IsExplicitPointer

        public static bool IsExplicitPointer(this IType Type)
        {
            return Type.GetIsPointer() && !Type.AsContainerType().AsPointerType().PointerKind.Equals(CppPointerExtensions.AtAddressPointer);
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

        private static ICppBlock CreateGenericFreeMemberBlock(IMethod Member, ICodeGenerator CodeGenerator)
        {
            return new RetypedBlock(BlockFromName(CodeGenerator, Member.GetGenericFreeName()), MethodType.Create(Member));
        }

        private static ICppBlock CreateGenericFreeBlock(IMethod Member, ICodeGenerator CodeGenerator)
        {
            var declType = Member.DeclaringType;
            if (declType == null)
            {
                return CreateGenericFreeMemberBlock(Member, CodeGenerator);
            }
            else
            {
                return new ScopeOperatorBlock(declType.CreateBlock(CodeGenerator), CreateGenericFreeMemberBlock(Member, CodeGenerator));
            }
        }

        private static ICppBlock CreateGenericBlock(ICppBlock Block, IGenericMember Member)
        {
            if (Member.GetIsGenericInstance())
            {
                var cg = Block.CodeGenerator;
                return new TypeArgumentBlock(Block, Member.GetGenericArguments().Select(item => item.CreateBlock(cg)));
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
                return Member.CreateMemberBlock(CodeGenerator);
            }
            else
            {
                return new ScopeOperatorBlock(declType.CreateBlock(CodeGenerator), Member.CreateMemberBlock(CodeGenerator));
            }
        }

        public static ICppBlock CreateMemberBlock(this IMethod Method, ICodeGenerator CodeGenerator)
        {
            return CreateGenericBlock(CreateGenericFreeMemberBlock(Method, CodeGenerator), Method);
        }

        public static ICppBlock CreateMemberBlock(this IField Member, ICodeGenerator CodeGenerator)
        {
            return new RetypedBlock(BlockFromName(CodeGenerator, Member.Name), Member.FieldType);
        }

        #endregion

        #region GetEnvironment

        public static ICppEnvironment GetEnvironment(this ICodeGenerator CodeGenerator)
        {
            if (CodeGenerator is CppCodeGenerator)
            {
                return ((CppCodeGenerator)CodeGenerator).Environment;
            }
            else
            {
                return CodeGenerator.Method.GetEnvironment();
            }
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

        #region GetOptions

        public static ICompilerOptions GetOptions(this ICodeGenerator CodeGenerator)
        {
            return CodeGenerator.GetEnvironment().Log.Options;
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

        #region ConvertVariableMember

        public static IVariableMember ConvertVariableMember(this ICodeGenerator CodeGenerator, IVariableMember Member)
        {
            var descMember = new DescribedVariableMember(Member.Name, CodeGenerator.ConvertType(Member.VariableType));
            foreach (var attr in Member.Attributes)
            {
                descMember.AddAttribute(attr);
            } 
            return descMember;
        }

        #endregion

        #region Local Usage

        public static bool UsesLocal(this ICppBlock Block, CppLocal Local)
        {
            return Block.LocalsUsed.Contains(Local);
        }

        #endregion

        #region Code Builder Extensions

        private static int GetFirstLineIndex(CodeBuilder Body)
        {
            int i;
            for (i = 0; i < Body.LineCount - 1 && Body[i].IsWhitespace; i++) ;

            return i;
        }

        private static string GetFirstLine(CodeBuilder Body)
        {
            return Body[GetFirstLineIndex(Body)].Text;
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

        public static CodeBuilder PrependStatement(this CodeBuilder BlockBuilder, CodeBuilder StatementBody)
        {
            int firstLineIndex = GetFirstLineIndex(BlockBuilder);
            string firstLine = BlockBuilder[firstLineIndex].Text.Trim();
            if (firstLine.StartsWith("{"))
            {
                var cb = new CodeBuilder();
                cb.AddLine("{");
                cb.IncreaseIndentation();
                cb.AddCodeBuilder(StatementBody);
                cb.AddLine(BlockBuilder[firstLineIndex]);
                for (int i = firstLineIndex + 1; i < BlockBuilder.LineCount; i++)
                {
                    cb.AddLine(BlockBuilder[i]);
                }
                return cb;
            }
            else
            {
                if (firstLine == ";" && BlockBuilder.ToString().Trim() == ";")
                {
                    return StatementBody;
                }
                else
                {
                    var cb = new CodeBuilder();
                    cb.AddLine("{");
                    cb.IncreaseIndentation();
                    cb.AddCodeBuilder(StatementBody);
                    cb.AddCodeBuilder(BlockBuilder);
                    cb.DecreaseIndentation();
                    cb.AddLine("}");
                    return cb;
                }
            }
        }

        #endregion

        #region Pointer Metrics

        public static int GetPointerDepth(this IType Type)
        {
            int depth = 0;
            var t = Type;
            while (t.GetIsPointer())
            {
                depth++;
                t = t.AsContainerType().ElementType;
            }
            return depth;
        }

        #endregion

        #region ResolveGenericInstance

        /*public static IType ResolveGenericInstance(this IType Type, IGenericMember GenericMember)
        {
            if (Type is IGenericParameter && GenericMember.GetIsGenericInstance())
            {
                var seq = GenericMember.GenericParameters.Zip(GenericMember.GetGenericArguments(), (a, b) => new KeyValuePair<IGenericParameter, IType>(a, b));
                foreach (var item in seq)
                {
                    if (Type.Equals(item.Key))
                    {
                        return item.Value;
                    }
                }
            }
            if (Type.GetIsArray())
            {
                var tArr = Type.AsContainerType().AsArrayType();
                return tArr.ElementType.ResolveGenericInstance(GenericMember).MakeArrayType(tArr.ArrayRank);
            }
            else if (Type.GetIsPointer())
            {
                var tPtr = Type.AsContainerType().AsPointerType();
                return tPtr.ElementType.ResolveGenericInstance(GenericMember).MakePointerType(tPtr.PointerKind);
            }
            else if (Type.GetIsVector())
            {
                var tPtr = Type.AsContainerType().AsVectorType();
                return tPtr.ElementType.ResolveGenericInstance(GenericMember).MakeVectorType(tPtr.Dimensions);
            }
            else if (Type.GetIsGenericInstance())
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
            else if (Member.GetIsGenericInstance())
            {
                if (Member is IType)
                {
                    return ((IType)Member).GetGenericDeclaration().GetTemplateDefinition();
                }
                else if (Member is IMethod)
                {
                    return ((IMethod)Member).GetGenericDeclaration().GetTemplateDefinition();
                }
            }
            return null;
        }

        #endregion

        #region GetFullTemplateDefinition

        public static CppTemplateDefinition GetFullTemplateDefinition(this IType Type)
        {
            if (Type.DeclaringNamespace is IType)
            {
                return ((IType)Type.DeclaringNamespace).GetFullTemplateDefinition().Merge(Type.GetTemplateDefinition());
            }
            else
            {
                return Type.GetTemplateDefinition();
            }
        }

        #endregion

        #region GetParameterlessConstructor

        public static IMethod GetParameterlessConstructor(this IType Type)
        {
            var method = Type.GetConstructor(new IType[] { }, false);
            if (method == null)
            {
                method = new ImplicitParameterlessConstructor(Type);
            }
            return method;
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
                if (((IGenericMember)Member).GetIsGeneric()) return true;
            }
            if (Member is INamespace)
            {
                if (((INamespace)Member).Types.Any(ContainsTemplates)) return true;
            }
            return false;
        }

        #endregion

        #region HasUniformAccess

        public static bool HasUniformAccess(this IProperty Property)
        {
            var propAccess = Property.GetAccess();
            return Property.Accessors.All(item => item.GetAccess() == propAccess);
        }

        #endregion
    }
}
