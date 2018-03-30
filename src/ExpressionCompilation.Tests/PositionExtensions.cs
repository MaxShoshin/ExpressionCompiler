using JetBrains.Annotations;

namespace ExpressionCompilation.Tests
{
    [UsedImplicitly]
    internal static class PositionExtensions
    {
        public static int GetId(this FilterExpressionExtensionsTest.Position position)
        {
            return position.Id;
        }
    }
}