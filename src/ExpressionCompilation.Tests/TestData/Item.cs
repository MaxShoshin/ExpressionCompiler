using JetBrains.Annotations;

namespace ExpressionCompilation.Tests.TestData
{
    [UsedImplicitly]
    public class Item
    {
        public Item(int id)
        {
            Id = id;
        }

        public int Id { get; }
    }
}