using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection.Emit;
using JetBrains.Annotations;
using Mono.Cecil;
using Mono.Cecil.Cil;
using OpCode = System.Reflection.Emit.OpCode;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace NeedfulThings.ExpressionCompilation
{
    internal sealed class ILCopier
    {
        private const string Ctor = ".ctor";

        [NotNull] private static readonly IReadOnlyDictionary<Code, OpCode> OpCodesMapping = CreateOpCodesMapping();

        [NotNull] private readonly ILGenerator _ilGenerator;
        private readonly TypeReferenceMapper _typeMapper;
        private readonly MethodReferenceMapper _methodMapper;
        [NotNull] private readonly Dictionary<int, Label> _labels = new Dictionary<int, Label>();
        [NotNull] private readonly Dictionary<int, LocalBuilder> _variables = new Dictionary<int, LocalBuilder>();

        private ILCopier([NotNull] ILGenerator ilGenerator, [NotNull] TypeReferenceMapper typeMapper, [NotNull] MethodReferenceMapper methodMapper)
        {
            _ilGenerator = ilGenerator ?? throw new ArgumentNullException(nameof(ilGenerator));
            _typeMapper = typeMapper ?? throw new ArgumentNullException(nameof(typeMapper));
            _methodMapper = methodMapper ?? throw new ArgumentNullException(nameof(methodMapper));
        }

        [NotNull]
        public static DynamicMethod CopyToDynamicMethod([NotNull] MethodDefinition method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));

            var typeMapper = new TypeReferenceMapper();
            var methodMapper = new MethodReferenceMapper(typeMapper);

            var dynamicMethod = new DynamicMethod(
                method.Name,
                typeMapper.GetRuntimeType(method.ReturnType),
                method.Parameters.Select(param => typeMapper.GetRuntimeType(param.ParameterType)).ToArray(),
                true);

            dynamicMethod.InitLocals = method.Body.InitLocals;

            var ilGenerator = dynamicMethod.GetILGenerator();

            var copier = new ILCopier(ilGenerator, typeMapper, methodMapper);
            copier.CopyFrom(method);

            return dynamicMethod;
        }

        [NotNull]
        private static IReadOnlyDictionary<Code, OpCode> CreateOpCodesMapping()
        {
            var mapping = new Dictionary<Code, OpCode>();
            var codeValues = (Code[])Enum.GetValues(typeof(Code));

            foreach (var code in codeValues)
            {
                // Not support such instruction
                // These instructions are not supported also: code == Code.Calli
                // there is no skip here for it as we would like to get exception with details
                if (code == Code.No || code == Code.Ldtoken)
                {
                    continue;
                }

                var codeName = code.ToString();

                // HACK: Change branch jumps in short form to long (as ILGenerator not correctly support it)
                if (codeName.StartsWith("B", StringComparison.Ordinal) && codeName.EndsWith("_S", StringComparison.Ordinal))
                {
                    codeName = codeName.Substring(0, codeName.Length - 2);
                }

                // Difference in names
                if (code == Code.Ldelem_Any)
                {
                    codeName = nameof(OpCodes.Ldelem);
                }
                else if (code == Code.Stelem_Any)
                {
                    codeName = nameof(OpCodes.Stelem);
                }
                else if (code == Code.Tail)
                {
                    codeName = nameof(OpCodes.Tailcall);
                }

                var fieldInfo = typeof(OpCodes).GetField(codeName);
                if (fieldInfo == null)
                {
                    throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, "OpCodes.{0} not found.", codeName));
                }

                mapping[code] = (OpCode)fieldInfo.GetValue(null);
            }

            return mapping;
        }

        private static OpCode ConvertOpCode(Mono.Cecil.Cil.OpCode instructionOpCode)
        {
            OpCode opCode;
            if (!OpCodesMapping.TryGetValue(instructionOpCode.Code, out opCode))
            {
                throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, "Instruction: {0} not supported.", instructionOpCode.Code));
            }

            return opCode;
        }

        private void CopyFrom([NotNull] MethodDefinition fromMethod)
        {
            if (fromMethod.Body.HasExceptionHandlers)
            {
                throw new NotSupportedException("Try-catch-finally construction are not supported yet.");
            }

            foreach (var instruction in fromMethod.Body.Instructions)
            {
                _labels.Add(instruction.Offset, _ilGenerator.DefineLabel());
            }

            foreach (var variable in fromMethod.Body.Variables)
            {
                _variables.Add(variable.Index, _ilGenerator.DeclareLocal(_typeMapper.GetRuntimeType(variable.VariableType), variable.IsPinned));
            }

            foreach (var instruction in fromMethod.Body.Instructions)
            {
                var currentLabel = _labels[instruction.Offset];
                _ilGenerator.MarkLabel(currentLabel);

                EmitInstruction(instruction);
            }
        }

        private void EmitInstruction([NotNull] Instruction instruction)
        {
            object operand = instruction.Operand;
            var opCode = ConvertOpCode(instruction.OpCode);

            if (operand == null)
            {
                _ilGenerator.Emit(opCode);
                return;
            }

            // All OpCodes and their operand types can be found at:
            // https://github.com/jbevain/cecil/blob/master/Mono.Cecil.Cil/CodeReader.cs

            // Double dispatching to avoid a lot of ifs by operand types.
            Emit(opCode, (dynamic)operand);
        }

        private void Emit(OpCode opCode, [NotNull] VariableDefinition variableDefinition)
        {
            _ilGenerator.Emit(opCode, _variables[variableDefinition.Index]);
        }

        private void Emit(OpCode opCode, [NotNull] Instruction instructionRef)
        {
            var label = _labels[instructionRef.Offset];

            _ilGenerator.Emit(opCode, label);
        }

        private void Emit(OpCode opCode, [NotNull] Instruction[] instructionRefs)
        {
            var labels = instructionRefs
                .Select(instructionRef => _labels[instructionRef.Offset])
                .ToArray();

            _ilGenerator.Emit(opCode, labels);
        }

        private void Emit(OpCode opCode, [NotNull] TypeReference typeReference)
        {
            var type = _typeMapper.GetRuntimeType(typeReference);
            _ilGenerator.Emit(opCode, type);
        }

        private void Emit(OpCode opCode, [NotNull] FieldReference fieldReference)
        {
            var type = _typeMapper.GetRuntimeType(fieldReference.DeclaringType);
            var field = type.GetField(fieldReference.Name);

            _ilGenerator.Emit(opCode, field);
        }

        private void Emit(OpCode opCode, [NotNull] MethodReference methodReference)
        {
            if (methodReference.Name == Ctor)
            {
                var constructorInfo = _methodMapper.GetConstructor(methodReference);
                _ilGenerator.Emit(opCode, constructorInfo);
                return;
            }

            var method = _methodMapper.GetMethod(methodReference);
            _ilGenerator.Emit(opCode, method);
        }

        private void Emit(OpCode opCode, double op)
        {
            _ilGenerator.Emit(opCode, op);
        }

        private void Emit(OpCode opCode, float op)
        {
            _ilGenerator.Emit(opCode, op);
        }

        private void Emit(OpCode opCode, byte op)
        {
            _ilGenerator.Emit(opCode, op);
        }

        private void Emit(OpCode opCode, int op)
        {
            _ilGenerator.Emit(opCode, op);
        }

        private void Emit(OpCode opCode, [NotNull] string op)
        {
            _ilGenerator.Emit(opCode, op);
        }

        private void Emit(OpCode opCode, long op)
        {
            _ilGenerator.Emit(opCode, op);
        }

        [UsedImplicitly]
        private void Emit(OpCode opCode, [NotNull] object operand)
        {
            throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, "Operand with type {0} is not supported. Instruction: {1}", operand.GetType().Name, opCode));
        }

        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "callSite", Justification = "Not supported. parameter is not used and cannot be removed due dynamic dipatching.")]
        [UsedImplicitly]
        private void Emit(OpCode opCode, CallSite callSite)
        {
            throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, "{0} instructions (Calli) are not supported.", opCode.OpCodeType));
        }
    }
}