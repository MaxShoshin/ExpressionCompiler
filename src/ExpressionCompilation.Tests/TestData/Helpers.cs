using System.Diagnostics.CodeAnalysis;

namespace ExpressionCompilation.Tests.TestData
{
#pragma warning disable CA2211 // Non-constant fields should not be visible
#pragma warning disable SA1401 // Fields must be private
#pragma warning disable CA1801 // Parameter is never used

    public static class Helpers
    {
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        public static object BoxedIntValue = 4;

        public static int IntValue = 4;

        public static uint UIntValue = 4;

        public static bool TrueValue = true;

        public static bool FalseValue = false;

        public static void DoNothing()
        {
        }

        public static void DoNothing(object someValue)
        {
        }
    }

#pragma warning restore CA1801 // Parameter is never used
#pragma warning restore SA1401 // Fields must be private
#pragma warning restore CA2211 // Non-constant fields should not be visible
}