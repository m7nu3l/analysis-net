using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Backend.Model;
using Model.ThreeAddressCode.Instructions;
using Model.ThreeAddressCode.Visitor;
using Model.ThreeAddressCode.Values;
using Model.ThreeAddressCode.Expressions;
using static Backend.Analyses.IteratorState;
using Backend.Utils;
using Model.Types;

namespace Backend.Analyses
{
    public class IteratorState
    {
        public enum IteratorInternalState { TOP = -100, BOTTOM = -2, INITIALIZED = -3, CONTINUING = 1, END = -1 };

        public IteratorInternalState IntState = IteratorInternalState.BOTTOM;

        internal IteratorState()
        {
            this.IntState = IteratorInternalState.BOTTOM;
        }
        internal IteratorState(IteratorInternalState intState)
        {
            this.IntState = intState;
        }
        public IteratorState Clone()
        {
            return new IteratorState(this.IntState);
        }
        internal IteratorState Union(IteratorState right)
        {
            var intState = Join(this.IntState, right.IntState);
            return new IteratorState(intState);
        }
        private static IteratorInternalState Join(IteratorInternalState left, IteratorInternalState right)
        {
            IteratorInternalState res = IteratorInternalState.BOTTOM;
            switch (right)
            {
                case IteratorInternalState.BOTTOM:
                    res = left;
                    break;
                case IteratorInternalState.TOP:
                    res = IteratorInternalState.TOP;
                    break;
                default:
                    res = left == right ? left : IteratorInternalState.TOP;
                    break;
            }
            return res;
        }
        public bool LessEqual(IteratorState right)
        {
            var left = this;
            var res = true;
            switch (right.IntState)
            {
                case IteratorInternalState.BOTTOM:
                    res = false;
                    break;
                case IteratorInternalState.TOP:
                    res = true;
                    break;
                default:
                    res = left.IntState == right.IntState ? true : false;
                    break;
            }
            return res;
        }
        public override string ToString()
        {
            return IntState.ToString();
        }
        public override bool Equals(object obj)
        {
            var oth = obj as IteratorState;
            return oth.IntState.Equals(this.IntState);
        }
        public override int GetHashCode()
        {
            return IntState.GetHashCode();
        }
    }

    public class IteratorStateAnalysis : ForwardDataFlowAnalysis<IteratorState>
    {
       
        internal class MoveNextVisitor : InstructionVisitor
        {
            internal IteratorState State { get; }
            private IDictionary<IVariable, IExpression> equalities;
   
            internal MoveNextVisitor(IteratorStateAnalysis itAnalysis, IDictionary<IVariable, IExpression> equalitiesMap, IteratorState state)
            {
                this.State = state;
                this.equalities = equalitiesMap;
            }
            public override void Visit(StoreInstruction instruction)
            {
                var storeStmt = instruction;
                if (storeStmt.Result is InstanceFieldAccess)
                {
                    var access = storeStmt.Result as InstanceFieldAccess;
                    if (access.Field.Name == "<>1__state")
                    {
                        State.IntState = (IteratorInternalState)int.Parse(this.equalities[storeStmt.Operand].ToString());
                    }
                }
            }
        }

        private IDictionary<IVariable, IExpression> equalities;
        DataFlowAnalysisResult<PointsToGraph>[] ptgs;

        public IteratorStateAnalysis(ControlFlowGraph cfg, DataFlowAnalysisResult<PointsToGraph>[] ptgs, IDictionary<IVariable, IExpression> equalitiesMap) : base(cfg)
        {
            this.ptgs = ptgs;
            this.equalities = equalitiesMap;
        }

        protected override bool Compare(IteratorState left, IteratorState right)
        {
            return left.LessEqual(right);
        }

        protected override IteratorState Flow(CFGNode node, IteratorState input)
        {
            var oldInput = input.Clone();
            var visitor = new MoveNextVisitor(this, this.equalities, oldInput);
            visitor.Visit(node);
            return visitor.State;
        }

        protected override IteratorState InitialValue(CFGNode node)
        {
            return new IteratorState();
        }

        protected override IteratorState Join(IteratorState left, IteratorState right)
        {
            return left.Union(right);
        }
    }

    public class Traceable
    {
        string node;
        string column;
    }

    public class DependencyDomain
    {
        public MapSet<IVariable, string> A2 { get; set; }
        public MapSet<string, string> A3 { get; set; }
        public MapSet<IVariable, string> A4 { get; set; }

        public MapSet<PTGNode, string> Escaping { get; set;  }

        public MapSet<PTGNode, string> Variables { get; set; }
        public MapSet<PTGNode, string> Clousures { get; set; }
        public MapSet<PTGNode, string> Output{ get; set;  }
        public  DependencyDomain()
        {
            A2 = new MapSet<IVariable, string>();
            A3 = new MapSet<string, string>();
            A4 = new MapSet<IVariable, string>();

            Escaping = new MapSet<PTGNode, string>();
            Clousures = new MapSet<PTGNode, string>();
            Variables = new MapSet<PTGNode, string>();
            Output = new MapSet<PTGNode, string>();
        }

    public override bool Equals(object obj)
        {
            var oth= obj as DependencyDomain;
            return oth.Escaping.MapEquals(Escaping)
                && oth.Clousures.MapEquals(Clousures)
                && oth.Variables.MapEquals(Variables)
                && oth.A4.MapEquals(A4)
                && oth.A2.MapEquals(A2) && oth.A3.MapEquals(A3);
        }
        public override int GetHashCode()
        {
            return Escaping.GetHashCode() 
                + Variables.GetHashCode()
                + Clousures.GetHashCode()
                + A4.GetHashCode()
                + A2.GetHashCode() + A3.GetHashCode();
        }
        public DependencyDomain Clone()
        {
            var result = new DependencyDomain();
            result.Escaping = new MapSet<PTGNode,string>(this.Escaping);
            result.Clousures = new MapSet<PTGNode, string>(this.Clousures);
            result.Variables = new MapSet<PTGNode, string>(this.Variables);
            result.Output= new MapSet<PTGNode, string>(this.Output);

            result.A2 = new MapSet<IVariable, string>(this.A2);
            result.A3 = new MapSet<string, string>(this.A3);
            result.A4 = new MapSet<IVariable, string>(this.A4);
            return result;
        }

        public DependencyDomain Join(DependencyDomain right)
        {
            var result = new DependencyDomain();
            result.Escaping = new MapSet<PTGNode, string>(this.Escaping);
            result.Clousures = new MapSet<PTGNode, string>(this.Clousures);
            result.Variables = new MapSet<PTGNode, string>(this.Variables);
            result.Output = new MapSet<PTGNode, string>(this.Output);

            foreach (var key in result.Escaping.Keys)
            {
                if (right.Escaping.ContainsKey(key))
                {
                    result.Escaping[key].UnionWith(right.Escaping[key]);
                }
            }
            foreach (var key in result.Clousures.Keys)
            {
                if (right.Escaping.ContainsKey(key))
                {
                    result.Clousures[key].UnionWith(right.Clousures[key]);
                }
            }
            foreach (var key in result.Variables.Keys)
            {
                if (right.Variables.ContainsKey(key))
                {
                    result.Variables[key].UnionWith(right.Variables[key]);
                }
            }
            foreach (var key in result.Output.Keys)
            {
                if (right.Output.ContainsKey(key))
                {
                    result.Output[key].UnionWith(right.Output[key]);
                }
            }


            foreach (var key in result.A2.Keys)
            {
                if (right.A2.ContainsKey(key))
                {
                    result.A2[key].UnionWith(right.A2[key]);
                }
            }
            foreach (var key in result.A3.Keys)
            {
                if (right.A3.ContainsKey(key))
                {
                    result.A3[key].UnionWith(right.A3[key]);
                }
            }
            foreach (var key in result.A4.Keys)
            {
                if (right.A4.ContainsKey(key))
                {
                    result.A4[key].UnionWith(right.A4[key]);
                }
            }

            return result;
        }
        public bool LessEqual(DependencyDomain right)
        {
            // TODO: FIX!!
            return this.Equals(right);
        }
    }
    public class IteratorDependencyAnalysis: ForwardDataFlowAnalysis<DependencyDomain>
    {
        internal class ScopeInfo
        {
            internal IDictionary<IVariable, IExpression> schemaMap = new Dictionary<IVariable, IExpression>();
            internal IDictionary<IVariable, string> columnMap = new Dictionary<IVariable, string>();
            internal IDictionary<IFieldReference, string> columnFIeldMap = new Dictionary<IFieldReference, string>();
            // Maybe a map for IEpression to IVariable?
            internal IVariable row = null;
            internal IVariable rowEnum = null;

            internal ScopeInfo()
            {
                schemaMap = new Dictionary<IVariable, IExpression>();
                columnMap = new Dictionary<IVariable, string>();
                row = null;
                rowEnum = null;
            }
        }
        internal class MoveNextVisitor : InstructionVisitor
        {
            private IDictionary<IVariable, IExpression> equalities;
            private IteratorDependencyAnalysis iteratorDependencyAnalysis;
            private DependencyDomain oldInput;
            private ScopeInfo scopeData;
            internal DependencyDomain State { get; private set;  }
            private PointsToGraph ptg;

            public MoveNextVisitor(IteratorDependencyAnalysis iteratorDependencyAnalysis, IDictionary<IVariable, IExpression> equalities, ScopeInfo scopeData, PointsToGraph ptg, DependencyDomain oldInput)
            {
                this.iteratorDependencyAnalysis = iteratorDependencyAnalysis;
                this.equalities = equalities;
                this.scopeData = scopeData;
                this.oldInput = oldInput;
                this.State = oldInput;
                this.ptg = ptg;
            }

            private bool ISClousureField(InstanceFieldAccess fieldAccess)
            {
                var field = fieldAccess.Field;
                if (field.Type.ToString() == "RowSet")
                {
                    return true;
                }
                if (field.Type.ToString() == "Row")
                {
                    return true;
                }
                if (field.Type.ToString() == "IEnumerable<Row>")
                {
                    return true;
                }
                if (field.Type.ToString() == "IEnumerable<Row>")
                {
                    return true;
                }
                if (field.Type.ToString() == "IEnumerator<Row>")
                {
                    return true;
                }

                if (fieldAccess.Instance.Type.ToString().Contains("<Reduce>d__1") && !fieldAccess.FieldName.Contains("<>1__state"))
                {
                    return true;
                }

                return false;
            }
            public override void Visit(LoadInstruction instruction)
            {
                var loadStmt = instruction;
                if (loadStmt.Operand is InstanceFieldAccess)
                {
                    var fieldAccess = loadStmt.Operand as InstanceFieldAccess;
                    var o = fieldAccess.Instance;
                    var field = fieldAccess.Field;
                    if (field.Name[0] == '<' && field.Name.Contains(">"))
                    {

                    }
                    var union1 = new HashSet<string>();
                    if (this.State.A2.ContainsKey(o))
                    {
                        union1.UnionWith(this.State.A2[o]);
                    }

                    if (ISClousureField(fieldAccess))
                    {
                        if (this.State.A3.ContainsKey(fieldAccess.FieldName))
                        {
                            union1.UnionWith(this.State.A3[fieldAccess.FieldName]);
                        }

                        if (loadStmt.Result.Type.ToString() == "IEnumerator<Row>" 
                            || loadStmt.Result.Type.ToString() == "IEnumerable<Row>"
                            || loadStmt.Result.Type.ToString() == "Row")
                        {
                            var inputTable = this.scopeData.schemaMap[this.scopeData.row];
                            this.scopeData.schemaMap[loadStmt.Result] = inputTable;
                            union1.Add(inputTable.ToString());
                        }
                        this.State.A2[loadStmt.Result] = union1;
                    }
                    else
                    {
                        this.State.A2[loadStmt.Result] = union1;
                    }
                    // TODO: Filter for columns only
                    if (scopeData.columnFIeldMap.ContainsKey(fieldAccess.Field))
                    {
                        scopeData.columnMap[loadStmt.Result] = scopeData.columnFIeldMap[fieldAccess.Field];
                    }
                }
            }
            public override void Visit(StoreInstruction instruction)
            {
                var fieldAccess = instruction.Result as InstanceFieldAccess;
                var o = fieldAccess.Instance;
                var field = fieldAccess.Field;
                if (ISClousureField(fieldAccess))
                {
                    var arg = instruction.Operand;
                    var inputTable = equalities[arg];
                    //scopeData.row = bindingVar;
                    //scopeData.schemaMap[bindingVar] = inputTable;

                    if (this.State.A2.ContainsKey(instruction.Operand))
                    {
                        var union1 = new HashSet<string>(this.State.A2[instruction.Operand]);
                        this.State.A3[fieldAccess.FieldName] = union1;
                    }
                    else
                    {
                        this.State.A3[fieldAccess.FieldName] = new HashSet<string>();
                    }
                }
                if(scopeData.columnMap.ContainsKey(instruction.Operand))
                {
                    var columnLiteral = scopeData.columnMap[instruction.Operand];
                    scopeData.columnFIeldMap[fieldAccess.Field] = columnLiteral;
                }

            }
            public override void Visit(MethodCallInstruction instruction)
            {
                var methodCallStmt = instruction;
                var methodInvoked = methodCallStmt.Method;
                var bindingVar = methodCallStmt.Result;
                if (methodInvoked.Name == "get_Schema" && methodInvoked.ContainingType.Name == "RowSet")
                {
                    var arg = methodCallStmt.Arguments[0];
                    var inputTable = equalities[arg];
                    scopeData.schemaMap[bindingVar] = inputTable;
                }
                if (methodInvoked.Name == "IndexOf" && methodInvoked.ContainingType.Name == "Schema")
                {
                    var column = equalities[methodCallStmt.Arguments[1]];
                    var previousBinding = methodCallStmt.Arguments[0];
                    var inputTable = scopeData.schemaMap[previousBinding];
                    //scopeData.columnMap[bindingVar] = inputTable + ":" + column;
                    scopeData.columnMap[bindingVar] = column.ToString();

                    this.State.A2.Add(bindingVar, inputTable + ":" + column);
                    // Y have the bidingVar that refer to the column, now I can find the "field"
                }


                if (methodInvoked.Name == "get_Rows" && methodInvoked.ContainingType.Name == "RowSet")
                {
                    var arg = methodCallStmt.Arguments[0];
                    var inputTable = equalities[arg];
                    scopeData.row = bindingVar;
                    scopeData.schemaMap[bindingVar] = inputTable;
                    // can do directly ptg.GetTargets(arg)
                    var access = inputTable as InstanceFieldAccess;
                    var ptgNodes = this.ptg.GetTargets(access.Instance, access.Field);
                    foreach (var ptgNode in ptgNodes)
                    {
//                        this.State.Variables[ptgNode] = new HashSet<string>() { scopeData.rowEnum.ToString() };
                    }
                    this.State.A2.Add(methodCallStmt.Result, inputTable.ToString()); // a2[ v = a2[arg[0]]] 
                }
                if (methodInvoked.Name == "GetEnumerator"  && methodInvoked.ContainingType.FullName == "IEnumerable<Row>")
                {
                    var arg = methodCallStmt.Arguments[0];
                    var rows = equalities[arg] as MethodCallExpression;
                    var inputTable = equalities[rows.Arguments[0]];
                    if (arg == scopeData.row)
                    {
                        scopeData.rowEnum = methodCallStmt.Result;
                    }
                    // v = arg0.GetEnumerator();
                    var access = scopeData.schemaMap[arg] as InstanceFieldAccess;

                    this.State.A2.Add(methodCallStmt.Result, inputTable.ToString());
                    scopeData.schemaMap[methodCallStmt.Result] = inputTable;
                    // this.State.A2[methodCallStmt.Result].Add(scopeData.rowEnum.ToString()); // a2[ v = a2[arg[0]]] 
                }
                if (methodInvoked.Name == "get_Current" && methodInvoked.ContainingType.FullName == "IEnumerator<Row>")
                {
                    var arg = methodCallStmt.Arguments[0];
                    if (this.State.A2.ContainsKey(arg))
                    {
                        var inputTables = this.State.A2[arg];
                        this.State.A2.Add(methodCallStmt.Result, inputTables);
                    }
                }
                if (methodInvoked.Name == "get_Item" && methodInvoked.ContainingType.FullName == "Row")
                {
                    var arg = methodCallStmt.Arguments[0];
                    var col = methodCallStmt.Arguments[1];
                    var columnLiteral = scopeData.columnMap[col];
                    var table = equalities[arg];
                  
                    scopeData.row = bindingVar;
                    scopeData.schemaMap[bindingVar] = table;
                  
                    var inputTable = scopeData.schemaMap[arg];

                    if (this.State.A2.ContainsKey(arg))
                    {
                        
                        this.State.A2.Add(methodCallStmt.Result, inputTable+":"+columnLiteral);
                    }
                }
                if (methodInvoked.Name == "Set" && methodInvoked.ContainingType.Name == "ColumnData")
                {
                    var arg0 = methodCallStmt.Arguments[0];
                    var arg1= methodCallStmt.Arguments[1];

                    // var table = this.equalities[arg0];

                    //scopeData.row = bindingVar;
                    //scopeData.schemaMap[bindingVar] = table;

                    var table2= scopeData.schemaMap[arg0];
                    var columns = this.State.A2[arg0];

                    if (this.State.A2.ContainsKey(arg1))
                    {

                        this.State.A4.Add(arg0, this.State.A2[arg1] );
                    }
                }


            }

        }


        private IDictionary<IVariable, IExpression> equalities;
        DataFlowAnalysisResult<PointsToGraph>[] ptgs;
        private ScopeInfo scopeData;

        public IteratorDependencyAnalysis(ControlFlowGraph cfg, DataFlowAnalysisResult<PointsToGraph>[] ptgs, IDictionary<IVariable, IExpression> equalitiesMap) :base(cfg)
        {
            this.ptgs = ptgs;
            this.equalities = equalitiesMap;
            this.scopeData = new ScopeInfo();
        }

        protected override DependencyDomain InitialValue(CFGNode node)
        {
           var depValues = new DependencyDomain();
           var currentPTG = ptgs[cfg.Exit.Id].Output;
           var thisVar = currentPTG.Variables.Single(v => v.Name == "this"); 
           foreach(var ptgNode in currentPTG.GetTargets(thisVar))
           {
                foreach(var target in ptgNode.Targets)
                {
                    if (target.Key.Type.ToString() == "RowSet" || target.Key.Type.ToString() == "Row")
                    {
                        depValues.A3.Add(target.Key.Name, target.Key.Name);
                    }
                }
           }

           return depValues;
        }

        protected override bool Compare(DependencyDomain left, DependencyDomain right)
        {
            return left.LessEqual(right);
        }

        protected override DependencyDomain Join(DependencyDomain left, DependencyDomain right)
        {
            return left.Join(right);
        }

        protected override DependencyDomain Flow(CFGNode node, DependencyDomain input)
        {
            var oldInput = input.Clone();
            var currentPTG = ptgs[node.Id].Output;
            var visitor = new MoveNextVisitor(this, this.equalities, this.scopeData, currentPTG, oldInput);
            visitor.Visit(node);
            return visitor.State;
        }
    }

}
