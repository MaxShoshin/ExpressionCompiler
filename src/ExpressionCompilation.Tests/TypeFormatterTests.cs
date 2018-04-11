using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Xunit;

namespace ExpressionCompilation.Tests
{
    public sealed class TypeFormatterTests
    {
        [Fact]
        public void ShouldFormatNameForStandardTypes()
        {
            // Arrange
            var type = typeof(IList);

            // Act
            var render = TypeFormatter.Format(type);

            // Assert
            Assert.Equal("System.Collections.IList", render);
        }

        [Fact]
        public void ShouldFormatFullNameForNotStandardTypes()
        {
            // Arrange
            var type = typeof(TypeFormatterTests);
            var expectedRender = typeof(TypeFormatterTests).FullName;

            // Act
            var render = TypeFormatter.Format(type);

            // Assert
            Assert.Equal(expectedRender, render);
        }

        [Theory]
        [InlineData(typeof(object), "object")]
        [InlineData(typeof(int), "int")]
        [InlineData(typeof(string), "string")]
        [InlineData(typeof(bool), "bool")]
        [InlineData(typeof(decimal), "decimal")]
        public void ShouldFormatKeywordForSystemTypes(Type type, string expectedRender)
        {
            // Act
            var render = TypeFormatter.Format(type);

            // Assert
            Assert.Equal(expectedRender, render);
        }

        [Theory]
        [InlineData(typeof(int?), "int?")]
        [InlineData(typeof(bool?), "bool?")]
        [InlineData(typeof(decimal?), "decimal?")]
        [InlineData(typeof(KeyValuePair<int?, string>?), "System.Collections.Generic.KeyValuePair<int?, string>?")]
        public void ShouldFormatNullableForStructureTypes(Type type, string expectedRender)
        {
            // Act
            var render = TypeFormatter.Format(type);

            // Assert
            Assert.Equal(expectedRender, render);
        }

        [Fact]
        public void ShouldFormatGenericType()
        {
            // Arrange
            var type = typeof(Dictionary<Lazy<TypeFormatterTests>, IList<string>>);
            var expectedRender = $"System.Collections.Generic.Dictionary<Lazy<{typeof(TypeFormatterTests).FullName}>, System.Collections.Generic.IList<string>>";

            // Act
            var render = TypeFormatter.Format(type);

            // Assert
            Assert.Equal(expectedRender, render);
        }

        [Fact]
        public void ShouldFormatNestedType()
        {
            // Arrange
            var type = typeof(NestedType);
            var expectedRender = $"{typeof(TypeFormatterTests).FullName}.NestedType";

            // Act
            var render = TypeFormatter.Format(type);

            // Assert
            Assert.Equal(expectedRender, render);
        }

        [Fact]
        public void ShouldFormatNestedGenericType()
        {
            // Arrange
            var type = typeof(NestedGenericType<int>);
            var expectedRender = $"{typeof(TypeFormatterTests).FullName}.NestedGenericType<int>";

            // Act
            var render = TypeFormatter.Format(type);

            // Assert
            Assert.Equal(expectedRender, render);
        }

        [Theory(Skip = "TODO")]
        [InlineData(typeof(Dictionary<int, string>.KeyCollection), "Dictionary<int, string>.KeyCollection")]
        [InlineData(typeof(DoubleNestedGenericType<int>.InDoubleNestedGenericType<string>), "ExpressionCompilation.Tests.TypeFormatterTests.DoubleNestedGenericType<int>.InDoubleNestedGenericType<string>")]
        public void ShouldFormatComplexGenericType(Type type, string expectedRender)
        {
            // Act
            var render = TypeFormatter.Format(type);

            // Assert
            Assert.Equal(expectedRender, render);
        }

        private sealed class NestedType
        {
        }

        // ReSharper disable UnusedTypeParameter
        private sealed class NestedGenericType<T>
        {
        }

        [UsedImplicitly]
        private sealed class DoubleNestedGenericType<TOne>
        {
            public sealed class InDoubleNestedGenericType<TTwo>
            {
            }
        }
    }
}