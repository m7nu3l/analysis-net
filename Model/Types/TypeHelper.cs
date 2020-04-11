﻿// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model.Types
{
	public enum TypeModifier
	{
		Pointer,
		Array
	}

	public static class TypeHelper
	{
		public static IEnumerable<TypeDefinition> GetAllTypes(this Namespace self)
		{
			var typeContainer = self as ITypeDefinitionContainer;
			var types = typeContainer.GetAllTypes();
			var nestedNamespacesTypes = self.Namespaces.SelectMany(n => n.GetAllTypes());
			var result = types.Union(nestedNamespacesTypes);
			return result;
		}

		public static IEnumerable<TypeDefinition> GetAllTypes(this ITypeDefinitionContainer self)
		{
			var types = self.Types;
			var nestedTypes = self.Types.OfType<ITypeDefinitionContainer>()
										.SelectMany(t => t.GetAllTypes());
			var result = types.Union(nestedTypes);
			return result;
		}

		public static IType TokenType(IMetadataReference token)
		{
			IType type = PlatformType.Unknown;

			if (token is IMethodReference)
			{
				type = PlatformType.RuntimeMethodHandle;
			}
			else if (token is IType)
			{
				type = PlatformType.RuntimeTypeHandle;
			}
			else if (token is IFieldReference)
			{
				type = PlatformType.RuntimeFieldHandle;
			}

			return type;
		}

		public static bool IsScalar(this IType type)
		{
			var result = type.TypeKind == TypeKind.ValueType;

			if (result && type is IBasicType)
			{
				var basicType = type as IBasicType;
				var scalarTypes = new IBasicType[]
				{
					 PlatformType.Void,
					 PlatformType.Boolean,
					 PlatformType.Char,
					 PlatformType.Float32,
					 PlatformType.Float64,
					 PlatformType.Decimal,
					 PlatformType.Int8,
					 PlatformType.Int16,
					 PlatformType.Int32,
					 PlatformType.Int64,
					 PlatformType.UInt8,
					 PlatformType.UInt16,
					 PlatformType.UInt32,
					 PlatformType.UInt64,
					 PlatformType.IntPtr,
					 PlatformType.UIntPtr
				};

				result = scalarTypes.Contains(basicType, BasicTypeDefinitionComparer.Default);
				result = result || basicType.IsEnum();
			}

			return result;
		}

		public static bool IsPointer(this IType type)
		{
			var result = type is PointerType;
			return result;
		}

		public static bool IsStruct(this IType type)
		{
			var result = type.TypeKind == TypeKind.ValueType && !type.IsScalar();
			return result;
		}

		public static bool IsEnum(this IType type)
		{
			var result = type.TypeKind == TypeKind.ValueType;

			if (result && type is IBasicType)
			{
				var basicType = type as IBasicType;
				result = basicType.ResolvedType != null && basicType.ResolvedType.Kind == TypeDefinitionKind.Enum;
			}

			return result;
		}

		public static bool IsContainer(this IType type)
		{
			var result = false;

			//if (type is SpecializedType)
			//{
			//	var specializedType = type as SpecializedType;
			//	type = specializedType.GenericType;
			//}

			//var typedef = TypeHelper.Resolve(type, host);

			var basicType = type as IBasicType;

			if (basicType != null && basicType.ResolvedType != null)
			{
				result = Type1ImplementsType2(basicType.ResolvedType, PlatformType.ICollection);
				result = result || Type1ImplementsType2(basicType.ResolvedType, PlatformType.GenericICollection);
			}

			return result;
		}

		public static bool IsDelegate(IType type)
		{
			var result = false;
			var basicType = type as IBasicType;

			if (basicType != null && basicType.ResolvedType != null)
			{
				result = Type1ImplementsType2(basicType.ResolvedType, PlatformType.MulticastDelegate);
				result = result || Type1ImplementsType2(basicType.ResolvedType, PlatformType.Delegate);
			}

			return result;
		}

		public static IType MergedType(IType type1, IType type2)
		{
			IType result = PlatformType.Object;

			if (type1 is PointerType && type2 is PointerType)
			{
				var pointerType1 = type1 as PointerType;
				var pointerType2 = type2 as PointerType;
				type1 = pointerType1.TargetType;
				type2 = pointerType2.TargetType;

				var targetType = MergedType(type1, type2);
				result = new PointerType(targetType);
			}
			else if (type1 is ArrayType && type2 is ArrayType)
			{
				var arrayType1 = type1 as ArrayType;
				var arrayType2 = type2 as ArrayType;

				if (arrayType1.Rank == arrayType2.Rank)
				{
					type1 = arrayType1.ElementsType;
					type2 = arrayType2.ElementsType;

					var elementsType = MergedType(type1, type2);
					result = new ArrayType(elementsType, arrayType1.Rank);
				}
			}
			else if (type1 is IBasicType && type2 is IBasicType)
			{
				var basicType1 = type1 as IBasicType;
				var basicType2 = type2 as IBasicType;

				result = MergedType(basicType1, basicType2);
			}

			return result;
		}

		/// <summary>
		/// If both type references can be resolved, this returns the merged type of two types as per the verification algorithm in CLR.
		/// Otherwise it returns either type1, or type2 or System.Object, depending on how much is known about either type.
		/// </summary>
		public static IType MergedType(IBasicType type1, IBasicType type2)
		{
			if (TypesAreEquivalent(type1, type2)) return type1;
			if (StackTypesAreEquivalent(type1, type2)) return StackType(type1);

			var typedef1 = type1.ResolvedType;
			var typedef2 = type2.ResolvedType;

			if (typedef1 != null && TypesAreAssignmentCompatible(typedef1, type2)) return type2;
			if (typedef2 != null && TypesAreAssignmentCompatible(typedef2, type1)) return type1;

			var mdcbc = MostDerivedCommonBaseClass(type1, type2);
			if (mdcbc != null) return mdcbc;

			//if (typedef1 != null && typedef2 != null)
			//{
			//	if (TypesAreEquivalent(typedef1, typedef2)) return type1;
			//	//if (StackTypesAreEquivalent(typedef1, typedef2)) return StackType(type1);
			//	if (typedef1 != null && TypesAreAssignmentCompatible(typedef1, type2)) return type2;
			//	if (typedef2 != null && TypesAreAssignmentCompatible(typedef2, type1)) return type1;

			//	var mdcbc = MostDerivedCommonBaseClass(type1, type2);
			//	if (mdcbc != null) return mdcbc;
			//}
			//else

			if (typedef1 != null && Type1ImplementsType2(typedef1, type2)) return type2;
			if (typedef2 != null && Type1ImplementsType2(typedef2, type1)) return type1;

			return PlatformType.Object;
		}

		public static bool TypesAreEquivalent(IType type1, IType type2)
		{
			if (type1 == null || type2 == null) return false;
			if (type1 == type2) return true;
			if (type1.Equals(type2)) return true;
			return false;
		}

		public static bool TypesAreEquivalent(TypeDefinition type1, TypeDefinition type2)
		{
			if (type1 == null || type2 == null) return false;
			if (type1 == type2) return true;
			if (type1.Equals(type2)) return true;
			return false;
		}

		public static bool TypesAreEquivalent(TypeDefinition type1, IType type2)
		{
			if (type1 == null || type2 == null) return false;
			if (type2 is IBasicType && type1.MatchReference(type2 as IBasicType)) return true;
			return false;
		}

		/// <summary>
		/// Returns the stack state type used by the CLR verification algorithm when merging control flow
		/// paths. For example, both signed and unsigned 16-bit integers are treated as the same as signed 32-bit
		/// integers for the purposes of verifying that stack state merges are safe.
		/// </summary>
		public static IType StackType(IType type)
		{
			var basicType = type as IBasicType;
			if (basicType == null) return type;

			// TODO: Improve these comparisons (optimize for performance)!!
			if (basicType.Equals( PlatformType.Boolean) ||
				basicType.Equals( PlatformType.Char) ||
				basicType.Equals( PlatformType.Int8) ||
				basicType.Equals( PlatformType.Int16) ||
				basicType.Equals( PlatformType.Int32) ||
				basicType.Equals( PlatformType.UInt8) ||
				basicType.Equals( PlatformType.UInt16) ||
				basicType.Equals( PlatformType.UInt32))
			{
				return PlatformType.Int32;
			}

			if (basicType.Equals( PlatformType.Int64) ||
				basicType.Equals( PlatformType.UInt64))
			{
				return PlatformType.Int64;
			}

			if (basicType.Equals( PlatformType.Float32) ||
				basicType.Equals( PlatformType.Float64))
			{
				return PlatformType.Float64;
			}

			if (basicType.Equals( PlatformType.IntPtr) ||
				basicType.Equals( PlatformType.UIntPtr))
			{
				return PlatformType.IntPtr;
			}

			if (basicType.ResolvedType != null && basicType.ResolvedType.Kind == TypeDefinitionKind.Enum)
			{
				return StackType(basicType.ResolvedType.UnderlayingType);
			}

			return type;
		}

		/// <summary>
		/// Returns true if the stack state types of the given two types are to be considered equivalent for the purpose of signature matching and so on.
		/// </summary>
		public static bool StackTypesAreEquivalent(IType type1, IType type2)
		{
			var stackType1 = StackType(type1);
			var stackType2 = StackType(type2);

			var result = TypesAreEquivalent(stackType1, stackType2);
			return result;
		}

		public static IList<TypeModifier> TypeModifiers(IType type)
		{
			var result = new List<TypeModifier>();

			while (type != null)
			{
				if (type is PointerType)
				{
					var pointerType = type as PointerType;
					type = pointerType.TargetType;
					result.Add(TypeModifier.Pointer);
				}
				else if (type is ArrayType)
				{
					var arrayType = type as ArrayType;
					type = arrayType.ElementsType;
					result.Add(TypeModifier.Array);
				}
				else
				{
					type = null;
				}
			}

			return result;
		}

		/// <summary>
		/// Returns true if the given type definition, or one of its base types, implements the given interface or an interface
		/// that derives from the given interface.
		/// </summary>
		public static bool Type1ImplementsType2(TypeDefinition type1, IType type2)
		{
			var interfaces = type1.Interfaces;
			var baseType = type1.Base;

			if (interfaces != null)
			{
				foreach (var implementedInterface in interfaces)
				{
					if (TypesAreEquivalent(implementedInterface, type2))
						return true;

					if (implementedInterface.ResolvedType != null &&
						Type1ImplementsType2(implementedInterface.ResolvedType, type2))
						return true;
				}
			}

			if (baseType != null && baseType.ResolvedType != null &&
				Type1ImplementsType2(baseType.ResolvedType, type2))
				return true;

			return false;
		}

		/// <summary>
		/// Returns true if a CLR supplied implicit reference conversion is available to convert a value of the given source type to a corresponding value of the given target type.
		/// </summary>
		public static bool TypesAreAssignmentCompatible(IType sourceType, IType targetType)
		{
			var result = false;

			if (sourceType is PointerType && targetType is PointerType)
			{
				var sourcePointerType = sourceType as PointerType;
				var targetPointerType = targetType as PointerType;
				sourceType = sourcePointerType.TargetType;
				targetType = targetPointerType.TargetType;

				result = TypesAreAssignmentCompatible(sourceType, targetType);
			}
			else if (sourceType is ArrayType && targetType is ArrayType)
			{
				var sourceArrayType = sourceType as ArrayType;
				var targetArrayType = targetType as ArrayType;

				if (sourceArrayType.Rank == targetArrayType.Rank)
				{
					sourceType = sourceArrayType.ElementsType;
					targetType = targetArrayType.ElementsType;

					result = TypesAreAssignmentCompatible(sourceType, targetType);
				}
			}
			else if (sourceType is IBasicType && targetType is IBasicType)
			{
				var sourceBasicType = sourceType as IBasicType;

				if (sourceBasicType.ResolvedType != null)
				{
					result = TypesAreAssignmentCompatible(sourceBasicType.ResolvedType, targetType);
				}
			}
			else if (targetType is IBasicType)
			{
				result = TypesAreEquivalent(targetType, PlatformType.Object);
			}

			return result;
		}

		/// <summary>
		/// Returns true if a CLR supplied implicit reference conversion is available to convert a value of the given source type to a corresponding value of the given target type.
		/// </summary>
		private static bool TypesAreAssignmentCompatible(TypeDefinition sourceType, IType targetType)
		{
			if (TypesAreEquivalent(sourceType, targetType)) return true;
			if (Type1DerivesFromType2(sourceType, targetType)) return true;
			if (Type1ImplementsType2(sourceType, targetType)) return true;
			if (TypesAreEquivalent(targetType, PlatformType.Object)) return true;
			if (Type1IsCovariantWithType2(sourceType, targetType)) return true;

			return false;
		}

		/// <summary>
		/// Returns true if type1 is the same as type2 or if it is derives from type2.
		/// Type1 derives from type2 if the latter is a direct or indirect base class.
		/// </summary>
		public static bool Type1DerivesFromOrIsTheSameAsType2(TypeDefinition type1, IType type2)
		{
			if (TypesAreEquivalent(type1, type2)) return true;
			return Type1DerivesFromType2(type1, type2);
		}

		/// <summary>
		/// Type1 derives from type2 if the latter is a direct or indirect base class.
		/// </summary>
		public static bool Type1DerivesFromType2(TypeDefinition type1, IType type2)
		{
			if (type1 != null && type1.Base != null)
			{
				if (TypesAreEquivalent(type1.Base, type2)) return true;
				if (Type1DerivesFromType2(type1.Base.ResolvedType, type2)) return true;
			}

			return false;
		}

		/// <summary>
		/// Returns true if Type1 is CovariantWith Type2 as per CLR.
		/// </summary>
		public static bool Type1IsCovariantWithType2(IType type1, IType type2)
		{
			var arrayType1 = type1 as ArrayType;
			var arrayType2 = type2 as ArrayType;

			if (arrayType1 == null || arrayType2 == null) return false;
			if (arrayType1.Rank != arrayType2.Rank) return false;

			return TypesAreAssignmentCompatible(arrayType1.ElementsType, arrayType2.ElementsType);
		}

		/// <summary>
		/// Returns the most derived base class that both given types have in common. Returns null if no such class exists.
		/// For example: if either or both are interface types, then the result is null.
		/// A class is considered its own base class for this algorithm, so if type1 derives from type2 the result is type2
		/// and if type2 derives from type1 the result is type1.
		/// </summary>
		public static IType MostDerivedCommonBaseClass(IType type1, IType type2)
		{
			IType result = null;

			var hierarchy1 = GetClassHierarchy(type1);
			var hierarchy2 = GetClassHierarchy(type2);

			foreach (var type in hierarchy1)
			{
				if (hierarchy2.Contains(type))
				{
					result = type;
					break;
				}
			}

			return result;
		}

		public static IEnumerable<IBasicType> GetClassHierarchy(IType type)
		{
			var result = new List<IBasicType>();
			var basicType = type as IBasicType;

			while (basicType != null)
			{
				result.Add(basicType);

				if (basicType.ResolvedType == null) break;

				basicType = basicType.ResolvedType.Base;
			}

			return result;
		}

		public static IType BinaryLogicalOperationType(IType left, IType right)
		{
			IType type = null;

			if (left.Equals( PlatformType.Boolean) && right.Equals( PlatformType.Boolean))
			{
				type = PlatformType.Boolean;
			}
			else
			{
				type = BinarySignedNumericOperationType(left, right);
			}

			return type;
		}

		public static IType BinaryNumericOperationType(IType left, IType right, bool unsigned)
		{
			var type = BinarySignedNumericOperationType(left, right);

			if (unsigned)
			{
				type = UnsignedEquivalent(type);
			}

			return type;
		}

		/// <summary>
		/// If the given type is a signed integer type, return the equivalent unsigned integer type.
		/// Otherwise return the given type.
		/// </summary>
		public static IType UnsignedEquivalent(IType type)
		{
			if (type.Equals( PlatformType.Int8)) return PlatformType.UInt8;
			else if (type.Equals( PlatformType.Int16)) return PlatformType.UInt16;
			else if (type.Equals( PlatformType.Int32)) return PlatformType.UInt32;
			else if (type.Equals( PlatformType.Int64)) return PlatformType.UInt64;
			else if (type.Equals( PlatformType.IntPtr)) return PlatformType.UIntPtr;
			else return type;
		}

		public static IType BinaryUnsignedNumericOperationType(IType left, IType right)
		{
			var type = BinaryNumericOperationType(left, right, true);
			return type;
		}

		public static IType BinarySignedNumericOperationType(IType left, IType right)
		{
			if (left.Equals( PlatformType.Boolean) ||
				left.Equals( PlatformType.Char) ||
				left.Equals( PlatformType.UInt8) ||
				left.Equals( PlatformType.UInt16) ||
				left.Equals( PlatformType.UInt32))
			{
				if (right.Equals( PlatformType.Boolean) ||
					right.Equals( PlatformType.Char) ||
					right.Equals( PlatformType.UInt8) ||
					right.Equals( PlatformType.UInt16) ||
					right.Equals( PlatformType.UInt32))
					return PlatformType.UInt32;

				else if (right.Equals( PlatformType.Int8) ||
					right.Equals( PlatformType.Int16) ||
					right.Equals( PlatformType.Int32))
					// Code generators will tend to make both operands be of the same type.
					// Assume this happened because the right operand is a polymorphic constant.
					return PlatformType.UInt32;

				//The cases below are not expected to happen in practice
				else if (right.Equals( PlatformType.UInt64) ||
					right.Equals( PlatformType.Int64))
					return PlatformType.UInt64;

				else if (right.Equals( PlatformType.UIntPtr) ||
					right.Equals( PlatformType.IntPtr))
					return PlatformType.UIntPtr;

				else if (right.Equals( PlatformType.Float32) ||
					right.Equals( PlatformType.Float64))
					return right;

				else
					// Code generators will tend to make both operands be of the same type.
					// Assume this happened because the right operand is an enum.
					return right;
			}
			else if (left.Equals( PlatformType.Int8) ||
				left.Equals( PlatformType.Int16) ||
				left.Equals( PlatformType.Int32))
			{
				if (right.Equals( PlatformType.Boolean) ||
					right.Equals( PlatformType.Char) ||
					right.Equals( PlatformType.UInt8) ||
					right.Equals( PlatformType.UInt16) ||
					right.Equals( PlatformType.UInt32))
					return PlatformType.UInt32;

				else if (right.Equals( PlatformType.Int8) ||
					right.Equals( PlatformType.Int16) ||
					right.Equals( PlatformType.Int32))
					// Code generators will tend to make both operands be of the same type.
					// Assume this happened because the right operand is a polymorphic constant.
					return PlatformType.Int32;

				// The cases below are not expected to happen in practice
				else if (right.Equals( PlatformType.UInt64) ||
					right.Equals( PlatformType.Int64) ||
					right.Equals( PlatformType.UIntPtr) ||
					right.Equals( PlatformType.IntPtr) ||
					right.Equals( PlatformType.Float32) ||
					right.Equals( PlatformType.Float64))
					return right;

				else
					// Code generators will tend to make both operands be of the same type.
					// Assume this happened because the right operand is an enum.
					return right;
			}
			else if (left.Equals( PlatformType.UInt64))
			{
				if (right.Equals( PlatformType.Boolean) ||
					right.Equals( PlatformType.Char) ||
					right.Equals( PlatformType.UInt8) ||
					right.Equals( PlatformType.UInt16) ||
					right.Equals( PlatformType.UInt32) ||
					right.Equals( PlatformType.UInt64))
					return PlatformType.UInt64;

				else if (right.Equals( PlatformType.Int8) ||
					right.Equals( PlatformType.Int16) ||
					right.Equals( PlatformType.Int32) ||
					right.Equals( PlatformType.Int64))
					// Code generators will tend to make both operands be of the same type.
					// Assume this happened because the right operand is a polymorphic constant.
					return PlatformType.UInt64;

				else if (right.Equals( PlatformType.UIntPtr) ||
					right.Equals( PlatformType.IntPtr) ||
					right.Equals( PlatformType.Float32) ||
					right.Equals( PlatformType.Float64))
					return right;

				else
					// Code generators will tend to make both operands be of the same type.
					// Assume this happened because the right operand is an enum.
					return right;
			}
			else if (left.Equals( PlatformType.Int64))
			{
				if (right.Equals( PlatformType.Boolean) ||
					right.Equals( PlatformType.Char) ||
					right.Equals( PlatformType.UInt8) ||
					right.Equals( PlatformType.UInt16) ||
					right.Equals( PlatformType.UInt32) ||
					right.Equals( PlatformType.UInt64))
					// Code generators will tend to make both operands be of the same type.
					// Assume this happened because the right operand is a polymorphic constant.
					return PlatformType.UInt64;

				else if (right.Equals( PlatformType.Int8) ||
					right.Equals( PlatformType.Int16) ||
					right.Equals( PlatformType.Int32) ||
					right.Equals( PlatformType.Int64))
					return PlatformType.Int64;

				else if (right.Equals( PlatformType.UIntPtr) ||
					right.Equals( PlatformType.IntPtr))
					return PlatformType.IntPtr;

				else if (right.Equals( PlatformType.Float32) ||
					right.Equals( PlatformType.Float64))
					return right;

				else
					// Code generators will tend to make both operands be of the same type.
					// Assume this happened because the right operand is an enum.
					return right;
			}
			else if (left.Equals( PlatformType.UIntPtr))
			{
				if (right.Equals( PlatformType.Boolean) ||
					right.Equals( PlatformType.Char) ||
					right.Equals( PlatformType.UInt8) ||
					right.Equals( PlatformType.UInt16) ||
					right.Equals( PlatformType.UInt32) ||
					right.Equals( PlatformType.UInt64) ||
					right.Equals( PlatformType.UIntPtr))
					return PlatformType.UIntPtr;

				else if (right.Equals( PlatformType.Int8) ||
					right.Equals( PlatformType.Int16) ||
					right.Equals( PlatformType.Int32) ||
					right.Equals( PlatformType.Int64) ||
					right.Equals( PlatformType.IntPtr))
					return PlatformType.UIntPtr;

				else if (right.Equals( PlatformType.Float32) ||
					right.Equals( PlatformType.Float64))
					return right;

				else if (right is IReferenceType)
					return right;
			}
			else if (left.Equals( PlatformType.IntPtr))
			{
				if (right.Equals( PlatformType.Boolean) ||
					right.Equals( PlatformType.Char) ||
					right.Equals( PlatformType.UInt8) ||
					right.Equals( PlatformType.UInt16) ||
					right.Equals( PlatformType.UInt32) ||
					right.Equals( PlatformType.UInt64) ||
					right.Equals( PlatformType.UIntPtr))
					return PlatformType.UIntPtr;

				else if (right.Equals( PlatformType.Int8) ||
					right.Equals( PlatformType.Int16) ||
					right.Equals( PlatformType.Int32) ||
					right.Equals( PlatformType.Int64) ||
					right.Equals( PlatformType.IntPtr))
					return PlatformType.IntPtr;

				else if (right.Equals( PlatformType.Float32) ||
					right.Equals( PlatformType.Float64))
					return right;

				else if (right is IReferenceType)
					return right;
			}
			else if (left.Equals( PlatformType.Float32) ||
					left.Equals( PlatformType.Float64))
				return right;

			else if (left is IReferenceType)
			{
				if (right is IReferenceType)
					return PlatformType.UIntPtr;

				else if (right.Equals( PlatformType.Int8) ||
					right.Equals( PlatformType.Int16) ||
					right.Equals( PlatformType.Int32) ||
					right.Equals( PlatformType.Int64) ||
					right.Equals( PlatformType.Char) ||
					right.Equals( PlatformType.UInt8) ||
					right.Equals( PlatformType.UInt16) ||
					right.Equals( PlatformType.UInt32) ||
					right.Equals( PlatformType.UInt64) ||
					right.Equals( PlatformType.IntPtr) ||
					right.Equals( PlatformType.UIntPtr))
					return left;
			}
			else
			{
				if (right.Equals( PlatformType.Int8) ||
					right.Equals( PlatformType.Int16) ||
					right.Equals( PlatformType.Int32) ||
					right.Equals( PlatformType.Int64) ||
					right.Equals( PlatformType.Boolean) ||
					right.Equals( PlatformType.Char) ||
					right.Equals( PlatformType.UInt8) ||
					right.Equals( PlatformType.UInt16) ||
					right.Equals( PlatformType.UInt32) ||
					right.Equals( PlatformType.UInt64))
					// Assume that the left operand has an enum type.
					return left;

				// Assume they are both enums
				return left;
			}

			return null;
		}
	}
}
