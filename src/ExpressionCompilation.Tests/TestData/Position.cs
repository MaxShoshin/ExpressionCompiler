using System;
using JetBrains.Annotations;

namespace NeedfulThings.ExpressionCompilation.Tests.TestData
{
    public sealed class Position
    {
        public Position(int id, PositionState state, DateTime updated, string note)
        {
            Id = id;
            State = state;
            Updated = updated;
            Note = note;
        }

        [UsedImplicitly] public int Id { get; private set; }

        [UsedImplicitly] public PositionState State { get; }

        [UsedImplicitly] public DateTime Updated { get; }

        [UsedImplicitly] public string Note { get; }
    }
}