using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using FluentAssertions;
using JetBrains.Annotations;
using NeedfulThings.ExpressionCompilation.Tests.Infrastructure;
using NeedfulThings.ExpressionCompilation.Tests.TestData;
using Xunit;

namespace NeedfulThings.ExpressionCompilation.Tests
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
            AssertFilter("return it.Id == 2;");
        }

        [Fact]
        public void ShouldFilterWithBoxAndUnboxInExpression()
        {
            AssertFilter("return (int)(object)it.Id==2;");
        }

        [Fact]
        public void ShouldFilterWithCastEnumToInt()
        {
            AssertFilter("return (int)(it.State) == 1;");
        }

        [Fact]
        public void ShouldFilterWithStatements()
        {
            AssertFilter(
                "var intVal = (int)it.State;",
                "return intVal == 1;");
        }

        [Fact]
        public void ShouldFilterWithCastIntToInt()
        {
            AssertFilter("return (Int32)(it.Id) == 2;");
        }

        [Fact]
        public void ShouldFilterByDate()
        {
            AssertFilter("return it.Updated !=new DateTime(2017, 01, 01);");
        }

        [Fact]
        public void ShouldFilterByContains()
        {
            AssertFilter("return it.Note.Contains(\"Abc2\");");
        }

        [Fact]
        public void ShouldFilterByIndexOf()
        {
            AssertFilter("return it.Note.IndexOf(\"Abc2\") != -1;");
        }

        [Fact]
        public void ShouldFilterWithNullComparison()
        {
            AssertFilter("return it.Note != null && it.Note.IndexOf(\"Abc2\") != -1;");
        }

        [Fact]
        public void ShouldFilterByIndexOfCaseInsensitive()
        {
            AssertFilter("return it.Note.IndexOf(\"abc2\", StringComparison.OrdinalIgnoreCase) != -1;");
        }

        [Fact]
        public void ShouldFilterWithExtensionMethod()
        {
            AssertFilter("return it.GetId() == 2;");
        }

        [Fact]
        public void ShouldContainErrorMessage()
        {
            try
            {
                CreatePredicate("return it.NotExitingProperty == 2;");
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

            expression.Insert(0, "return ");
            expression.Append(";");

            AssertFilter(expression.ToString());
        }

        [Theory]
        [InlineData(2000)]
        public void ShouldWorkWithBigExpressionTree(int count)
        {
            var expression = new StringBuilder();

            expression.Append("return (it.Id != -1)");

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

            expression.Append(";");

            AssertFilter(expression.ToString());
        }

        [Fact]
        public void SimpleExample()
        {
            Func<int> calculator = new ExpressionCompiler("return (int)Math.Round(Math.PI - 1);")
                .WithUsing("System")
                .Returns(typeof(int))
                .Compile<Func<int>>();

            calculator.Invoke().Should().Be(2);
        }

        [Fact]
        public void ShouldNotLoadAdditionalAssembliesInDomain()
        {
            using (Sandbox.Create<AssemblyCounter>())
            {
            }
        }

        private static void AssertFilter([NotNull] params string[] statements)
        {
            var predicate = CreatePredicate(statements);

            Assert.NotNull(predicate);

            var actual = Assert.Single(DataSource.Where(i => predicate(i)));

            Assert.NotNull(actual);
            Assert.Equal(2, actual.Id);
        }

        [NotNull]
        private static Func<Position, bool> CreatePredicate([NotNull] params string[] statements)
        {
            return (Func<Position, bool>)new ExpressionCompiler(statements)
                .WithParameter("it", typeof(Position))
                .WithReference(Assembly.GetExecutingAssembly())
                .WithUsing(typeof(DateTime))
                .WithUsing(typeof(Position))
                .Returns(typeof(bool))
                .Compile(typeof(Func<Position, bool>));
        }

        internal sealed class AssemblyCounter : Sandbox
        {
            protected override void Start()
            {
                AssertFilter("return it.Id == 2;");

                var beforeCount = AppDomain.CurrentDomain.GetAssemblies().Length;

                AssertFilter("return ((int)it.State) == 1;");

                var afterCount = AppDomain.CurrentDomain.GetAssemblies().Length;

                Assert.Equal(beforeCount, afterCount);
            }

            protected override void Stop()
            {
            }
        }
    }
}