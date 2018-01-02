// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Cci;
using Backend;
using Backend.Analyses;
using Backend.Serialization;
using Backend.ThreeAddressCode;
using Backend.Transformations;
using System.IO;

namespace TinyBCT
{
	class MethodVisitor : MetadataTraverser
	{
		private IMetadataHost host;
		private ISourceLocationProvider sourceLocationProvider;

		public MethodVisitor(IMetadataHost host, ISourceLocationProvider sourceLocationProvider)
		{
			this.host = host;
			this.sourceLocationProvider = sourceLocationProvider;
		}

        private void dummyTraverseTypeDef(ITypeDefinition typeDefinition)
        {
            // test code - playground

            Console.WriteLine(typeDefinition); // prints class name
            foreach (IMethodDefinition method in typeDefinition.Methods) // loops class methods
            {
                // translates code method to TAC representation
                var disassembler = new Disassembler(host, method, sourceLocationProvider);
                var methodBody = disassembler.Execute();
                Console.WriteLine(methodBody); // prints instructions
            }

            foreach (IFieldDefinition field in typeDefinition.Fields) // loops fields
            {
                Console.WriteLine(field);
                Console.WriteLine(field.Type);
            }

            foreach (IPropertyDefinition properties in typeDefinition.Properties)
            {
                Console.WriteLine(properties);
            }
        }

        /*public override void TraverseChildren(ITypeDefinition typeDefinition)
        {
            // nested classes are not traversed here
            //dummyTraverseTypeDef(typeDefinition);
        }*/

        private String getBoogieType(ITypeReference type)
        {
            if (type.TypeCode.Equals(PrimitiveTypeCode.Int32))
                return "int";

            // hack 
            if (type.TypeCode.Equals(PrimitiveTypeCode.NotPrimitive))
                return "Ref";
            
            return null;
        }

        private String getMethodName(IMethodDefinition methodDefinition)
        {
            var signature = MemberHelper.GetMethodSignature(methodDefinition, NameFormattingOptions.Signature);
            var split = signature.Split('(');
            return split[0];
        }

        private String getMethodBoogieReturnType(IMethodDefinition methodDefinition)
        {
            return getBoogieType(methodDefinition.Type);
        }

        private String getParametersWithBoogieType(MethodBody methodBody)
        {
            return String.Join(",", methodBody.Parameters.Select(v => v.Name + " : " + getBoogieType(v.Type)));
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

        public override void TraverseChildren(IMethodDefinition methodDefinition)
		{
            // it's not supported currently
            if (methodDefinition.IsConstructor)
                return;

            var disassembler = new Disassembler(host, methodDefinition, sourceLocationProvider);
            var methodBody = disassembler.Execute();

            transformBody(methodBody);

            StreamWriter streamWriter = new StreamWriter(@"C:\result.bpl");

            // prelude
            streamWriter.WriteLine("type Ref;");

            streamWriter.WriteLine("procedure " + getMethodName(methodDefinition) + "(" + getParametersWithBoogieType(methodBody) + ") returns (r : " + getMethodBoogieReturnType(methodDefinition) + ") {");
            InstructionTranslator instTranslator = new InstructionTranslator();

            // improve this, last element is not appended by ;
            streamWriter.Write(String.Join(";" + Environment.NewLine, methodBody.Variables.Except(methodBody.Parameters).Select(v => "var " + v.Name + " : " + getBoogieType(v.Type))));
            streamWriter.Write(";" + Environment.NewLine);

            foreach (var instruction in methodBody.Instructions)
            {
                instruction.Accept(instTranslator);
                streamWriter.Write(instTranslator.Result);
            }

            streamWriter.Write("}" + Environment.NewLine);
            streamWriter.Close();
        }
	}
}
