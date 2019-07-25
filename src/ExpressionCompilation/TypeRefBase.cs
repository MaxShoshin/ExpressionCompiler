using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace NeedfulThings.ExpressionCompilation
{
    internal abstract class TypeRefBase
    {
        [NotNull] private readonly IReadOnlyList<TypeRefBase> _genericArguments;

        protected TypeRefBase([NotNull] IReadOnlyList<TypeRefBase> genericArguments)
        {
            if (genericArguments == null) throw new ArgumentNullException(nameof(genericArguments));

            _genericArguments = genericArguments.ToList();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;

            return Equals((TypeRefBase)obj);
        }

        public override int GetHashCode()
        {
            return _genericArguments.Count;
        }

        protected bool Equals([NotNull] TypeRefBase other)
        {
            return _genericArguments.SequenceEqual(other._genericArguments);
        }
    }
}