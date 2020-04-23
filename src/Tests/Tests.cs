using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Tests
{
    public class Tests
    {
        public static string GetResourceAsString(string res)
        {
            Stream sourceStream = System.Reflection.Assembly.GetAssembly(typeof(Tests)).GetManifestResourceStream(res);
            System.IO.StreamReader streamReader = new System.IO.StreamReader(sourceStream);
            string source = streamReader.ReadToEnd();
            streamReader.Dispose();
            return source;
        }

        public class TestCaseOptions
        {
            public string File, ClassName, MethodName;
            public object Parameter = null;

            public TestCaseOptions(string file, string className, string methodName, object parameter = null)
            {
                File = file;
                ClassName = className;
                MethodName = methodName;
                Parameter = parameter;
            }

            public override string ToString()
            {
                return string.Format("File: {0} Class: {1} Method: {2}", File, ClassName, MethodName);
            }
        }
        public class MyDataClass
        {
            public static IEnumerable TestCases
            {
                get
                {
                    yield return new TestCaseData(new TestCaseOptions("AccessIntField.cs", "Test.Program", "Test")); //0
                    yield return new TestCaseData(new TestCaseOptions("Sum.cs", "Test.Program", "Test")); //1
                    yield return new TestCaseData(new TestCaseOptions("RefParameter.cs", "Test.Program", "Test")); //2
                    yield return new TestCaseData(new TestCaseOptions("RefParameter.cs", "Test.Program", "TestBool")); //3
                    yield return new TestCaseData(new TestCaseOptions("RefParameter.cs", "Test.Program", "TestFloat")); // 4
                    yield return new TestCaseData(new TestCaseOptions("RefParameter.cs", "Test.Program", "TestObject")); // 5
                    yield return new TestCaseData(new TestCaseOptions("RefParameter2.cs", "Test.Program", "TestInt")); // 6
                    yield return new TestCaseData(new TestCaseOptions("RefParameter2.cs", "Test.Program", "TestBool")); // 7
                    yield return new TestCaseData(new TestCaseOptions("RefParameter2.cs", "Test.Program", "TestFloat")); // 8
                    yield return new TestCaseData(new TestCaseOptions("RefParameter2.cs", "Test.Program", "TestObject")); // 9
                    yield return new TestCaseData(new TestCaseOptions("InOutParameters.cs", "Test.Program", "TestIn")); // 10
                    yield return new TestCaseData(new TestCaseOptions("InOutParameters.cs", "Test.Program", "TestOut")); // 11

                    yield return new TestCaseData(new TestCaseOptions("IfConditionals.cs", "Test.Program", "Test", 49)); // 12
                    yield return new TestCaseData(new TestCaseOptions("IfConditionals.cs", "Test.Program", "Test", 199)); // 13
                    yield return new TestCaseData(new TestCaseOptions("IfConditionals.cs", "Test.Program", "Test",-2)); // 14

                    yield return new TestCaseData(new TestCaseOptions("Loop.cs", "Test.Program", "Test", 0)); // 15
                    yield return new TestCaseData(new TestCaseOptions("Loop.cs", "Test.Program", "Test", 8)); // 16

                    yield return new TestCaseData(new TestCaseOptions("Arrays.cs", "Test.Program", "Test1")); // 17
                    yield return new TestCaseData(new TestCaseOptions("Arrays.cs", "Test.Program", "Test2")); // 18
                    yield return new TestCaseData(new TestCaseOptions("Arrays.cs", "Test.Program", "Test3")); // 19
                    yield return new TestCaseData(new TestCaseOptions("Arrays.cs", "Test.Program", "Test4")); // 20

                    yield return new TestCaseData(new TestCaseOptions("Generics.cs", "Test.Program", "Test")); // 21
                    yield return new TestCaseData(new TestCaseOptions("Generics.cs", "Test.Program", "Test1")); // 22

                    yield return new TestCaseData(new TestCaseOptions("ExternGeneric.cs", "Test.Program", "Test")); // 23
                    yield return new TestCaseData(new TestCaseOptions("ExternGeneric.cs", "Test.Program", "Test1")); // 24

                    yield return new TestCaseData(new TestCaseOptions("GenericMethods.cs", "Test.Program", "Test")); // 25

                    yield return new TestCaseData(new TestCaseOptions("GenericsWhere.cs", "Test.Program", "Test")); // 26

                    yield return new TestCaseData(new TestCaseOptions("Interfaces.cs", "Test.Program", "Test1", 10)); // 27
                    yield return new TestCaseData(new TestCaseOptions("Interfaces.cs", "Test.Program", "Test2", 10)); // 28
                    yield return new TestCaseData(new TestCaseOptions("Interfaces.cs", "Test.Program", "Test3")); // 29

                    yield return new TestCaseData(new TestCaseOptions("Inheritance.cs", "Test.Program", "Test1", 10)); //30
                    yield return new TestCaseData(new TestCaseOptions("Inheritance.cs", "Test.Program", "Test2", 10)); // 31
                    yield return new TestCaseData(new TestCaseOptions("Inheritance.cs", "Test.Program", "Test3")); // 32

                    yield return new TestCaseData(new TestCaseOptions("GenericMethod2.cs", "Test.Program", "Test0")); // 33
                    yield return new TestCaseData(new TestCaseOptions("GenericMethod2.cs", "Test.Program", "Test1")); // 34

                    yield return new TestCaseData(new TestCaseOptions("ArrayList.cs", "Test.Program", "Test0")); // 35
                    yield return new TestCaseData(new TestCaseOptions("NestedGenerics.cs", "Test.Program", "Test0")); // 36
                    yield return new TestCaseData(new TestCaseOptions("ArrayList.cs", "Test.Program", "Test1")); // 37
                    yield return new TestCaseData(new TestCaseOptions("ArrayList.cs", "Test.Program", "Test2")); // 38
                    yield return new TestCaseData(new TestCaseOptions("ArrayList.cs", "Test.Program", "Test3")); // 39
                    yield return new TestCaseData(new TestCaseOptions("ArrayList.cs", "Test.Program", "Test4")); // 40
                    yield return new TestCaseData(new TestCaseOptions("ArrayList.cs", "Test.Program", "Test5")); // 41
                    yield return new TestCaseData(new TestCaseOptions("ArrayList.cs", "Test.Program", "Test6")); // 42
                    yield return new TestCaseData(new TestCaseOptions("ArrayList.cs", "Test.Program", "Test7")); // 43
                    yield return new TestCaseData(new TestCaseOptions("ArrayList.cs", "Test.Program", "Test8")); // 44
                    yield return new TestCaseData(new TestCaseOptions("ArrayList.cs", "Test.Program", "Test9")); // 45
                    yield return new TestCaseData(new TestCaseOptions("ArrayList.cs", "Test.Program", "Test10")); // 46
                    yield return new TestCaseData(new TestCaseOptions("ArrayList.cs", "Test.Program", "Test11")); // 47

                    yield return new TestCaseData(new TestCaseOptions("Delegates.cs", "Test.Program", "Test0")).Ignore("issues with function pointer types"); // 48
                    yield return new TestCaseData(new TestCaseOptions("Delegates.cs", "Test.Program", "Test1")).Ignore("issues with function pointer types"); // 49
                    yield return new TestCaseData(new TestCaseOptions("Delegates.cs", "Test.Program", "Test2")).Ignore("issues with function pointer types"); // 50

                    yield return new TestCaseData(new TestCaseOptions("Switch.cs", "Test.Program", "Test",1)); // 51
                    yield return new TestCaseData(new TestCaseOptions("Switch.cs", "Test.Program", "Test",2)); // 52
                    yield return new TestCaseData(new TestCaseOptions("Switch.cs", "Test.Program", "Test",3)); // 53

                    yield return new TestCaseData(new TestCaseOptions("Struct.cs", "Test.Program", "Test")); // 54

                    yield return new TestCaseData(new TestCaseOptions("InOutParameters2.cs", "Test.Program", "Test1")); // 55
                    yield return new TestCaseData(new TestCaseOptions("InOutParameters2.cs", "Test.Program", "Test2")); // 56
                    yield return new TestCaseData(new TestCaseOptions("InOutParameters2.cs", "Test.Program", "Test3")); // 57

                    yield return new TestCaseData(new TestCaseOptions("Arrays2.cs", "Test.Program", "Test")).Ignore("issues with function pointer types"); // 58

                    yield return new TestCaseData(new TestCaseOptions("IfConditionals2.cs", "Test.Program", "Test", 5)); // 59
                    yield return new TestCaseData(new TestCaseOptions("Loop2.cs", "Test.Program", "Test", 5)); // 60
                }
            }
        }

        // compares the result of the normal exectuable vs the one generated by the library
        private void TestSourceCodeByReturnValue(string sourceCodeFile, string type, string method, object parameter, ProviderType providerType, bool tac)
        {
            string source = GetResourceAsString(sourceCodeFile);
            TestHandler testHandler = new TestHandler();
            object[] param = parameter == null ? null : new object[1] { parameter };

            object r = testHandler.Test(source, type, method, param, tac, providerType);
            object expectedResult = testHandler.RunOriginalCode(source, type, method, param);

            Assert.AreEqual(expectedResult, r);
        }

        [TestCaseSource(typeof(MyDataClass), "TestCases")]
        public void Sil2Cil(TestCaseOptions options)
        {
            TestSourceCodeByReturnValue("Tests.Resources." + options.File, options.ClassName, options.MethodName, options.Parameter, ProviderType.CECIL, false);
        }

        [TestCaseSource(typeof(MyDataClass), "TestCases")]
        public void Tac2Cil(TestCaseOptions options)
        {
            TestSourceCodeByReturnValue("Tests.Resources." + options.File, options.ClassName, options.MethodName, options.Parameter, ProviderType.CECIL, true);
        }

        [Test]
        public void DSA_Sil2Cil()
        {
            Model.Host host = new Model.Host();
            Model.ILoader provider = new CecilCodeLoader.Loader(host);
            string buildDir =
                Path.GetDirectoryName(System.Reflection.Assembly.GetAssembly(typeof(CecilCodeLoader.Loader))
                    .Location);

            // create temporary directory where we place generated dlls
            string tempDir = Utils.GetTemporaryDirectory();

            // sanity check for needed libraries
            // resourceDSALibrary is the library that is going to be processed
            // resourceDSANUnit has test cases for the DSA library
            string resourceDSALibrary = Path.Combine(buildDir, "Resources/DSA/Library/DSA.dll");
            string resourceDSANUnit = Path.Combine(buildDir, "Resources/DSA/NUnit/DSANUnitTests.dll");
            if (!File.Exists(resourceDSALibrary) || !File.Exists((resourceDSANUnit)))
            {
                throw new FileNotFoundException();
            }

            // read the DSA library and re compile it using our framework
            provider.LoadAssembly(resourceDSALibrary);
            CecilCodeGenerator.CecilCodeGenerator exporter = new CecilCodeGenerator.CecilCodeGenerator(host);
            exporter.WriteAssemblies(tempDir);

            // copy nunit test library to temp dir
            string dsaNUnit = Path.Combine(tempDir, "DSANUnitTests.dll");
            File.Copy(resourceDSANUnit, dsaNUnit);

            // execute nunit test suite
            NUnitLite.AutoRun autoRun = new NUnitLite.AutoRun(System.Reflection.Assembly.LoadFrom(dsaNUnit));
            string outputTxt = Path.Combine(tempDir, "output.txt");
            string outputCmd = "--out=" + outputTxt;
            autoRun.Execute(new string[1] { outputCmd });

            // check results
            string output = File.ReadAllText(outputTxt);
            Assert.IsTrue(output.Contains("Test Count: 618, Passed: 618"));
        }
    }

    public class ExampleTests
    {
        private static string CompileExample()
        {
            // The file is located at the Resources folder of this project.
            var source = Tests.GetResourceAsString("Tests.Resources.Example.cs");
            // Compiler encapsulates calls to Roslyn.
            var compiler = new Compiler();
            var output = compiler.CompileSource(source);
            return output;
        }

        [Test]
        public void SimplifiedBytecode()
        {
            var assemblyPath = CompileExample();
            // Simply put, a host is a collection of loaded assemblies.
            var host = new Model.Host();
            // A loader is the frontend of the framework.
            // It is able to load a .net assembly and transform it into
            // analyses-net representations.
            // All loaded assemblies are added to the host.
            var loader = new CecilCodeLoader.Loader(host);
            // This assembly variable holds an analysis-net's object modeling the original .net assembly.
            var assembly = loader.LoadAssembly(assemblyPath);

            // Next, we are going to iterate over each defined type and print their method's bodies
            // Note: If there are nested types, you should check definedType.Types
            foreach (var definedType in assembly.RootNamespace.Types)
            {
                foreach (var method in definedType.Methods)
                {
                    if (method.HasBody)
                    {
                        // At first, a method's body is in a simplified bytecode form (aka sil).
                        // It is still a stack based representation.

                        TestContext.Out.WriteLine("Method: " + method.GenericName);
                        TestContext.Out.WriteLine(method.Body.ToString());
                    }
                }
            }
        }

        [Test]
        public void ThreeAddressCode()
        {
            var assemblyPath = CompileExample();
            // Simply put, a host is a collection of loaded assemblies.
            var host = new Model.Host();
            // A loader is the frontend of the framework.
            // It is able to load a .net assembly and transform it into
            // analyses-net representations.
            // All loaded assemblies are added to the host.
            var loader = new CecilCodeLoader.Loader(host);
            // This assembly variable holds an analysis-net's object modeling the original .net assembly.
            var assembly = loader.LoadAssembly(assemblyPath);

            // Next, we are going to iterate over each defined type and print their method's bodies
            // Note: If there are nested types, you should check definedType.Types
            foreach (var definedType in assembly.RootNamespace.Types)
            {
                foreach (var method in definedType.Methods)
                {
                    if (method.HasBody)
                    {
                        // At first, a method's body is in a simplified bytecode form (aka sil).
                        // It is still a stack based representation.

                        // Transform the simplified bytecode to the three-address code (not typed yet)
                        var disassembler = new TacAnalyses.Transformations.Disassembler(method);
                        var methodBody = disassembler.Execute();
                        method.Body = methodBody;

                        // Required for next transformations
                        var cfAnalysis = new TacAnalyses.Analyses.ControlFlowAnalysis(method.Body);
                        TacAnalyses.Model.ControlFlowGraph cfg = cfAnalysis.GenerateExceptionalControlFlow();

                        // It differentiates unrelated def-uses of the same variable (improves readability)
                        var splitter = new TacAnalyses.Analyses.WebAnalysis(cfg);
                        splitter.Analyze();
                        splitter.Transform();

                        methodBody.UpdateVariables();

                        // TypeInferenceAnalysis types each register (aka. variable) of the three-address code
                        var typeAnalysis = new TacAnalyses.Analyses.TypeInferenceAnalysis(cfg, method.ReturnType);
                        typeAnalysis.Analyze();

                        // Optionally you can also call to ForwardCopyPropagation and BackwardCopyPropagation to improve readability
                        // After that, you must call methodBody.UpdateVariables()

                        TestContext.Out.WriteLine("Method: " + method.GenericName);
                        TestContext.Out.WriteLine(method.Body.ToString());
                    }
                }
            }
        }

        [Test]
        public void SerializedControlFlowGraph()
        {
            var assemblyPath = CompileExample();
            // Simply put, a host is a collection of loaded assemblies.
            var host = new Model.Host();
            // A loader is the frontend of the framework.
            // It is able to load a .net assembly and transform it into
            // analyses-net representations.
            // All loaded assemblies are added to the host.
            var loader = new CecilCodeLoader.Loader(host);
            // This assembly variable holds an analysis-net's object modeling the original .net assembly.
            var assembly = loader.LoadAssembly(assemblyPath);

            var method = (from types in assembly.RootNamespace.Types
                                  from m in types.Methods
                                  where m.Name.Equals("Fibonacci")
                                  select m).Single();

            // Transform the simplified bytecode to the three-address code (not typed yet)
            var disassembler = new TacAnalyses.Transformations.Disassembler(method);
            var methodBody = disassembler.Execute();
            method.Body = methodBody;

            // Required for next transformations
            var cfAnalysis = new TacAnalyses.Analyses.ControlFlowAnalysis(method.Body);
            TacAnalyses.Model.ControlFlowGraph cfg = cfAnalysis.GenerateExceptionalControlFlow();

            // It differentiates unrelated def-uses of the same variable (improves readability)
            var splitter = new TacAnalyses.Analyses.WebAnalysis(cfg);
            splitter.Analyze();
            splitter.Transform();

            methodBody.UpdateVariables();

            // TypeInferenceAnalysis types each register (aka. variable) of the three-address code
            var typeAnalysis = new TacAnalyses.Analyses.TypeInferenceAnalysis(cfg, method.ReturnType);
            typeAnalysis.Analyze();

            var dotRepresentation = TacAnalyses.Serialization.DOTSerializer.Serialize(cfg);

            TestContext.Out.WriteLine("Method: " + method.GenericName);
            TestContext.Out.WriteLine(dotRepresentation);
        }

        [Test]
        public void TacInstructionVisitor()
        {
            var assemblyPath = CompileExample();
            // Simply put, a host is a collection of loaded assemblies.
            var host = new Model.Host();
            // A loader is the frontend of the framework.
            // It is able to load a .net assembly and transform it into
            // analyses-net representations.
            // All loaded assemblies are added to the host.
            var loader = new CecilCodeLoader.Loader(host);
            // This assembly variable holds an analysis-net's object modeling the original .net assembly.
            var assembly = loader.LoadAssembly(assemblyPath);

            var method = (from types in assembly.RootNamespace.Types
                          from m in types.Methods
                          where m.Name.Equals("Fibonacci")
                          select m).Single();

            // Transform the simplified bytecode to the three-address code (not typed yet)
            var disassembler = new TacAnalyses.Transformations.Disassembler(method);
            var methodBody = disassembler.Execute();
            method.Body = methodBody;

            // Required for next transformations
            var cfAnalysis = new TacAnalyses.Analyses.ControlFlowAnalysis(method.Body);
            TacAnalyses.Model.ControlFlowGraph cfg = cfAnalysis.GenerateExceptionalControlFlow();

            // It differentiates unrelated def-uses of the same variable (improves readability)
            var splitter = new TacAnalyses.Analyses.WebAnalysis(cfg);
            splitter.Analyze();
            splitter.Transform();

            methodBody.UpdateVariables();

            // TypeInferenceAnalysis types each register (aka. variable) of the three-address code
            var typeAnalysis = new TacAnalyses.Analyses.TypeInferenceAnalysis(cfg, method.ReturnType);
            typeAnalysis.Analyze();

            var visitor = new ExampleVisitor();
            visitor.Visit(methodBody);
            
            // This is also valid.
            // If you add or remove an instruction to a cfgNode, it won't affect the original method.Body
            // Although references to instructions are shared.
            foreach (var cfgNode in cfg.Nodes)
                visitor.Visit(cfgNode);
        }

        // If you need, a Model.Bytecode.Visitor.InstructionVisitor is available (which is for sil - simplified bytecode)
        class ExampleVisitor : Model.ThreeAddressCode.Visitor.InstructionVisitor
        {
            public override void Visit(Model.ThreeAddressCode.Instructions.MethodCallInstruction instruction) {
                TestContext.Out.WriteLine(instruction);
            }
        }
        [Test]
        public void ComputeCallGraph()
        {
            var assemblyPath = CompileExample();
            // Simply put, a host is a collection of loaded assemblies.
            var host = new Model.Host();
            // A loader is the frontend of the framework.
            // It is able to load a .net assembly and transform it into
            // analyses-net representations.
            // All loaded assemblies are added to the host.
            var loader = new CecilCodeLoader.Loader(host);
            // This assembly variable holds an analysis-net's object modeling the original .net assembly.
            var assembly = loader.LoadAssembly(assemblyPath);

            // This doesn't includes nested types
            var method = (from types in assembly.RootNamespace.Types
                          from m in types.Methods
                          where m.Name.Equals("Fibonacci")
                          select m).Single();

            // Transform the simplified bytecode to the three-address code (not typed yet)
            var disassembler = new TacAnalyses.Transformations.Disassembler(method);
            var methodBody = disassembler.Execute();
            method.Body = methodBody;

            // Required for next transformations
            var cfAnalysis = new TacAnalyses.Analyses.ControlFlowAnalysis(method.Body);
            TacAnalyses.Model.ControlFlowGraph cfg = cfAnalysis.GenerateExceptionalControlFlow();

            // It differentiates unrelated def-uses of the same variable (improves readability)
            var splitter = new TacAnalyses.Analyses.WebAnalysis(cfg);
            splitter.Analyze();
            splitter.Transform();

            methodBody.UpdateVariables();

            // TypeInferenceAnalysis types each register (aka. variable) of the three-address code
            var typeAnalysis = new TacAnalyses.Analyses.TypeInferenceAnalysis(cfg, method.ReturnType);
            typeAnalysis.Analyze();

            // call graph based on hierarchy information (to solve virtual calls)
            var cha = new TacAnalyses.Analyses.ClassHierarchyAnalysis();
            var callGraph = cha.Analyze(host);
            // one element per each call site to 'method'
            var invocationInfos = callGraph.GetInvocations(method);

            foreach (var invocationInfo in invocationInfos)
                TestContext.Out.WriteLine(invocationInfo);

            // call graph based on points to analysis (to solve virtual calls)
            var programAnalysisInfo = new TacAnalyses.Utils.ProgramAnalysisInfo();
            var pts = new TacAnalyses.Analyses.InterPointsToAnalysis(programAnalysisInfo);
            callGraph = pts.Analyze(method);
            invocationInfos = callGraph.GetInvocations(method);

            foreach (var invocationInfo in invocationInfos)
                TestContext.Out.WriteLine(invocationInfo);
        }
    }
}
