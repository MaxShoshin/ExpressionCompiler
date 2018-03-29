using System;
using ExpressionCompilation.Tests.TestData;
using FluentAssertions;
using Xunit;

namespace ExpressionCompilation.Tests
{
    public class SimpleExpressionTests
    {
        [Fact]
        public void ShouldFilterByProperty()
        {
            var predicate = new ExpressionCompiler("arg.Id == 2")
                .WithParameter("arg", typeof(Item))
                .Returns(typeof(bool))
                .Compile<Func<Item, bool>>();

            predicate(new Item(2)).Should().BeTrue();
            predicate(new Item(1)).Should().BeFalse();
        }
    }
}