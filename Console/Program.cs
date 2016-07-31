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
using Backend.Model;
using Backend.Utils;
using Model.ThreeAddressCode.Instructions;
using Model.ThreeAddressCode.Values;
using Model.ThreeAddressCode.Visitor;
using Model.ThreeAddressCode.Expressions;
using System.IO;

namespace Console
{
    //public interface IDependencyGraph
    //{
    //    string AddVertex(DependencyNode vertex, ISet<string> edges);
    //    ISet<string> Slice();

    //    void PrintGraph(string writeToFile);
    //}
    class DependencyInfo
    {
        public PTGNode SymbolicObject { get; private set; }
        public string Traceable { get; private set; }

        public DependencyInfo(PTGNode symObj, string traceable)
        {
            SymbolicObject = symObj;
            Traceable = traceable;
        }


        public override bool Equals(object obj)
        {
            var oth = obj as DependencyInfo;
            return oth.SymbolicObject.Equals(SymbolicObject)
                && oth.Traceable.Equals(Traceable);
        }
        public override int GetHashCode()
        {
            return SymbolicObject.GetHashCode() + Traceable.GetHashCode();
        }
        public override string ToString()
        {
            return String.Format("{0}:{1}.{2}", SymbolicObject.Offset, SymbolicObject.Type, Traceable);
        }
    }

    class DependencyGraph // : IDependencyGraph
    {
        Graph<DependencyInfo, string> graph = new Graph<DependencyInfo, string>();
        public void AddVertex(DependencyInfo vertex)
        {
            graph.AddNode(vertex);
        }

        public void ConnectVertex(DependencyInfo src, DependencyInfo dst)
        {
            graph.ConnectNodes(src, dst);
        }

        public void PrintGraph(string writeToFile)
        {
            throw new NotImplementedException();
        }
        public override string ToString()
        {
            var result = "";
            foreach (var n in graph.Nodes)
            {
                result += String.Format("{0}->{1}\n", n.Data, n.Successors.Select(n2 => n2.Data).ToArray());
            }
            return result;
        }
    }

    class SongTaoDependencyAnalysis
    {
        private Host host;
        private DataFlowAnalysisResult<PointsToGraph>[] ptAnalysisResult;
        private MethodDefinition moveNextMethod;
        private IDictionary<IVariable, IExpression> equalities;
        private MethodDefinition entryMethod;
        private IList<InstanceFieldAccess> specialFields; 

        public SongTaoDependencyAnalysis(Host host,
                                        MethodDefinition method,
                                        MethodDefinition entryMethod)
        {
            this.host = host;
            this.moveNextMethod = method;
            this.entryMethod = entryMethod;
            this.equalities = new Dictionary<IVariable, IExpression>();
        }

        public void AnalyzeMoveNextMethod()
        {
            var cfgEntry = DoAnalysisPhases(entryMethod, host, false);

            var especialFields = cfgEntry.ForwardOrder[1].Instructions.OfType<StoreInstruction>()
                .Where(st => st.Result is InstanceFieldAccess).Select(st => st.Result as InstanceFieldAccess);
            this.specialFields = especialFields.ToList();

            Backend.Model.ControlFlowGraph cfg = DoAnalysisPhases(this.moveNextMethod, this.host);

            var pointsTo = new PointsToAnalysis(cfg, this.moveNextMethod);
            this.ptAnalysisResult = pointsTo.Analyze();

            this.PropagateExpressions(cfg);
            this.AnalyzeScopeMethods(cfg, ptAnalysisResult);

            //var sorted_nodes = cfg.ForwardOrder;
            //var ptgExit = ptAnalysisResult[cfg.Exit.Id].Output;
            //ptgExit.RemoveTemporalVariables();
            //ptgExit.RemoveDerivedVariables();
            //var ptgDGML = DGMLSerializer.Serialize(ptgExit);

            var dgml = DGMLSerializer.Serialize(cfg);

        }

        void AnalyzeScopeMethods(ControlFlowGraph cfg, DataFlowAnalysisResult<PointsToGraph>[] ptgs)
        {

            var iteratorAnalysis = new IteratorStateAnalysis(cfg, ptgs, this.equalities);
            var result = iteratorAnalysis.Analyze();

            var dependencyAnalysis = new IteratorDependencyAnalysis(cfg, ptgs, this.equalities);
            var resultDepAnalysis = dependencyAnalysis.Analyze();

            var node = cfg.Exit;
            System.Console.Out.WriteLine("At {0}\nBefore {1}\nAfter {2}\n", node.Id, resultDepAnalysis[node.Id].Input, resultDepAnalysis[node.Id].Output);

            //foreach (var node in cfg.ForwardOrder)
            //{
            //    System.Console.Out.WriteLine("At {0}\nBefore {1}\nAfter {2}\n", node.Id, resultDepAnalysis[node.Id].Input, resultDepAnalysis[node.Id].Output);
            //    //System.Console.Out.WriteLine(String.Join(Environment.NewLine, node.Instructions));
            //}
        }

        private static Backend.Model.ControlFlowGraph DoAnalysisPhases(MethodDefinition method, Host host, bool inline = true)
        {
            var disassembler = new Disassembler(method);
            var methodBody = disassembler.Execute();
            method.Body = methodBody;

            if (inline)
            {
                DoInlining(method, host, methodBody);
            }

            var cfAnalysis = new ControlFlowAnalysis(method.Body);
            var cfg = cfAnalysis.GenerateNormalControlFlow();

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


            var analysis = new TypeInferenceAnalysis(cfg);
            analysis.Analyze();

            var copyProgapagtion = new ForwardCopyPropagationAnalysis(cfg);
            copyProgapagtion.Analyze();
            copyProgapagtion.Transform(methodBody);

            var backwardCopyProgapagtion = new BackwardCopyPropagationAnalysis(cfg);
            backwardCopyProgapagtion.Analyze();
            backwardCopyProgapagtion.Transform(methodBody);

            var liveVariables = new LiveVariablesAnalysis(cfg);
            var resultLiveVar = liveVariables.Analyze();


            var ssa = new StaticSingleAssignment(methodBody, cfg);
            ssa.Transform();
            methodBody.UpdateVariables();


            method.Body = methodBody;

            // We use live variables to remove unnecessary phi nodes
            foreach (var cfgNode in cfg.ForwardOrder)
            {
                var liveVars = resultLiveVar[cfgNode.Id].Input;
                var insToRemove = new HashSet<Instruction>();

                foreach (var def in cfgNode.Instructions.OfType<PhiInstruction>().ToArray())
                {
                    var derived = def.Result as DerivedVariable;
                    if (!liveVars.Contains(derived.Original))
                    {
                        cfgNode.Instructions.Remove(def);
                    }
                }

            }
            methodBody.UpdateVariables();

            var dgml = DGMLSerializer.Serialize(cfg);
            return cfg;
        }

        private static void DoInlining(MethodDefinition method, Host host, MethodBody methodBody)
        {
            var methodCalls = methodBody.Instructions.OfType<MethodCallInstruction>().ToList();
            foreach (var methodCall in methodCalls)
            {
                var calleeM = host.ResolveReference(methodCall.Method);
                var callee = calleeM as MethodDefinition;
                if (callee != null)
                {
                    // var calleeCFG = DoAnalysisPhases(callee, host);
                    var disassemblerCallee = new Disassembler(callee);
                    var methodBodyCallee = disassemblerCallee.Execute();
                    callee.Body = methodBodyCallee;
                    methodBody.Inline(methodCall, callee.Body);
                }
            }

            methodBody.UpdateVariables();

            method.Body = methodBody;
        }

        #region Methods to Compute a sort of propagation of Equalities (should be moved to extensions or utils)
        private void PropagateExpressions(ControlFlowGraph cfg)
        {
            foreach (var node in cfg.ForwardOrder)
            {
                this.PropagateExpressions(node);
            }
        }

        private void PropagateExpressions(CFGNode node)
        {
            foreach (var instruction in node.Instructions)
            {
                this.PropagateExpressions(instruction);
            }
        }

        private void PropagateExpressions(IInstruction instruction)
        {
            var definition = instruction as DefinitionInstruction;

            if (definition != null && definition.HasResult)
            {
                var expr = definition.ToExpression().ReplaceVariables(equalities);
                equalities.Add(definition.Result, expr);
            }
        }

        #endregion
    }


    class Program
    {
        private Host host;

        public object ContainingClassUnderAnalysis { get; private set; }
        public string ClassUnderAnalysis { get; private set; }
        public object MethodUnderAnalysis { get; private set; }

        public Program(Host host)
        {
            this.host = host;
        }

        //private void VisitMethods()
        //{
        //    var methods = host.Assemblies.SelectMany(a => a.RootNamespace.GetAllTypes())
        //                                 .SelectMany(t => t.Members.OfType<MethodDefinition>())
        //                                 .Where(md => md.Body != null);

        //    foreach (var method in methods)
        //    {
        //        VisitMethod(method);
        //    }
        //}
        //private void VisitMethod(MethodDefinition method)
        //{
        //    System.Console.WriteLine(method.Name);

        //    Backend.Model.ControlFlowGraph cfg = DoAnalysisPhases(method, host);

        //    var pointsTo = new PointsToAnalysis(cfg, method);
        //    var result = pointsTo.Analyze();

        //    var dependencyAnalysis = new AlternativeDependencyAnalysis(method, cfg, result);
        //    dependencyAnalysis.Analyze();

        //    var dgml = DGMLSerializer.Serialize(cfg);

        //    //dgml = DGMLSerializer.Serialize(host, typeDefinition);
        //}


        static void Main(string[] args)
        {
            //const string root = @"C:\Users\t-diga\Source\Repos\ScopeExamples\ILAnalyzer\"; // @"..\..\..";
            //const string input = root + @"\bin\Debug\ILAnalyzer.exe";

            const string root = @"c:\users\t-diga\source\repos\scopeexamples\metting\";
            const string input = root + @"\__scopecodegen__.dll";

            AnalyzeDll(input,ScopeMethodKind.Reducer);

            //AnalyzeScopeScript(new string[] { @"D:\ScriptExamples\Files", @"D:\Temp\", "Reducer" } );

            System.Console.ReadKey();

        }

        enum ScopeMethodKind {  Producer, Reducer };

        private static void AnalyzeDll(string input, ScopeMethodKind kind)
        {
            var host = new Host();
            PlatformTypes.Resolve(host);
            //host.Assemblies.Add(assembly);

            var loader = new Loader(host);
            loader.LoadAssembly(input);

            // loader.LoadCoreAssembly();

            var program = new Program(host);

            var classFilter = "";
            var clousureFilter = "<Reduce>d__";
            var entryMethod = "Reduce";
            program.ContainingClassUnderAnalysis = "CVBaseDataSummaryReducer";
            if (kind == ScopeMethodKind.Reducer)
            {
                classFilter = "Reducer";
                program.ClassUnderAnalysis = "<Reduce>d__";
            }
            else
            {
                classFilter = "Producer";
                clousureFilter = "<Produce>d__";
                entryMethod = "Produce";
                program.ClassUnderAnalysis = "<Produce>d__";
            }
            program.MethodUnderAnalysis = "MoveNext";


            var candidateClasses = host.Assemblies.SelectMany(a => a.RootNamespace.GetAllTypes().OfType<ClassDefinition>())
                            .Where(c => c.Base!=null && c.Base.Name==classFilter);
            //foreach (var t in types)
            //{
            //    System.Console.WriteLine("{0}", t.FullPathName());
            //}

            if (candidateClasses.Any())
            {
                foreach (var candidateClass in candidateClasses)
                {
                    var assembly = host.Assemblies.Where(a => a.Name == candidateClass.Name);
                    var candidateClousures = candidateClass.Types.OfType<ClassDefinition>()
                                    .Where(c => c.Name.StartsWith(clousureFilter));
                    var methods = candidateClousures.SelectMany(t => t.Members.OfType<MethodDefinition>())
                                                .Where(md => md.Body != null
                                                && md.Name.Equals(program.MethodUnderAnalysis));
                    if (methods.Any())
                    {
                        var entryMethodDef = candidateClass.Methods.Where(m => m.Name == entryMethod).Single();
                        System.Console.WriteLine("Analyzing {0} on class {1}.{2} for {3}", program.MethodUnderAnalysis, program.ContainingClassUnderAnalysis, program.ClassUnderAnalysis, input);
                        var dependencyAnalysis = new SongTaoDependencyAnalysis(host, methods.First(), entryMethodDef);
                        dependencyAnalysis.AnalyzeMoveNextMethod();
                        System.Console.WriteLine("Done!");
                    }
                    else
                    {
                        System.Console.WriteLine("No method {0} on class {1} in {2}", program.MethodUnderAnalysis, candidateClass.FullName, input);
                    }
                }
            }
            else
            {
                System.Console.WriteLine("No {0} class in {1}", kind, input);
            }
        }

        public static void AnalyzeScopeScript(string[] args)
        {
            var inputFolder = args[0];
            var outputFolder = args[1];
            var kind = args[2];
            string[] files = Directory.GetFiles(inputFolder, "__ScopeCodeGen__.dll", SearchOption.AllDirectories);
            foreach(var dllToAnalyze in files)
            {
                System.Console.WriteLine("Analyzing {0}", dllToAnalyze);
                AnalyzeDll(dllToAnalyze,ScopeMethodKind.Reducer);
       
            }
            System.Console.WriteLine("Done!");
            System.Console.ReadKey();


        }
    }

    #region Alternative version using classic dependency analysis (needs to be updated)
    class AlternativeDependencyAnalysis
    {
        private class AlternativeDependencyAnalyzer : InstructionVisitor
        {
            private CFGNode cfgNode;
            private PointsToGraph ptg;
            private AlternativeDependencyAnalysis depAnalysis;
            public AlternativeDependencyAnalyzer(CFGNode cfgNode, PointsToGraph ptg, AlternativeDependencyAnalysis depAnalysis)
            {
                this.cfgNode = cfgNode;
                this.ptg = ptg;

                this.depAnalysis = depAnalysis;
            }
            public override void Visit(StoreInstruction instruction)
            {
                var store = instruction as StoreInstruction;
                if (store.Result is InstanceFieldAccess)
                {
                    var fieldAccess = store.Result as InstanceFieldAccess;
                    var access = fieldAccess.FieldName;

                    var lasDefs = depAnalysis.LastDefGet(store.Operand);
                    depAnalysis.SetDataDependency((int)instruction.Offset, lasDefs);
                    depAnalysis.LastDefSet(fieldAccess.Instance, fieldAccess.Field, (int)instruction.Offset, ptg);
                }
            }
            public override void Visit(LoadInstruction instruction)
            {
                var load = instruction as LoadInstruction;
                if (load.Operand is Constant)
                {
                }
                else if (load.Operand is IVariable)
                {
                    var variable = load.Operand as IVariable;
                    var lastDefs = depAnalysis.LastDefGet(variable);
                    depAnalysis.SetDataDependency((int)load.Offset, lastDefs);
                }

                else if (load.Operand is InstanceFieldAccess)
                {
                    var fieldAccess = load.Operand as InstanceFieldAccess;
                    var lastDefs = depAnalysis.LastDefGet(fieldAccess.Instance, fieldAccess.Field, ptg);
                    depAnalysis.SetDataDependency((int)load.Offset, lastDefs);
                }
                depAnalysis.LastDefSet(load.Result, (int)load.Offset);
            }
            public override void Visit(BinaryInstruction instruction)
            {
                base.Visit(instruction);
            }
            public override void Visit(MethodCallInstruction instruction)
            {
                var methodCall = instruction;
                if (methodCall.Method.Name.Equals(".ctor") && methodCall.HasResult)
                {
                    var variable = methodCall.Arguments[0] as IVariable;
                    var lastDefs = depAnalysis.LastDefGet(variable);
                    depAnalysis.SetDataDependency((int)methodCall.Offset, lastDefs);
                    depAnalysis.LastDefSet(methodCall.Result, (int)methodCall.Offset);
                }
                else
                {
                    MyDefault(instruction);
                    // base.Visit(instruction);
                }
            }


            public override void Visit(CreateObjectInstruction instruction)
            {
                MyDefault(instruction);
                //base.Visit(instruction);
            }
            public override void Visit(ConvertInstruction instruction)
            {
                MyDefault(instruction);
            }
            public void MyDefault(Instruction instruction)
            {
                var uses = instruction.UsedVariables;
                var defs = instruction.ModifiedVariables;
                foreach (var def in defs)
                {
                    foreach (var use in uses)
                    {
                        if (use is IVariable)
                        {
                            var variable = use as IVariable;
                            var lastDefs = depAnalysis.LastDefGet(variable);
                            depAnalysis.SetDataDependency((int)instruction.Offset, lastDefs);
                        }
                        else
                        { }

                    }
                    depAnalysis.LastDefSet(def, (int)instruction.Offset);
                }

            }
        }


        private void SetDataDependency(int offset, IEnumerable<int> locations)
        {
            foreach (var loc in locations)
            {
                depGraph.ConnectVertex(offset, loc);
            }
        }

        private void LastDefSet(IVariable v, int location)
        {
            ICollection<int> lastDefs = InternalGetLastDefs(v);
            lastDefs.Add(location);
        }
        private void LastDefSet(IVariable v, IEnumerable<int> locations)
        {
            ICollection<int> lastDefs = InternalGetLastDefs(v);
            lastDefs.AddRange(locations);
        }

        private ICollection<int> LastDefGet(IVariable v)
        {
            ICollection<int> lastDefs = InternalGetLastDefs(v);
            return lastDefs;
        }

        private ICollection<int> InternalGetLastDefs(IVariable v)
        {
            ICollection<int> lastDefs = new HashSet<int>();
            if (LastDefsVar.ContainsKey(v))
            {
                lastDefs = LastDefsVar[v];
            }
            else
            {
                LastDefsVar[v] = lastDefs;
            }

            return lastDefs;
        }

        private void LastDefSet(PTGNode ptgNode, IFieldReference f, int location)
        {
            ICollection<int> lastDefs = InternalGetLastDefs(ptgNode, f);
            lastDefs.Add(location);
        }
        private void LastDefSet(PTGNode ptgNode, IFieldReference f, IEnumerable<int> locations)
        {
            ICollection<int> lastDefs = InternalGetLastDefs(ptgNode, f);
            lastDefs.AddRange(locations);

        }
        private ICollection<int> LastDefGet(PTGNode ptgNode, IFieldReference f)
        {
            ICollection<int> lastDefs = InternalGetLastDefs(ptgNode, f);
            return lastDefs;
        }

        private ICollection<int> InternalGetLastDefs(PTGNode ptgNode, IFieldReference f)
        {
            IDictionary<IFieldReference, ICollection<int>> lastDefsDict = new Dictionary<IFieldReference, ICollection<int>>();
            ICollection<int> lastDefs = new HashSet<int>();
            if (LastDefsPtg.ContainsKey(ptgNode))
            {
                lastDefsDict = LastDefsPtg[ptgNode];
            }
            else
            {
                LastDefsPtg[ptgNode] = lastDefsDict;
            }
            if (lastDefsDict.ContainsKey(f))
            {
                lastDefs = lastDefsDict[f];
            }
            else
            {
                lastDefsDict[f] = lastDefs;
            }

            return lastDefs;
        }

        private ICollection<int> LastDefGet(IVariable variable, IFieldReference field, PointsToGraph ptg)
        {
            var query = ptg.GetTargets(variable).SelectMany(ptgNode => LastDefGet(ptgNode, field));
            var result = new HashSet<int>();
            result.AddRange(query);
            return result;
        }
        private void LastDefSet(IVariable variable, IFieldReference field, int location, PointsToGraph ptg)
        {
            var query = ptg.GetTargets(variable);
            foreach (var ptgNode in query)
            {
                LastDefSet(ptgNode, field, location);
            }
        }


        private ControlFlowGraph cfg;
        private InstructionDependencyGraph depGraph;

        IDictionary<IVariable, ICollection<int>> LastDefsVar = new Dictionary<IVariable, ICollection<int>>();
        IDictionary<PTGNode, IDictionary<IFieldReference, ICollection<int>>>
            LastDefsPtg = new Dictionary<PTGNode, IDictionary<IFieldReference, ICollection<int>>>();
        private DataFlowAnalysisResult<PointsToGraph>[] ptAnalysisResult;
        private MethodDefinition method;

        private IDictionary<IVariable, IExpression> equalities;



        public AlternativeDependencyAnalysis(MethodDefinition method, ControlFlowGraph cfg, DataFlowAnalysisResult<PointsToGraph>[] ptAnalysisResult)
        {
            this.method = method;
            this.cfg = cfg;
            this.ptAnalysisResult = ptAnalysisResult;
            this.depGraph = new InstructionDependencyGraph(cfg);
            this.equalities = new Dictionary<IVariable, IExpression>();
        }

        public void Analyze()
        {
            foreach (var p in this.method.Body.Parameters)
            {
                LastDefSet(p, -1);
            }
            var sorted_nodes = cfg.ForwardOrder;

            for (var i = 0; i < sorted_nodes.Length; ++i)
            {
                var cfgNode = sorted_nodes[i];
                var ptg = ptAnalysisResult[cfgNode.Id].Output;
                foreach (var instruction in cfgNode.Instructions)
                {
                    var inferer = new AlternativeDependencyAnalyzer(cfgNode, ptg, this);

                    var uses = instruction.UsedVariables;
                    var defs = instruction.ModifiedVariables;
                    inferer.Visit(cfgNode);
                }
            }
            System.Console.WriteLine("Finish Dep analysis");
            System.Console.WriteLine(depGraph);
            var depGraphDGML = DGMLSerializer.Serialize(depGraph);

            var cfgGraphDGML = DGMLSerializer.Serialize(cfg);
            var ptgExit = ptAnalysisResult[cfg.Exit.Id].Output;

            this.PropagateExpressions(cfg);
        }

        private bool CheckIterationStateModification(IInstruction instruction, ref int state)
        {
            bool res = false;
            if (instruction is StoreInstruction)
            {
                var storeStmt = instruction as StoreInstruction;
                if (storeStmt.Result is InstanceFieldAccess)
                {
                    var access = storeStmt.Result as InstanceFieldAccess;
                    if (access.Field.Name == "<>1__state")
                    {
                        res = true;
                        state = int.Parse(this.equalities[storeStmt.Operand].ToString());

                    }
                }
            }
            return res;
        }
        private int AnalyzeIteratorState(int state, IInstruction instruction)
        {
            // Need to add logic to determine the state
            // 
            var isIterator = CheckIterationStateModification(instruction, ref state);

            return state;
        }
        private void ComputeDependencyGraph(ControlFlowAnalysis cfg)
        {
            //var dependencyGraph = new DependencyGraph();

            //foreach (var cfgNode in cfg.ForwardOrder)
            //{
            //    var ptg = result[cfgNode.Id].Output;
            //    foreach (var instruction in cfgNode.Instructions)
            //    {
            //        var uses = instruction.UsedVariables;
            //        var defs = instruction.ModifiedVariables;
            //        var access = "";
            //        //if (instruction is StoreInstruction)
            //        //{
            //        //    var store = instruction as StoreInstruction;
            //        //    if (store.Result is InstanceFieldAccess)
            //        //    {
            //        //        var fieldAccess = store.Result as InstanceFieldAccess;
            //        //        access = fieldAccess.FieldName;
            //        //        defs.Add(store.Operand);
            //        //        LastDefSet(store.Operand, fieldAccess.Field, cfgNode.Id, ptg);
            //        //    }

            //        //}
            //        //else
            //        //{

            //        //}
            //        //// TODO: Complete
            //        foreach (var def in defs)
            //        {
            //            var v = def.Variables.Single();
            //            if (ptg.Variables.Contains(v))
            //            {
            //                var ptgNodes = ptg.GetTargets(v);
            //                foreach (var ptgNode in ptgNodes)
            //                {
            //                    var depNode = new DependencyInfo(ptgNode, access);
            //                    dependencyGraph.AddVertex(depNode);
            //                    var useAccess = "";
            //                    if (instruction is LoadInstruction)
            //                    {
            //                        var load = instruction as LoadInstruction;
            //                        if (load.Operand is InstanceFieldAccess)
            //                        {
            //                            var fieldAccess = load.Operand as InstanceFieldAccess;
            //                            useAccess = fieldAccess.FieldName;
            //                            uses.Add(load.Result);
            //                        }
            //                    }

            //                    foreach (var use in uses)
            //                    {
            //                        var v2 = use.Variables.Single();
            //                        if (ptg.Variables.Contains(v2))
            //                        {
            //                            var ptgUseNodes = ptg.GetTargets(v2);
            //                            foreach (var ptgNode2 in ptgUseNodes)
            //                            {
            //                                var useNode = new DependencyInfo(ptgNode2, useAccess);
            //                                dependencyGraph.ConnectVertex(depNode, useNode);
            //                            }
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //   }
            //}
            //System.Console.WriteLine(dependencyGraph);
        }


        private void PropagateExpressions(ControlFlowGraph cfg)
        {
            foreach (var node in cfg.ForwardOrder)
            {
                this.PropagateExpressions(node);
            }
        }

        private void PropagateExpressions(CFGNode node)
        {
            foreach (var instruction in node.Instructions)
            {
                this.PropagateExpressions(instruction);
            }
        }

        private void PropagateExpressions(IInstruction instruction)
        {
            var definition = instruction as DefinitionInstruction;

            if (definition != null && definition.HasResult)
            {
                var expr = definition.ToExpression().ReplaceVariables(equalities);
                equalities.Add(definition.Result, expr);
            }
        }
    }
    #endregion
}