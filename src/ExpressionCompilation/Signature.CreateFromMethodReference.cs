using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Mono.Cecil;

namespace NeedfulThings.ExpressionCompilation
{
    internal partial class Signature
    {
        [NotNull]
        public static Signature Create([NotNull] MethodReference methodReference)
        {
            if (methodReference == null) throw new ArgumentNullException(nameof(methodReference));

            var genericMethod = methodReference as GenericInstanceMethod;

            var genericArguments = genericMethod != null
                ? (IReadOnlyList<TypeRefBase>)genericMethod.ElementMethod.GenericParameters.Select(item => CreateTypeRef(item, methodReference.DeclaringType)).ToList()
                : Array.Empty<TypeRefBase>();

            var signature = new Signature(
                returnType: CreateTypeRef(methodReference.ReturnType, methodReference.DeclaringType),
                name: methodReference.Name,
                genericArguments: genericArguments,
                arguments: methodReference.Parameters.Select(item => CreateTypeRef(item.ParameterType, methodReference.DeclaringType)).ToList());

            return signature;
        }

        private static TypeRefBase CreateTypeRef([NotNull] TypeReference typeReference, [NotNull] TypeReference declaringType)
        {
            if (typeReference is GenericParameter genericParam)
            {
                // Resolve generic argument if possible as MethodInfo will contains already resolved types
                if (declaringType is GenericInstanceType declaringGenericInstanceType &&
                    genericParam.MetadataType == MetadataType.Var)
                {
                    var resolvedGenericArgument = declaringGenericInstanceType.GenericArguments[genericParam.Position];

                    return CreateTypeRef(resolvedGenericArgument, declaringType);
                }

                return new GenericParameterRef(
                    genericParam.MetadataType == MetadataType.Var,
                    genericParam.Position);
            }

            IReadOnlyList<TypeRefBase> genericArguments = Array.Empty<TypeRefBase>();

            if (typeReference is GenericInstanceType genericInstanceType)
            {
                genericArguments = genericInstanceType.GenericArguments
                    .Select(genericArgument => CreateTypeRef(genericArgument, declaringType))
                    .ToList();
            }

            return new TypeRef(GetTypeName(typeReference), genericArguments);
        }

        [NotNull]
        private static string GetTypeName([NotNull] TypeReference typeReference)
        {
            // HACK: FullName of TypeReference on Generic nested type can be differ, so create type name manually
            var prefix = typeReference.IsNested
                ? GetTypeName(typeReference.DeclaringType) + "+"
                : typeReference.Namespace + ".";

            return prefix + typeReference.Name;
        }
    }
}