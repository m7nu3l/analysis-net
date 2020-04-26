// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

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

    public static class CoreLibraries
    {
        // Names of the libraries that contain the .net fundamental types
        public const string Mscorlib = "mscorlib"; // net framework
        public const string NetStandard = "netstandard"; // net standard
        public const string SystemRuntime = "System.Runtime"; // net core
    }

    // A PlatformType represents a unified way to model .net fundamental types independently of where they are defined. 
    // Their implementations can be in different assemblies depending on the runtime.
    public class PlatformType : IBasicType
    {
        private PlatformType(string name, TypeKind kind = TypeKind.Unknown) 
        {
            this.Name = name;
            this.TypeKind = kind;
            this.GenericArguments = new List<IType>();
            this.Attributes = new HashSet<CustomAttribute>();
        }

        public override string ToString()
        {
            return GenericName;
        }

        public override bool Equals(object obj)
        {
            if (obj is IBasicType basicType)
            {
                return basicType.Name.Equals(this.Name) && basicType.ContainingNamespace.Equals(this.ContainingNamespace)
                    && basicType.ContainingType == null && basicType.GenericParameterCount == this.GenericParameterCount;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        // This is not implemented as an extension as it is for BasicType because
        // PlatformType's constructor is private
        public IBasicType Instantiate(IEnumerable<IType> genericArguments)
        {
            var result = new PlatformType(this.Name, this.TypeKind)
            {
                ContainingAssembly = this.ContainingAssembly,
                ContainingNamespace = this.ContainingNamespace,
                ContainingType = this.ContainingType,
                GenericParameterCount = this.GenericParameterCount,
                GenericType = this
            };

            result.GenericArguments.AddRange(genericArguments);
            // A platform type can't be resolved
            // You should call ToImplementation after Instantiate
            // result.Resolve(() => this.ResolvedType);
            return result;
        }
        public BasicType ToImplementation(string coreLibrary)
        {
            // the user must do result.Resolve(host) so it can be resolvable!
            var result = new BasicType(this.Name, this.TypeKind)
            {
                ContainingAssembly = new AssemblyReference(coreLibrary),
                ContainingNamespace = this.ContainingNamespace,
                GenericParameterCount = this.GenericParameterCount
            };

            return result;
        }

        private static PlatformType New(string containingNamespace, string name, TypeKind kind, int genericParameterCount = 0)
        {
            var result = new PlatformType(name, kind)
            {
                // A PlatformType doesn't belong to any real assembly
                // It is a handy way to represent every possible implementation of it.
                ContainingAssembly = new AssemblyReference(""),
                ContainingNamespace = containingNamespace,
                GenericParameterCount = genericParameterCount
            };

            return result;
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
                return string.Format("{0}{1}", this.Name, arguments);
            }
        }
        public ISet<CustomAttribute> Attributes { get; private set; }
		public TypeKind TypeKind { get; set; }
		public IAssemblyReference ContainingAssembly { get; set; }
		public string ContainingNamespace { get; set; }
        public IBasicType ContainingType { get; set; }
		public string Name { get; set; }
		public int GenericParameterCount { get; set; }
		public IList<IType> GenericArguments { get; private set; }
        public IBasicType GenericType { get; set; }

        public TypeDefinition ResolvedType => throw new NotImplementedException("PlatformType can't be resolved because it doesn't belong to any assembly.");
        
        public static readonly UnknownType Unknown = UnknownType.Value;
        public static readonly PlatformType Void = New("System", "Void", TypeKind.ValueType);
        public static readonly PlatformType Boolean = New("System", "Boolean", TypeKind.ValueType);
        public static readonly PlatformType Char = New("System", "Char", TypeKind.ValueType);
        public static readonly PlatformType String = New("System", "String", TypeKind.ReferenceType);
        public static readonly PlatformType Byte = New("System", "Byte", TypeKind.ValueType);
        public static readonly PlatformType SByte = New("System", "SByte", TypeKind.ValueType);
        public static readonly PlatformType Int16 = New("System", "Int16", TypeKind.ValueType);
        public static readonly PlatformType Int32 = New("System", "Int32", TypeKind.ValueType);
        public static readonly PlatformType Int64 = New("System", "Int64", TypeKind.ValueType);
        public static readonly PlatformType UInt16 = New("System", "UInt16", TypeKind.ValueType);
        public static readonly PlatformType UInt32 = New("System", "UInt32", TypeKind.ValueType);
        public static readonly PlatformType UInt64 = New("System", "UInt64", TypeKind.ValueType);
        public static readonly PlatformType Decimal = New("System", "Decimal", TypeKind.ValueType);
        public static readonly PlatformType Single = New("System", "Single", TypeKind.ValueType);
        public static readonly PlatformType Double = New("System", "Double", TypeKind.ValueType);
        public static readonly PlatformType Object = New("System", "Object", TypeKind.ReferenceType);
        public static readonly PlatformType IntPtr = New("System", "IntPtr", TypeKind.ValueType);
        public static readonly PlatformType UIntPtr = New("System", "UIntPtr", TypeKind.ValueType);
        public static readonly PlatformType RuntimeMethodHandle = New("System", "RuntimeMethodHandle", TypeKind.ValueType);
        public static readonly PlatformType RuntimeTypeHandle = New("System", "RuntimeTypeHandle", TypeKind.ValueType);
        public static readonly PlatformType RuntimeFieldHandle = New("System", "RuntimeFieldHandle", TypeKind.ValueType);
        public static readonly PlatformType ArrayLengthType = UInt32;
        public static readonly PlatformType SizeofType = UInt32;
        public static readonly PlatformType Int8 = SByte;
        public static readonly PlatformType UInt8 = Byte;
        public static readonly PlatformType Float32 = Single;
        public static readonly PlatformType Float64 = Double;

        public static readonly PlatformType Enum = New("System", "Enum", TypeKind.ValueType);
        public static readonly PlatformType ValueType = New("System", "ValueType", TypeKind.ValueType);
        public static readonly PlatformType MulticastDelegate = New("System", "MulticastDelegate", TypeKind.ReferenceType);
        public static readonly PlatformType Delegate = New("System", "Delegate", TypeKind.ReferenceType);

        public static readonly PlatformType PureAttribute = New("System.Diagnostics.Contracts", "PureAttribute", TypeKind.ReferenceType);
        public static readonly PlatformType ICollection = New("System.Collections", "ICollection", TypeKind.ReferenceType);
        public static readonly PlatformType IEnumerable = New("System.Collections", "IEnumerable", TypeKind.ReferenceType);
        public static readonly PlatformType IEnumerator = New("System.Collections", "IEnumerator", TypeKind.ReferenceType);
        public static readonly PlatformType GenericICollection = New("System.Collections.Generic", "ICollection", TypeKind.ReferenceType, 1);

        public static readonly PlatformType Task = New("System.Threading.Tasks", "Task", TypeKind.ReferenceType);
        public static readonly PlatformType GenericTask = New("System.Threading.Tasks", "Task", TypeKind.ReferenceType, 1);
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
        IBasicType Instantiate(IEnumerable<IType> genericArguments);
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
						  "To bind all platform types use  PlatformType.Resolve method.";

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

        public void Resolve(Func<TypeDefinition> ResolutionFunc)
        {
            ResolveType = ResolutionFunc;
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
            if (obj is PlatformType platformType)
                return platformType.Equals(this);

			var other = obj as IBasicType;
			// TODO: Maybe we should also compare the TypeKind?
			var result = other != null &&
						 this.Name == other.Name &&
						 this.GenericParameterCount == other.GenericParameterCount &&
						 this.ContainingNamespace == other.ContainingNamespace &&
                         this.ContainingAssembly.Equals(other.ContainingAssembly) &&
						 this.GenericArguments.SequenceEqual(other.GenericArguments) &&
						 (this.ContainingType != null == (other.ContainingType != null)) &&
						 (this.ContainingType == null || other.ContainingType == null || this.ContainingType.Equals(other.ContainingType));

			return result;
		}

        public IBasicType Instantiate(IEnumerable<IType> genericArguments)
        {
            var result = new BasicType(this.Name, this.TypeKind)
            {
                ContainingAssembly = this.ContainingAssembly,
                ContainingNamespace = this.ContainingNamespace,
                ContainingType = this.ContainingType,
                GenericParameterCount = this.GenericParameterCount,
                GenericType = this
            };

            result.GenericArguments.AddRange(genericArguments);
            result.Resolve(() => this.ResolvedType);
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
        public ISet<IType> Constraints { get; private set; }
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
            this.Constraints = new HashSet<IType>();
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
