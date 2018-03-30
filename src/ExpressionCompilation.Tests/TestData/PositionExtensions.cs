using JetBrains.Annotations;

namespace ExpressionCompilation.Tests.TestData
{
    [UsedImplicitly]
    internal static class PositionExtensions
    {
        public static int GetId([NotNull] this Position position)
        {
            return position.Id;
        }
    }
}