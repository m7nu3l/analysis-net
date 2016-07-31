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
    #region Iterator State Analysis (to be completed)
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

        internal class MoveNextVisitorForItStateAnalysis : InstructionVisitor
        {
            internal IteratorState State { get; }
            private IDictionary<IVariable, IExpression> equalities;

            internal MoveNextVisitorForItStateAnalysis(IteratorStateAnalysis itAnalysis, IDictionary<IVariable, IExpression> equalitiesMap, IteratorState state)
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
            var visitor = new MoveNextVisitorForItStateAnalysis(this, this.equalities, oldInput);
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

    #endregion

    #region Dependency Analysis (based of SongTao paper)
    public abstract class Traceable
    {
        public string TableName { get; set; }
        public Traceable(string name)
        {
            this.TableName = name;
        }

        public override bool Equals(object obj)
        {
            var oth = obj as Traceable;
            return oth!= null && oth.TableName.Equals(this.TableName);
        }
        public override int GetHashCode()
        {
            return TableName.GetHashCode();
        }
        public override string ToString()
        {
            return TableName;
        }

    }
    public class TraceableTable: Traceable
    {
        public TraceableTable(string name): base(name)
        {
            this.TableName = name;
        }
        public override string ToString()
        {
            return String.Format("Table({0})", TableName);
        }

    }

    public class ColumnDomain
    {
        public static readonly ColumnDomain TOP = new ColumnDomain(-2); 
        public string ColumnName { get; private set; }
        public int ColumnPosition { get; private set; }
        public bool IsString { get; private set; }
        public bool IsTOP { get; private set; }

        public ColumnDomain(string columnName)
        {
            this.ColumnName = columnName;
            this.IsString = true;
            this.ColumnPosition = -1;
            IsTOP = columnName == "_TOP_";
            if (IsTOP)
            {
                this.ColumnPosition = -2;
            }
        }
        public ColumnDomain(int columnPosition)
        {
            this.ColumnName = null;
            this.IsString = false;
            this.ColumnPosition = columnPosition;
            IsTOP = columnPosition == -2;
        }
        public override string ToString()
        {
            if (IsTOP)
                return "_TOP_";
            if (IsString)
            {
                return ColumnName;
            }
            else
            {
                return ColumnPosition.ToString();
            }
        }
        public override bool Equals(object obj)
        {
            var oth = obj as ColumnDomain;

            return oth.IsString==this.IsString && oth.IsTOP == oth.IsTOP 
                    && oth.ColumnName==this.ColumnName 
                    && oth.ColumnPosition==this.ColumnPosition;
        }
        public override int GetHashCode()
        {
            if (IsString)
            {
                return this.ColumnName.GetHashCode();
            }
            return this.ColumnPosition.GetHashCode();
        }
    }


    public class TraceableColumnNumber: Traceable
    {
        public int Column { get; private set; }
        public TraceableColumnNumber(string name, int column): base(name)
        {
            this.Column = column;
        }
        public override string ToString()
        {
            return String.Format("Col({0},{1})", TableName, Column);
        }
        public override bool Equals(object obj)
        {
            var oth = obj as TraceableColumnName;
            return oth != null && oth.Column.Equals(this.Column) && base.Equals(oth);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode() + Column.GetHashCode();
        }
    }
    public class TraceableColumnName: Traceable
    {
        public string Column { get; private set; }
        public TraceableColumnName(string name, string column): base(name)
        {
            this.Column = column;
        }
        public override string ToString()
        {
            return String.Format("Col({0},{1})",TableName, Column);
        }
        public override bool Equals(object obj)
        {
            var oth = obj as TraceableColumnName;
            return oth != null && oth.Column.Equals(this.Column) && base.Equals(oth);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode()+Column.GetHashCode();
        }
    }

    public class TraceableColumn : Traceable
    {
        public ColumnDomain Column { get; private set; }
        public TraceableColumn(string name, ColumnDomain column) : base(name)
        {
            this.Column = column;
        }
        public override string ToString()
        {
            return String.Format("Col({0},{1})", TableName, Column);
        }
        public override bool Equals(object obj)
        {
            var oth = obj as TraceableColumn;
            return oth != null && oth.Column.Equals(this.Column) && base.Equals(oth);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode() + Column.GetHashCode();
        }
    }


    public class TraceableCounter : Traceable
    {
        public TraceableCounter(string name) : base(name)
        {
        }
        public override string ToString()
        {
            return String.Format("RC({0})" , TableName);
        }
        public override int GetHashCode()
        {
            return 1 + base.GetHashCode();
        }
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
    public interface ISymbolicValue
    {
        string Name { get; }
    }
    public class EscalarVariable: ISymbolicValue
    {
        private IVariable variable;
        public EscalarVariable(IVariable variable)
        {
            this.variable = variable;
        }

        public string Name
        {
            get
            {
                return variable.Name;
            }
        }
        public override bool Equals(object obj)
        {
            var oth = obj as EscalarVariable;
            return oth!=null && variable.Equals(oth.variable);
        }
        public override int GetHashCode()
        {
            return variable.GetHashCode();
        }
    }
    public class AbstractObject : ISymbolicValue
    {
        // private IVariable variable;
        private PTGNode ptgNode;
        public AbstractObject(PTGNode ptgNode)
        {
            //this.variable = variable;

        }
        public string Name
        {
            get
            {
                return String.Join(",", ptgNode.Variables);

            }
        }
        public override bool Equals(object obj)
        {
            var oth = obj as AbstractObject;
            return oth!=null && oth.ptgNode.Equals(this.ptgNode);
        }
        public override int GetHashCode()
        {
            return ptgNode.GetHashCode();
        }
    }

    public class DependencyDomain
    {
        public MapSet<IVariable, Traceable> A2_Variables { get; set; }
        public MapSet<Location, Traceable> A3_Clousures { get; set; }

        public MapSet<IVariable, Traceable> A4_Ouput { get; set; }

        public ISet<Traceable> Escaping { get; set; }

        public ISet<IVariable> ControlVariables { get; set; }

        public DependencyDomain()
        {
            A2_Variables = new MapSet<IVariable, Traceable>();
            A3_Clousures = new MapSet<Location, Traceable>();
            A4_Ouput = new MapSet<IVariable, Traceable>();

            Escaping = new HashSet<Traceable>();

            ControlVariables = new HashSet<IVariable>();
        }

        public override bool Equals(object obj)
        {
            // Add ControlVariables
            var oth = obj as DependencyDomain;
            return oth.Escaping.SetEquals(Escaping)
                && oth.A2_Variables.MapEquals(A2_Variables)
                && oth.A3_Clousures.MapEquals(A3_Clousures)
                && oth.A4_Ouput.MapEquals(A4_Ouput)
                && oth.ControlVariables.SetEquals(ControlVariables);

        }
        public override int GetHashCode()
        {
            // Add ControlVariables
            return Escaping.GetHashCode()
                + A2_Variables.GetHashCode()
                + A3_Clousures.GetHashCode()
                + A4_Ouput.GetHashCode()
                + ControlVariables.GetHashCode();
                
        }
        public DependencyDomain Clone()
        {
            var result = new DependencyDomain();
            result.Escaping = new HashSet<Traceable>(this.Escaping);
            result.A2_Variables = new MapSet<IVariable, Traceable>(this.A2_Variables);
            result.A3_Clousures = new MapSet<Location, Traceable>(this.A3_Clousures);
            result.A4_Ouput = new MapSet<IVariable, Traceable>(this.A4_Ouput);
            result.ControlVariables = new HashSet<IVariable>(this.ControlVariables);
            return result;
        }

        public DependencyDomain Join(DependencyDomain right)
        {
            var result = new DependencyDomain();
            result.Escaping = new HashSet<Traceable>(this.Escaping);
           
            result.A2_Variables = new MapSet<IVariable, Traceable>(this.A2_Variables);
            result.A3_Clousures = new MapSet<Location, Traceable>(this.A3_Clousures);
            result.A4_Ouput = new MapSet<IVariable, Traceable>(this.A4_Ouput);

            result.ControlVariables = new HashSet<IVariable>(this.ControlVariables);

            result.Escaping.UnionWith(right.Escaping);

            result.A2_Variables.UnionWith(right.A2_Variables);
            result.A3_Clousures.UnionWith(right.A3_Clousures);
            result.A4_Ouput.UnionWith(right.A4_Ouput);

            result.ControlVariables.UnionWith(right.ControlVariables);

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
            //result += "A2\n";
            //foreach(var var in this.A2_Variables.Keys)
            //{
            //    result += String.Format("{0}:{1}\n", var, ToString(A2_Variables[var]));
            //}
            result += "A3\n";
            foreach (var var in this.A3_Clousures.Keys)
            {
                result += String.Format("{0}:{1}\n", var, ToString(A3_Clousures[var]));
            }
            result += "A4\n";
            foreach (var var in this.A4_Ouput.Keys)
            {
                result += String.Format("({0}){1}= dep({2})\n", var, ToString(A2_Variables[var]), ToString(A4_Ouput[var]));
                //result += String.Format("{0}:{1}\n", var, ToString(A4_Ouput[var]));
            }

            return result;
        }
        private string ToString(ISet<Traceable> set)
        {
            var result = String.Join(",", set.Select(e => e.ToString()));
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
        internal class MoveNextVisitorForDependencyAnalysis : InstructionVisitor
        {
            private IDictionary<IVariable, IExpression> equalities;
            private IteratorDependencyAnalysis iteratorDependencyAnalysis;
            private DependencyDomain oldInput;
            private ScopeInfo scopeData;
            internal DependencyDomain State { get; private set; }
            private PointsToGraph ptg;
            private CFGNode cfgNode;
            

            public MoveNextVisitorForDependencyAnalysis(IteratorDependencyAnalysis iteratorDependencyAnalysis, CFGNode cfgNode,  IDictionary<IVariable, IExpression> equalities, 
                                   ScopeInfo scopeData, PointsToGraph ptg, DependencyDomain oldInput)
            {
                this.iteratorDependencyAnalysis = iteratorDependencyAnalysis;
                this.equalities = equalities;
                this.scopeData = scopeData;
                this.oldInput = oldInput;
                this.State = oldInput;
                this.ptg = ptg;
                this.cfgNode = cfgNode;
            }

            private bool IsClousureParamerField(IFieldAccess fieldAccess)
            {
                var result = true;
                result = this.iteratorDependencyAnalysis.specialFields.Contains(fieldAccess);
                return result;
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

                if(IsClousureParamerField(fieldAccess))
                {
                    return true;
                }

                if (fieldAccess.Instance.Type.ToString()== this.iteratorDependencyAnalysis.iteratorClass.Name) 
                    // && !fieldAccess.FieldName.StartsWith.Contains("<>1__state"))
                {
                    return true;
                }


                return false;
            }

            private ISet<ISymbolicValue> GetSymbolicValues(IVariable v)
            {
                if(v.Type.TypeKind == TypeKind.ValueType)
                {
                    return new HashSet<ISymbolicValue>() { new EscalarVariable(v) } ;
                }
                var res = new HashSet<ISymbolicValue>();
                if(ptg.Contains(v))
                {
                    res.UnionWith(ptg.GetTargets(v).Select( ptg => new AbstractObject(ptg) ));
                }
                return res;
            }
            private ISet<PTGNode> GetPtgNodes(IVariable v)
            {
                var res = new HashSet<PTGNode>();
                if (ptg.Contains(v))
                {
                    res.UnionWith(ptg.GetTargets(v));
                }
                return res;
            }

            private ISet<IVariable> GetAliases(IVariable v)
            {
                var res = new HashSet<IVariable>() { v } ;
                foreach (var ptgNode in GetPtgNodes(v))
                {
                    res.UnionWith(ptgNode.Variables);
                }
                return res;
            }
            public override void Visit(LoadInstruction instruction)
            {
               //  v = o.f   (v is instruction.Result, o.f is instruction.Operand)
                var loadStmt = instruction;
                if(loadStmt.Operand is StaticFieldAccess)
                {
                    // TODO: Need to properly check 
                }
                else if (loadStmt.Operand is InstanceFieldAccess)
                {
                    var fieldAccess = loadStmt.Operand as InstanceFieldAccess;
                    var o = fieldAccess.Instance;
                    var field = fieldAccess.Field;

                    // Check for special field
                    if (field.Name[0] == '<' && field.Name.Contains(">"))
                    {

                    }

                    ProcessLoad(loadStmt, fieldAccess, o, field);

                    // TODO: Filter for columns only
                    if (scopeData.columnFIeldMap.ContainsKey(fieldAccess.Field))
                    {
                        scopeData.columnMap[loadStmt.Result] = scopeData.columnFIeldMap[fieldAccess.Field];
                    }
                }
                else if (loadStmt.Operand is ArrayElementAccess)
                {
                    var arrayAccess = loadStmt.Operand as ArrayElementAccess;
                    var baseArray = arrayAccess.Array;
                    var index = arrayAccess.Index;
                    var union1 = new HashSet<Traceable>();
                    // a2:= [v <- a2[o] U a3[loc(o.f)] if loc(o.f) is CF
                    // TODO: Check this. I think it is too conservative to add a2[o]
                    // this is a2[o]
                    union1 = GetTraceablesFromA2_Variables(baseArray);

                    foreach (var ptgNode in ptg.GetTargets(baseArray))
                    {
                        var fakeField = new FieldReference("[]", arrayAccess.Type);
                        fakeField.ContainingType = PlatformTypes.Object;
                        var loc = new Location(ptgNode, fakeField);
                        if (this.State.A3_Clousures.ContainsKey(loc))
                        {
                            union1.UnionWith(this.State.A3_Clousures[loc]);
                        }
                    }
                    this.State.A2_Variables[loadStmt.Result] = union1;
                }
                else if (loadStmt.Operand is ArrayLengthAccess)
                {
                    UpdateUsingDefUsed(loadStmt);
                }
                else if (loadStmt.Operand is IVariable)
                {
                    var v = loadStmt.Operand as IVariable;
                    this.State.A2_Variables[loadStmt.Result] = GetTraceablesFromA2_Variables(v);
                }
                else if(loadStmt.Operand is Reference)
                {
                }
                else if (loadStmt.Operand is Dereference)
                {
                }
                else if(loadStmt.Operand is IndirectMethodCallExpression)
                { }
                else if(loadStmt.Operand is Constant)
                { }
                else
                { }
            }

            private void ProcessLoad(LoadInstruction loadStmt, InstanceFieldAccess fieldAccess, IVariable o, IFieldReference field)
            {
                var union1 = new HashSet<Traceable>();
                // a2:= [v <- a2[o] U a3[loc(o.f)] if loc(o.f) is CF
                // TODO: Check this. I think it is too conservative to add a2[o]
                // this is a2[o]
                union1 = GetTraceablesFromA2_Variables(o);
                if (ISClousureField(fieldAccess))
                {

                    // this is a[loc(o.f)]
                    foreach (var ptgNode in ptg.GetTargets(o))
                    {
                        var loc = new Location(ptgNode, field);
                        if (this.State.A3_Clousures.ContainsKey(loc))
                        {
                            union1.UnionWith(this.State.A3_Clousures[loc]);
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

                this.State.A2_Variables[loadStmt.Result] = union1;
            }

            public override void Visit(StoreInstruction instruction)
            {
                //  o.f = v  (v is instruction.Operand, o.f is instruction.Result)
                if (instruction.Result is InstanceFieldAccess)
                {
                    var fieldAccess = instruction.Result as InstanceFieldAccess;

                    var o = fieldAccess.Instance;
                    var field = fieldAccess.Field;
                    if (ISClousureField(fieldAccess))
                    {
                        var arg = instruction.Operand;
                        var inputTable = equalities.GetValue(arg);

                        // a3 := a3[loc(o.f) <- a2[v]] 
                        // union = a2[v]
                        var union = GetTraceablesFromA2_Variables(instruction.Operand);
                        foreach (var ptgNode in ptg.GetTargets(o))
                        {
                            this.State.A3_Clousures[new Location(ptgNode, field)] = union;
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
                else if(instruction.Result is ArrayElementAccess)
                {
                    var arrayAccess = instruction.Result as ArrayElementAccess;
                    var baseArray = arrayAccess.Array;
                    var index = arrayAccess.Index;
                    var arg = instruction.Operand;
                    var inputTable = equalities.GetValue(arg);

                    // a3 := a3[loc(o[f]) <- a2[v]] 
                    // union = a2[v]
                    var union = GetTraceablesFromA2_Variables(instruction.Operand);
                    foreach (var ptgNode in ptg.GetTargets(baseArray))
                    {
                        var fakeField = new FieldReference("[]", arrayAccess.Type);
                        fakeField.ContainingType = PlatformTypes.Object;
                        var loc = new Location(ptgNode, fakeField);
                        this.State.A3_Clousures[new Location(ptgNode, fakeField)] = union;
                    }
                }
                else if(instruction.Result is StaticFieldAccess)
                { }

            }
            public override void Visit(ConditionalBranchInstruction instruction)
            {
                this.State.ControlVariables.UnionWith(instruction.UsedVariables.Where( v => GetTraceablesFromA2_Variables(v).Any()));

            }
            public override void Visit(MethodCallInstruction instruction)
            {
                var methodCallStmt = instruction;
                var methodInvoked = methodCallStmt.Method;
                var callResult = methodCallStmt.Result;


                // We are analyzing instructions of the form this.table.Schema.IndexOf("columnLiteral")
                // to maintain a mapping between column numbers and literals 
                var isSchemaMethod = AnalyzeSchemaRelatedMethod(methodCallStmt, methodInvoked, callResult);
                if (!isSchemaMethod)
                {
                    var isScopeRowMethod = AnalyzeScopeRowMethods(methodCallStmt, methodInvoked, callResult);

                    if (!isScopeRowMethod)
                    {
                        var isCollectionMethod = VisitCollectionMethods(methodCallStmt, methodInvoked);
                        if(!isCollectionMethod)
                        {
                            // Pure Methods
                            UpdateUsingDefUsed(methodCallStmt);
                        }
                    }
                }
            }

            private bool VisitCollectionMethods(MethodCallInstruction methodCallStmt, IMethodReference methodInvoked)
            {
                var result = true;
                if (methodInvoked.Name == "Any") //  && methodInvoked.ContainingType.FullName == "Enumerable")
                {
                    var arg = methodCallStmt.Arguments[0];
                    var tablesCounters = GetTraceablesFromA2_Variables(arg)
                                        .Where(t => t is Traceable)
                                        .Select(table_i => new TraceableCounter(table_i.TableName));
                    var any = GetTraceablesFromA2_Variables(arg).Any();
                    // this.State.A2_Variables.Add(methodCallStmt.Result, new TraceableCounter(table.TableName));
                    //this.State.A2_Variables[methodCallStmt.Result] = new HashSet<Traceable>(tablesCounters);
                    UpdateUsingDefUsed(methodCallStmt);
                }
                else if (methodInvoked.Name == "Select") // && methodInvoked.ContainingType.FullName.Contains("Enumerable"))
                {
                    var arg0 = methodCallStmt.Arguments[0];
                    var arg1 = methodCallStmt.Arguments[1];
                    //this.State.A2_Variables.AddRange(arg0, new HashSet<Traceable>(GetTraceablesFromA2_Variables(arg1)));
                    UpdateUsingDefUsed(methodCallStmt);

                }
                else if (methodInvoked.Name == "Add" && methodInvoked.ContainingType.FullName.Contains("Set"))
                {
                    var arg0 = methodCallStmt.Arguments[0];
                    var arg1 = methodCallStmt.Arguments[1];
                    this.State.A2_Variables.AddRange(arg0, new HashSet<Traceable>(GetTraceablesFromA2_Variables(arg1)));
                    //UpdateUsingDefUsed(methodCallStmt);

                }
                else if (methodInvoked.Name == "get_Item" && methodInvoked.ContainingType.FullName.Contains("Set"))
                {
                    var arg0 = methodCallStmt.Arguments[0];
                    var arg1 = methodCallStmt.Arguments[1];
                    //this.State.A2_Variables.AddRange(arg0, new HashSet<Traceable>(GetTraceablesFromA2_Variables(arg1)));
                    UpdateUsingDefUsed(methodCallStmt);
                }
                else if (methodInvoked.Name == "ContainsKey" && methodInvoked.ContainingType.FullName.Contains("Set"))
                {
                    var arg0 = methodCallStmt.Arguments[0];
                    var arg1 = methodCallStmt.Arguments[1];
                    //this.State.A2_Variables.AddRange(arg0, new HashSet<Traceable>(GetTraceablesFromA2_Variables(arg1)));
                    UpdateUsingDefUsed(methodCallStmt);
                }
                else
                {
                    result = false;
                }
                return result;
            }

            private void AssignTraceables(IVariable source, IVariable destination)
            {
                HashSet<Traceable> union = GetTraceablesFromA2_Variables(source);
                this.State.A2_Variables[destination] = union; 
            }
            private void AddTraceables(IVariable source, IVariable destination)
            {
                HashSet<Traceable> union = GetTraceablesFromA2_Variables(source);
                this.State.A2_Variables.Add(destination, union);
            }

            private bool  AnalyzeScopeRowMethods(MethodCallInstruction methodCallStmt, IMethodReference methodInvoked, IVariable callResult)
            {
                var result = true;
                // This is when you get rows
                // a2 = a2[v<- a[arg_0]] 
                if (methodInvoked.Name == "get_Rows" && methodInvoked.ContainingType.Name == "RowSet")
                {
                    var arg = methodCallStmt.Arguments[0];
                    AssignTraceables(arg, methodCallStmt.Result);

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

                    // a2[ v = a2[arg[0]]] 
                    AssignTraceables(arg, methodCallStmt.Result);
                    
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
                    AssignTraceables(arg, methodCallStmt.Result);
                }
                // v = arg.Current
                // a2 := a2[v <- Table(i)] if Table(i) in a2[arg]
                else if (methodInvoked.Name == "MoveNext" && methodInvoked.ContainingType.FullName == "IEnumerator")
                {
                    var arg = methodCallStmt.Arguments[0];
                    var tablesCounters = GetTraceablesFromA2_Variables(arg)
                                        .Where(t => t is TraceableTable)
                                        .Select(table_i => new TraceableCounter(table_i.TableName));
                    this.State.A2_Variables[methodCallStmt.Result] = new HashSet<Traceable>(tablesCounters);
                }
                // v = arg.getItem(col)
                // a2 := a2[v <- Col(i, col)] if Table(i) in a2[arg]
                else if (methodInvoked.Name == "get_Item" && methodInvoked.ContainingType.FullName == "Row")
                {
                    var arg = methodCallStmt.Arguments[0];
                    var col = methodCallStmt.Arguments[1];
                    var columnLiteral = ObtainColumnLiteral(col);

                    var tableColumns = GetTraceablesFromA2_Variables(arg)
                                        .Where(t => t is TraceableTable)
                                        .Select(table_i => new TraceableColumn(table_i.TableName, columnLiteral));

                    this.State.A2_Variables[methodCallStmt.Result] = new HashSet<Traceable>(tableColumns); ;

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


                    var tables = GetTraceablesFromA2_Variables(arg1);
                    this.State.A4_Ouput.AddRange(arg0, tables);

                    //
                    var traceables = this.State.ControlVariables.SelectMany(controlVar => GetTraceablesFromA2_Variables(controlVar));
                    this.State.A4_Ouput.AddRange(arg0, traceables);
                }
                //else if ((methodInvoked.Name == "get_String" || methodInvoked.Name == "Get") && methodInvoked.ContainingType.Name == "ColumnData")
                //{
                //    var arg = methodCallStmt.Arguments[0];

                //    this.State.A2_Variables[methodCallStmt.Result] = new HashSet<Traceable>(GetTraceablesFromA2_Variables(arg)); ;

                //    // Do I still need this
                //    var table = equalities.GetValue(arg);
                //    scopeData.row = callResult;
                //    scopeData.schemaMap[callResult] = table;
                //}
                else
                {
                    result = false;
                }
                return result;

            }

            private ColumnDomain ObtainColumnLiteral(IVariable col)
            {
                ColumnDomain result = result = ColumnDomain.TOP; 
                var columnLiteral = "";
                if (col.Type.ToString() == "String")
                {
                    var columnValue = this.equalities.GetValue(col);
                    if (columnValue is Constant)
                    {
                        columnLiteral = columnValue.ToString();
                        result = new ColumnDomain(columnLiteral);
                    }
                }
                else
                {
                    if (scopeData.columnMap.ContainsKey(col))
                    {
                        columnLiteral = scopeData.columnMap[col];
                        result = new ColumnDomain(columnLiteral);
                    }
                    else
                    {
                        var colValue = this.equalities.GetValue(col);
                        if(colValue is Constant)
                        {
                            var value = colValue as Constant;
                            result = new ColumnDomain((int)value.Value);
                        }
                    }
                }
                return result;
            }

            private bool IsSchemaMethod(IMethodReference methodInvoked)
            {
                return methodInvoked.Name == "get_Schema"
                    && (methodInvoked.ContainingType.Name == "RowSet" || methodInvoked.ContainingType.Name == "Row");
            }
            private bool IsIndexOfMethod(IMethodReference methodInvoked)
            {
                return methodInvoked.Name == "IndexOf" && methodInvoked.ContainingType.Name == "Schema";
            }

            private bool AnalyzeSchemaRelatedMethod(MethodCallInstruction methodCallStmt, IMethodReference methodInvoked, IVariable callResult)
            {
                var result = true;
                // this is callResult = arg.Schema(...)
                // we associate arg the table and callResult with the schema
                if (IsSchemaMethod(methodInvoked))
                {
                    var arg = methodCallStmt.Arguments[0];
                    var table = equalities.GetValue(arg);
                    scopeData.schemaMap[callResult] = table;
                }
                // callResult = arg.IndexOf(colunm)
                // we recover the table from arg and associate the column number with the call result
                else if (IsIndexOfMethod(methodInvoked))
                {
                    var arg = methodCallStmt.Arguments[0];
                    var table = scopeData.schemaMap[arg];
                    var columnLiteral = ObtainColumnLiteral(methodCallStmt.Arguments[1]);

                    scopeData.columnMap[callResult] = columnLiteral.ColumnName;
                    this.State.A2_Variables.Add(callResult, new TraceableColumn(table.ToString(), columnLiteral));
                    // Y have the bidingVar that refer to the column, now I can find the "field"
                }
                else
                {
                    result = false;
                }
                return result;
            }

            private HashSet<Traceable> GetTraceablesFromA2_Variables(IVariable arg)
            {
                var union = new HashSet<Traceable>();
                foreach (var argAlias in GetAliases(arg))
                {
                    if (this.State.A2_Variables.ContainsKey(argAlias))
                    {
                        union.UnionWith(this.State.A2_Variables[argAlias]);
                    }
                }

                return union;
            }
            public override void Visit(PhiInstruction instruction)
            {
                UpdateUsingDefUsed(instruction);
            }
            public override void Default(Instruction instruction)
            {
                UpdateUsingDefUsed(instruction);

                // base.Default(instruction);
            }

            private void UpdateUsingDefUsed(Instruction instruction)
            {
                foreach (var result in instruction.ModifiedVariables)
                {
                    var union = new HashSet<Traceable>();
                    foreach (var arg in instruction.UsedVariables)
                    {
                        var tables = GetTraceablesFromA2_Variables(arg);
                        union.UnionWith(tables);

                    }
                    this.State.A2_Variables[result] = union;
                }
            }
        }


        private IDictionary<IVariable, IExpression> equalities;
        DataFlowAnalysisResult<PointsToGraph>[] ptgs;
        private ScopeInfo scopeData;
        private IList<InstanceFieldAccess> specialFields;
        private ITypeDefinition iteratorClass; 

        public IteratorDependencyAnalysis(ITypeDefinition iteratorClass, ControlFlowGraph cfg, DataFlowAnalysisResult<PointsToGraph>[] ptgs,
                                            IList<InstanceFieldAccess> specialFields, IDictionary<IVariable, IExpression> equalitiesMap) : base(cfg)
        {
            this.iteratorClass = iteratorClass;
            this.specialFields = specialFields;
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
                        depValues.A3_Clousures.Add(new Location(ptgNode, target.Key), new TraceableTable(target.Key.Name));
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
            // var dominatorState = this.Result[node.ImmediateDominator.Id].Output;

            //var traceables = dominatorState.ControlVariables.SelectMany(controlVar => oldInput.A2_Variables.ContainsKey(controlVar)? 
            //                                                                    oldInput.A2_Variables[controlVar]: new HashSet<Traceable>());

            //if (traceables.Any())
            //{
            //    foreach (var v in oldInput.A2_Variables.Keys)
            //    {
            //        oldInput.A2_Variables.AddRange(v, traceables);
            //    }
            //}

            var visitor = new MoveNextVisitorForDependencyAnalysis(this, node, this.equalities, this.scopeData, currentPTG, oldInput);
            visitor.Visit(node);
            return visitor.State;
        }
    }
    #endregion
}
