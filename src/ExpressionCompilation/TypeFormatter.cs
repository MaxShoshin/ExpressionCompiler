using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;

namespace NeedfulThings.ExpressionCompilation
{
    internal static class TypeFormatter
    {
        private static readonly ISet<string> StandardNamespaces = new HashSet<string>
        {
            "System"
        };

        private static readonly Dictionary<Type, string> KnownTypes = new Dictionary<Type, string>
        {
            { typeof(object), "object" },
            { typeof(string), "string" },
            { typeof(bool), "bool" },
            { typeof(byte), "byte" },
            { typeof(sbyte), "sbyte" },
            { typeof(short), "short" },
            { typeof(ushort), "ushort" },
            { typeof(int), "int" },
            { typeof(uint), "uint" },
            { typeof(long), "long" },
            { typeof(ulong), "ulong" },
            { typeof(float), "float" },
            { typeof(double), "double" },
            { typeof(decimal), "decimal" },
            { typeof(char), "char" }
        };

        [NotNull]
        public static string Format([NotNull] Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            string knownRender;
            if (KnownTypes.TryGetValue(type, out knownRender))
            {
                return knownRender;
            }

            var genericArguments = type.GetGenericArguments();
            if (genericArguments.Length == 0)
            {
                return FormatTypeName(type);
            }

            if (type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var underlyingType = Nullable.GetUnderlyingType(type);
                Debug.Assert(underlyingType != null, "Undelying type for Nullable type should be not null.");
                return Format(underlyingType) + "?";
            }

            // BUG: Can't work with double nested generic types (see test)
            var typeName = type.Name.Substring(0, type.Name.IndexOf("`", StringComparison.Ordinal));
            var argDefinition = string.Join(", ", genericArguments.Select(Format));

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}<{1}>",
                FormatTypeName(type, typeName),
                argDefinition);
        }

        private static string FormatTypeName([NotNull] Type type, [CanBeNull] string overrideName = null)
        {
            if (type.IsGenericParameter)
            {
                return type.Name;
            }

            var prefix = UseFullName(type) ? type.Namespace : null;

            var declaringType = type.DeclaringType;
            if (declaringType != null)
            {
                prefix = Format(declaringType);
            }

            var nameDefinition = overrideName ?? type.Name;

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}{1}",
                prefix == null ? null : prefix + ".",
                nameDefinition);
        }

        private static bool UseFullName([NotNull] Type type)
        {
            return !StandardNamespaces.Contains(type.Namespace);
        }
    }
}