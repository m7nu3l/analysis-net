﻿// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Model.ThreeAddressCode.Values;
using TacAnalyses.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Model.Types;
using Model;

namespace TacAnalyses.Model
{
	// Unknown PTG nodes represent placeholders
	// (external nodes that can be null or
	// stand for multiple nodes).
	// Useful to model parameter values.
    public enum PTGNodeKind
    {
        Null,
        Object,
		Global,
		Delegate,
		Unknown
	}

	public class PTGNode
	{
		public int Id { get; private set; }
		public PTGNodeKind Kind { get; private set; }
		public uint Offset { get; set; }
		public IType Type { get; set; }
		public IMethodReference Method { get; set; }

		public PTGNode(int id, PTGNodeKind kind = PTGNodeKind.Null)
        {
			this.Id = id;
            this.Kind = kind;
			this.Type = PlatformType.Object;
        }

		//public PTGNode(int id, IType type, PTGNodeKind kind = PTGNodeKind.Object, uint offset = 0)
		//	: this(id, kind)
		//{
		//	this.Offset = offset;
		//	this.Type = type;
		//}

		public PTGNode(int id, IType type, IMethodReference method, PTGNodeKind kind = PTGNodeKind.Object, uint offset = 0)
			: this(id, kind)
		{
			this.Offset = offset;
			this.Type = type;
			this.Method = method;
		}

		public override bool Equals(object obj)
		{
			if (object.ReferenceEquals(this, obj)) return true;
			var other = obj as PTGNode;

			return other != null &&
				this.Id == other.Id &&
				this.Kind == other.Kind &&
				this.Offset == other.Offset &&
				object.Equals(this.Type, other.Type);
		}

		public override int GetHashCode()
		{
			return this.Id.GetHashCode();
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
					result = string.Format("{0}: ({1}) {2}", this.Id, this.Kind, this.Type);
					break;
			}

			return result;
		}
    }

	public class PTGDelegateNode : PTGNode
	{
		public IFunctionReference Target { get; set; }

		public PTGDelegateNode(int id, IType type, IMethodReference method, uint offset = 0)
			: base(id, type, method, PTGNodeKind.Delegate, offset)
		{
		}

		public bool IsStatic
		{
			get
			{
				// This doesn't work because even instance methods are StaticMethodReference.
				// return this.Target is StaticMethodReference;

				return this.Target.Method.IsStatic;
			}
		}
	}

	// TODO: This class should be a struct or remove it and just use an string.
	public class PTGNodeField
	{
		public IType Type { get; set; }
		public string Name { get; set; }

		public PTGNodeField(string name, IType type)
		{
			this.Name = name;
			this.Type = type;
		}

		public override string ToString()
		{
			return string.Format("{0} {1}", this.Type, this.Name);
		}

		public override int GetHashCode()
		{
			return this.Name.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			var other = obj as PTGNodeField;

			var result = other != null &&
						 this.Name == other.Name;
						 // Sometimes we don't know the type of an special field like []
						 //&& this.Type.Equals(other.Type);

			return result;
		}
	}

    public class PointsToGraph
    {
		public const int NullNodeId = 0;
		public const string ResultVariableName = "$result";

		private IDictionary<int, PTGNode> nodes;
		private MapSet<IVariable, PTGNode> roots;
		private MapSet<PTGNode, IVariable> variables;
		private IDictionary<PTGNode, MapSet<PTGNodeField, PTGNode>> targets;
		private IDictionary<PTGNode, MapSet<PTGNodeField, PTGNode>> sources;

		public PTGNode Null { get; private set; }
		public IVariable ResultVariable { get; set; }

		public PointsToGraph()
			: this(null)
        {
        }

		private PointsToGraph(PTGNode nullNode)
		{
			this.nodes = new Dictionary<int, PTGNode>();
			this.roots = new MapSet<IVariable, PTGNode>();
			this.variables = new MapSet<PTGNode, IVariable>();
			this.targets = new Dictionary<PTGNode, MapSet<PTGNodeField, PTGNode>>();
			this.sources = new Dictionary<PTGNode, MapSet<PTGNodeField, PTGNode>>();

			if (nullNode == null)
			{
				this.Null = new PTGNode(NullNodeId, PTGNodeKind.Null);
			}
			else
			{
				this.Null = nullNode;
			}

			this.Add(this.Null);
		}

        public IEnumerable<IVariable> Variables
        {
            get { return roots.Keys; }
        }

		public IEnumerable<PTGNode> Nodes
		{
			get { return nodes.Values; }
		}

		public PointsToGraph Clone()
		{
			var ptg = new PointsToGraph(this.Null);
			ptg.ResultVariable = this.ResultVariable;
            ptg.Union(this);
			return ptg;
		}

		public void Union(PointsToGraph ptg)
		{
			// add all nodes
			foreach (var node in ptg.Nodes)
			{
				Add(node);
			}

			// add all edges

			// add variable ---> node edges
			foreach (var entry in ptg.roots)
			{
				roots.AddRange(entry.Key, entry.Value);
			}

			// add node ---> variable edges
			foreach (var entry in ptg.variables)
			{
				variables.AddRange(entry.Key, entry.Value);
			}

			// add node -field-> target edges
			foreach (var entry in ptg.targets)
			{
				var edges = GetEdges(targets, entry.Key);

				foreach (var edge in entry.Value)
				{
					edges.AddRange(edge.Key, edge.Value);
				}
			}

			// add source -field-> node edges
			foreach (var entry in ptg.sources)
			{
				var edges = GetEdges(sources, entry.Key);

				foreach (var edge in entry.Value)
				{
					edges.AddRange(edge.Key, edge.Value);
				}
			}
		}

		public bool Contains(IVariable variable)
		{
			return roots.ContainsKey(variable);
		}

		public bool Contains(PTGNode node)
		{
			return ContainsNode(node.Id);
		}

		public bool ContainsNode(int id)
		{
			return nodes.ContainsKey(id);
		}

		public void Add(IVariable variable)
		{
			roots.Add(variable);
		}

		public void Add(PTGNode node)
		{
			if (Contains(node)) return;
			nodes.Add(node.Id, node);

			variables.Add(node);
			targets.Add(node, new MapSet<PTGNodeField, PTGNode>());
			sources.Add(node, new MapSet<PTGNodeField, PTGNode>());
		}

		public PTGNode GetNode(int id)
		{
			return nodes[id];
		}

		public ISet<PTGNode> GetTargets(IVariable variable)
		{
			return roots[variable];
		}

		public ISet<IVariable> GetVariables(PTGNode node)
		{
			return variables[node];
		}

		public MapSet<PTGNodeField, PTGNode> GetTargets(PTGNode node)
		{
			return targets[node];
		}

		public MapSet<PTGNodeField, PTGNode> GetSources(PTGNode node)
		{
			return sources[node];
		}

		public ISet<PTGNode> GetTargets(IVariable variable, PTGNodeField field)
		{
			var result = new HashSet<PTGNode>();
			var targets = GetTargets(variable);

			foreach (var node in targets)
			{
				HashSet<PTGNode> fieldTargets;
				var nodeTargets = GetTargets(node);

				var ok = nodeTargets.TryGetValue(field, out fieldTargets);

				if (ok)
				{
					result.AddRange(fieldTargets);
				}
			}

			return result;
		}

		public ISet<PTGNode> GetTargets(PTGNode node, PTGNodeField field)
		{
			var targets = GetTargets(node);
			return targets[field];
		}

		public ISet<PTGNode> GetSources(PTGNode node, PTGNodeField field)
		{
			var sources = GetSources(node);
			return sources[field];
		}

		public void Remove(IVariable variable)
		{
			RemoveEdges(variable);
			roots.Remove(variable);
		}

		public void Remove(PTGNode node)
		{
			RemoveEdges(node);
			targets.Remove(node);
			sources.Remove(node);
			variables.Remove(node);
			nodes.Remove(node.Id);
		}

		public void PointsTo(IVariable variable, PTGNode target)
        {
#if DEBUG
			if (!Contains(target))
				throw new ArgumentException("Target node does not belong to this Points-to graph.", "target");
#endif

            roots.Add(variable, target);
			variables.Add(target, variable);
        }

        public void PointsTo(PTGNode source, PTGNodeField field, PTGNode target)
        {
#if DEBUG
			if (!Contains(source))
				throw new ArgumentException("Source node does not belong to this Points-to graph.", "source");

			if (!Contains(target))
				throw new ArgumentException("Target node does not belong to this Points-to graph.", "target");
#endif
			// Null node cannot points-to other nodes.
			if (source.Kind == PTGNodeKind.Null) return;

			var edges = GetEdges(targets, source);
			edges.Add(field, target);

			edges = GetEdges(sources, target);
			edges.Add(field, source);
        }

		public void RemoveEdges(PTGNode node)
		{
			var hasNode = Contains(node);
			if (!hasNode) return;

			{
				// Remove variable -> node edges
				var variables = GetVariables(node);

				foreach (var variable in variables)
				{
					var targets = GetTargets(variable);
					targets.Remove(node);
				}

				// Remove only the variable edges of the node,
				// but not the node itself
				variables.Clear();
			}

			{
				// Remove node -> target edges
				var edges = GetTargets(node);

				foreach (var edge in edges)
				{
					foreach (var target in edge.Value)
					{
						var sources = GetSources(target, edge.Key);

						sources.Remove(node);
					}
				}

				// Remove only the target edges of the node,
				// but not the node itself
				edges.Clear();
			}

			{
				// Remove source -> node edges
				var edges = GetSources(node);

				foreach (var edge in edges)
				{
					foreach (var source in edge.Value)
					{
						var targets = GetTargets(source, edge.Key);

						targets.Remove(node);
					}
				}

				// Remove only the source edges of the node,
				// but not the node itself
				edges.Clear();
			}
		}

		public void RemoveEdges(IVariable variable)
        {
			var hasVariable = Contains(variable);
			if (!hasVariable) return;

			{
				// Remove target -> variable edges
				var targets = GetTargets(variable);

				foreach (var target in targets)
				{
					var variables = GetVariables(target);
					variables.Remove(variable);
				}

				// Remove only the target edges of the variable,
				// but not the variable itself
				targets.Clear();
			}
        }

        public bool GraphEquals(object obj)
        {
			if (object.ReferenceEquals(this, obj)) return true;
            var other = obj as PointsToGraph;

			Func<PTGNode, PTGNode, bool> nodeEquals = (a, b) => a.Equals(b);
			Func<MapSet<PTGNodeField, PTGNode>, MapSet<PTGNodeField, PTGNode>, bool> edgeEquals = (a, b) => a.MapEquals(b);

			return other != null &&
				roots.MapEquals(other.roots) &&
				//variables.MapEquals(other.variables) &&
				nodes.DictionaryEquals(other.nodes, nodeEquals) &&
				//sources.DictionaryEquals(other.sources, edgeEquals) &&
				targets.DictionaryEquals(other.targets, edgeEquals);
        }

		// binding: parameter -> argument
		public MapSet<IVariable, PTGNode> NewFrame(IDictionary<IVariable, IVariable> binding)
		{
			// TODO: We should keep global variables (variables with names starting with # and points-to global nodes)

			// Remove node -> variable edges
			//variables.Clear();
			foreach (var entry in variables)
			{
				entry.Value.Clear();
			}

			var callerFrame = roots;
			// Clear variable -> node edges
			roots = new MapSet<IVariable, PTGNode>();

			foreach (var entry in binding)
			{
				if (!callerFrame.ContainsKey(entry.Value)) continue;

				// Get argument targets
				IEnumerable<PTGNode> targets = callerFrame[entry.Value];

				if (entry.Key.IsParameter && entry.Key.Name == "this")
				{
					// Implicit this parameter cannot points-to null.
					targets = targets.Where(n => n.Kind != PTGNodeKind.Null);
				}

				// Add parameter -> node edges
				roots.AddRange(entry.Key, targets);

				// Add node -> parameter edges
				foreach (var node in targets)
				{
					variables.Add(node, entry.Key);
				}
			}

			/* This is not needed anymore since clonned PT graphs always share the same nodes.
			
				// This is needed so it works with clonned PT graphs (different nodes but with the same id)
				var result = callerFrame.ToDictionary(entry => entry.Key, entry => entry.Value.Select(n => n.Id)).ToMapSet();
			*/

			var result = callerFrame;
			return result;
		}

		// binding: parameter -> argument
		public void RestoreFrame(MapSet<IVariable, PTGNode> frame, IDictionary<IVariable, IVariable> binding)
		{
			/* This is not needed anymore since clonned PT graphs always share the same nodes.
			
				// This is needed so it works with clonned PT graphs (different nodes but with the same id)
				var frame2 = frame.ToDictionary(entry => entry.Key, entry => entry.Value.Select(id => nodes[id])).ToMapSet();
			*/

			// Remove node -> variable edges
			//variables.Clear();
			foreach (var entry in variables)
			{
				entry.Value.Clear();
			}

			var calleeFrame = roots;
			// Restore variable -> node edges
			roots = frame;

			foreach (var entry in binding)
			{
				if (!calleeFrame.ContainsKey(entry.Key)) continue;

				// Get parameter targets
				var targets = calleeFrame[entry.Key];

				// Set argument -> node edges
				roots[entry.Value] = targets;
			}

			// Add node -> variable edges
			foreach (var entry in roots)
			{
				foreach (var node in entry.Value)
				{
					variables.Add(node, entry.Key);
				}
			}

			// Set result variable
			var result = roots.Keys.SingleOrDefault(v => v.Name == ResultVariableName);
			this.ResultVariable = result;
		}

		// binding: parameter -> argument
		// Extracts a subgraph that only contains nodes reachable from
		// parameters and global variables (and all the edges between them).
		public PointsToGraph NewFrame2(IDictionary<IVariable, IVariable> binding)
		{
			var ptg = new PointsToGraph(this.Null);
			var worklist = new HashSet<PTGNode>();

			// Add global nodes and global variables
			foreach (var edge in roots)
			{
				// TODO: Replace v.Name.StartsWith("#") by a better way to recognize global variables!
				if (edge.Key.Name.StartsWith("#"))
				{
					var globalVariable = edge.Key;
					// Should be only one global node per global variable!
					var globalNode = edge.Value.Single();

					// Add global node
					ptg.Add(globalNode);

					// Add global variable ---> global node edge
					ptg.roots.Add(globalVariable, globalNode);

					// Add global node ---> global variable edge
					ptg.variables.Add(globalNode, globalVariable);

					// Include global nodes in the reachable worklist
					worklist.Add(globalNode);
				}
			}

			// Add all edges between parameters and nodes

			foreach (var entry in binding)
			{
				HashSet<PTGNode> targets;
				var parameter = entry.Key;
				var argument = entry.Value;
				
				if (roots.TryGetValue(argument, out targets))
				{
					foreach (var target in targets)
					{
						// Add target node
						ptg.Add(target);

						// Add node ---> parameter edge
						ptg.variables.Add(target, parameter);
					}

					// Add parameter ---> node edges
					ptg.roots.AddRange(parameter, targets);

					// Include parameter targets in the reachable worklist
					worklist.UnionWith(targets);
				}
			}

			// Add all reachable nodes and edges between them

			while (worklist.Count > 0)
			{
				var node = worklist.First();
				worklist.Remove(node);

				var targetEdges = targets[node];
				var ptgTargetEdges = ptg.targets[node];

				foreach (var edge in targetEdges)
				{
					// Include new target nodes in the reachable
					// worklist before adding them to ptg.
					var newNodes = edge.Value.Except(ptg.nodes.Values);
					worklist.UnionWith(newNodes);

					// Add node -field-> target edges
					ptgTargetEdges.AddRange(edge.Key, edge.Value);

					foreach (var target in edge.Value)
					{
						// Add target node
						ptg.Add(target);

						var ptgSourceEdges = ptg.sources[target];

						// Add source -field-> node edges
						ptgSourceEdges.Add(edge.Key, node);
					}
				}
			}

			return ptg;
		}

		// binding: parameter -> argument
		// Generates a clone of this graph that has all the effects made by the given callee graph applied. 
		public PointsToGraph RestoreFrame2(PointsToGraph ptgCallee, IDictionary<IVariable, IVariable> binding)
		{
			var ptg = this.Clone(); // Not sure if clonning is needed
			var worklist = new HashSet<PTGNode>();
			var visited = new HashSet<PTGNode>();

			// Add global nodes and global variables
			foreach (var edge in ptgCallee.roots)
			{
				// TODO: Replace v.Name.StartsWith("#") by a better way to recognize global variables!
				if (edge.Key.Name.StartsWith("#"))
				{
					var globalVariable = edge.Key;
					// Should be only one global node per global variable!
					var globalNode = edge.Value.Single();

					// Add global node
					ptg.Add(globalNode);

					// Add global variable ---> global node edge
					ptg.roots.Add(globalVariable, globalNode);

					// Add global node ---> global variable edge
					ptg.variables.Add(globalNode, globalVariable);

					// Include global nodes in the reachable worklist
					worklist.Add(globalNode);
				}
			}

			// Add returning nodes
			if (ptgCallee.ResultVariable != null)
			{
				var parameter = ptgCallee.ResultVariable;
				var argument = binding[parameter];
				var parameterTargets = ptgCallee.roots[parameter];

				// Strong update
				ptg.RemoveEdges(argument);

				foreach (var target in parameterTargets)
				{
					// Add target node
					ptg.Add(target);

					// Add node ---> argument edge
					ptg.variables.Add(target, argument);
				}

				// Add argument ---> node edges
				ptg.roots.AddRange(argument, parameterTargets);
			}

			// Include argument targets in the reachable worklist
			foreach (var argument in binding.Values)
			{
				HashSet<PTGNode> targets;

				if (ptg.roots.TryGetValue(argument, out targets))
				{
					worklist.UnionWith(targets);
				}
			}

			// Add all reachable nodes and edges between them

			while (worklist.Count > 0)
			{
				var node = worklist.First();
				worklist.Remove(node);
				visited.Add(node);

				var ptgCalleeTargetEdges = ptgCallee.targets[node];
				var ptgTargetEdges = ptg.targets[node];

				foreach (var edge in ptgCalleeTargetEdges)
				{
					// Add node -field-> target edges
					ptgTargetEdges.AddRange(edge.Key, edge.Value);

					foreach (var target in edge.Value)
					{
						// Add target node
						ptg.Add(target);

						var ptgSourceEdges = ptg.sources[target];

						// Add source -field-> node edges
						ptgSourceEdges.Add(edge.Key, node);
					}

					var newNodes = edge.Value.Except(visited);
					worklist.UnionWith(newNodes);
				}
			}

			return ptg;
		}

		private static MapSet<PTGNodeField, PTGNode> GetEdges(IDictionary<PTGNode, MapSet<PTGNodeField, PTGNode>> mapping, PTGNode node)
		{
			MapSet<PTGNodeField, PTGNode> result;

			var ok = mapping.TryGetValue(node, out result);

			if (!ok)
			{
				result = new MapSet<PTGNodeField, PTGNode>();
				mapping.Add(node, result);
			}

			return result;
		}
	}
}
