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
        Parameter
    }

    public class PTGNode
    {
		public int Id { get; private set; }
		public PTGNodeKind Kind { get; private set; }
		public uint Offset { get; set; }
        public IType Type { get; set; }
        public ISet<IVariable> Variables { get; private set; }
        public MapSet<IFieldReference, PTGNode> Sources { get; private set; }
        public MapSet<IFieldReference, PTGNode> Targets { get; private set; }

		public PTGNode(int id, PTGNodeKind kind = PTGNodeKind.Null)
        {
			this.Id = id;
            this.Kind = kind;
            this.Variables = new HashSet<IVariable>();
            this.Sources = new MapSet<IFieldReference, PTGNode>();
            this.Targets = new MapSet<IFieldReference, PTGNode>();
        }

		public PTGNode(int id, IType type, uint offset = 0, PTGNodeKind kind = PTGNodeKind.Object)
			: this(id, kind)
		{
			this.Offset = offset;
			this.Type = type;
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
				//this.Id == other.Id &&
				this.Kind == other.Kind &&
				this.Offset == other.Offset &&
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
					result = string.Format("{0:X4}: {1}", this.Offset, this.Type);
					break;
			}

			return result;
		}
    }

    public class NullNode : PTGNode
    {
        public NullNode() : base(0, PTGNodeKind.Null)
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
    }

    public class ParameterNode : PTGNode
    {
        public  string Parameter { get; private set;  }
        public ParameterNode(int id, string parameter, PTGNodeKind kind = PTGNodeKind.Null) : base(id, PTGNodeKind.Parameter)
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
    }
    public class PointsToGraph
    {
		private MapSet<IVariable, PTGNode> variables;
		private IDictionary<int, PTGNode> nodes;

        private IDictionary<uint, int> nodeIdAtOffset;
        private int nextPTGNodeId;

        public PTGNode Null { get; private set; }

        public PointsToGraph()
        {
            this.Null = new NullNode();
            this.variables = new MapSet<IVariable, PTGNode>();            
			this.nodes = new Dictionary<int, PTGNode>();
            this.Add(this.Null);

            this.nodeIdAtOffset = new Dictionary<uint, int>();
            nextPTGNodeId=1;

            
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
			// add all new nodes
			foreach (var node in ptg.Nodes)
			{
				if (this.Contains(node)) continue;
				var clone = new PTGNode(node.Id, node.Type, node.Offset, node.Kind);

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

		public bool ContainsNode(int id)
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

		public PTGNode GetNode(uint offset, IType type, PTGNodeKind kind = PTGNodeKind.Object)
		{

            if(nodes.ContainsKey((int)offset))
            {
                return nodes[(int)offset];
            }

            var node = new PTGNode((int)offset, type, offset, kind);
            this.Add(node);
            return node;

            //if (nodeIdAtOffset.ContainsKey(offset))
            //{
            //    var nodeId = nodeIdAtOffset[offset];
            //    return nodes[nodeId];
            //}
            //else
            //{
            //    var nodeId = ++nextPTGNodeId;
            //    var node = new PTGNode(nodeId, type, offset, kind);
            //    this.Add(node);
            //    nodeIdAtOffset.Add(offset, nodeId);
            //    return node;
            //}
            // return nodes[id];
		}

		public ISet<PTGNode> GetTargets(IVariable variable)
		{
			return variables[variable];
		}

		public void Remove(IVariable variable)
		{
			this.RemoveEdges(variable);
			variables.Remove(variable);
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
