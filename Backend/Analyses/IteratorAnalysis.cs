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
                        State.IntState = (IteratorInternalState)int.Parse(this.equalities.GetValue(storeStmt.Operand).ToString());
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

    public class Location : PTGNode
    {
        public Location(int id) : base(id)
        {
        }
        public Location(PTGNode node, IFieldReference f) : base(node.Id, node.Type, node.Offset, node.Kind)
        {
            this.Field = f;
        }

        public IFieldReference Field { get; set; }
        public override bool Equals(object obj)
        {
            var oth = obj as Location;
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode() + Field.GetHashCode();
        }
        public override string ToString()
        {
            return "[" + base.ToString() +"."+  Field.ToString() + "]";
        }
    }

    public class DependencyDomain
    {
        public MapSet<IVariable, string> A2 { get; set; }
        public MapSet<string, string> A3 { get; set; }
        public MapSet<IVariable, string> A4 { get; set; }

        public MapSet<PTGNode, string> Escaping { get; set; }

        public MapSet<PTGNode, string> Variables { get; set; }
        public MapSet<Location, string> Clousures { get; set; }
        public MapSet<PTGNode, string> Output { get; set; }
        public DependencyDomain()
        {
            A2 = new MapSet<IVariable, string>();
            A3 = new MapSet<string, string>();
            A4 = new MapSet<IVariable, string>();
            // This is A3 in the paper
            Clousures = new MapSet<Location, string>();

            Escaping = new MapSet<PTGNode, string>();
            Variables = new MapSet<PTGNode, string>();
            Output = new MapSet<PTGNode, string>();

        }

        public override bool Equals(object obj)
        {
            var oth = obj as DependencyDomain;
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
            result.Escaping = new MapSet<PTGNode, string>(this.Escaping);
            result.Clousures = new MapSet<Location, string>(this.Clousures);
            result.Variables = new MapSet<PTGNode, string>(this.Variables);
            result.Output = new MapSet<PTGNode, string>(this.Output);

            result.A2 = new MapSet<IVariable, string>(this.A2);
            result.A3 = new MapSet<string, string>(this.A3);
            result.A4 = new MapSet<IVariable, string>(this.A4);
            return result;
        }

        public DependencyDomain Join(DependencyDomain right)
        {
            var result = new DependencyDomain();
            result.Escaping = new MapSet<PTGNode, string>(this.Escaping);
            result.Clousures = new MapSet<Location, string>(this.Clousures);
            result.Variables = new MapSet<PTGNode, string>(this.Variables);
            result.Output = new MapSet<PTGNode, string>(this.Output);

            result.A2 = new MapSet<IVariable, string>(this.A2);
            result.A4 = new MapSet<IVariable, string>(this.A4);

            result.A2.UnionWith(right.A2);
            result.A3.UnionWith(right.A3);
            result.A4.UnionWith(right.A4);
            result.Clousures.UnionWith(right.Clousures);
            result.Escaping.UnionWith(right.Escaping);
            result.Variables.UnionWith(right.Variables);

            //foreach (var entry in right.Escaping)
            //{
            //    result.Escaping.AddRange(entry.Key, entry.Value);
            //}
            //foreach (var entry in right.Variables)
            //{
            //    result.Variables.AddRange(entry.Key, entry.Value);
            //}

            //foreach (var entry in right.Output)
            //{
            //    result.Output.AddRange(entry.Key, entry.Value);
            //}

            //foreach (var entry in right.Clousures)
            //{
            //    result.Clousures.AddRange(entry.Key, entry.Value);
            //}


            //foreach (var entry in right.A2)
            //{
            //    result.A2.AddRange(entry.Key, entry.Value);
            //}

            //foreach (var entry in right.A3)
            //{
            //    result.A3.AddRange(entry.Key,entry.Value);
            //}
            //foreach (var entry in right.A4)
            //{
            //    result.A4.AddRange(entry.Key, entry.Value);
            //}

            return result;
        }
        public bool LessEqual(DependencyDomain right)
        {
            // TODO: FIX!!
            return this.Equals(right);
        }
        public override string ToString()
        {
            var result = "";
            result += "A2\n";
            foreach(var var in this.A2.Keys)
            {
                result += String.Format("{0}:{1}\n", var, ToString(A2[var]));
            }
            result += "A3\n";
            foreach (var var in this.Clousures.Keys)
            {
                result += String.Format("{0}:{1}\n", var, ToString(Clousures[var]));
            }
            result += "A4\n";
            foreach (var var in this.A4.Keys)
            {
                result += String.Format("{0}:{1}\n", var, ToString(A4[var]));
            }

            return result;
        }
        private string ToString(ISet<string> set)
        {
            var result = String.Join(",", set);
            return result;
        }
    }
    public class IteratorDependencyAnalysis : ForwardDataFlowAnalysis<DependencyDomain>
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
            internal DependencyDomain State { get; private set; }
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
                if (fieldAccess.Instance.Type.ToString().Contains("<Reduce>d__4") && !fieldAccess.FieldName.Contains("<>1__state"))
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

                    // Check for special field
                    if (field.Name[0] == '<' && field.Name.Contains(">"))
                    {

                    }

                    var union1 = new HashSet<string>();
                    // a2:= [v <- a2[o] U a3[loc(o.f)] if loc(o.f) is CF
                    if (this.State.A2.ContainsKey(o))
                    {
                        union1.UnionWith(this.State.A2[o]);
                    }

                    if (ISClousureField(fieldAccess))
                    {
                        // Delete: 
                        //if (this.State.A3.ContainsKey(fieldAccess.FieldName))
                        //{
                        //    union1.UnionWith(this.State.A3[fieldAccess.FieldName]);
                        //}

                        // this is a[loc(o.f)]
                        foreach (var ptgNode in ptg.GetTargets(o))
                        {
                            var loc = new Location(ptgNode, field);
                            if (this.State.Clousures.ContainsKey(loc))
                            {
                                union1.UnionWith(this.State.Clousures[loc]);
                            }
                        }

                        // I need this to keep track of this like r = this.table
                        //if (loadStmt.Result.Type.ToString() == "IEnumerator<Row>"
                        //    || loadStmt.Result.Type.ToString() == "IEnumerable<Row>"
                        //    || loadStmt.Result.Type.ToString() == "Row")
                        //{
                        //    var inputTable = this.scopeData.schemaMap[this.scopeData.row];
                        //    this.scopeData.schemaMap[loadStmt.Result] = inputTable;
                        //    // union1.Add(inputTable.ToString());
                        //}
                    }

                    this.State.A2[loadStmt.Result] = union1;

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
                if(fieldAccess==null)
                {
                    // Is array! Skipping for now
                    return;
                }
                var o = fieldAccess.Instance;
                var field = fieldAccess.Field;
                if (ISClousureField(fieldAccess))
                {
                    var arg = instruction.Operand;
                    var inputTable = equalities.GetValue(arg);

                    // a3 := a2[loc(o.f):=a2[v]] 
                    if (this.State.A2.ContainsKey(instruction.Operand))
                    {
                        // union = a2[v] 
                        var union = new HashSet<string>(this.State.A2[instruction.Operand]);
                        // fieldAccess.FieldName = loc.f 
                        // Delete: this.State.A3[fieldAccess.FieldName] = union;
                        // It should be this.f or N.f
                        foreach (var ptgNode in ptg.GetTargets(o))
                        {
                            this.State.Clousures[new Location(ptgNode, field)] = union;
                        }
                    }
                    else
                    {
                        // Delete: this.State.A3[fieldAccess.FieldName] = new HashSet<string>();
                        foreach (var ptgNode in ptg.GetTargets(o))
                        {
                            this.State.Clousures[new Location(ptgNode, field)] = new HashSet<string>();
                        }

                    }
                }
                // This is to connect the column field with the literal
                // Do I need this?
                if (scopeData.columnMap.ContainsKey(instruction.Operand))
                {
                    var columnLiteral = scopeData.columnMap[instruction.Operand];
                    scopeData.columnFIeldMap[fieldAccess.Field] = columnLiteral;
                }

            }
            public override void Visit(MethodCallInstruction instruction)
            {
                var methodCallStmt = instruction;
                var methodInvoked = methodCallStmt.Method;
                var callResult = methodCallStmt.Result;


                // We are analyzing instructions of the form this.table.Schema.IndexOf("columnLiteral")
                // 
                // this is callResult = arg.Schema(...)
                // we associate arg the table and callResult with the schema
                if (methodInvoked.Name == "get_Schema" && methodInvoked.ContainingType.Name == "RowSet")
                {
                    var arg = methodCallStmt.Arguments[0];
                    var table = equalities.GetValue(arg);
                    scopeData.schemaMap[callResult] = table;
                }
                // callResult = arg.IndexOf(colunm)
                // we recover the table from arg and associate the column number with the call result
                else if (methodInvoked.Name == "IndexOf" && methodInvoked.ContainingType.Name == "Schema")
                {
                    var column = equalities.GetValue(methodCallStmt.Arguments[1]);
                    var arg = methodCallStmt.Arguments[0];
                    var table = scopeData.schemaMap[arg];

                    scopeData.columnMap[callResult] = column.ToString();

                    this.State.A2.Add(callResult, table + ":" + column);
                    // Y have the bidingVar that refer to the column, now I can find the "field"
                }
                // This is when you get rows
                // a2 = a2[v<- a[arg_0]] 
                else if (methodInvoked.Name == "get_Rows" && methodInvoked.ContainingType.Name == "RowSet")
                {
                    var arg = methodCallStmt.Arguments[0];

                    var union = new HashSet<string>();
                    if (this.State.A2.ContainsKey(arg))
                    {
                        union.UnionWith(this.State.A2[arg]);
                    }
                    this.State.A2.Add(methodCallStmt.Result, union); // a2[ v = a2[arg[0]]] 

                    // TODO: I don't know I need this
                    var inputTable = equalities.GetValue(arg);
                    scopeData.row = callResult;
                    scopeData.schemaMap[callResult] = inputTable;
                }
                // This is when you get enumerator (same as get rows)
                // a2 = a2[v <- a[arg_0]] 
                else if (methodInvoked.Name == "GetEnumerator" && methodInvoked.ContainingType.FullName == "IEnumerable<Row>")
                {
                    var arg = methodCallStmt.Arguments[0];

                    var union = new HashSet<string>();
                    if (this.State.A2.ContainsKey(arg))
                    {
                        union.UnionWith(this.State.A2[arg]);
                    }
                    this.State.A2.Add(methodCallStmt.Result, union); // a2[ v = a2[arg[0]]] 

                    // TODO: Do I need this?
                    var rows = equalities.GetValue(arg) as MethodCallExpression;
                    var inputTable = equalities.GetValue(rows.Arguments[0]);
                    if (arg == scopeData.row)
                    {
                        scopeData.rowEnum = methodCallStmt.Result;
                    }
                    var access = scopeData.schemaMap[arg] as InstanceFieldAccess;
                    scopeData.schemaMap[methodCallStmt.Result] = inputTable;
                }
                // v = arg.Current
                // a2 := a2[v <- Table(i)] if Table(i) in a2[arg]
                else if (methodInvoked.Name == "get_Current" && methodInvoked.ContainingType.FullName == "IEnumerator<Row>")
                {
                    var arg = methodCallStmt.Arguments[0];
                    if (this.State.A2.ContainsKey(arg))
                    {
                        var tables = this.State.A2[arg];
                        this.State.A2.Add(methodCallStmt.Result, tables);
                    }
                }
                // v = arg.Current
                // a2 := a2[v <- Table(i)] if Table(i) in a2[arg]
                else if (methodInvoked.Name == "MoveNext" && methodInvoked.ContainingType.FullName == "IEnumerator")
                {
                    var arg = methodCallStmt.Arguments[0];
                    if (this.State.A2.ContainsKey(arg))
                    {
                        var tables = this.State.A2[arg];
                        foreach (var table in tables)
                        {
                            this.State.A2.Add(methodCallStmt.Result, table.ToString() + ":RC");
                        }
                    }
                }
                // v = arg.getItem(col)
                // a2 := a2[v <- Col(i, col)] if Table(i) in a2[arg]
                else if (methodInvoked.Name == "get_Item" && methodInvoked.ContainingType.FullName == "Row")
                {
                    var arg = methodCallStmt.Arguments[0];
                    var col = methodCallStmt.Arguments[1];
                    var columnLiteral = ""; 
                    if (col.Type.ToString() == "String")
                    {
                        columnLiteral = this.equalities.GetValue(col).ToString();
                    }
                    else
                    {
                        columnLiteral = scopeData.columnMap[col];
                    }

                    

                    if (this.State.A2.ContainsKey(arg))
                    {
                        var tables = this.State.A2[arg];
                        foreach (var table_i in tables)
                        {
                            this.State.A2.Add(methodCallStmt.Result, table_i + ":" + columnLiteral);
                        }
                    }
                    // Do I still need this
                    var table = equalities.GetValue(arg);
                    scopeData.row = callResult;
                    scopeData.schemaMap[callResult] = table;
                }
                // arg.Set(arg1)
                // a4 := a4[arg0 <- a4[arg0] U a2[arg1]] 
                else if (methodInvoked.Name == "Set" && methodInvoked.ContainingType.Name == "ColumnData")
                {
                    var arg0 = methodCallStmt.Arguments[0];
                    var arg1 = methodCallStmt.Arguments[1];


                    if (this.State.A2.ContainsKey(arg1))
                    {
                        this.State.A4.Add(arg0, this.State.A2[arg1]);
                    }

                    //var table2 = scopeData.schemaMap[arg0];
                    //var columns = this.State.A2[arg0];

                    // var table = this.equalities[arg0];

                    //scopeData.row = bindingVar;
                    //scopeData.schemaMap[bindingVar] = table;
                }
                // other methdos
                else
                {
                    // Pure Methods
                    foreach(var result in methodCallStmt.ModifiedVariables)
                    {
                        foreach (var arg in methodCallStmt.Arguments)
                        {
                            if (State.A2.ContainsKey(arg))
                            {
                                var tables = this.State.A2[arg];
                                this.State.A2.AddRange(result, tables);
                            }

                        }
                    }
                    // Unpure methods
                }
            }
        }


        private IDictionary<IVariable, IExpression> equalities;
        DataFlowAnalysisResult<PointsToGraph>[] ptgs;
        private ScopeInfo scopeData;

        public IteratorDependencyAnalysis(ControlFlowGraph cfg, DataFlowAnalysisResult<PointsToGraph>[] ptgs, IDictionary<IVariable, IExpression> equalitiesMap) : base(cfg)
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
            foreach (var ptgNode in currentPTG.GetTargets(thisVar))
            {
                foreach (var target in ptgNode.Targets)
                {
                    if (target.Key.Type.ToString() == "RowSet" || target.Key.Type.ToString() == "Row")
                    {
                        depValues.A3.Add(target.Key.Name, target.Key.Name);

                        depValues.Clousures.Add(new Location(ptgNode, target.Key), target.Key.Name);
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
