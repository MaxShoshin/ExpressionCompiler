using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mono.Cecil;

namespace ExpressionCompilation
{
    public sealed class ExpressionCompiler
    {
        public const string ExpressionCompilerAssemblyName = "ExpressionCompilerAssembly";

        private const string MethodName = "ExpressionMethod";

        [NotNull] private readonly List<ParameterDef> _parameters = new List<ParameterDef>();
        [NotNull] private readonly List<string> _usings = new List<string>();
        [NotNull] private readonly List<string> _referenceLocations = new List<string>();
        [NotNull] private readonly string _expressionText;

        [NotNull] private Type _returnType = typeof(void);
        [NotNull] private CSharpCompilationOptions _compilerOptions;

        public ExpressionCompiler([NotNull] string expressionText)
        {
            if (expressionText == null) throw new ArgumentNullException(nameof(expressionText));

            _expressionText = expressionText;

            _compilerOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithOptimizationLevel(OptimizationLevel.Release);

            WithReference(typeof(object).Assembly);
            WithReference(typeof(Uri).Assembly);
            WithReference(typeof(HashSet<>).Assembly);
        }

        [NotNull]
        public ExpressionCompiler WithParameter([NotNull] string name, [NotNull] Type parameterType)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (parameterType == null) throw new ArgumentNullException(nameof(parameterType));

            _parameters.Add(new ParameterDef(name.Trim(), parameterType));

            return this;
        }

        [NotNull]
        public ExpressionCompiler Returns([NotNull] Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            _returnType = type;

            return this;
        }

        [NotNull]
        public ExpressionCompiler WithCompilerOptions([NotNull] Func<CSharpCompilationOptions, CSharpCompilationOptions> optionsBuilder)
        {
            if (optionsBuilder == null) throw new ArgumentNullException(nameof(optionsBuilder));

            _compilerOptions = optionsBuilder(_compilerOptions);

            return this;
        }

        [NotNull]
        public ExpressionCompiler WithUsing([NotNull] Type usingType)
        {
            if (usingType == null) throw new ArgumentNullException(nameof(usingType));
            if (usingType.Namespace == null) throw new ArgumentException("Using type has null namespace.", nameof(usingType));

            _usings.Add(usingType.Namespace);

            return this;
        }

        [NotNull]
        public ExpressionCompiler WithReference([NotNull] Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            _referenceLocations.Add(assembly.Location);

            return this;
        }

        [NotNull]
        public TDelegate Compile<TDelegate>()
            where TDelegate : class // Delegate
        {
            return (TDelegate)(object)Compile(typeof(TDelegate));
        }

        [NotNull]
        public Delegate Compile([NotNull] Type delegateType)
        {
            var syntaxTree = CreateSyntaxTree();

            var references = _referenceLocations
                .Distinct()
                .Where(File.Exists)
                .Select(location => MetadataReference.CreateFromFile(location))
                .ToList();

            var compilation = CSharpCompilation.Create(ExpressionCompilerAssemblyName)
                .WithReferences(references)
                .WithOptions(_compilerOptions)
                .AddSyntaxTrees(syntaxTree);

            using (var memoryStream = new MemoryStream())
            {
                var emitResult = compilation.Emit(memoryStream);

                if (!emitResult.Success)
                {
                    throw new CompilationErrorException(emitResult, syntaxTree);
                }

                memoryStream.Position = 0;

                var module = ModuleDefinition.ReadModule(memoryStream);

                var method = module.Types
                    .SelectMany(item => item.Methods)
                    .Single(item => item.Name == MethodName);

                var dynamicMethod = ILCopier.CopyToDynamicMethod(method);
                return dynamicMethod.CreateDelegate(delegateType);
            }
        }

        [NotNull]
        private SyntaxTree CreateSyntaxTree()
        {
            var usingDirectives = _usings
                .Distinct()
                .Select(item => SyntaxFactory.UsingDirective(CreateUsingName(item)))
                .ToArray();

            return SyntaxFactory.SyntaxTree(
                SyntaxFactory.CompilationUnit()
                    .AddUsings(usingDirectives)
                    .AddMembers(SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName("ExpressionCompilerNamespace"))
                        .AddMembers(SyntaxFactory.ClassDeclaration("ExpressionCompilerClass")
                            .AddMembers(CreateSyntaxTreeForMethod()))));
        }

        [NotNull]
        private NameSyntax CreateUsingName([NotNull] string usingImport)
        {
            var parts = usingImport.Split('.');

            NameSyntax last = SyntaxFactory.IdentifierName(parts[0]);

            for (int i = 1; i < parts.Length; i++)
            {
                last = SyntaxFactory.QualifiedName(
                    last,
                    SyntaxFactory.IdentifierName(parts[i]));
            }

            return last;
        }

        [NotNull]
        private MemberDeclarationSyntax CreateSyntaxTreeForMethod()
        {
            var returnType = _returnType == typeof(void)
                ? SyntaxFactory.ParseTypeName("void")
                : SyntaxFactory.ParseTypeName(_returnType.FullName);

            return SyntaxFactory.MethodDeclaration(returnType, MethodName)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                .AddParameterListParameters(_parameters
                    .Select(parameterDef => SyntaxFactory
                        .Parameter(SyntaxFactory.Identifier(parameterDef.Name))
                        .WithType(SyntaxFactory.ParseTypeName(TypeFormatter.Format(parameterDef.Type))))
                    .ToArray())
                .WithBody(SyntaxFactory.Block(SyntaxFactory.ReturnStatement(SyntaxFactory.ParseExpression(_expressionText))));
        }

        private sealed class ParameterDef
        {
            public ParameterDef([NotNull] string name, [NotNull] Type type)
            {
                if (name == null) throw new ArgumentNullException(nameof(name));
                if (type == null) throw new ArgumentNullException(nameof(type));

                Name = name;
                Type = type;
            }

            [NotNull] public string Name { get; }

            [NotNull] public Type Type { get; }
        }
    }
}