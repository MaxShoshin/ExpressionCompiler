using System;
using System.Text;
using JetBrains.Annotations;

namespace ExpressionCompilation
{
    internal static class TypeFormatter
    {
        public static string GetFullName([NotNull] Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            if (!type.IsGenericType)
            {
                return type.FullName;
            }

            var text = new StringBuilder();

            var typeFullName = type.FullName;
            text.Append(typeFullName.Substring(0, typeFullName.LastIndexOf("`", StringComparison.Ordinal)));

            text.Append("<");
            var first = true;
            foreach (var genericArgument in type.GetGenericArguments())
            {
                if (!first)
                {
                    text.Append(", ");
                }

                text.Append(GetFullName(genericArgument));
                first = false;
            }

            text.Append(">");

            return text.ToString();
        }
    }
}