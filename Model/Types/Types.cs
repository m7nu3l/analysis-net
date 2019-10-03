﻿// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Model.ThreeAddressCode.Values;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model.Types
{
	public interface IMetadataReference
	{
		ISet<CustomAttribute> Attributes { get; }
	}

	public enum TypeKind
	{
		Unknown,
		ValueType,
		ReferenceType
	}

	public interface IType : IMetadataReference
	{
		TypeKind TypeKind { get; }
	}

	public interface IValueType : IType
	{

	}

	public interface IReferenceType : IType
	{

	}

	public static class PlatformTypes
	{
		private static readonly ICollection<BasicType> platformTypes = new List<BasicType>();

		public static readonly UnknownType Unknown = UnknownType.Value;
		public static readonly BasicType Void = New("mscorlib", "System", "Void", TypeKind.ValueType);
		public static readonly BasicType Boolean = New("mscorlib", "System", "Boolean", TypeKind.ValueType);
		public static readonly BasicType Char = New("mscorlib", "System", "Char", TypeKind.ValueType);
		public static readonly BasicType String = New("mscorlib", "System", "String", TypeKind.ReferenceType);
		public static readonly BasicType Byte = New("mscorlib", "System", "Byte", TypeKind.ValueType);
		public static readonly BasicType SByte = New("mscorlib", "System", "SByte", TypeKind.ValueType);
		public static readonly BasicType Int16 = New("mscorlib", "System", "Int16", TypeKind.ValueType);
		public static readonly BasicType Int32 = New("mscorlib", "System", "Int32", TypeKind.ValueType);
		public static readonly BasicType Int64 = New("mscorlib", "System", "Int64", TypeKind.ValueType);
		public static readonly BasicType UInt16 = New("mscorlib", "System", "UInt16", TypeKind.ValueType);
		public static readonly BasicType UInt32 = New("mscorlib", "System", "UInt32", TypeKind.ValueType);
		public static readonly BasicType UInt64 = New("mscorlib", "System", "UInt64", TypeKind.ValueType);
		public static readonly BasicType Decimal = New("mscorlib", "System", "Decimal", TypeKind.ValueType);
		public static readonly BasicType Single = New("mscorlib", "System", "Single", TypeKind.ValueType);
		public static readonly BasicType Double = New("mscorlib", "System", "Double", TypeKind.ValueType);
		public static readonly BasicType Object = New("mscorlib", "System", "Object", TypeKind.ReferenceType);
		public static readonly BasicType IntPtr = New("mscorlib", "System", "IntPtr", TypeKind.ValueType);
		public static readonly BasicType UIntPtr = New("mscorlib", "System", "UIntPtr", TypeKind.ValueType);
		public static readonly BasicType RuntimeMethodHandle = New("mscorlib", "System", "RuntimeMethodHandle", TypeKind.ValueType);
		public static readonly BasicType RuntimeTypeHandle = New("mscorlib", "System", "RuntimeTypeHandle", TypeKind.ValueType);
		public static readonly BasicType RuntimeFieldHandle = New("mscorlib", "System", "RuntimeFieldHandle", TypeKind.ValueType);
		public static readonly BasicType ArrayLengthType = UInt32;
		public static readonly BasicType SizeofType = UInt32;
		public static readonly BasicType Int8 = SByte;
		public static readonly BasicType UInt8 = Byte;
		public static readonly BasicType Float32 = Single;
		public static readonly BasicType Float64 = Double;

		public static readonly BasicType Enum = New("mscorlib", "System", "Enum", TypeKind.ValueType);		
		public static readonly BasicType ValueType = New("mscorlib", "System", "ValueType", TypeKind.ValueType);
		public static readonly BasicType MulticastDelegate = New("mscorlib", "System", "MulticastDelegate", TypeKind.ReferenceType);
		public static readonly BasicType Delegate = New("mscorlib", "System", "Delegate", TypeKind.ReferenceType);

		public static readonly BasicType PureAttribute = New("mscorlib", "System.Diagnostics.Contracts", "PureAttribute", TypeKind.ReferenceType);
		public static readonly BasicType ICollection = New("mscorlib", "System.Collections", "ICollection", TypeKind.ReferenceType);
        public static readonly BasicType IEnumerable = New("mscorlib", "System.Collections", "IEnumerable", TypeKind.ReferenceType);
        public static readonly BasicType IEnumerator = New("mscorlib", "System.Collections", "IEnumerator", TypeKind.ReferenceType);
		public static readonly BasicType GenericICollection = New("mscorlib", "System.Collections.Generic", "ICollection", TypeKind.ReferenceType, 1);

		public static readonly BasicType Task = New("mscorlib", "System.Threading.Tasks", "Task", TypeKind.ReferenceType);
		public static readonly BasicType GenericTask = New("mscorlib", "System.Threading.Tasks", "Task", TypeKind.ReferenceType, 1);

		public static void Resolve(Host host)
		{
			foreach (var type in platformTypes)
			{
				type.Resolve(host);
			}
		}

		private static BasicType New(string containingAssembly, string containingNamespace, string name, TypeKind kind, int genericParameterCount = 0)
		{
			var result = new BasicType(name, kind)
			{
				ContainingAssembly = new AssemblyReference(containingAssembly),
				ContainingNamespace = containingNamespace,
				GenericParameterCount = genericParameterCount
			};

			//for (ushort i = 0; i < genericParameterCount; ++i)
			//{
			//	var typevar = new GenericParameterReference(GenericParameterKind.Type, i);
			//	result.GenericArguments.Add(typevar);
			//}

			platformTypes.Add(result);
			return result;
		}
	}

	public class UnknownType : IType
	{
		private static UnknownType value;

		private UnknownType() { }

		public static UnknownType Value
		{
			get
			{
				if (value == null) value = new UnknownType();
				return value;
			}
		}

		public TypeKind TypeKind
		{
			get { return TypeKind.Unknown; }
		}

		public ISet<CustomAttribute> Attributes
		{
			get { return null; }
		}

		public override string ToString()
		{
			return "Unknown";
		}
	}

	public interface IBasicType : IType, IGenericReference
	{
		IAssemblyReference ContainingAssembly { get; }
		string ContainingNamespace { get; }
		//IBasicType ContainingType { get; }
		string Name { get; }
		string GenericName { get; }
		//new int GenericParameterCount { get; }
		IList<IType> GenericArguments { get; }
        IBasicType GenericType { get; }
		TypeDefinition ResolvedType { get; }
	}

	public class BasicType : IBasicType
	{
		private Func<TypeDefinition> ResolveType;
		private TypeDefinition resolvedType;

		public ISet<CustomAttribute> Attributes { get; private set; }
		public TypeKind TypeKind { get; set; }
		public IAssemblyReference ContainingAssembly { get; set; }
		public string ContainingNamespace { get; set; }
        public IBasicType ContainingType { get; set; }
		public string Name { get; set; }
		public int GenericParameterCount { get; set; }
		public IList<IType> GenericArguments { get; private set; }
        public IBasicType GenericType { get; set; }

		public BasicType(string name, TypeKind kind = TypeKind.Unknown)
		{
			this.Name = name;
			this.TypeKind = kind;
			this.GenericArguments = new List<IType>();
			this.Attributes = new HashSet<CustomAttribute>();

			this.ResolveType = () =>
			{
				var msg = "Use Resolve method to bind this reference with some host. " +
						  "To bind all platform types use PlatformTypes.Resolve method.";

				throw new InvalidOperationException(msg);
			};
		}

		public string GenericName
		{
			get
			{
				var arguments = string.Empty;

				if (this.GenericArguments.Count > 0)
				{
					arguments = string.Join(", ", this.GenericArguments);
					arguments = string.Format("<{0}>", arguments);
				}
				else if (this.GenericParameterCount > 0)
				{
					arguments = string.Join(", T", Enumerable.Range(1, this.GenericParameterCount));
					arguments = string.Format("<T{0}>", arguments);
				}
				//else if (this.GenericParameterCount > 0)
				//{
				//	var startIndex = this.ContainingType.TotalGenericParameterCount();
				//	arguments = string.Join(", T", Enumerable.Range(startIndex, this.GenericParameterCount));
				//	arguments = string.Format("<T{0}>", arguments);
				//}

				return string.Format("{0}{1}", this.Name, arguments);
			}
		}

		public TypeDefinition ResolvedType
		{
			get
			{
				if (resolvedType == null)
				{
					resolvedType = ResolveType();
				}

				return resolvedType;
			}
		}

		//public TypeDefinition Resolve(Host host)
		//{
		//	this.ResolvedType = host.ResolveReference(this);
		//	return this.ResolvedType;
		//}

		public void Resolve(Host host)
		{
			ResolveType = () => host.ResolveReference(this);
		}

		public override string ToString()
		{
			return this.GenericName;
		}

		public override int GetHashCode()
		{
			return this.Name.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			var other = obj as IBasicType;
			// TODO: Maybe we should also compare the TypeKind?
			var result = other != null &&
						 this.Name == other.Name &&
						 this.GenericParameterCount == other.GenericParameterCount &&
						 this.ContainingNamespace == other.ContainingNamespace &&
                         this.ContainingAssembly.Equals(other.ContainingAssembly) &&
						 this.GenericArguments.SequenceEqual(other.GenericArguments);

			return result;
		}
	}

	#region class IBasicType

	//public class IBasicType : IType
	//{
	//	public ISet<Attribute> Attributes { get; private set; }
	//	public TypeKind TypeKind { get; set; }
	//	public IAssemblyReference Assembly { get; set; }
	//	public string Namespace { get; set; }
	//	public string Name { get; set; }

	//	public IBasicType(string name, TypeKind kind = TypeKind.Unknown)
	//	{
	//		this.Name = name;
	//		this.TypeKind = kind;
	//		this.Attributes = new HashSet<Attribute>();
	//	}

	//	public virtual string FullName
	//	{
	//		get
	//		{
	//			var containingAssembly = string.Empty;
	//			var containingNamespace = string.Empty;

	//			if (this.Assembly != null)
	//			{
	//				containingAssembly = string.Format("[{0}]", this.Assembly.Name);
	//			}

	//			if (!string.IsNullOrEmpty(this.Namespace))
	//			{
	//				containingNamespace = string.Format("{0}.", this.Namespace);
	//			}

	//			return string.Format("{0}{1}{2}", containingAssembly, containingNamespace, this.Name);
	//		}
	//	}

	//	public override string ToString()
	//	{
	//		return this.Name;
	//	}

	//	public override int GetHashCode()
	//	{
	//		return this.Name.GetHashCode();
	//	}

	//	public override bool Equals(object obj)
	//	{
	//		var other = obj as IBasicType;
	//		// TODO: Maybe we should also compare the TypeKind?
	//		var result = other != null &&
	//					 this.Assembly.Equals(other.Assembly) &&
	//					 this.Namespace == other.Namespace &&
	//					 this.Name == other.Name;

	//		return result;
	//	}
	//}

	#endregion
	
	#region class GenericType

	//public class GenericType : IBasicType
	//{
	//	public int GenericParameterCount { get; set; }

	//	public GenericType(string name, TypeKind kind = TypeKind.Unknown)
	//		: base(name, kind)
	//	{
	//	}

	//	public string NonGenericFullName
	//	{
	//		get { return base.FullName; }
	//	}

	//	public override string FullName
	//	{
	//		get
	//		{
	//			var parameters = string.Empty;

	//			if (this.GenericParameterCount > 0)
	//			{
	//				parameters = string.Join(", T", Enumerable.Range(1, this.GenericParameterCount + 1));
	//				parameters = string.Format("<T{0}>", parameters);
	//			}

	//			return string.Format("{0}{1}", this.NonGenericFullName, parameters);
	//		}
	//	}

	//	public override string ToString()
	//	{
	//		var parameters = string.Empty;

	//		if (this.GenericParameterCount > 0)
	//		{
	//			parameters = string.Join(", T", Enumerable.Range(1, this.GenericParameterCount + 1));
	//			parameters = string.Format("<T{0}>", parameters);
	//		}

	//		return string.Format("{0}{1}", base.ToString(), parameters);
	//	}

	//	public override int GetHashCode()
	//	{
	//		return this.Name.GetHashCode();
	//	}

	//	public override bool Equals(object obj)
	//	{
	//		var other = obj as GenericType;

	//		var result = other != null &&
	//					 base.Equals(other) &&
	//					 this.GenericParameterCount == other.GenericParameterCount;

	//		return result;
	//	}
	//}

	#endregion
	
	#region class SpecializedType

	//public class SpecializedType : IType
	//{
	//	public ISet<Attribute> Attributes { get; private set; }
	//	public GenericType GenericType { get; set; }
	//	public IList<IType> GenericArguments { get; private set; }

	//	public SpecializedType(GenericType genericType)
	//	{
	//		this.GenericType = genericType;
	//		this.GenericArguments = new List<IType>();
	//		this.Attributes = new HashSet<Attribute>();
	//	}

	//	public TypeKind TypeKind
	//	{
	//		get { return this.GenericType.TypeKind; }
	//	}

	//	public string NonGenericFullName
	//	{
	//		get { return this.GenericType.NonGenericFullName; }
	//	}

	//	public string FullName
	//	{
	//		get
	//		{
	//			var arguments = string.Empty;

	//			if (this.GenericArguments.Count > 0)
	//			{
	//				arguments = string.Join(", ", this.GenericArguments);
	//				arguments = string.Format("<{0}>", arguments);
	//			}

	//			return string.Format("{0}{1}", this.NonGenericFullName, arguments);
	//		}
	//	}

	//	public override string ToString()
	//	{
	//		var arguments = string.Empty;

	//		if (this.GenericArguments.Count > 0)
	//		{
	//			arguments = string.Join(", ", this.GenericArguments);
	//			arguments = string.Format("<{0}>", arguments);
	//		}

	//		return string.Format("{0}{1}", this.GenericType.Name, arguments);
	//	}

	//	public override int GetHashCode()
	//	{
	//		return this.GenericType.Name.GetHashCode();
	//	}

	//	public override bool Equals(object obj)
	//	{
	//		var other = obj as SpecializedType;

	//		var result = other != null &&
	//					 this.GenericType.Equals(other.GenericType) &&
	//					 this.GenericArguments.SequenceEqual(other.GenericArguments);

	//		return result;
	//	}
	//}

	#endregion
	
	public enum GenericParameterKind
	{
		Type,
		Method
	}

	public interface IGenericParameterReference : IType
	{
		IGenericReference GenericContainer { get; }
		GenericParameterKind Kind { get; }
		ushort Index { get; }
		string Name { get; }
	}

	public class GenericParameterReference : IGenericParameterReference
	{
		public ISet<CustomAttribute> Attributes { get; private set; }
		public IGenericReference GenericContainer { get; set; }
		public GenericParameterKind Kind { get; set; }
		public ushort Index { get; set; }

		public GenericParameterReference(GenericParameterKind kind, ushort index)
		{
			this.Kind = kind;
			this.Index = index;
			this.Attributes = new HashSet<CustomAttribute>();
		}

		public string Name
		{
			get { return GetName(this.Kind, this.Index); }
		}

		public TypeKind TypeKind
		{
			get { return TypeKind.Unknown; }
		}

		public override string ToString()
		{
			return this.Name;
		}

		public override int GetHashCode()
		{
			return this.Name.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			var other = obj as IGenericParameterReference;
			// TODO: Maybe we should also compare the TypeKind?
			var result = other != null &&
						 this.Kind == other.Kind &&
						 this.Index == other.Index;

			return result;
		}

		private static string GetName(GenericParameterKind kind, ushort index)
		{
			string prefix;

			switch (kind)
			{
				case GenericParameterKind.Type: prefix = "!"; break;
				case GenericParameterKind.Method: prefix = "!!"; break;

				default: throw kind.ToUnknownValueException();
			}

			return string.Format("{0}{1}", prefix, index);
		}
	}

	public class GenericParameter : IGenericParameterReference
	{
		public ISet<CustomAttribute> Attributes { get; private set; }
		public TypeKind TypeKind { get; set; }
		public IGenericDefinition GenericContainer { get; set; }
		public GenericParameterKind Kind { get; set; }
		public ushort Index { get; set; }
		public string Name { get; set; }

		public GenericParameter(GenericParameterKind kind, ushort index, string name, TypeKind typeKind = TypeKind.Unknown)
		{
			this.Kind = kind;
			this.Index = index;
			this.Name = name;
			this.TypeKind = typeKind;
			this.Attributes = new HashSet<CustomAttribute>();
		}

		#region IGenericParameterReference members

		IGenericReference IGenericParameterReference.GenericContainer
		{
			get { return this.GenericContainer; }
		}

		#endregion

		public override string ToString()
		{
			return this.Name;
		}

		public override int GetHashCode()
		{
			return this.Name.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			var other = obj as IGenericParameterReference;
			// TODO: Maybe we should also compare the TypeKind?
			var result = other != null &&
						 this.Kind == other.Kind &&
						 this.Index == other.Index;
						 //this.Name == other.Name;

			return result;
		}
	}

	public class FunctionPointerType : IReferenceType
	{
		public ISet<CustomAttribute> Attributes { get; private set; }
		public IType ReturnType { get; set; }
		public IList<IMethodParameterReference> Parameters { get; private set; }
		public bool IsStatic { get; set; }

		// TODO: Not sure if we should add GenericParameterCount property.

		public FunctionPointerType(IType returnType)
		{
			this.ReturnType = returnType;
			this.Parameters = new List<IMethodParameterReference>();
			this.Attributes = new HashSet<CustomAttribute>();
		}

		public FunctionPointerType(IMethodReference method)
			: this(method.ReturnType)
		{
			this.IsStatic = method.IsStatic;
			this.Parameters.AddRange(method.Parameters);
		}

		public TypeKind TypeKind
		{
			get { return TypeKind.ReferenceType; }
		}

		public override string ToString()
		{
			var result = new StringBuilder();
			var parameters = string.Join(", ", this.Parameters);

			if (this.IsStatic)
			{
				result.Append("static ");
			}
			
			result.Append(this.ReturnType);
			result.AppendFormat("({0})", parameters);
			return result.ToString();
		}

		public override int GetHashCode()
		{
			var result = this.ReturnType.GetHashCode() ^
						 this.IsStatic.GetHashCode() ^
						 this.Parameters.Aggregate(0, (hc, p) => hc ^ p.GetHashCode());

			return result;
		}

		public override bool Equals(object obj)
		{
			var other = obj as FunctionPointerType;

			var result = other != null &&
						 this.IsStatic == other.IsStatic &&
						 this.ReturnType.Equals(other.ReturnType) &&
						 this.Parameters.SequenceEqual(other.Parameters);

			return result;
		}
	}

	public class PointerType : IReferenceType
	{
		public ISet<CustomAttribute> Attributes { get; private set; }
		public IType TargetType { get; set; }

		public PointerType(IType targetType)
		{
			this.TargetType = targetType;
			this.Attributes = new HashSet<CustomAttribute>();
		}

		public TypeKind TypeKind
		{
			get { return TypeKind.ReferenceType; }
		}

		public override string ToString()
		{
			return string.Format("{0}*", this.TargetType);
		}

		public override int GetHashCode()
		{
			return this.TargetType.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			var other = obj as PointerType;

			var result = other != null &&
						 this.TargetType.Equals(other.TargetType);

			return result;
		}
	}

	public class ArrayType : IReferenceType
	{
		public ISet<CustomAttribute> Attributes { get; private set; }
		public IType ElementsType { get; set; }
		public uint Rank { get; set; }

		public ArrayType(IType elementsType, uint rank = 1)
		{
			this.ElementsType = elementsType;
			this.Rank = rank;
			this.Attributes = new HashSet<CustomAttribute>();
		}

		public TypeKind TypeKind
		{
			get { return TypeKind.ReferenceType; }
		}

		public bool IsVector
		{
			get { return this.Rank == 1; }
		}

		public override string ToString()
		{
			var rank = new string(',', (int)this.Rank - 1);
			return string.Format("{0}[{1}]", this.ElementsType, rank);
		}

		public override int GetHashCode()
		{
			return this.ElementsType.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			var other = obj as ArrayType;

			var result = other != null &&
						 this.ElementsType.Equals(other.ElementsType);

			return result;
		}
	}
}
