using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;

namespace ExpressionCompilation
{
    internal partial class Signature
    {
        [NotNull]
        public static Signature Create([NotNull] MethodInfo methodInfo)
        {
            if (methodInfo == null) throw new ArgumentNullException(nameof(methodInfo));

            return new Signature(
                returnType: CreateTypeRef(methodInfo.ReturnType),
                name: methodInfo.Name,
                genericArguments: methodInfo.GetGenericArguments().Select(CreateTypeRef).ToList(),
                arguments: methodInfo.GetParameters().Select(item => CreateTypeRef(item.ParameterType)).ToList());
        }

        [NotNull]
        public static Signature Create([NotNull] ConstructorInfo constructorInfo)
        {
            if (constructorInfo == null) throw new ArgumentNullException(nameof(constructorInfo));

            return new Signature(
                returnType: CreateTypeRef(typeof(void)),
                name: constructorInfo.Name,
                genericArguments: Array.Empty<TypeRefBase>(),
                arguments: constructorInfo.GetParameters().Select(item => CreateTypeRef(item.ParameterType)).ToList());
        }

        [NotNull]
        private static TypeRefBase CreateTypeRef([NotNull] Type type)
        {
            if (type.IsGenericParameter)
            {
                return new GenericParameterRef(
                    type.DeclaringMethod == null,
                    type.GenericParameterPosition);
            }

            IReadOnlyList<TypeRefBase> genericArguments = Array.Empty<TypeRefBase>();

            if (type.IsGenericType)
            {
                genericArguments = type.GenericTypeArguments
                    .Select(CreateTypeRef)
                    .ToList();
            }

            return new TypeRef(
                GetTypeName(type),
                genericArguments);
        }

        [NotNull]
        private static string GetTypeName([NotNull] Type type)
        {
            // HACK: FullName of Type on Generic nested type can be differ, so create type name manually
            var prefix = type.IsNested
                ? GetTypeName(type.DeclaringType) + "+"
                : type.Namespace + ".";

            return prefix + type.Name;
        }
    }
}