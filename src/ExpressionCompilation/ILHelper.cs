using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using JetBrains.Annotations;
using Mono.Cecil;
using Mono.Cecil.Cil;

using OpCode = System.Reflection.Emit.OpCode;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace ExpressionCompilation
{
    internal sealed class ILHelper
    {
        private const string Ctor = ".ctor";

        [NotNull] private static readonly IReadOnlyDictionary<Code, OpCode> OpCodesMapping = CreateOpCodesMapping();

        [NotNull] private readonly ILGenerator _ilGenerator;
        [NotNull] private readonly Dictionary<int, Label> _labels = new Dictionary<int, Label>();
        [NotNull] private readonly Dictionary<int, LocalBuilder> _variables = new Dictionary<int, LocalBuilder>();

        [CanBeNull] private readonly Action<int, string, object> _logger;

        private ILHelper([NotNull] ILGenerator ilGenerator, [CanBeNull] Action<int, string, object> logger)
        {
            _ilGenerator = ilGenerator ?? throw new ArgumentNullException(nameof(ilGenerator));
            _logger = logger;
        }

        [NotNull]
        public static DynamicMethod CopyToDynamicMethod([NotNull] MethodDefinition method, [CanBeNull] Action<int, string, object> logger = null)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));

            var dynamicMethod = new DynamicMethod(
                method.Name,
                method.ReturnType.GetRuntimeType(),
                method.Parameters.Select(param => param.ParameterType.GetRuntimeType()).ToArray(),
                true);

            dynamicMethod.InitLocals = method.Body.InitLocals;

            var ilGenerator = dynamicMethod.GetILGenerator();

            var helper = new ILHelper(ilGenerator, logger);
            helper.CopyFrom(method);

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
                if (code == Code.No || code == Code.Calli || code == Code.Ldtoken)
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

        private OpCode ConvertOpCode(Mono.Cecil.Cil.OpCode instructionOpCode)
        {
            if (!OpCodesMapping.TryGetValue(instructionOpCode.Code, out var opCode))
            {
                throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, "Instruction: {0} not supported.", instructionOpCode.Code));
            }

            return opCode;
        }

        private void CopyFrom([NotNull] MethodDefinition fromMethod)
        {
            foreach (var instruction in fromMethod.Body.Instructions)
            {
                _labels.Add(instruction.Offset, _ilGenerator.DefineLabel());
            }

            foreach (var variable in fromMethod.Body.Variables)
            {
                _variables.Add(variable.Index, _ilGenerator.DeclareLocal(variable.VariableType.GetRuntimeType(), variable.IsPinned));
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
                Emit(opCode);
                return;
            }

            // All OpCodes and their operand types can be found at:
            // https://github.com/jbevain/cecil/blob/master/Mono.Cecil.Cil/CodeReader.cs
            Emit(opCode, (dynamic)operand);
        }

        // ReSharper disable once UnusedParameter.Local
        private void Emit(OpCode opCode, [NotNull] object operand)
        {
            throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, "Operand with type {0} is not supported.", operand.GetType().Name));
        }

        // ReSharper disable once UnusedParameter.Local
        private void Emit(OpCode opCode, IMetadataTokenProvider metadataTokenProvider)
        {
            throw new NotSupportedException("IMetadataTokenProvider instructions (Ldtoken) are not supported.");
        }

        // ReSharper disable once UnusedParameter.Local
        private void Emit(OpCode opCode, CallSite callSite)
        {
            throw new NotSupportedException("CallSite instructions (Calli) are not supported.");
        }

        private void Emit(OpCode opCode, [NotNull] VariableDefinition variableDefinition)
        {
            Emit(opCode, _variables[variableDefinition.Index]);
        }

        private void Emit(OpCode opCode, [NotNull] Instruction instructionRef)
        {
            var label = _labels[instructionRef.Offset];

            Emit(opCode, label);
        }

        private void Emit(OpCode opCode, [NotNull] Instruction[] instructionRefs)
        {
            var labels = instructionRefs
                .Select(instructionRef => _labels[instructionRef.Offset])
                .ToArray();

            Emit(opCode, labels);
        }

        private void Emit(OpCode opCode, [NotNull] TypeReference typeReference)
        {
            var type = typeReference.GetRuntimeType();
            Emit(opCode, type);
        }

        private void Emit(OpCode opCode, [NotNull] FieldReference fieldReference)
        {
            var type = fieldReference.DeclaringType.GetRuntimeType();
            var field = type.GetField(fieldReference.Name);

            Emit(opCode, field);
        }

        private void Emit(OpCode opCode, [NotNull] MethodReference methodRefernce)
        {
            var type = methodRefernce.DeclaringType.GetRuntimeType();
            var parameterTypes = methodRefernce.Parameters.Select(param => param.ParameterType.GetRuntimeType()).ToArray();

            if (methodRefernce.Name == Ctor)
            {
                var constructor = type.GetConstructor(parameterTypes);
                if (constructor == null)
                {
                    throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, "Constructor {0} not found", methodRefernce.ToString()));
                }

                Emit(opCode, constructor);
                return;
            }

            var method = type.GetMethod(methodRefernce.Name, parameterTypes);
            if (method == null)
            {
                throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, "Method: {0} not found", methodRefernce.ToString()));
            }

            Emit(opCode, method);
        }

        private void Emit(OpCode opCode, [NotNull] Type op)
        {
            _logger?.Invoke(_ilGenerator.ILOffset, opCode.Name, op);
            _ilGenerator.Emit(opCode, op);
        }

        private void Emit(OpCode opCode, [NotNull] FieldInfo op)
        {
            _logger?.Invoke(_ilGenerator.ILOffset, opCode.Name, op);
            _ilGenerator.Emit(opCode, op);
        }

        private void Emit(OpCode opCode, [NotNull] ConstructorInfo op)
        {
            _logger?.Invoke(_ilGenerator.ILOffset, opCode.Name, op);
            _ilGenerator.Emit(opCode, op);
        }

        private void Emit(OpCode opCode)
        {
            _logger?.Invoke(_ilGenerator.ILOffset, opCode.Name, null);
            _ilGenerator.Emit(opCode);
        }

        private void Emit(OpCode opCode, [NotNull] LocalBuilder localVariable)
        {
            _logger?.Invoke(_ilGenerator.ILOffset, opCode.Name, string.Format(CultureInfo.InvariantCulture, "{1} var{0:0000}", localVariable.LocalIndex, localVariable.LocalType?.Name ?? string.Empty));
            _ilGenerator.Emit(opCode, localVariable);
        }

        private void Emit(OpCode opCode, Label label)
        {
            if (_logger != null)
            {
                var offset = _labels.Where(keyValue => keyValue.Value == label).Select(item => item.Key).First();

                _logger.Invoke(_ilGenerator.ILOffset, opCode.Name, string.Format(CultureInfo.InvariantCulture, "Lbl{0:0000}-wrong", offset));
            }

            _ilGenerator.Emit(opCode, label);
        }

        private void Emit(OpCode opCode, [NotNull] Label[] labels)
        {
            _logger?.Invoke(_ilGenerator.ILOffset, opCode.Name, string.Format(CultureInfo.InvariantCulture, "label count: {0}", labels.Length));

            _ilGenerator.Emit(opCode, labels);
        }


        private void Emit(OpCode opCode, [NotNull] MethodInfo op)
        {
            _logger?.Invoke(_ilGenerator.ILOffset, opCode.Name, op);
            _ilGenerator.Emit(opCode, op);
        }

        private void Emit(OpCode opCode, double op)
        {
            _logger?.Invoke(_ilGenerator.ILOffset, opCode.Name, op);
            _ilGenerator.Emit(opCode, op);
        }

        private void Emit(OpCode opCode, float op)
        {
            _logger?.Invoke(_ilGenerator.ILOffset, opCode.Name, op);
            _ilGenerator.Emit(opCode, op);
        }

        private void Emit(OpCode opCode, byte op)
        {
            _logger?.Invoke(_ilGenerator.ILOffset, opCode.Name, op);
            _ilGenerator.Emit(opCode, op);
        }

        private void Emit(OpCode opCode, int op)
        {
            _logger?.Invoke(_ilGenerator.ILOffset, opCode.Name, op);
            _ilGenerator.Emit(opCode, op);
        }

        private void Emit(OpCode opCode, [NotNull] string op)
        {
            _logger?.Invoke(_ilGenerator.ILOffset, opCode.Name, op);
            _ilGenerator.Emit(opCode, op);
        }

        private void Emit(OpCode opCode, long op)
        {
            _logger?.Invoke(_ilGenerator.ILOffset, opCode.Name, op);
            _ilGenerator.Emit(opCode, op);
        }
    }
}