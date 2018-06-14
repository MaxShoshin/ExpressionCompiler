using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Mono.Cecil;

namespace ExpressionCompilation.Tests.TestData
{
    public static class Sample
    {
        private static readonly TypeDefinition SampleType =
            ModuleDefinition.ReadModule(Assembly.GetExecutingAssembly().Location)
                .Types
                .First(type => type.Name == nameof(SamplesForCopy));

        [NotNull]
        [UsedImplicitly]
        public static IEnumerable<object[]> GetSampleMethodNames()
        {
            return GetAllSampleMethods()
                .Select(method => new[] { method.Name });
        }

        [NotNull]
        public static MethodDefinition GetMethod([NotNull] string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            return SampleType.Methods.FirstOrDefault(item => item.Name == name)
                ?? throw new InvalidOperationException($"Method {name} not found in {SampleType.Name}.");
        }

        [NotNull]
        public static IReadOnlyList<MethodDefinition> GetAllSampleMethods()
        {
            return SampleType.Methods
                .Where(method => method.ReturnType.IsPrimitive)
                .ToList();
        }
    }
}