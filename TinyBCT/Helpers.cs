using Backend;
using Backend.Analyses;
using Backend.ThreeAddressCode.Instructions;
using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyBCT
{
    class Helpers
    {
        public static bool IsInstructionImplemented(Instruction inst)
        {
            if (inst is MethodCallInstruction ||
                inst is LoadInstruction ||
                inst is UnconditionalBranchInstruction ||
                inst is BinaryInstruction ||
                inst is NopInstruction ||
                inst is ReturnInstruction)
                return true;

            return false;
        }
        public static String GetBoogieType(ITypeReference type)
        {
            if (type.TypeCode.Equals(PrimitiveTypeCode.Int32))
                return "int";

            // hack 
            if (type.TypeCode.Equals(PrimitiveTypeCode.NotPrimitive))
                return "Ref";

            return null;
        }

        public static String GetMethodName(IMethodDefinition methodDefinition)
        {
            var signature = MemberHelper.GetMethodSignature(methodDefinition, NameFormattingOptions.OmitContainingType | NameFormattingOptions.PreserveSpecialNames);
            return signature;
        }

        public static String GetMethodBoogieReturnType(IMethodDefinition methodDefinition)
        {
            return GetBoogieType(methodDefinition.Type);
        }

        public static String GetParametersWithBoogieType(MethodBody methodBody)
        {
            return String.Join(",", methodBody.Parameters.Select(v => v.Name + " : " + GetBoogieType(v.Type)));
        }

        private void transformBody(MethodBody methodBody)
        {
            var cfAnalysis = new ControlFlowAnalysis(methodBody);
            var cfg = cfAnalysis.GenerateNormalControlFlow();

            var splitter = new WebAnalysis(cfg);
            splitter.Analyze();
            splitter.Transform();

            methodBody.UpdateVariables();

            var typeAnalysis = new TypeInferenceAnalysis(cfg);
            typeAnalysis.Analyze();

            var forwardCopyAnalysis = new ForwardCopyPropagationAnalysis(cfg);
            forwardCopyAnalysis.Analyze();
            forwardCopyAnalysis.Transform(methodBody);

            var backwardCopyAnalysis = new BackwardCopyPropagationAnalysis(cfg);
            backwardCopyAnalysis.Analyze();
            backwardCopyAnalysis.Transform(methodBody);
        }
    }
}
