using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ExpressionCompilation.Tests.Infrastructure;
using ExpressionCompilation.Tests.TestData;
using JetBrains.Annotations;
using Xunit;

namespace ExpressionCompilation.Tests
{
    public sealed class SimpleExpressionTest
    {
        private static readonly IReadOnlyList<Position> DataSource = new[]
        {
            new Position(1, PositionState.Deleted, new DateTime(2017, 01, 01), "Abc11"),
            new Position(2, PositionState.Active, new DateTime(2016, 03, 01), "Abc22"),
            new Position(3, PositionState.Deleted, new DateTime(2017, 01, 01), "Abc33")
        };

        [Fact]
        public void ShouldFilterWithItName()
        {
            AssertFilter("it.Id == 2");
        }

        [Fact]
        public void ShouldFilterWithFullLambdaSyntax()
        {
            AssertFilter("it.Id == 2");
        }

        [Fact]
        public void ShouldFilterWithBoxAndUnboxInExpression()
        {
            AssertFilter("(int)(object)it.Id==2");
        }

        [Fact]
        public void ShouldFilterWithCastEnumToInt()
        {
            AssertFilter("(int)(it.State) == 1");
        }

        [Fact]
        public void ShouldFilterWithCastIntToInt()
        {
            AssertFilter("(Int32)(it.Id) == 2");
        }

        [Fact]
        public void ShouldFilterByDate()
        {
            AssertFilter("it.Updated !=new DateTime(2017, 01, 01)");
        }

        [Fact]
        public void ShouldFilterByContains()
        {
            AssertFilter("it.Note.Contains(\"Abc2\")");
        }

        [Fact]
        public void ShouldFilterByIndexOf()
        {
            AssertFilter("it.Note.IndexOf(\"Abc2\") != -1");
        }

        [Fact]
        public void ShouldFilterWithNullComparison()
        {
            AssertFilter("it.Note != null && it.Note.IndexOf(\"Abc2\") != -1");
        }

        [Fact]
        public void ShouldFilterByIndexOfCaseInsensitive()
        {
            AssertFilter("it.Note.IndexOf(\"abc2\", StringComparison.OrdinalIgnoreCase) != -1");
        }

        [Fact]
        public void ShouldFilterWithExtensionMethod()
        {
            AssertFilter("it.GetId() == 2");
        }

        [Fact]
        public void ShouldContainErrorMessage()
        {
            try
            {
                CreatePredicate("it.NotExitingProperty == 2");
            }
            catch (CompilationErrorException ex)
            {
                Assert.Contains("'NotExitingProperty'", ex.ToString(), StringComparison.Ordinal);
            }
        }

        [Theory]
        [InlineData(500)]
        public void ShouldWorkWithDeepExpressionTree(int deep)
        {
            var expression = new StringBuilder();

            expression.Append("(it.Id != -1)");

            for (int i = 0; i < deep + 1; i++)
            {
                if (i == 2)
                {
                    continue;
                }

                expression.Insert(0, "(");

                expression.Append(" && (it.Id != ")
                    .Append(i)
                    .Append("))");
            }

            AssertFilter(expression.ToString());
        }

        [Theory]
        [InlineData(2000)]
        public void ShouldWorkWithBigExpressionTree(int count)
        {
            var expression = new StringBuilder();

            expression.Append("(it.Id != -1)");

            for (int i = 0; i < count + 1; i++)
            {
                if (i == 2)
                {
                    continue;
                }

                expression.Append(" && (it.Id != ")
                    .Append(i)
                    .Append(")");
            }

            AssertFilter(expression.ToString());
        }

        [Fact]
        public void ShouldNotLoadAdditionalAssembliesInDomain()
        {
            using (Sandbox.Create<AssemblyCounter>())
            {
            }
        }

        private static void AssertFilter([NotNull] string condition)
        {
            var predicate = CreatePredicate(condition);

            Assert.NotNull(predicate);

            var actual = Assert.Single(DataSource.Where(i => predicate(i)));

            Assert.NotNull(actual);
            Assert.Equal(2, actual.Id);
        }

        [NotNull]
        private static Func<Position, bool> CreatePredicate([NotNull] string condition)
        {
            return new ExpressionCompiler(condition)
                .WithParameter("it", typeof(Position))
                .WithReference(Assembly.GetExecutingAssembly())
                .WithUsing("System")
                .WithUsing("ExpressionCompilation.Tests.TestData")
                .Returns(typeof(bool))
                .Compile<Func<Position, bool>>();
        }

        internal sealed class AssemblyCounter : Sandbox
        {
            protected override void Start()
            {
                AssertFilter("it.Id == 2");

                var beforeCount = AppDomain.CurrentDomain.GetAssemblies().Length;

                AssertFilter("((int)it.State) == 1");

                var afterCount = AppDomain.CurrentDomain.GetAssemblies().Length;

                Assert.Equal(beforeCount, afterCount);
            }

            protected override void Stop()
            {
            }
        }
    }
}