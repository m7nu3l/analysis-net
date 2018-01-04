using Backend.ThreeAddressCode.Instructions;
using Backend.ThreeAddressCode.Values;
using Backend.Visitors;
using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyBCT
{
    class InstructionTranslator : InstructionVisitor
    {

        public string Result() { return sb.ToString(); }
        private StringBuilder sb = new StringBuilder();

        private void addLabel(Instruction instr)
        {
            sb.AppendLine(String.Format("\t{0}:", instr.Label));
        }

        public override void Visit(NopInstruction instruction)
        {
            //addLabel(instruction);
            sb.Append(String.Format("\t{0}:", instruction.Label));
        }

        public override void Visit(BinaryInstruction instruction)
        {
            addLabel(instruction);

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
                case BinaryOperation.Shr: operation = ">>"; break;*/
                case BinaryOperation.Eq: operation = "=="; break;
                case BinaryOperation.Neq: operation = "!="; break;
                case BinaryOperation.Gt: operation = ">"; break;
                case BinaryOperation.Ge: operation = ">="; break;
                case BinaryOperation.Lt: operation = "<"; break;
                case BinaryOperation.Le: operation = "<="; break;
            }

            sb.Append(String.Format("\t\t{0} {1} {2} {3} {4};", instruction.Result, ":=", left, operation, right));
        }

        public override void Visit(UnconditionalBranchInstruction instruction)
        {
            addLabel(instruction);
            sb.Append(String.Format("\t\tgoto {0};", instruction.Target));
        }

        public override void Visit(ReturnInstruction instruction)
        {
            addLabel(instruction);
            if (instruction.HasOperand)
                sb.Append(String.Format("\t\tr := {0};", instruction.Operand.Name));
        }

        public override void Visit(LoadInstruction instruction)
        {
            addLabel(instruction);
            sb.Append(String.Format("\t\t{0} := {1};", instruction.Result, instruction.Operand));
        }

        public override void Visit(MethodCallInstruction instruction)
        {
            addLabel(instruction);
            var signature = MemberHelper.GetMethodSignature(instruction.Method, NameFormattingOptions.OmitContainingType | NameFormattingOptions.PreserveSpecialNames);
            var arguments = string.Join(", ", instruction.Arguments);

            if (instruction.HasResult)
                sb.Append(String.Format("\t\tcall {0} := {1}({2});", instruction.Result, signature, arguments));
            else
                sb.Append(String.Format("\t\t{0} := {1}({2});", instruction.Result, signature, arguments));
        }

        public override void Visit(ConditionalBranchInstruction instruction)
        {
            addLabel(instruction);

            IVariable leftOperand = instruction.LeftOperand;
            IInmediateValue rightOperand = instruction.RightOperand;

            var operation = string.Empty;

            switch (instruction.Operation)
            {
                case BranchOperation.Eq: operation = "=="; break;
                case BranchOperation.Neq: operation = "!="; break;
                case BranchOperation.Gt: operation = ">"; break;
                case BranchOperation.Ge: operation = ">="; break;
                case BranchOperation.Lt: operation = "<"; break;
                case BranchOperation.Le: operation = "<="; break;
            }


            sb.AppendLine(String.Format("\t\tif ({0} {1} {2})", leftOperand, operation, rightOperand));
            sb.AppendLine("\t\t{");
            sb.AppendLine(String.Format("\t\t\tgoto {0};", instruction.Target));
            sb.Append("\t\t}");

        }
    }
}
