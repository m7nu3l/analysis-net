﻿// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Model.ThreeAddressCode.Instructions;
using Model.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Bytecode = Model.Bytecode;
using Tac = Model.ThreeAddressCode.Instructions;

namespace Model
{
	public static class Extensions
	{
		public static void AddRange<T>(this ICollection<T> self, IEnumerable<T> elements)
		{
			foreach (var element in elements)
			{
				self.Add(element);
			}
		}

		public static void RemoveAll<T>(this ICollection<T> self, IEnumerable<T> elements)
		{
			foreach (var element in elements)
			{
				self.Remove(element);
			}
		}

		public static void SetRange<K, V>(this IDictionary<K, V> self, IEnumerable<KeyValuePair<K, V>> elements)
		{
			foreach (var element in elements)
			{
				self[element.Key] = element.Value;
			}
		}

		public static ISet<T> ToSet<T>(this IEnumerable<T> self)
		{
			return new HashSet<T>(self);
		}

		public static IEnumerable<T> ToEnumerable<T>(this T item)
		{
			yield return item;
		}

		public static UnknownValueException<T> ToUnknownValueException<T>(this T self)
			where T : struct
		{
			return new UnknownValueException<T>(self);
		}

		public static BinaryOperation ToBinaryOperation(this BranchOperation operation)
		{
			switch (operation)
			{
				case BranchOperation.Eq: return BinaryOperation.Eq;
				case BranchOperation.Neq: return BinaryOperation.Neq;
				case BranchOperation.Gt: return BinaryOperation.Gt;
				case BranchOperation.Ge: return BinaryOperation.Ge;
				case BranchOperation.Lt: return BinaryOperation.Lt;
				case BranchOperation.Le: return BinaryOperation.Le;

				default: throw operation.ToUnknownValueException();
			}
		}

		internal static bool BothNullOrMatchReference(this TypeDefinition def, IBasicType @ref)
		{
			return (def == null && @ref == null) ||
				   (def != null && @ref != null && def.MatchReference(@ref));
		}

		public static int TotalGenericParameterCount(this IBasicType type)
		{
			var result = 0;

			while (type != null)
			{
				result += type.GenericParameterCount;
				type = type.ContainingType;
			}

			return result;
		}

		public static string GetMetadataName(this IBasicType type)
		{
			var name = type.Name;

			if (type.GenericParameterCount > 0)
			{
				name = string.Format("{0}´{1}", name, type.GenericParameterCount);
			}

			return name;
		}

		public static string GetFullNameWithAssembly(this IType type)
		{
			var result = string.Empty;

			if (type is IBasicType)
			{
				var basicType = type as IBasicType;
				result = GetFullNameWithAssembly(basicType);
			}
			else if (type is ArrayType)
			{
				var arrayType = type as ArrayType;
				var elementsType = GetFullNameWithAssembly(arrayType.ElementsType);
				result = string.Format("{0}[]", elementsType);
			}
			else if (type is PointerType)
			{
				var pointerType = type as PointerType;
				var targetType = GetFullNameWithAssembly(pointerType.TargetType);
				result = string.Format("{0}*", targetType);
			}

			return result;
		}

		public static string GetFullNameWithAssembly(this IBasicType type)
		{
			var fullName = type.GetFullName();
			var containingAssembly = string.Empty;

			if (type.ContainingAssembly != null)
			{
				containingAssembly = string.Format("[{0}]", type.ContainingAssembly.Name);
			}

			return string.Format("{0}{1}", containingAssembly, fullName);
		}

		public static string GetFullName(this IType type)
		{
			var result = string.Empty;

			if (type is IBasicType)
			{
				var basicType = type as IBasicType;
				result = GetFullName(basicType);
			}
			else if (type is ArrayType)
			{
				var arrayType = type as ArrayType;
				var elementsType = GetFullName(arrayType.ElementsType);
				result = string.Format("{0}[]", elementsType);
			}
			else if (type is PointerType)
			{
				var pointerType = type as PointerType;
				var targetType = GetFullName(pointerType.TargetType);
				result = string.Format("{0}*", targetType);
			}

			return result;
		}

		public static string GetFullName(this IBasicType type)
		{
			var genericTypes = string.Empty;
			var containingNamespace = string.Empty;
			var containingTypes = string.Empty;
			var containingType = type.ContainingType;

			if (!string.IsNullOrEmpty(type.ContainingNamespace))
			{
				containingNamespace = string.Format("{0}.", type.ContainingNamespace);
			}

			while (containingType != null)
			{
				containingTypes = string.Format("{0}{1}.", containingTypes, containingType.Name);
				containingType = containingType.ContainingType;
			}

			if (type.GenericArguments.Count > 0)
			{
				var genericArgumentTypes = type.GenericArguments.Select(a => a.GetFullName());
				var genericArguments = string.Join(", ", genericArgumentTypes);
				genericTypes = string.Format("<{0}>", genericArguments);
			}
			else if (type.GenericParameterCount > 0)
			{
				var genericArguments = string.Join(", T", Enumerable.Range(1, type.GenericParameterCount));
				genericTypes = string.Format("<T{0}>", genericArguments);
			}

			return string.Format("{0}{1}{2}{3}", containingNamespace, containingTypes, type.Name, genericTypes);
		}

		public static string ToSignatureString(this IMethodReference method)
		{
			var result = new StringBuilder();

			if (method.IsStatic)
			{
				result.Append("static ");
			}

			result.AppendFormat("{0} {1}::{2}", method.ReturnType, method.ContainingType.GenericName, method.GenericName);

			var parameters = string.Join(", ", method.Parameters);
			result.AppendFormat("({0})", parameters);

			return result.ToString();
		}

		public static bool MatchType(this IType definitionType, IType referenceType, IDictionary<IType, IType> typeParameterBinding)
		{
			var result = false;

			if (definitionType is IGenericParameterReference)
			{
				IType typeArgument;

				if (typeParameterBinding != null &&
					typeParameterBinding.TryGetValue(definitionType, out typeArgument))
				{
					definitionType = typeArgument;
				}
			}

			result = definitionType.Equals(referenceType);
			return result;
		}
		public static IBasicType Instantiate(this IBasicType type, params IType[] genericArguments)
		{
			return type.Instantiate(genericArguments.AsEnumerable());
		}

		public static MethodReference Instantiate(this IMethodReference method, params IType[] genericArguments)
		{
			return method.Instantiate(genericArguments.AsEnumerable());
		}

		public static MethodReference Instantiate(this IMethodReference method, IEnumerable<IType> genericArguments)
		{
			var result = new MethodReference(method.Name, method.ReturnType)
			{
				IsStatic = method.IsStatic,
				ContainingType = method.ContainingType,
				GenericParameterCount = method.GenericParameterCount,
				GenericMethod = method
			};

			result.Parameters.AddRange(method.Parameters);
			result.GenericArguments.AddRange(genericArguments);
			//result.Resolve(host);
			return result;
		}

		public static Assembly LoadAssemblyWithReferences(this ILoader loader, string fileName)
		{
			var directory = Path.GetDirectoryName(fileName);
			var result = loader.LoadAssembly(fileName);
			var references = new HashSet<IAssemblyReference>(result.References);

			while (references.Count > 0)
			{
				var reference = references.First();
				references.Remove(reference);

				var assembly = loader.Host.ResolveReference(reference);
				if (assembly != null) continue;

				fileName = string.Format("{0}.dll", reference.Name);
				fileName = Path.Combine(directory, fileName);

				if (!File.Exists(fileName))
				{
					fileName = string.Format("{0}.exe", reference.Name);
					fileName = Path.Combine(directory, fileName);
					if (!File.Exists(fileName)) continue;
				}

				assembly = loader.LoadAssembly(fileName);
				references.UnionWith(assembly.References);
			}

			return result;
		}

		public static IEnumerable<MethodDefinition> GetRootMethods(this Host host)
		{
			var result = host.Assemblies.SelectMany(a => a.GetRootMethods());
			return result;
		}

		public static IEnumerable<MethodDefinition> GetRootMethods(this Assembly assembly)
		{
			var mainSignature = new MethodReference("Main", PlatformType.Void);
			var args = new MethodParameterReference(0, new ArrayType( PlatformType.String));

			mainSignature.IsStatic = true;
			mainSignature.Parameters.Add(args);

			var rootMethods = from t in assembly.RootNamespace.GetAllTypes()
							  from m in t.Members.OfType<MethodDefinition>()
							  where m.Body != null &&
									m.MatchSignature(mainSignature)
							  select m;
			return rootMethods;
		}

		public static bool IsDelegate(this IType type)
		{
			var result = false;

			if (type is IBasicType)
			{
				var basicType = type as IBasicType;

				if (basicType.GenericType != null)
				{
					basicType = basicType.GenericType;
				}

				if (basicType.ResolvedType != null)
				{
					result = basicType.ResolvedType.Kind == TypeDefinitionKind.Delegate;
				}
				else
				{
					var delegateTypes = new HashSet<string>
					{
						"System.Func", "System.Action", "System.Predicate"
					};

					var fullName = basicType.GetFullName();
					result = delegateTypes.Any(name => fullName.StartsWith(name));
				}
			}

			return result;
		}

		#region Control-flow helper methods

		public static bool IsBranch(this IInstruction instruction, out IList<string> targets)
		{
			var result = false;
			targets = null;

			// Bytecode
			if (instruction is Bytecode.BranchInstruction)
			{
				var branch = instruction as Bytecode.BranchInstruction;

				targets = new List<string>() { branch.Target };
				result = true;
			}
			else if (instruction is Bytecode.SwitchInstruction)
			{
				var branch = instruction as Bytecode.SwitchInstruction;

				targets = branch.Targets;
				result = true;
			}
			// TAC
			else if (instruction is Tac.UnconditionalBranchInstruction ||
					 instruction is Tac.ConditionalBranchInstruction)
			{
				var branch = instruction as Tac.BranchInstruction;

				targets = new List<string>() { branch.Target };
				result = true;
			}
			else if (instruction is Tac.SwitchInstruction)
			{
				var branch = instruction as Tac.SwitchInstruction;

				targets = branch.Targets;
				result = true;
			}

			return result;
		}

		public static bool IsExitingMethod(this IInstruction instruction)
		{
			var result = false;

			// Bytecode
			if (instruction is Bytecode.BasicInstruction)
			{
				var basic = instruction as Bytecode.BasicInstruction;

				result = basic.Operation == Bytecode.BasicOperation.Return ||
						 basic.Operation == Bytecode.BasicOperation.Throw ||
						 basic.Operation == Bytecode.BasicOperation.Rethrow;
			}
			// TAC
			else
			{
				result = instruction is Tac.ReturnInstruction ||
						 instruction is Tac.ThrowInstruction;
			}

			return result;
		}

		public static bool CanFallThroughNextInstruction(this IInstruction instruction)
		{
			var result = false;

			// Bytecode
			if (instruction is Bytecode.BranchInstruction)
			{
				var branch = instruction as Bytecode.BranchInstruction;

				result = branch.Operation == Bytecode.BranchOperation.Branch ||
						 branch.Operation == Bytecode.BranchOperation.Leave;
			}
			// TAC
			else
			{
				result = instruction is Tac.UnconditionalBranchInstruction;
			}

			result = result || IsExitingMethod(instruction);
			return !result;
		}

		#endregion

		public static void RemoveUnusedLabels(this MethodBody body)
		{
			var usedLabels = new HashSet<string>();

			foreach (var protectedBlock in body.ExceptionInformation)
			{
				usedLabels.Add(protectedBlock.Start);
				usedLabels.Add(protectedBlock.End);
				usedLabels.Add(protectedBlock.Handler.Start);
				usedLabels.Add(protectedBlock.Handler.End);
			}

			foreach (var instruction in body.Instructions)
			{
				IList<string> targets;
				var isBranch = instruction.IsBranch(out targets);

				if (isBranch)
				{
					usedLabels.UnionWith(targets);
				}
			}

			foreach (var instruction in body.Instructions)
			{
				var used = usedLabels.Contains(instruction.Label);

				if (used)
				{
					// This is to also remove duplicated labels,
					// leaving only the label in the first instruction.
					usedLabels.Remove(instruction.Label);
				}
				else
				{
					instruction.Label = null;
				}
			}
		}

		public static string ToDisplayName(this TypeDefinitionKind kind)
		{
			switch (kind)
			{
				case TypeDefinitionKind.Unknown: return "type";
				case TypeDefinitionKind.Class: return "class";
				case TypeDefinitionKind.Interface: return "interface";
				case TypeDefinitionKind.Struct: return "struct";
				case TypeDefinitionKind.Enum: return "enum";
				case TypeDefinitionKind.Delegate: return "delegate";

				default: throw kind.ToUnknownValueException();
			}
		}

		public static TypeKind ToTypeKind(this TypeDefinitionKind kind)
		{
			switch (kind)
			{
				case TypeDefinitionKind.Unknown: return TypeKind.Unknown;
				case TypeDefinitionKind.Class:
				case TypeDefinitionKind.Interface:
				case TypeDefinitionKind.Delegate: return TypeKind.ReferenceType;
				case TypeDefinitionKind.Struct:
				case TypeDefinitionKind.Enum: return TypeKind.ValueType;

				default: throw kind.ToUnknownValueException();
			}
		}
	}
}
