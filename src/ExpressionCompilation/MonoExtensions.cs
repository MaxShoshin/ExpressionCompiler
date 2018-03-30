using System;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;
using Mono.Cecil;

namespace ExpressionCompilation
{
    internal static class MonoExtensions
    {
        [NotNull]
        internal static Type GetRuntimeType([NotNull] this TypeReference typeRef)
        {
            if (typeRef == null) throw new ArgumentNullException(nameof(typeRef));

            if (typeRef is GenericInstanceType genericType)
            {
                var mainType = GetRuntimeType(genericType.ElementType);

                return mainType.MakeGenericType(genericType.GenericArguments.Select(GetRuntimeType).ToArray());
            }

            var typeName = typeRef.FullName.Replace("/", "+");

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

            var type = assembly.GetType(typeName, true);

            if (type == null)
            {
                throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, "Type reference {0} not found", typeRef.FullName));
            }

            return type;
        }
    }
}