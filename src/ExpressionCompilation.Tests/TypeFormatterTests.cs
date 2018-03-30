using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Xunit;

namespace ExpressionCompilation.Tests
{
    public sealed class TypeFormatterTest
    {
        [Fact]
        public void ShouldFormatNameForStandardTypes()
        {
            // Arrange
            var type = typeof(IList);

            // Act
            var render = TypeFormatter.GetFullName(type);

            // Assert
            Assert.Equal("IList", render);
        }

        [Fact]
        public void ShouldFormatFullNameForNotStandardTypes()
        {
            // Arrange
            var type = typeof(TypeFormatterTest);
            var expectedRender = typeof(TypeFormatterTest).FullName;

            // Act
            var render = TypeFormatter.GetFullName(type);

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
            var render = TypeFormatter.GetFullName(type);

            // Assert
            Assert.Equal(expectedRender, render);
        }

        [Theory]
        [InlineData(typeof(int?), "int?")]
        [InlineData(typeof(bool?), "bool?")]
        [InlineData(typeof(decimal?), "decimal?")]
        [InlineData(typeof(KeyValuePair<int?, string>?), "KeyValuePair<int?, string>?")]
        public void ShouldFormatNullableForStructureTypes(Type type, string expectedRender)
        {
            // Act
            var render = TypeFormatter.GetFullName(type);

            // Assert
            Assert.Equal(expectedRender, render);
        }

        [Fact]
        public void ShouldFormatGenericType()
        {
            // Arrange
            var type = typeof(Dictionary<Lazy<TypeFormatterTest>, IList<string>>);
            var expectedRender = $"Dictionary<Lazy<{typeof(TypeFormatterTest).FullName}>, IList<string>>";

            // Act
            var render = TypeFormatter.GetFullName(type);

            // Assert
            Assert.Equal(expectedRender, render);
        }

        [Fact]
        public void ShouldFormatNestedType()
        {
            // Arrange
            var type = typeof(NestedType);
            var expectedRender = $"{typeof(TypeFormatterTest).FullName}.NestedType";

            // Act
            var render = TypeFormatter.GetFullName(type);

            // Assert
            Assert.Equal(expectedRender, render);
        }

        [Fact]
        public void ShouldFormatNestedGenericType()
        {
            // Arrange
            var type = typeof(NestedGenericType<int>);
            var expectedRender = $"{typeof(TypeFormatterTest).FullName}.NestedGenericType<int>";

            // Act
            var render = TypeFormatter.GetFullName(type);

            // Assert
            Assert.Equal(expectedRender, render);
        }

        [Theory(Skip = "TODO")]
        [InlineData(typeof(Dictionary<int, string>.KeyCollection), "Dictionary<int, string>.KeyCollection")]
        [InlineData(typeof(DoubleNestedGenericType<int>.InDoubleNestedGenericType<string>), "Abacus.Tests.Generation.TypeFormatterTest.DoubleNestedGenericType<int>.InDoubleNestedGenericType<string>")]
        public void ShouldFormatComplexGenericType(Type type, string expectedRender)
        {
            // Act
            var render = TypeFormatter.GetFullName(type);

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