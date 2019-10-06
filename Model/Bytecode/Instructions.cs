﻿// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Model.Bytecode.Visitor;
using Model.ThreeAddressCode.Values;
using Model.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Model.Bytecode
{
	public enum BasicOperation
	{
		Add,
		Sub,
		Mul,
		Div,
		Rem,
		And,
		Or,
		Xor,
		Shl,
		Shr,
		Eq,
		Lt,
		Gt,
		Throw,
		Rethrow,
		Not,
		Neg,
		Nop,
		Pop,
		Dup,
		EndFinally,
		EndFilter,
		LocalAllocation,
		InitBlock,
		InitObject,
		CopyObject,
		CopyBlock,
		LoadArrayLength,
		Breakpoint,
		Return
	}

	public enum BranchOperation
	{
		False,
		True,
		Eq,
		Neq,
		Lt,
		Le,
		Gt,
		Ge,
		Branch,
		Leave
	}

	public enum ConvertOperation
	{
		Conv,
		Cast,
		Box,
		Unbox,
		UnboxPtr
	}

	public enum MethodCallOperation
	{
		Static,
		Virtual,
		Jump
	}

	public enum LoadOperation
	{
		Value,
		Content,
		Address
	}

	public enum LoadFieldOperation
	{
		Content,
		Address
	}

	public enum LoadArrayElementOperation
	{
		Content,
		Address
	}

	public enum LoadMethodAddressOperation
	{
		Static,
		Virtual
	}

	public abstract class Instruction : IInstruction
	{
		public uint Offset { get; set; }
		public string Label { get; set; }

		protected Instruction(uint offset)
		{
			this.Offset = offset;
			this.Label = string.Format("L_{0:X4}", offset);
		}

		public ISet<IVariable> Variables
		{
			get
			{
				var result = new HashSet<IVariable>();
				result.UnionWith(this.ModifiedVariables);
				result.UnionWith(this.UsedVariables);
				return result;
			}
		}

		public virtual ISet<IVariable> ModifiedVariables
		{
			get { return new HashSet<IVariable>(); }
		}

		public virtual ISet<IVariable> UsedVariables
		{
			get { return new HashSet<IVariable>(); }
		}

		public virtual void Replace(IVariable oldvar, IVariable newvar)
		{
		}

		public virtual void Accept(IInstructionVisitor visitor)
		{
			visitor.Visit(this);
		}

		protected string ToString(string format, params object[] arguments)
		{
			string label;

			if (string.IsNullOrEmpty(this.Label))
			{
				label = new string(' ', 7);
			}
			else
			{
				label = string.Format("{0}:", this.Label);
			}

			var text = string.Format(format, arguments);
			return string.Format("{0}  {1};", label, text);
		}

		protected string ToString(object argument)
		{
			return this.ToString("{0}", argument);
		}
	}

	public class BasicInstruction : Instruction
	{
		public BasicOperation Operation { get; set; }
		public bool OverflowCheck { get; set; }
		public bool UnsignedOperands { get; set; }

		public BasicInstruction(uint label, BasicOperation operation)
			: base(label)
		{
			this.Operation = operation;
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override string ToString()
		{
			return this.ToString(this.Operation);
		}
	}

	public class BranchInstruction : Instruction
	{
		public BranchOperation Operation { get; set; }
		public string Target { get; set; }
		public bool UnsignedOperands { get; set; }

		public BranchInstruction(uint label, BranchOperation operation, uint target)
			: base(label)
		{
			this.Target = string.Format("L_{0:X4}", target);
			this.Operation = operation;
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override string ToString()
		{
			return this.ToString("if {0} goto {1}", this.Operation, this.Target);
		}
	}

	public class SwitchInstruction : Instruction
	{
		public IList<string> Targets { get; private set; }

		public SwitchInstruction(uint label, IEnumerable<uint> targets)
			: base(label)
		{
			this.Targets = targets.Select(target => string.Format("L_{0:X4}", target)).ToList();
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override string ToString()
		{
			var targets = string.Join(", ", this.Targets);
			return this.ToString("switch {0}", targets);
		}
    }

    public class ConstrainedInstruction : Instruction
    {
        public IType ThisType { get; private set; }

        public ConstrainedInstruction(uint label, IType thisType)
            : base(label)
        {
            this.ThisType = thisType;
        }

        public override void Accept(IInstructionVisitor visitor)
        {
            base.Accept(visitor);
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return String.Format("constrain virtual call to type: {0}", ThisType.ToString());
        }
    }

	public class CreateArrayInstruction : Instruction
	{
		public ArrayType Type { get; set; }
		public bool WithLowerBound { get; set; }

		public CreateArrayInstruction(uint label, ArrayType type)
			: base(label)
		{
			this.Type = type;
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override string ToString()
		{
			return this.ToString("new {0}", this.Type);
		}
	}

	public class LoadArrayElementInstruction : Instruction
    {
		public LoadArrayElementOperation Operation { get; set; }
		public ArrayType Array { get; set; }

        public LoadArrayElementInstruction(uint label, LoadArrayElementOperation operation, ArrayType array)
            : base(label)
        {
			this.Operation = operation;
			this.Array = array;
        }

        public override void Accept(IInstructionVisitor visitor)
        {
            base.Accept(visitor);
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return this.ToString("load {0} of array {1} element", this.Operation, this.Array);
        }
    }

	public class StoreArrayElementInstruction : Instruction
	{
		public ArrayType Array { get; set; }

		public StoreArrayElementInstruction(uint label, ArrayType array)
			: base(label)
		{
			this.Array = array;
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override string ToString()
		{
			return this.ToString("store array {0} element", this.Array);
		}
	}

	public class CreateObjectInstruction : Instruction
	{
		public IMethodReference Constructor { get; set; }

		public CreateObjectInstruction(uint label, IMethodReference constructor)
			: base(label)
		{
			this.Constructor = constructor;
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override string ToString()
		{
			return this.ToString("new {0} with <{1}>", this.Constructor.ContainingType, this.Constructor);
		}
	}

	public class MethodCallInstruction : Instruction
	{
		public MethodCallOperation Operation { get; set; }
		public IMethodReference Method { get; set; }

		public MethodCallInstruction(uint label, MethodCallOperation operation, IMethodReference method)
			: base(label)
		{
			this.Operation = operation;
			this.Method = method;
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override string ToString()
		{
			return this.ToString("{0} Call <{1}>", this.Operation, this.Method);
		}
	}

	public class IndirectMethodCallInstruction : Instruction
	{
		public FunctionPointerType Function { get; set; }

		public IndirectMethodCallInstruction(uint label, FunctionPointerType function)
			: base(label)
		{
			this.Function = function;
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override string ToString()
		{
			return this.ToString("Indirect Call <{0}>", this.Function);
		}
	}

	public class SizeofInstruction : Instruction
	{
		public IType MeasuredType { get; set; }

		public SizeofInstruction(uint label, IType measuredType)
			: base(label)
		{
			this.MeasuredType = measuredType;
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override string ToString()
		{
			return this.ToString("sizeof {0}", this.MeasuredType);
		}
	}
    public class LoadIndirectInstruction : Instruction
    {
        public IType Type { get; set; }

        public LoadIndirectInstruction(uint label, IType type)
            : base(label)
        {
            this.Type = type;
        }

        public override void Accept(IInstructionVisitor visitor)
        {
            base.Accept(visitor);
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return this.ToString("indirect load {0}", this.Type);
        }
    }

    public class LoadInstruction : Instruction
	{
		public LoadOperation Operation { get; set; }
		public IInmediateValue Operand { get; set; }

		public LoadInstruction(uint label, LoadOperation operation, IInmediateValue operand)
			: base(label)
		{
			this.Operation = operation;
			this.Operand = operand;
		}

		public override ISet<IVariable> UsedVariables
		{
			get
			{
				var result = new HashSet<IVariable>();

				if (this.Operand is IVariable)
				{
					var variable = this.Operand as IVariable;
					result.Add(variable);
				}

				return result;
			}
		}

		public override void Replace(IVariable oldvar, IVariable newvar)
		{
			if (this.Operand.Equals(oldvar)) this.Operand = newvar;
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override string ToString()
		{
			return this.ToString("load {0} of {1}", this.Operation, this.Operand);
		}
	}

	public class LoadFieldInstruction : Instruction
	{
		public LoadFieldOperation Operation { get; set; }
		public IFieldReference Field { get; set; }

		public LoadFieldInstruction(uint label, LoadFieldOperation operation, IFieldReference field)
			: base(label)
		{
			this.Operation = operation;
			this.Field = field;
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override string ToString()
		{
			return this.ToString("load {0} of field {1}", this.Operation, this.Field.Name);
		}
	}

	public class LoadMethodAddressInstruction : Instruction
	{
		public LoadMethodAddressOperation Operation { get; set; }
		public IMethodReference Method { get; set; }

		public LoadMethodAddressInstruction(uint label, LoadMethodAddressOperation operation, IMethodReference method)
			: base(label)
		{
			this.Operation = operation;
			this.Method = method;
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override string ToString()
		{
			return this.ToString("load address of {0} method <{1}>", this.Operation, this.Method);
		}
	}

	public class LoadTokenInstruction : Instruction
	{
		public IMetadataReference Token { get; set; }

		public LoadTokenInstruction(uint label, IMetadataReference token)
			: base(label)
		{
			this.Token = token;
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override string ToString()
		{
			return this.ToString("load token {0}", this.Token);
		}
	}

    public class StoreIndirectInstruction : Instruction
    {
        public IType Type { get; set; }

        public StoreIndirectInstruction(uint label, IType type)
            : base(label)
        {
            this.Type = type;
        }

        public override void Accept(IInstructionVisitor visitor)
        {
            base.Accept(visitor);
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return this.ToString("indirect store {0}", this.Type);
        }
    }

    public class StoreInstruction : Instruction
	{
		public IVariable Target { get; set; }

		public StoreInstruction(uint label, IVariable operand)
			: base(label)
		{
			this.Target = operand;
		}

		public override ISet<IVariable> ModifiedVariables
		{
			get { return new HashSet<IVariable>() { this.Target }; }
		}

		public override void Replace(IVariable oldvar, IVariable newvar)
		{
			if (this.Target.Equals(oldvar)) this.Target = newvar;
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override string ToString()
		{
			return this.ToString("store {0}", this.Target);
		}
	}

	public class StoreFieldInstruction : Instruction
	{
		public IFieldReference Field { get; set; }

		public StoreFieldInstruction(uint label, IFieldReference field)
			: base(label)
		{
			this.Field = field;
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override string ToString()
		{
			return this.ToString("store field {0}", this.Field.Name);
		}
	}

	public class ConvertInstruction : Instruction
	{
		public ConvertOperation Operation { get; set; }
		public IType ConversionType { get; set; }
		public bool OverflowCheck { get; set; }
		public bool UnsignedOperands { get; set; }

		public ConvertInstruction(uint label, ConvertOperation operation, IType conversionType)
			: base(label)
		{
			this.Operation = operation;
			this.ConversionType = conversionType;
		}

		public override void Accept(IInstructionVisitor visitor)
		{
			base.Accept(visitor);
			visitor.Visit(this);
		}

		public override string ToString()
		{
			return this.ToString("{0} as {1}", this.Operation ,this.ConversionType);
		}
	}
}
