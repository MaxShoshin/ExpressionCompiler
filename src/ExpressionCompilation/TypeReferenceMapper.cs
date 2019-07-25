using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Mono.Cecil;

namespace NeedfulThings.ExpressionCompilation
{
    internal sealed class TypeReferenceMapper
    {
        [NotNull]
        public Type GetRuntimeType([NotNull] TypeReference typeRef, [CanBeNull] TypeReference declaringType = null)
        {
            if (typeRef == null) throw new ArgumentNullException(nameof(typeRef));

            if (typeRef is GenericInstanceType genericType)
            {
                return GetGenericType(genericType, (GenericInstanceType)declaringType);
            }

            if (typeRef is ArrayType arrayType)
            {
                return GetArrayType(arrayType, declaringType);
            }

            var typeName = typeRef.FullName.Replace("/", "+");

            var assembly = GetAssembly(typeRef);

            var type = assembly.GetType(typeName, true);

            if (type == null)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Type reference {0} not found in loaded assemblies.",
                        typeRef.FullName));
            }

            return type;
        }

        [NotNull]
        private Type GetArrayType([NotNull] ArrayType arrayType, [CanBeNull] TypeReference declaringType)
        {
            var elementType = GetRuntimeType(arrayType.ElementType, declaringType);
            if (arrayType.Dimensions.Count != 1)
            {
                // HACK: To create multidimensional arrays (i have no idea how to create it: MakeArrayType(rank) not working for me.
                return Array.CreateInstance(elementType, arrayType.Dimensions.Select(item => 0).ToArray()).GetType();
            }

            return elementType.MakeArrayType();
        }

        [NotNull]
        private Type GetGenericType([NotNull] GenericInstanceType genericType, [NotNull] GenericInstanceType genericParent)
        {
            var genericArguments = new List<TypeReference>(genericType.GenericArguments);

            for (var i = 0; i < genericType.GenericArguments.Count; i++)
            {
                // Substitute generic parameters from generic parent arguments
                if (genericType.GenericArguments[i] is GenericParameter param)
                {
                    genericArguments[i] = genericParent.GenericArguments[param.Position];
                }
            }

            var mainType = GetRuntimeType(genericType.ElementType);

            return mainType.MakeGenericType(genericArguments.Select(s => GetRuntimeType(s, genericParent)).ToArray());
        }

        [NotNull]
        private Assembly GetAssembly([NotNull] TypeReference typeRef)
        {
            var moduleDefinition = typeRef.Scope as ModuleDefinition;
            string assemblyFullName;

            if (moduleDefinition != null)
            {
                assemblyFullName = moduleDefinition.Assembly.FullName;
            }
            else
            {
                assemblyFullName = ((AssemblyNameReference)typeRef.Scope).FullName;
            }

            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .First(asm => asm.FullName == assemblyFullName);

            return assembly;
        }
    }
}