using System;
using ExpressionCompilation.Tests.TestData;
using JetBrains.Annotations;
using Xunit;

namespace ExpressionCompilation.Tests
{
    public sealed class ILCopierTests
    {
        [Theory]
        [MemberData(nameof(Sample.GetSampleMethodNames), MemberType = typeof(Sample))]
        public void ShouldCopyILCode([NotNull] string sampleName)
        {
            if (sampleName == null) throw new ArgumentNullException(nameof(sampleName));

            var method = Sample.GetMethod(sampleName);

            var dynamicMethod = ILCopier.CopyToDynamicMethod(method);
            var action = (Func<bool>)dynamicMethod.CreateDelegate(typeof(Func<bool>));

            try
            {
                action();
            }
            catch (ExpectedException)
            {
            }
        }
    }
}