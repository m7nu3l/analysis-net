using Backend.ThreeAddressCode.Instructions;
using Backend.ThreeAddressCode.Values;
using Backend.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyBCT
{
    class InstructionTranslator : InstructionVisitor
    {
        private string result;

        public string Result { get => result; set => result = value; }

        public override void Visit(NopInstruction instruction)
        {
            result = instruction.Label + ":" + Environment.NewLine;
        }

        public override void Visit(BinaryInstruction instruction)
        {
            result = instruction.Label + ":" + Environment.NewLine;

            IVariable left = instruction.LeftOperand;
            IVariable right = instruction.RightOperand;

            String operation = String.Empty;

            switch (instruction.Operation)
            {
                case BinaryOperation.Add: operation = "+"; break;
                case BinaryOperation.Sub: operation = "-"; break;
                case BinaryOperation.Mul: operation = "*"; break;
                case BinaryOperation.Div: operation = "/"; break;
                /*case BinaryOperation.Rem: operation = "%"; break;
                case BinaryOperation.And: operation = "&"; break;
                case BinaryOperation.Or: operation = "|"; break;
                case BinaryOperation.Xor: operation = "^"; break;
                case BinaryOperation.Shl: operation = "<<"; break;
                case BinaryOperation.Shr: operation = ">>"; break;
                case BinaryOperation.Eq: operation = "=="; break;
                case BinaryOperation.Neq: operation = "!="; break;
                case BinaryOperation.Gt: operation = ">"; break;
                case BinaryOperation.Ge: operation = ">="; break;
                case BinaryOperation.Lt: operation = "<"; break;
                case BinaryOperation.Le: operation = "<="; break;*/
            }

            result += String.Format("{0} {1} {2} {3} {4}", instruction.Result, ":=", left, operation, right);
            result += ";" + Environment.NewLine;
        }

        public override void Visit(UnconditionalBranchInstruction instruction)
        {
            result = instruction.Label + ":" + Environment.NewLine;
            result += "goto " + instruction.Target + ";" + Environment.NewLine;
        }

        public override void Visit(ReturnInstruction instruction)
        {
            // fijarse si puede haber return sin valor

            result = instruction.Label + ":" + Environment.NewLine;
            result += "r := " + instruction.Operand.Name + ";" + Environment.NewLine;
        }
    }
}
