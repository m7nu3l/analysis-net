﻿// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

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
using Backend.ThreeAddressCode.Instructions;
using Backend.ThreeAddressCode.Values;

namespace TinyBCT
{
    class TACWriterVisitor : MetadataTraverser
    {
        private IMetadataHost host;
        private ISourceLocationProvider sourceLocationProvider;
        private StringBuilder sb = new StringBuilder();

        public TACWriterVisitor(IMetadataHost host, ISourceLocationProvider sourceLocationProvider)
        {
            this.host = host;
            this.sourceLocationProvider = sourceLocationProvider;
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
            var disassembler = new Disassembler(host, methodDefinition, sourceLocationProvider);
            var methodBody = disassembler.Execute();

            transformBody(methodBody);

            sb.AppendLine(methodBody.ToString());
        }

        public override String ToString()
        {
            return sb.ToString();
        }
    }
}