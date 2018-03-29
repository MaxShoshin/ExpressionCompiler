using System;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Formatter = Microsoft.CodeAnalysis.Formatting.Formatter;

namespace Abacus.Amazonia.ExpressionCompilation
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

        private static string CreateCompilationErrorMessage([NotNull] EmitResult emitResult, [NotNull] SyntaxTree syntaxTree)
        {
            if (emitResult == null) throw new ArgumentNullException(nameof(emitResult));
            if (syntaxTree == null) throw new ArgumentNullException(nameof(syntaxTree));

            var message = new StringBuilder()
                .AppendLine("Compilation failed:");

            foreach (var diagnostic in emitResult.Diagnostics.Where(item => item.Severity == DiagnosticSeverity.Error))
            {
                message
                    .Append(diagnostic.Id)
                    .Append(": ")
                    .AppendLine(diagnostic.GetMessage(CultureInfo.InvariantCulture));
            }

            // HACK: To copy to output necessary dll (Microsoft.CodeAnalysis.CSharp.Workspaces)
            // otherwice we will get 'C# not supported exception'
            var _ = typeof(Microsoft.CodeAnalysis.CSharp.Formatting.CSharpFormattingOptions);
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            _.ToString();

            var code = Formatter.Format(syntaxTree.GetRoot(), new AdhocWorkspace()).ToString();
            message.AppendLine()
                   .AppendLine("Generated code:")
                   .AppendLine()
                   .AppendLine(code);

            return message.ToString();
        }
    }
}