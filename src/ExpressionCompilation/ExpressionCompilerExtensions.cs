using System;
using JetBrains.Annotations;

namespace ExpressionCompilation
{
    public static class ExpressionCompilerExtensions
    {
        [NotNull]
        public static TDelegate Compile<TDelegate>([NotNull] this ExpressionCompiler compiler)
            where TDelegate : class
        {
            if (compiler == null) throw new ArgumentNullException(nameof(compiler));

            return (TDelegate)(object)compiler.Compile(typeof(TDelegate));
        }

        [NotNull]
        public static ExpressionCompiler WithUsing([NotNull] this ExpressionCompiler compiler, [NotNull] Type usingType)
        {
            if (compiler == null) throw new ArgumentNullException(nameof(compiler));
            if (usingType == null) throw new ArgumentNullException(nameof(usingType));
            if (usingType.Namespace == null) throw new ArgumentException("Using type has null namespace.", nameof(usingType));

            return compiler.WithUsing(usingType.Namespace);
        }
    }
}