using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace ExpressionCompilation
{
    internal sealed partial class Signature
    {
        private readonly TypeRefBase _returnType;
        private readonly IReadOnlyList<TypeRefBase> _genericArguments;
        private readonly IReadOnlyList<TypeRefBase> _arguments;
        private readonly string _name;

        private Signature([NotNull] TypeRefBase returnType, [NotNull] string name, [NotNull] IReadOnlyList<TypeRefBase> genericArguments, [NotNull] IReadOnlyList<TypeRefBase> arguments)
        {
            _returnType = returnType ?? throw new ArgumentNullException(nameof(returnType));
            _genericArguments = genericArguments ?? throw new ArgumentNullException(nameof(genericArguments));
            _arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
            _name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;

            return Equals((Signature)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = _returnType.GetHashCode();
                foreach (var genericArgument in _genericArguments)
                {
                    hashCode = (hashCode * 397) ^ genericArgument.GetHashCode();
                }

                foreach (var argument in _arguments)
                {
                    hashCode = (hashCode * 397) ^ argument.GetHashCode();
                }

                hashCode = (hashCode * 397) ^ _name.GetHashCode();

                return hashCode;
            }
        }

        private bool Equals([NotNull] Signature other)
        {
            return Equals(_returnType, other._returnType) &&
                   _genericArguments.SequenceEqual(other._genericArguments) &&
                   _arguments.SequenceEqual(other._arguments) &&
                   string.Equals(_name, other._name, StringComparison.Ordinal);
        }
    }
}