// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Model.ThreeAddressCode.Instructions;
using Model.ThreeAddressCode.Values;
using Backend.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Model.Types;
using Model;

namespace Backend.Model
{
	// Unknown PTG nodes represent placeholders
	// (external objects that can be null or
	// stand for multiple objects).
	// Useful to model parameter values.
    public enum PTGNodeKind
    {
        Null,
        Object,
		Unknown,
        Parameter,
        Delegate
    }

    
    public interface PTGContext
    {

    }

    public class MethodContex : PTGContext
    {
        public MethodContex(IMethodReference method)
        {
            this.Method = method;
        }
        public IMethodReference Method { get; set;  }
        public override string ToString()
        {
            if (Method != null)
            {
                return Method.Name.ToString();
            }
            else return "--";
        }
    }

    public class PTGID
    {
        public PTGID(PTGContext context, int offset)
        {
            this.Context = context;
            this.OffSet = offset;
        }
        PTGContext Context { get; set; }
        public int OffSet { get; set; }
        public override string ToString()
        {
            return String.Format("{0}:{1:X4}",Context,OffSet);
        }
        public override bool Equals(object obj)
        {
            var ptgID = obj as PTGID;
            return ptgID!=null && ptgID.OffSet==OffSet 
                && ptgID.Context==Context || ptgID.Context.Equals(Context);
        }
        public override int GetHashCode()
        {
            if (Context == null) return OffSet.GetHashCode();
            return Context.GetHashCode()+OffSet.GetHashCode();
        }
    }

    public class PTGNode
    {
		public PTGID Id { get; private set; }
		public PTGNodeKind Kind { get; private set; }
		public uint Offset { get; set; }
        public IType Type { get; set; }
        public ISet<IVariable> Variables { get; private set; }
        public MapSet<IFieldReference, PTGNode> Sources { get; private set; }
        public MapSet<IFieldReference, PTGNode> Targets { get; private set; }

		//public PTGNode(PTGID id, PTGNodeKind kind = PTGNodeKind.Null)
  //      {
		//	this.Id = id;
  //          this.Kind = kind;
  //          this.Variables = new HashSet<IVariable>();
  //          this.Sources = new MapSet<IFieldReference, PTGNode>();
  //          this.Targets = new MapSet<IFieldReference, PTGNode>();
  //      }

		public PTGNode(PTGID id, IType type, PTGNodeKind kind = PTGNodeKind.Object)
		//	: this(id, kind)
		{
            this.Id = id;
			this.Type = type;
            this.Kind = kind;
            this.Variables = new HashSet<IVariable>();
            this.Sources = new MapSet<IFieldReference, PTGNode>();
            this.Targets = new MapSet<IFieldReference, PTGNode>();
        }

		public bool SameEdges(PTGNode node)
		{
			if (node == null) throw new ArgumentNullException("node");

			return this.Variables.SetEquals(node.Variables) &&
				this.Sources.MapEquals(node.Sources) &&
				this.Targets.MapEquals(node.Targets);
		}

		public override bool Equals(object obj)
		{
			if (object.ReferenceEquals(this, obj)) return true;
			var other = obj as PTGNode;

			return other != null &&
				this.Id == other.Id &&
				this.Kind == other.Kind &&
				//this.Offset == other.Offset &&
				object.Equals(this.Type, other.Type);
		}

		public override int GetHashCode()
		{
            //return this.Id.GetHashCode();
            return this.Id.GetHashCode() + this.Kind.GetHashCode();
        }

		public override string ToString()
		{
			string result;

			switch (this.Kind)
			{
				case PTGNodeKind.Null:
					result = "null";
					break;

				default:
					result = string.Format("{0:X$}: {1}", this.Id, this.Type);
					break;
			}

			return result;
		}
        public virtual PTGNode Clone()
        {
            var clone = new PTGNode(this.Id, this.Type, this.Kind);
            return clone;
        }
    }

    public class NullNode : PTGNode
    {
        public static PTGID nullID = new PTGID(null,  0);

        public NullNode() : base(nullID, PlatformTypes.Object, PTGNodeKind.Null)
        {
        }
        public override bool Equals(object obj)
        {
            var oth = obj as NullNode;
            return oth!=null;
        }
        public override int GetHashCode()
        {
            return 0;
        }
        public override string ToString()
        {
            return "Null";
        }
        public override PTGNode Clone()
        {
            return this;
        }

    }

    public class ParameterNode : PTGNode
    {
        public  string Parameter { get; private set;  }
        public ParameterNode(PTGID id, string parameter, IType type, PTGNodeKind kind = PTGNodeKind.Null) : base(id, type, PTGNodeKind.Parameter)
        {
            this.Parameter = parameter;
        }
        public override bool Equals(object obj)
        {
            var oth = obj as ParameterNode;
            return oth != null && oth.Parameter.Equals(Parameter) && base.Equals(oth);
        }
        public override int GetHashCode()
        {
            return this.Parameter.GetHashCode() + base.GetHashCode();
        }
        public override PTGNode Clone()
        {
            var clone = new ParameterNode(this.Id, this.Parameter, this.Type);
            return clone;
        }

    }

    public class DelegateNode : PTGNode
    {
        public  IMethodReference Method { get; private set; }
        public  IVariable Instance { get; set; }
        public  bool IsStatic { get; private set; }

        public DelegateNode(PTGID id, IMethodReference method, IVariable instance) : base(id, method.ReturnType, PTGNodeKind.Delegate)
        {
            this.Method = method;
            this.Instance = instance;
            this.IsStatic = instance == null;
        }
        public override bool Equals(object obj)
        {
            var oth = obj as DelegateNode;
            return oth != null && oth.Method.Equals(Method) 
                && oth.Instance == this.Instance
                && base.Equals(oth);
        }
        public override int GetHashCode()
        {
            return this.Method.GetHashCode() + (this.IsStatic? 1: this.Instance.GetHashCode())
                + base.GetHashCode();
        }
        public override PTGNode Clone()
        {
            var node = new DelegateNode(this.Id, this.Method, this.Instance);
            return node;
        }
    }
    public class PointsToGraph
    {
        private Stack<MapSet<IVariable, PTGNode>> stackFrame;
		private MapSet<IVariable, PTGNode> variables;
		private IDictionary<PTGID, PTGNode> nodes;

        public PTGNode Null { get; private set; }

        public PointsToGraph()
        {
            this.Null = new NullNode();
            this.variables = new MapSet<IVariable, PTGNode>();            
			this.nodes = new Dictionary<PTGID, PTGNode>();
            this.Add(this.Null);

            //this.nodeIdAtOffset = new Dictionary<uint, int>();
            //nextPTGNodeId=1;

            //this.stackFrame = new Stack<MapSet<IVariable, PTGNode>>();
        }

        public IEnumerable<IVariable> Variables
        {
            get { return this.variables.Keys; }
        }

		public IEnumerable<PTGNode> Nodes
		{
			get { return nodes.Values; }
		}

		public PointsToGraph Clone()
		{
			var ptg = new PointsToGraph();
            ptg.Union(this);
			return ptg;
		}

		public void Union(PointsToGraph ptg)
		{
            // We assume they have the same stack frame
            if (this.stackFrame == null)
            {
                this.stackFrame = ptg.stackFrame;
            }
            System.Diagnostics.Debug.Assert(this.stackFrame == ptg.stackFrame);
            // add all new nodes
            foreach (var node in ptg.Nodes)
			{
				if (this.Contains(node)) continue;
				var clone = node.Clone();

				nodes.Add(clone.Id, clone);
			}

            // add all variables
			foreach (var variable in ptg.Variables)
			{
				this.variables.Add(variable);
			}

			// add all edges
            foreach (var node in ptg.Nodes)
            {
                var clone = nodes[node.Id];

                // add variable <---> node edges
                foreach (var variable in node.Variables)
                {
                    clone.Variables.Add(variable);
                    this.variables.Add(variable, clone);
                }

                // add source -field-> node edges
                foreach (var entry in node.Sources)
                    foreach (var source in entry.Value)
                    {
                        var source_clone = nodes[source.Id];

                        clone.Sources.Add(entry.Key, source_clone);
                    }

                // add node -field-> target edges
                foreach (var entry in node.Targets)
                    foreach (var target in entry.Value)
                    {
                        var target_clone = nodes[target.Id];

                        clone.Targets.Add(entry.Key, target_clone);
                    }
            }
		}

		public bool Contains(IVariable variable)
		{
			return this.variables.ContainsKey(variable);
		}

		public bool Contains(PTGNode node)
		{
			return this.ContainsNode(node.Id);
		}

		public bool ContainsNode(PTGID id)
		{
			return nodes.ContainsKey(id);
		}

		public void Add(IVariable variable)
		{
			variables.Add(variable);
		}

		public void Add(PTGNode node)
		{
			nodes.Add(node.Id, node);
		}

		public PTGNode GetNode(PTGID ptgId, IType type, PTGNodeKind kind = PTGNodeKind.Object)
		{
            if(nodes.ContainsKey(ptgId))
            {
                return nodes[ptgId];
            }

            var node = new PTGNode(ptgId, type, kind);
            this.Add(node);
            return node;
		}

		public ISet<PTGNode> GetTargets(IVariable variable, bool failIfNotExists = true)
		{
            if (failIfNotExists) return variables[variable];

            if (variables.ContainsKey(variable))
                return variables[variable];

            return new HashSet<PTGNode>();
        }

		public void Remove(IVariable variable)
		{
			this.RemoveEdges(variable);
			variables.Remove(variable);
		}

        public void Rename(IVariable oldVar, IVariable newVar)
        {
            var nodes = this.GetTargets(oldVar);
            foreach(var node in nodes)
            {
                node.Variables.Remove(oldVar);
                node.Variables.Add(newVar);
            }
            variables.AddRange(newVar, nodes);

            variables.Remove(oldVar);
            variables.Add(newVar);
        }

        public void RenameVariables(IEnumerable<KeyValuePair<IVariable,IVariable>> binding)
        {
            foreach(var entry in binding)
            {
                Rename(entry.Key, entry.Value);
            }
        }

        public void RemoveLocalVariables()
        {
            foreach (var variable in Variables.ToArray())
            {
                if (!variable.IsParameter)
                {
                    Remove(variable);
                }
            }
        }
        
        public MapSet<IVariable, PTGNode> NewFrame()
        {
            if(this.stackFrame == null)
            {
                this.stackFrame = new Stack<MapSet<IVariable, PTGNode>>();
            }
            var result = variables;
            foreach (var entry in variables)
            {
                var nodes = entry.Value;
                foreach (var node in nodes)
                {
                    node.Variables.Remove(entry.Key);
                }
            }

            var frame = new MapSet<IVariable, PTGNode>();
            stackFrame.Push(variables);
            variables = frame;
            return result;
        }

        public void CleanUnreachableNodes()
        {
            var reacheableNodes = this.ReachableNodesFromVariables();
            var unreacheableNodes = this.nodes.Values.Except(reacheableNodes);
            foreach (var n in unreacheableNodes.ToList())
            {

                foreach(var target in n.Targets.SelectMany(t => t.Value))
                {
                    var keysOfSourcesToRemove = target.Sources.Where(kv => kv.Value.Contains(n)).Select(kv => kv.Key);
                    foreach(var edgeKey in keysOfSourcesToRemove)
                    {
                        target.Sources[edgeKey].Remove(n);
                    }
                    
                }
                this.nodes.Remove(n.Id);
            }
        }


        public MapSet<IVariable, PTGNode> NewFrame(IEnumerable<KeyValuePair<IVariable, IVariable>> binding)
        {
            var oldFrame = NewFrame();
            foreach (var entry in binding)
            {
                if (oldFrame.ContainsKey(entry.Key))
                {
                    this.variables.Add(entry.Value);
                    foreach (var node in oldFrame[entry.Key])
                    {
                        PointsTo(entry.Value, node);
                    }
                }
            }
            return oldFrame;
        }

        public void RestoreFrame(bool cleanUnreachable = true)
        {
            var frame = stackFrame.Pop();
            variables = frame;
            foreach (var entry in variables)
            {
                var nodes = entry.Value;
                foreach (var node in nodes)
                {
                    node.Variables.Add(entry.Key);
                }
            }
            if(cleanUnreachable)
                CleanUnreachableNodes();
        }

        public void RestoreFrame(IVariable retVariable, IVariable dest, bool cleanUnreachable = true)
        {
            ISet<PTGNode> nodes = null;

            var validReturn =  (dest.Type != null && dest.Type.TypeKind == TypeKind.ReferenceType);

            if (validReturn)
                nodes = GetTargets(retVariable);

            RestoreFrame(false);
            if (validReturn)
            {
                foreach (var node in nodes)
                {
                    PointsTo(dest, node);
                }
            }
            if (cleanUnreachable)
                CleanUnreachableNodes();
        }

        public void PointsTo(IVariable variable, PTGNode target)
        {
#if DEBUG
			if (!this.Contains(target))
				throw new ArgumentException("Target node does not belong to this Points-to graph.", "target");
#endif

            target.Variables.Add(variable);
            this.variables.Add(variable, target);
        }
        public void PointsTo(IVariable variable, IEnumerable<PTGNode> targets)
        {
            foreach(var n in targets)
            {
                PointsTo(variable, n);
            }
        }

        public void PointsTo(PTGNode source, IFieldReference field, PTGNode target)
        {
#if DEBUG
			if (!this.Contains(source))
				throw new ArgumentException("Source node does not belong to this Points-to graph.", "source");

			if (!this.Contains(target))
				throw new ArgumentException("Target node does not belong to this Points-to graph.", "target");
#endif

            source.Targets.Add(field, target);
            target.Sources.Add(field, source);
        }

        public ISet<PTGNode> GetTargets(IVariable variable, IFieldReference field)
        {
            var result = new HashSet<PTGNode>();
            foreach(var ptg in variables[variable])
            {
                result.AddRange(ptg.Targets[field]);
            }
            return result;
        }
        public void RemoveEdges(IVariable variable)
        {
			var hasVariable = this.Contains(variable);
			if (!hasVariable) return;

			var targets = this.variables[variable];

			foreach (var target in targets)
			{
				target.Variables.Remove(variable);
			}

			// If we uncomment the next line
			// the variable will be removed from
			// the graph, not only its edges
			//this.Roots.Remove(variable);

			// Remove only the edges of the variable,
			// but not the variable itself
			targets.Clear();
        }

        public bool GraphEquals(object obj)
        {
			if (object.ReferenceEquals(this, obj)) return true;
            var other = obj as PointsToGraph;

			Func<PTGNode, PTGNode, bool> nodeEquals = (a, b) => a.Equals(b) && a.SameEdges(b);

			return other != null &&
				this.variables.MapEquals(other.variables) &&
				this.nodes.DictionaryEquals(other.nodes, nodeEquals);
        }
    }
}
