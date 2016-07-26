// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Backend.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Model.ThreeAddressCode.Instructions;
using Model.ThreeAddressCode.Values;
using Model;
using Backend.Model;

namespace Backend.Analyses
{
	public class LiveVariablesAnalysis :BackwardDataFlowAnalysis<ISet<IVariable>>
	{
		private DataFlowAnalysisResult<ISet<IVariable>>[] result;
		private ISet<IVariable>[] GEN;
		private ISet<IVariable>[] KILL;

		public LiveVariablesAnalysis(ControlFlowGraph cfg)
			: base(cfg)
		{
		}

		public override DataFlowAnalysisResult<ISet<IVariable>>[] Analyze()
		{
			
			this.ComputeGen();
			this.ComputeKill();

			var result = base.Analyze();

			this.result = result;
			this.GEN = null;
			this.KILL = null;

			return result;
		}

		protected override ISet<IVariable> InitialValue(CFGNode node)
		{
			return GEN[node.Id];
		}

		protected override bool Compare(ISet<IVariable> left, ISet<IVariable> right)
		{
			return left.SetEquals(right);
		}

		protected override ISet<IVariable> Join(ISet<IVariable> left, ISet<IVariable> right)
		{
			var result = new HashSet<IVariable>(left);
			result.UnionWith(right);
			return result;
		}

		protected override ISet<IVariable> Flow(CFGNode node, ISet<IVariable> input)
		{
			var output = new HashSet<IVariable>(input);
            var successor = node.Successors.ToArray();
            for(int i=0; i<successor.Length; i++)
            {
                output.ExceptWith(GetPhiVariables(i, successor[i]));
            }

            var kill = KILL[node.Id];
			var gen = GEN[node.Id];

			output.ExceptWith(kill);
			output.UnionWith(gen);
			return output;
		}

        private ISet<IVariable> GetPhiVariables(int succesorOrder, CFGNode node)
        {
            var result = new HashSet<IVariable>();
            foreach(var instruction in node.Instructions.OfType<PhiInstruction>())
            {
                result.Add(instruction.Arguments[succesorOrder]);
            }
            return result;
        }

		private void ComputeGen()
		{
			GEN = new HashSet<IVariable>[this.cfg.Nodes.Count];
			foreach (var node in this.cfg.Nodes)
			{
                var gen = new HashSet<IVariable>();
                for (int i = node.Instructions.Count-1; i>=0; i--)
				{
                    var instruction = node.Instructions[i];
                    gen.ExceptWith(instruction.ModifiedVariables);
                    gen.UnionWith(instruction.UsedVariables);
                }

				// We only add to gen those definitions of node
				// that reach the end of the basic block

				GEN[node.Id] = gen;
			}
		}

		private void ComputeKill()
		{
			KILL = new ISet<IVariable>[this.cfg.Nodes.Count];
           
            foreach (var node in this.cfg.Nodes)
			{
                var kill = new HashSet<IVariable>();
                kill.UnionWith(node.GetModifiedVariables());
                KILL[node.Id] = kill;
            }
        }
	}
}
