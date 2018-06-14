using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ExpressionCompilation.Tests.TestData;
using FluentAssertions;
using JetBrains.Annotations;
using Xunit;

namespace ExpressionCompilation.Tests
{
    public sealed class OpCodeCoverage
    {
        private static readonly HashSet<string> CopiedInstructions = new HashSet<string>();

        public OpCodeCoverage()
        {
            if (CopiedInstructions.Count == 0)
            {
                ProcessAllSamples();
            }
        }

        [NotNull]
        [UsedImplicitly]
        public static IEnumerable<object[]> GetAllOpCodeTypes()
        {
            return typeof(OpCodes).GetFields(BindingFlags.Static | BindingFlags.Public)
                .Select(field => new[] { field.GetValue(null) });
        }

        [Theory(Skip = "Run manually, not all IL instructions were tested")]
        [MemberData(nameof(GetAllOpCodeTypes))]
        public void ShouldCovereAllInstructions(OpCode opCode)
        {
            CopiedInstructions.Contains(opCode.Name).Should().BeTrue($"OpCodes.{opCode.Name} missing in sample methods.");
        }

        private void ProcessAllSamples()
        {
            foreach (var method in Sample.GetAllSampleMethods())
            {
                CopiedInstructions.UnionWith(
                    method.Body.Instructions
                        .Select(instruction => instruction.OpCode.Name));
            }
        }
    }
}