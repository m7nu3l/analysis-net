using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

using Model.ThreeAddressCode;
using Model.ThreeAddressCode.Instructions;
using Model;
using Model.Types;

namespace Backend.Model
{
	public class Edge<N,F>
	{
		public Node<N> Source { get; set; }
		public Node<N> Target { get; set; }

		public Edge(Node<N> source, Node<N> target, F label)
		{
			this.Source = source;
			this.Target = target;
		}

		public override string ToString()
		{
			return string.Format("{0} -> {1}", this.Source, this.Target);
		}
	}

    public class Node<N>
    {
        public int Id { get; private set; }
        public N Data { get; private set; }

        public ISet<Node<N>> Predecessors { get; private set; }
        public ISet<Node<N>> Successors { get; private set; }

        public Node(N data)
        {
            this.Id = 0;
            this.Data = data;
            this.Successors = new HashSet<Node<N>>();
            this.Predecessors = new HashSet<Node<N>>();
        }

        public override string ToString()
        {
            string result = this.Data.ToString();

            return result;
        }
        public override bool Equals(object obj)
        {
            var oth = obj as Node<N>;
            return oth.Data.Equals(this.Data);
        }
        public override int GetHashCode()
        {
            return Data.GetHashCode();
        }
    }

	public class Graph<N,F>
	{
		private Node<N>[] forwardOrder;
		private Node<N>[] backwardOrder;

        public IDictionary<N, Node<N>> mapNodes;
        public ICollection<Node<N>> Nodes { 
                get 
                { return mapNodes.Values; }
        }

		public Graph()
		{
			this.mapNodes = new Dictionary<N,Node<N>>();
		}

		public Node<N>[] ForwardOrder
		{
			get
			{
				if (this.forwardOrder == null)
				{
					//this.forwardOrder = this.ComputeForwardTopologicalSort();
				}
				return this.forwardOrder;
			}
		}

		public Node<N>[] BackwardOrder
		{
			get
			{
				if (this.backwardOrder == null)
				{
					//this.backwardOrder = this.ComputeBackwardTopologicalSort();
				}

				return this.backwardOrder;
			}
		}


        public Node<N> AddNode(N data)
        {
            if(!mapNodes.ContainsKey(data))
            {
                mapNodes[data] = new Node<N>(data);
            }
            return mapNodes[data];
        }

        public void ConnectNodes( N predecessor,  N successor)
		{
            var predecessorNode = this.AddNode(predecessor);
            var successorNode = this.AddNode(successor);

            successorNode.Predecessors.Add(predecessorNode);
			predecessorNode.Successors.Add(successorNode);
		}

		#region Topological Sort
 
		private enum TopologicalSortNodeStatus
		{
			NeverVisited, // never pushed into stack
			FirstVisit, // pushed into stack for the first time
			SecondVisit // pushed into stack for the second time
		}

		//private Node<N>[] ComputeForwardTopologicalSort()
		//{
		//	// reverse postorder traversal from entry node
		//	var stack = new Stack<CFGNode>();
		//	var result = new CFGNode[this.Nodes.Count];
		//	var status = new TopologicalSortNodeStatus[this.Nodes.Count];
		//	var index = this.Nodes.Count - 1;

		//	stack.Push(this.Entry);
		//	status[this.Entry.Id] = TopologicalSortNodeStatus.FirstVisit;

		//	do
		//	{
		//		var node = stack.Peek();
		//		var node_status = status[node.Id];

		//		if (node_status == TopologicalSortNodeStatus.FirstVisit)
		//		{
		//			status[node.Id] = TopologicalSortNodeStatus.SecondVisit;

		//			foreach (var succ in node.Successors)
		//			{
		//				if (status[succ.Id] == 0)
		//				{
		//					stack.Push(succ);
		//					status[succ.Id] = TopologicalSortNodeStatus.FirstVisit;
		//				}
		//			}
		//		}
		//		else if (node_status == TopologicalSortNodeStatus.SecondVisit)
		//		{
		//			stack.Pop();
		//			node.ForwardIndex = index;
		//			result[index] = node;
		//			index--;
		//		}
		//	}
		//	while (stack.Count > 0);
                			
		//	return result;
		//}

		//private Node<N>[] ComputeBackwardTopologicalSort()
		//{
		//	// reverse postorder traversal from exit node
		//	var stack = new Stack<Node<N>>();
		//	var result = new Node<N>[this.Nodes.Count];
		//	var status = new TopologicalSortNodeStatus[this.Nodes.Count];
		//	var index = this.Nodes.Count - 1;

		//	stack.Push(this.Exit);
		//	status[this.Exit.Id] = TopologicalSortNodeStatus.FirstVisit;

		//	do
		//	{
		//		var node = stack.Peek();
		//		var node_status = status[node.Id];

		//		if (node_status == TopologicalSortNodeStatus.FirstVisit)
		//		{
		//			status[node.Id] = TopologicalSortNodeStatus.SecondVisit;

		//			foreach (var pred in node.Predecessors)
		//			{
		//				if (status[pred.Id] == 0)
		//				{
		//					stack.Push(pred);
		//					status[pred.Id] = TopologicalSortNodeStatus.FirstVisit;
		//				}
		//			}
		//		}
		//		else if (node_status == TopologicalSortNodeStatus.SecondVisit)
		//		{
		//			stack.Pop();
		//			node.BackwardIndex = index;
		//			result[index] = node;
		//			index--;
		//		}
		//	}
		//	while (stack.Count > 0);

		//	//if (result.Any(n => n == null))
		//	//{
		//	//    var nodes = cfg.Nodes.Where(n => n.Predecessors.Count == 0);

		//	//    throw new Exception("Error");
		//	//}

		//	return result;
		//}

		#endregion
	}
    public class InstructionDependencyGraph // : IDependencyGraph
    {
        Graph<int, string> graph = new Graph<int, string>();
        ControlFlowGraph cfg;
        public InstructionDependencyGraph(ControlFlowGraph cfg)
        {
            this.cfg = cfg;
        }
        public IEnumerable<int> Nodes
        {
            get { return graph.Nodes.Select(n => n.Data); }
        }

        public string Instruction(int offset)
        {
            var result = cfg.Nodes.SelectMany(n => n.Instructions).Where(i => i.Offset == offset);
            if(result.Count()>0)
            {
                var ins = result.Last();
                return ins.ToString();
            }
            return "Entry";
        }

        public IEnumerable<int> Successors(int n)
        {
            return graph.mapNodes[n].Successors.Select(n2 => n2.Data); 
        }

        public void AddVertex(int vertex)
        {
            graph.AddNode(vertex);
        }

        public void ConnectVertex(int src, int dst)
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
                result += String.Format("{0}->{1}\n", n.Data, string.Join(",", n.Successors));
            }
            return result;
        }
    }

}
