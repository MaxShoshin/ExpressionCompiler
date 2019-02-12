using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Mono.Cecil;

namespace ExpressionCompilation
{
    internal sealed class MethodReferenceMapper
    {
        private readonly TypeReferenceMapper _typeMapper;

        public MethodReferenceMapper([NotNull] TypeReferenceMapper typeMapper)
        {
            _typeMapper = typeMapper ?? throw new ArgumentNullException(nameof(typeMapper));
        }

        [NotNull]
        public MethodInfo GetMethod(MethodReference methodReference)
        {
            var referenceSignature = Signature.Create(methodReference);

            var declaringType = _typeMapper.GetRuntimeType(methodReference.DeclaringType);

            var methods = declaringType.GetMembers()
                .OfType<MethodInfo>()
                .Where(methodInfo => methodInfo.Name == methodReference.Name &&
                                     methodInfo.GetParameters().Length == methodReference.Parameters.Count);

            foreach (var methodInfo in methods)
            {
                var methodSignature = Signature.Create(methodInfo);

                if (Equals(referenceSignature, methodSignature))
                {
                    if (methodInfo.IsGenericMethodDefinition)
                    {
                        var genericMethod = (GenericInstanceMethod)methodReference;
                        var genericArguments = genericMethod.GenericArguments
                            .Select(item => _typeMapper.GetRuntimeType(item, methodReference.DeclaringType))
                            .ToArray();

                        return methodInfo.MakeGenericMethod(genericArguments);
                    }

                    return methodInfo;
                }
            }

            throw new NotSupportedException(string.Format(
                CultureInfo.InvariantCulture,
                "Method not found:\r\n{0}\r\nExisting methods:\r\n{1}",
                methodReference.ToString(),
                string.Join("\r\n", methods)));
        }

        [NotNull]
        public ConstructorInfo GetConstructor(MethodReference methodReference)
        {
            var referenceSignature = Signature.Create(methodReference);

            var declaringType = _typeMapper.GetRuntimeType(methodReference.DeclaringType);

            var constructors = declaringType.GetConstructors()
                .Where(constructorInfo => constructorInfo.GetParameters().Length == methodReference.Parameters.Count);

            foreach (var constructorInfo in constructors)
            {
                var ctorSignature = Signature.Create(constructorInfo);

                if (Equals(referenceSignature, ctorSignature))
                {
                    return constructorInfo;
                }
            }

            throw new NotSupportedException(string.Format(
                CultureInfo.InvariantCulture,
                "Ctor not found:\r\n{0}\r\nExisting methods:\r\n{1}",
                methodReference.ToString(),
                string.Join("\r\n", constructors)));
        }
    }
}