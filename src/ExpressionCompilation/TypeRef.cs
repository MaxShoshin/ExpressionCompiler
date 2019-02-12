using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace ExpressionCompilation
{
    internal sealed class TypeRef : TypeRefBase
    {
        [NotNull] private readonly string _typeName;

        public TypeRef([NotNull] string typeName, [NotNull] IReadOnlyList<TypeRefBase> genericArguments)
            : base(genericArguments)
        {
            _typeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;

            return Equals((TypeRef)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ _typeName.GetHashCode();
            }
        }

        private bool Equals(TypeRef other)
        {
            return base.Equals(other) &&
                   string.Equals(_typeName, other._typeName, StringComparison.Ordinal);
        }
    }
}