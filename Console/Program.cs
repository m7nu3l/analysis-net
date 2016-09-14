﻿// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CCIProvider;
using Model;
using Model.Types;
using Backend.Analyses;
using Backend.Serialization;
using Backend.Transformations;
using Backend.Utils;
using Model.ThreeAddressCode.Values;
using Backend.Model;
using Tac = Model.ThreeAddressCode.Instructions;
using Bytecode = Model.Bytecode;

namespace Console
{
	class Program
	{
		private Host host;

		public Program(Host host)
		{
			this.host = host;
		}
		
		public void VisitMethods()
		{
			var allDefinedMethods = from a in host.Assemblies
									from t in a.RootNamespace.GetAllTypes()
									from m in t.Members.OfType<MethodDefinition>()
									where m.Body != null
									select m;

			foreach (var method in allDefinedMethods)
			{
				VisitMethod(method);
			}
		}

		private void VisitMethod(MethodDefinition method)
		{
			System.Console.WriteLine(method.Name);

			var methodBodyBytecode = method.Body;
			var disassembler = new Disassembler(method);
			var methodBody = disassembler.Execute();			
			method.Body = methodBody;

			var cfAnalysis = new ControlFlowAnalysis(method.Body);
			//var cfg = cfAnalysis.GenerateNormalControlFlow();
			var cfg = cfAnalysis.GenerateExceptionalControlFlow();

			var dgml = DGMLSerializer.Serialize(cfg);

			//if (method.Name == "ExampleTryCatchFinally")
			//	;

			var domAnalysis = new DominanceAnalysis(cfg);
			domAnalysis.Analyze();
			domAnalysis.GenerateDominanceTree();

			var loopAnalysis = new NaturalLoopAnalysis(cfg);
			loopAnalysis.Analyze();

			var domFrontierAnalysis = new DominanceFrontierAnalysis(cfg);
			domFrontierAnalysis.Analyze();

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

			//var pointsTo = new PointsToAnalysis(cfg);
			//var result = pointsTo.Analyze();

			var liveVariables = new LiveVariablesAnalysis(cfg);
			liveVariables.Analyze();

			var ssa = new StaticSingleAssignment(methodBody, cfg);
			ssa.Transform();
			ssa.Prune(liveVariables);

			methodBody.UpdateVariables();

			//var dot = DOTSerializer.Serialize(cfg);
			//var dgml = DGMLSerializer.Serialize(cfg);

			//dgml = DGMLSerializer.Serialize(host, typeDefinition);
		}

		private static void RunSomeTests()
		{
			const string root = @"..\..\..";
			//const string root = @"C:"; // casa
			//const string root = @"C:\Users\Edgar\Projects"; // facu

			const string input = root + @"\Test\bin\Debug\Test.dll";

			//using (var host = new PeReader.DefaultHost())
			//using (var assembly = new Assembly(host))
			//{
			//	assembly.Load(input);

			//	Types.Initialize(host);

			//	//var extractor = new TypesExtractor(host);
			//	//extractor.Extract(assembly.Module);

			//	var visitor = new MethodVisitor(host, assembly.PdbReader);
			//	visitor.Rewrite(assembly.Module);
			//}

			var host = new Host();
			//host.Assemblies.Add(assembly);

			PlatformTypes.Resolve(host);

			var loader = new Loader(host);
			loader.LoadAssembly(input);
			//loader.LoadCoreAssembly();

			var type = new BasicType("ExamplesPointsTo")
			{
				ContainingAssembly = new AssemblyReference("Test"),
				ContainingNamespace = "Test"
			};

			var typeDefinition = host.ResolveReference(type);

			var method = new MethodReference("Example1", PlatformTypes.Void)
			{
				ContainingType = type,
			};

			//var methodDefinition = host.ResolveReference(method) as MethodDefinition;

			var program = new Program(host);
			program.VisitMethods();

			// Testing method calls inlining
			var methodDefinition = host.ResolveReference(method) as MethodDefinition;
			var methodCalls = methodDefinition.Body.Instructions.OfType<Tac.MethodCallInstruction>().ToList();

			foreach (var methodCall in methodCalls)
			{
				var callee = host.ResolveReference(methodCall.Method) as MethodDefinition;
				methodDefinition.Body.Inline(methodCall, callee.Body);
			}

			methodDefinition.Body.UpdateVariables();

			type = new BasicType("ExamplesCallGraph")
			{
				ContainingAssembly = new AssemblyReference("Test"),
				ContainingNamespace = "Test"
			};

			method = new MethodReference("Example1", PlatformTypes.Void)
			{
				ContainingType = type,
			};

			methodDefinition = host.ResolveReference(method) as MethodDefinition;

			var ch = new ClassHierarchyAnalysis(host);
			ch.Analyze();

			var dgml = DGMLSerializer.Serialize(ch);

			var chcga = new ClassHierarchyCallGraphAnalysis(host, ch);
			var cg = chcga.Analyze(methodDefinition.ToEnumerable());

			dgml = DGMLSerializer.Serialize(cg);
		}

		private static void RunGenericsTests()
		{
			const string root = @"..\..\..";
			const string input = root + @"\Test\bin\Debug\Test.dll";

			var host = new Host();

			PlatformTypes.Resolve(host);

			var loader = new Loader(host);
			loader.LoadAssembly(input);
			//loader.LoadCoreAssembly();

			var assembly = new AssemblyReference("Test");

			var typeA = new GenericParameterReference(GenericParameterKind.Type, 0);
			var typeB = new GenericParameterReference(GenericParameterKind.Type, 0);

			var type = new BasicType("NestedClass")
			{
				ContainingAssembly = assembly,
				ContainingNamespace = "Test",
				GenericParameterCount = 1,
				ContainingType = new BasicType("ExamplesGenerics")
				{
					ContainingAssembly = assembly,
					ContainingNamespace = "Test",
					GenericParameterCount = 1
				}				
			};

			//type.GenericArguments.Add(typeB);

			var typeDefinition = host.ResolveReference(type);

			var typeK = new GenericParameterReference(GenericParameterKind.Method, 0);
			var typeV = new GenericParameterReference(GenericParameterKind.Method, 1);

			var typeKeyValuePair = new BasicType("KeyValuePair")
			{
				ContainingAssembly = new AssemblyReference("mscorlib"),
				ContainingNamespace = "System.Collections.Generic",
				GenericParameterCount = 2
			};

			typeKeyValuePair.GenericArguments.Add(typeK);
			typeKeyValuePair.GenericArguments.Add(typeV);

			var method = new MethodReference("ExampleGenericMethod", typeKeyValuePair)
			{
				ContainingType = type,
				GenericParameterCount = 2
			};

			//method.GenericArguments.Add(typeK);
			//method.GenericArguments.Add(typeV);

			method.Parameters.Add(new MethodParameterReference(typeA));
			method.Parameters.Add(new MethodParameterReference(typeB));
			method.Parameters.Add(new MethodParameterReference(typeK));
			method.Parameters.Add(new MethodParameterReference(typeV));
			method.Parameters.Add(new MethodParameterReference(typeKeyValuePair));

			var methodDefinition = host.ResolveReference(method) as MethodDefinition;
			var calls = methodDefinition.Body.Instructions.OfType<Bytecode.MethodCallInstruction>();

			foreach (var call in calls)
			{
				methodDefinition = host.ResolveReference(call.Method) as MethodDefinition;
			}
		}

		static void Main(string[] args)
		{
			//RunSomeTests();
			RunGenericsTests();

			System.Console.WriteLine("Done!");
			System.Console.ReadKey();
		}
	}
}
