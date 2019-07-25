using System;
using JetBrains.Annotations;

namespace NeedfulThings.ExpressionCompilation
{
    internal sealed class GenericParameterRef : TypeRefBase
    {
        private readonly bool _isMethodArgument;
        private readonly int _position;

        public GenericParameterRef(bool isMethodArgument, int position)
            : base(Array.Empty<TypeRefBase>())
        {
            _isMethodArgument = isMethodArgument;
            _position = position;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;

            return Equals((GenericParameterRef)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ _isMethodArgument.GetHashCode();
                hashCode = (hashCode * 397) ^ _position;
                return hashCode;
            }
        }

        private bool Equals([NotNull] GenericParameterRef other)
        {
            return base.Equals(other) &&
                   _isMethodArgument == other._isMethodArgument &&
                   _position == other._position;
        }
    }
}