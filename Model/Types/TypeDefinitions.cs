﻿// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Model.ThreeAddressCode;
using Model.ThreeAddressCode.Values;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model.Types
{
	public interface ITypeDefinitionContainer
	{
		IList<TypeDefinition> Types { get; }
	}

	public interface ITypeMemberReference
	{
		IBasicType ContainingType { get; }
	}

	public interface ITypeMemberDefinition : ITypeMemberReference
	{
		new TypeDefinition ContainingType { get; set; }

		bool MatchReference(ITypeMemberReference member);
	}

	public interface IGenericReference : ITypeMemberReference
	{
		int GenericParameterCount { get; }
	}

	public interface IGenericDefinition : IGenericReference, ITypeMemberDefinition
	{
		IList<GenericParameter> GenericParameters { get; }
	}

	public class CustomAttribute
	{
		public IType Type { get; set; }
		public IMethodReference Constructor { get; set; }
		public IList<Constant> Arguments { get; private set; }

		public CustomAttribute()
		{
			this.Arguments = new List<Constant>();
		}

		public override string ToString()
		{
			var arguments = string.Join(", ", this.Arguments);

			return string.Format("[{0}({1})]", this.Type, arguments);
		}
	}

	public interface IFieldReference : ITypeMemberReference, IMetadataReference
	{
		IType Type { get; }
		string Name { get; }
		bool IsStatic { get; }
	}

	public class FieldReference : IFieldReference
	{
		public ISet<CustomAttribute> Attributes { get; private set; }
		public IBasicType ContainingType { get; set; }
		public IType Type { get; set; }
		public string Name { get; set; }
		public bool IsStatic { get; set; }

		public FieldReference(string name, IType type)
		{
			this.Name = name;
			this.Type = type;
			this.Attributes = new HashSet<CustomAttribute>();
		}

		public override string ToString()
		{
			var modifier = this.IsStatic ? "static " : string.Empty;
			return string.Format("{0}{1} {2}", modifier, this.Type, this.Name);
		}

		public override int GetHashCode()
		{
			return this.Name.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			var other = obj as IFieldReference;

			var result = other != null &&
						 this.Name == other.Name &&
						 this.IsStatic == other.IsStatic &&
						 this.Type.Equals(other.Type) &&
						 this.ContainingType.Equals(other.ContainingType);

			return result;
		}
	}

	public class FieldDefinition : ITypeMemberDefinition, IFieldReference
	{
		public VisibilityKind Visibility { get; set; }
		public ISet<CustomAttribute> Attributes { get; private set; }
		public TypeDefinition ContainingType { get; set; }
		public IType Type { get; set; }
		public string Name { get; set; }
		public Constant Value { get; set; }
		public bool IsStatic { get; set; }
		public byte[] InitialValue { get; set; }
		public FieldDefinition(string name, IType type)
		{
			this.Name = name;
			this.Type = type;
			this.Attributes = new HashSet<CustomAttribute>();
			this.InitialValue = new byte[0];
		}

		#region ITypeMemberReference members

		IBasicType ITypeMemberReference.ContainingType
		{
			get { return this.ContainingType; }
		}

		#endregion

		public bool MatchReference(ITypeMemberReference member)
		{
			var result = false;

			if (member is ITypeMemberDefinition)
			{
				result = this.Equals(member);
			}
			else
			{
				var field = member as IFieldReference;

				result = field != null &&
						 this.Name == field.Name &&
						 this.IsStatic == field.IsStatic &&
						 this.ContainingType.MatchReference(field.ContainingType) &&
						 this.Type.Equals(field.Type);
			}

			return result;
		}

		public override string ToString()
		{
			var modifier = this.IsStatic ? "static " : string.Empty;
			var value = this.Value == null ? string.Empty : string.Format(" = {0}", this.Value);
			return string.Format("{0}{1} {2}{3}", modifier, this.Type, this.Name, value);
		}
	}

	public enum MethodParameterKind
	{
		Normal, // none
		In, // in keyword
		Out, // out keyword
		Ref // ref keyword
	}

	public interface IMethodParameterReference
	{
		ushort Index { get; }
		IType Type { get; }
		MethodParameterKind Kind { get; }
	}

	public class MethodParameterReference : IMethodParameterReference
	{
		public ushort Index { get; set; }
		public IType Type { get; set; }
		public MethodParameterKind Kind { get; set; }

		public MethodParameterReference(ushort index, IType type)
		{
			this.Index = index;
			this.Type = type;
			this.Kind = MethodParameterKind.Normal;
		}

		public override string ToString()
		{
			string kind;

			switch (this.Kind)
			{
				case MethodParameterKind.Normal: kind = String.Empty; break;
				case MethodParameterKind.In: kind = "in "; break;
				case MethodParameterKind.Out: kind = "out "; break;
				case MethodParameterKind.Ref: kind = "ref "; break;

				default: throw this.Kind.ToUnknownValueException();
			}

			return string.Format("{0}{1}", kind, this.Type);
		}

		public override int GetHashCode()
		{
			return this.Type.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			var other = obj as IMethodParameterReference;

			var result = other != null &&
						 this.Kind == other.Kind &&
						 this.Type.Equals(other.Type);

			return result;
		}
	}

	public class MethodParameter : IMethodParameterReference
	{
		public ISet<CustomAttribute> Attributes { get; private set; }
		public ushort Index { get; set; }
		public string Name { get; set; }
		public IType Type { get; set; }
		public MethodParameterKind Kind { get; set; }
		public Constant DefaultValue { get; set; }

		public MethodParameter(ushort index, string name, IType type)
		{
			this.Index = index;
			this.Name = name;
			this.Type = type;
			this.Kind = MethodParameterKind.Normal;
			this.Attributes = new HashSet<CustomAttribute>();
		}

		public bool HasDefaultValue
		{
			get { return this.DefaultValue != null; }
		}

		public bool MatchReference(IMethodParameterReference parameter)
		{
			var result = false;

			if (parameter is MethodParameter)
			{
				result = this.Equals(parameter);
			}
			else
			{
				result = this.Kind == parameter.Kind &&
						 this.Type.Equals(parameter.Type);
			}

			return result;
		}

		public override string ToString()
		{
			string kind;

			switch (this.Kind)
			{
				case MethodParameterKind.Normal: kind = string.Empty; break;
				case MethodParameterKind.In: kind = "in "; break;
				case MethodParameterKind.Out: kind = "out "; break;
				case MethodParameterKind.Ref: kind = "ref "; break;

				default: throw this.Kind.ToUnknownValueException();
			}

			return string.Format("{0}{1} {2}", kind, this.Type, this.Name);
		}
	}

	public interface IMethodReference : ITypeMemberReference, IMetadataReference, IGenericReference
	{
		IType ReturnType { get; }
		string Name { get; }
		string GenericName { get; }
		IList<IMethodParameterReference> Parameters { get; }
		IList<IType> GenericArguments { get; }
		IMethodReference GenericMethod { get; }
		MethodDefinition ResolvedMethod { get; }
		bool IsStatic { get; }
	}

	public class MethodReference : IMethodReference
	{
		private Func<MethodDefinition> ResolveMethod;
		private MethodDefinition resolvedMethod;

		public ISet<CustomAttribute> Attributes { get; private set; }
		public IBasicType ContainingType { get; set; }
		public IType ReturnType { get; set; }
		public string Name { get; set; }
		public int GenericParameterCount { get; set; }
		public IList<IType> GenericArguments { get; private set; }
		public IList<IMethodParameterReference> Parameters { get; private set; }
		public IMethodReference GenericMethod { get; set; }
		public bool IsStatic { get; set; }

		public MethodReference(string name, IType returnType)
		{
			this.Name = name;
			this.ReturnType = returnType;
			this.Parameters = new List<IMethodParameterReference>();
			this.Attributes = new HashSet<CustomAttribute>();
			this.GenericArguments = new List<IType>();

			this.ResolveMethod = () =>
			{
				var msg = "Use Resolve method to bind this reference with some host.";

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
					var startIndex = this.ContainingType.GenericParameterCount + 1;
					arguments = string.Join(", T", Enumerable.Range(startIndex, this.GenericParameterCount));
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

		public MethodDefinition ResolvedMethod
		{
			get
			{
				if (resolvedMethod == null)
				{
					resolvedMethod = ResolveMethod();
				}

				return resolvedMethod;
			}
		}

		//public MethodDefinition Resolve(Host host)
		//{
		//	this.ResolvedMethod = host.ResolveReference(this) as MethodDefinition;
		//	return this.ResolvedMethod;
		//}

		public void Resolve(Host host)
		{
			ResolveMethod = () => host.ResolveReference(this) as MethodDefinition;
		}

		public override string ToString()
		{
			return this.ToSignatureString();
		}

		public override int GetHashCode()
		{
			return this.Name.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			var other = obj as IMethodReference;

			var result = other != null &&
						 this.Name == other.Name &&
						 this.IsStatic == other.IsStatic &&
						 this.GenericParameterCount == other.GenericParameterCount &&
						 this.ReturnType.Equals(other.ReturnType) &&
						 this.ContainingType.Equals(other.ContainingType) &&
						 this.GenericArguments.SequenceEqual(other.GenericArguments) &&
						 this.Parameters.SequenceEqual(other.Parameters);

			return result;
		}
	}
	public class PropertyDefinition : ITypeMemberDefinition
	{
		public PropertyDefinition(string name, IType propType)
		{
			PropertyType = propType;
			Name = name;
			Attributes = new HashSet<CustomAttribute>();
		}
 
		public ISet<CustomAttribute> Attributes { get; private set; }
		public IType PropertyType { get; set; }
		public string Name { get; set; }
		public MethodDefinition Getter { get; set; }
		public MethodDefinition Setter { get; set; }
		public TypeDefinition ContainingType { get; set; }
		IBasicType ITypeMemberReference.ContainingType
		{
			get { return this.ContainingType; }
		}
		public bool MatchReference(ITypeMemberReference member)
		{
			if (member is PropertyDefinition)
				return member.Equals(this);

			return false;
		}
		public override bool Equals(object obj)
		{
			if (obj is PropertyDefinition propertyDef)
			{
				bool hasSetter = (propertyDef.Setter != null) == (this.Setter != null);
				bool hasGetter = (propertyDef.Getter != null) == (this.Getter != null);
				return  propertyDef.Name.Equals(this.Name) &&
					propertyDef.PropertyType.Equals(this.PropertyType) &&
					hasSetter && hasGetter &&
					(propertyDef.Getter == null || propertyDef.Getter.Equals(this.Getter)) &&
					(propertyDef.Setter == null || propertyDef.Setter.Equals(this.Setter)) &&
					(propertyDef.ContainingType.Equals(this.ContainingType));
			}
			return false;
		}
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("Property definition");
			stringBuilder.AppendLine(String.Format("Name: {0}", Name));
			stringBuilder.AppendLine(String.Format("Property type: {0}", PropertyType));
			stringBuilder.AppendLine(String.Format("Containing type: {0}", ContainingType));
			if (Getter != null)
			stringBuilder.AppendLine(String.Format("Getter: {0}", Getter.ToSignatureString()));
			if (Setter != null)
			stringBuilder.AppendLine(String.Format("Setter: {0}", Setter.ToSignatureString()));
			return stringBuilder.ToString();
		}
		public override int GetHashCode()
		{
			return this.Name.GetHashCode();
		}
	}
	public class MethodDefinition : ITypeMemberDefinition, IMethodReference, IGenericDefinition
	{
		public VisibilityKind Visibility { get; set; }
		public ISet<CustomAttribute> Attributes { get; private set; }
		public TypeDefinition ContainingType { get; set; }
		public IType ReturnType { get; set; }
		public string Name { get; set; }
		public IList<GenericParameter> GenericParameters { get; private set; }
		public IList<MethodParameter> Parameters { get; private set; }
		public bool IsStatic { get; set; }
		public bool IsAbstract { get; set; }
		public bool IsVirtual { get; set; }
		public bool IsOverrider { get; set; }
		public bool IsFinal { get; set; }
		public bool IsConstructor { get; set; }
		public bool IsExternal { get; set; }
		public MethodBody Body { get; set; }

		public MethodDefinition(string name, IType returnType)
		{
			this.Name = name;
			this.ReturnType = returnType;
			this.Attributes = new HashSet<CustomAttribute>();
			this.GenericParameters = new List<GenericParameter>();
			this.Parameters = new List<MethodParameter>();
		}

		public bool HasBody
		{
			get { return this.Body != null; }
		}

		public string GenericName
		{
			get
			{
				var parameters = string.Empty;

				if (this.GenericParameters.Count > 0)
				{
					parameters = string.Join(", ", this.GenericParameters);
					parameters = string.Format("<{0}>", parameters);
				}

				return string.Format("{0}{1}", this.Name, parameters);
			}
		}

		#region ITypeMemberReference members

		IBasicType ITypeMemberReference.ContainingType
		{
			get { return this.ContainingType; }
		}

		#endregion

		#region IGenericReference members

		int IGenericReference.GenericParameterCount
		{
			get { return this.GenericParameters.Count; }
		}

		#endregion

		#region IMethodReference members

		IList<IMethodParameterReference> IMethodReference.Parameters
		{
			get { return new List<IMethodParameterReference>(this.Parameters); }
		}

		IList<IType> IMethodReference.GenericArguments
		{
			get { return new List<IType>(); }
		}

		MethodDefinition IMethodReference.ResolvedMethod
		{
			get { return this; }
		}

		public IMethodReference GenericMethod
		{
			get { return null; }
		}

		#endregion

		public bool MatchReference(ITypeMemberReference member)
		{
			var result = false;

			if (member is MethodDefinition)
			{
				result = this.Equals(member);
			}
			else if (member is IMethodReference)
			{
				var method = member as IMethodReference;

				if (method.GenericMethod != null)
				{
					method = method.GenericMethod;
				}

				result = this.ContainingType.MatchReference(method.ContainingType) &&
						 this.MatchSignature(method);
			}

			return result;
		}

		public bool MatchSignature(IMethodReference method)
		{
			var result = this.Name == method.Name &&
						 this.IsStatic == method.IsStatic &&
						 this.GenericParameters.Count == method.GenericParameterCount &&
						 this.ReturnType.Equals(method.ReturnType) &&
						 this.MatchParameters(method);
			return result;
		}

		public bool MatchParameters(IMethodReference method)
		{
			var result = false;

			if (this.Parameters.Count == method.Parameters.Count)
			{
				result = true;

				for (var i = 0; i < this.Parameters.Count && result; ++i)
				{
					var parameterdef = this.Parameters[i];
					var parameterref = method.Parameters[i];

					result = parameterdef.MatchReference(parameterref);
				}
			}

			return result;
		}

		public string ToSignatureString()
		{
			var result = new StringBuilder();

			if (this.IsStatic)
			{
				result.Append("static ");
			}

			if (this.IsAbstract)
			{
				result.Append("abstract ");
			}

			if (this.IsVirtual)
			{
				result.Append("virtual ");
			}

			result.AppendFormat("{0} {1}::{2}", this.ReturnType, this.ContainingType.GenericName, this.GenericName);

			var parameters = string.Join(", ", this.Parameters);
			result.AppendFormat("({0})", parameters);

			return result.ToString();
		}

		public override string ToString()
		{
			var result = new StringBuilder();

			var signature = this.ToSignatureString();
			result.Append(signature);

			if (this.HasBody)
			{
				result.AppendLine();
				result.AppendLine("{");
				result.Append(this.Body);
				result.AppendLine("}");
			}

			return result.ToString();
		}
	}

	public enum TypeDefinitionKind
	{
		Unknown,
		Class,
		Interface,
		Struct,
		Enum,
		Delegate
	}

	[Flags]
	public enum VisibilityKind
	{
		Unknown = 0,
		Private = 1,
		Protected = 2,
		Internal = 4,
		Public = 8
	}
	public enum LayoutKind
	{
		Unknown,
		AutoLayout,    // Class fields are auto-laid out
		SequentialLayout,  // Class fields are laid out sequentially
		ExplicitLayout,	// Layout is supplied explicitly
	}
	public class LayoutInformation
	{
		public LayoutKind Kind { get; set; }
		public short PackingSize { get; set; }
		public int ClassSize { get; set; }
		public LayoutInformation(LayoutKind kind = LayoutKind.Unknown)
		{
			Kind = kind;
			PackingSize = -1;
			ClassSize = -1;
		}
	}

	public class MethodImplementation
	{
		public IMethodReference ImplementedMethod { get; private set; }
		public IMethodReference ImplementingMethod { get; private set; }

		public MethodImplementation(IMethodReference implemented, IMethodReference implementing)
		{
			this.ImplementedMethod = implemented;
			this.ImplementingMethod = implementing;
		}
	}
	public class TypeDefinition : IBasicType, IGenericDefinition, ITypeMemberDefinition, ITypeDefinitionContainer
	{
		public TypeKind TypeKind { get; set; }
		public TypeDefinitionKind Kind { get; set; }
		public VisibilityKind Visibility { get; set; }
		public Assembly ContainingAssembly { get; set; }
		public Namespace ContainingNamespace { get; set; }
		public TypeDefinition ContainingType { get; set; }
		public ISet<CustomAttribute> Attributes { get; private set; }
		public string Name { get; set; }
		public IBasicType Base { get; set; }
		public IList<IBasicType> Interfaces { get; private set; }
		public IList<GenericParameter> GenericParameters { get; private set; }
		public IList<FieldDefinition> Fields { get; private set; }
		public IList<MethodDefinition> Methods { get; private set; }
		public IList<TypeDefinition> Types { get; private set; }
		public IBasicType UnderlayingType { get; set; }
		public bool IsSealed { get; set; }
		public bool IsAbstract { get; set; }
		public LayoutInformation LayoutInformation { get; set; }
		public ISet<MethodImplementation> ExplicitOverrides { get; private set; }
		public ISet<PropertyDefinition> PropertyDefinitions { get; private set; }
		public TypeDefinition(string name, TypeKind typeKind = TypeKind.Unknown, TypeDefinitionKind kind = TypeDefinitionKind.Unknown)
		{
			this.Name = name;
			this.TypeKind = typeKind;
			this.Kind = kind;
			this.Attributes = new HashSet<CustomAttribute>();
			this.Interfaces = new List<IBasicType>();
			this.GenericParameters = new List<GenericParameter>();
			this.Fields = new List<FieldDefinition>();
			this.Methods = new List<MethodDefinition>();
			this.Types = new List<TypeDefinition>();
			this.ExplicitOverrides = new HashSet<MethodImplementation>();
			this.PropertyDefinitions = new HashSet<PropertyDefinition>();
			this.LayoutInformation = new LayoutInformation();		
		}

		public string GenericName
		{
			get
			{
				var parameters = string.Empty;

				if (this.GenericParameters.Count > 0)
				{
					parameters = string.Join(", ", this.GenericParameters);
					parameters = string.Format("<{0}>", parameters);
				}

				return string.Format("{0}{1}", this.Name, parameters);
			}
		}

		public IEnumerable<ITypeMemberDefinition> Members
		{
			get
			{
				var result = this.Types.AsEnumerable<ITypeMemberDefinition>();
				result = result.Union(this.Fields);
				result = result.Union(this.Methods);
				result = result.Union(this.Types);
				return result;
			}
		}

		#region ITypeMemberReference members

		IBasicType ITypeMemberReference.ContainingType
		{
			get { return this.ContainingType; }
		}

		#endregion

		#region IGenericReference members

		int IGenericReference.GenericParameterCount
		{
			get { return this.GenericParameters.Count; }
		}

		#endregion

		#region IBasicType members

		IAssemblyReference IBasicType.ContainingAssembly
		{
			get { return this.ContainingAssembly; }
		}

		string IBasicType.ContainingNamespace
		{
			get { return this.ContainingNamespace.FullName; }
		}

		IList<IType> IBasicType.GenericArguments
		{
			get { return new List<IType>(); }
		}

		TypeDefinition IBasicType.ResolvedType
		{
			get { return this; }
		}

		IBasicType IBasicType.GenericType
		{
			get { return null; }
		}

		#endregion

		public bool MatchReference(ITypeMemberReference member)
		{
			var type = member as IBasicType;
			var result = type != null && this.MatchReference(type);

			return result;
		}

		public bool MatchReference(IBasicType type)
		{
			var result = false;

			if (type is TypeDefinition)
			{
				result = this.Equals(type);
			}
			else
			{
				if (type.GenericType != null)
				{
					type = type.GenericType;
				}

				// TODO: Maybe we should also compare the TypeKind?
				result = this.Name == type.Name &&
						 this.GenericParameters.Count == type.GenericParameterCount &&
						 this.ContainingNamespace.FullName == type.ContainingNamespace &&
						 this.ContainingAssembly.MatchReference(type.ContainingAssembly) &&
						 this.ContainingType.BothNullOrMatchReference(type.ContainingType);
			}

			return result;
		}

		public override string ToString()
		{
			return this.GenericName;
		}

		public override int GetHashCode()
		{
			return this.Name.GetHashCode();
		}
	}

	public enum MethodBodyKind
	{
		Bytecode,
		ThreeAddressCode,
		StaticSingleAssignment
	}

	public class MethodBody : IInstructionContainer
	{
		public IList<IVariable> Parameters { get; private set; }
		public IList<IVariable> LocalVariables { get; private set; }
		public IList<IInstruction> Instructions { get; private set; }
		public IList<ProtectedBlock> ExceptionInformation { get; private set; }
		public ushort MaxStack { get; set; }
		public MethodBodyKind Kind { get; set; }

		public MethodBody(MethodBodyKind kind)
		{
			this.Kind = kind;
			this.Parameters = new List<IVariable>();
			this.LocalVariables = new List<IVariable>();
			this.Instructions = new List<IInstruction>();
			this.ExceptionInformation = new List<ProtectedBlock>();
		}

		public void UpdateVariables()
		{
			this.LocalVariables.Clear();
			//this.LocalVariables.AddRange(this.Parameters);

			// TODO: SSA is not inserting phi instructions into method's body instructions collection.

			var locals = new HashSet<IVariable>();

			foreach (var instruction in this.Instructions)
			{
				locals.UnionWith(instruction.Variables);
			}

			locals.ExceptWith(this.Parameters);
			this.LocalVariables.AddRange(locals);
		}

		public override string ToString()
		{
			var result = new StringBuilder();

			if (this.Parameters.Count > 0)
			{
				foreach (var parameter in this.Parameters)
				{
					result.AppendFormat("  parameter {0} {1};", parameter.Type, parameter.Name);
					result.AppendLine();
				}

				result.AppendLine();
			}

			if (this.LocalVariables.Count > 0)
			{
				foreach (var local in this.LocalVariables)
				{
					var type = local.Type == null ? "??" : local.Type.ToString();

					result.AppendFormat("  local {0} {1};", type, local.Name);
					result.AppendLine();
				}

				result.AppendLine();
			}

			foreach (var instruction in this.Instructions)
			{
				result.Append("  ");
				result.Append(instruction);
				result.AppendLine();
			}

			foreach (var handler in this.ExceptionInformation)
			{
				result.AppendLine();
				result.Append(handler);
			}

			return result.ToString();
		}
	}
}
