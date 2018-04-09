using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Formatter = Microsoft.CodeAnalysis.Formatting.Formatter;

namespace ExpressionCompilation
{
    [Serializable]
    public sealed class CompilationErrorException : Exception
    {
        public CompilationErrorException()
        {
        }

        public CompilationErrorException(string message) : base(message)
        {
        }

        public CompilationErrorException([NotNull] EmitResult emitResult, [NotNull] SyntaxTree syntaxTree)
            :this(CreateCompilationErrorMessage(emitResult, syntaxTree))
        {
        }

        public CompilationErrorException(string message, Exception innerException) : base(message, innerException)
        {
        }

        private CompilationErrorException([NotNull] SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        [NotNull]
        private static string CreateCompilationErrorMessage([NotNull] EmitResult emitResult, [NotNull] SyntaxTree syntaxTree)
        {
            if (emitResult == null) throw new ArgumentNullException(nameof(emitResult));
            if (syntaxTree == null) throw new ArgumentNullException(nameof(syntaxTree));
            if (emitResult.Success) throw new ArgumentException("EmitResult should contain failed compilation result.", nameof(emitResult));

            var message = new StringBuilder()
                .AppendLine("Compilation failed:");

            foreach (var diagnostic in emitResult.Diagnostics.Where(item => item.Severity == DiagnosticSeverity.Error))
            {
                message
                    .Append(diagnostic.Id)
                    .Append(": ")
                    .AppendLine(diagnostic.GetMessage(CultureInfo.InvariantCulture));
            }

            var code = Formatter.Format(syntaxTree.GetRoot(), new AdhocWorkspace()).ToString();
            message.AppendLine()
                   .AppendLine("Generated code:")
                   .AppendLine()
                   .AppendLine(code);

            return message.ToString();
        }

        // HACK: To copy to output necessary dll (Microsoft.CodeAnalysis.CSharp.Workspaces)
        // otherwice we will get 'C# not supported exception'
        [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
        [UsedImplicitly]
        private sealed class Dummy
        {
            public Dummy()
            {
                var unused = typeof(Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions);
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                unused.ToString();
            }
        }
    }
}