using Model;
using Model.ThreeAddressCode.Instructions;
using Model.ThreeAddressCode.Values;
using Model.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using TacAnalyses.Model;
using TacAnalyses.Utils;

namespace TacAnalyses.Analyses
{
    // Based on Copy Propagation in Advanced Compiler Design Implementation
    public class CopyAssignments : Subset<LoadInstruction>
    {
        public IDictionary<LoadInstruction, int> Positions = new Dictionary<LoadInstruction, int>();

        public CopyAssignments(CopyAssignments subset) : base(subset)
        {
            Positions = subset.Positions;
        }

        public CopyAssignments(LoadInstruction[] universe, bool empty = true) : base(universe, empty)
        {
            for (int i = 0; i < universe.Length; i++)
                Positions.Add(universe[i], i);
        }

        // Slow implementation, improve it!
        public LoadInstruction GetAssignmentTo(IVariable v)
        {
            var copyAssignments = ToSet();
            return copyAssignments.Where(c => c.Result.Equals(v)).SingleOrDefault();
        }
        // Slow implementation, improve it!
        public LoadInstruction[] GetCopiesOf(IVariable v)
        {
            var copyAssignments = ToSet();
            return copyAssignments.Where(c => c.Operand.Equals(v)).ToArray();
        }

        public new CopyAssignments Clone()
        {
            return new CopyAssignments(this);
        }
    }

    public class CopyPropagationAnalysis : ForwardDataFlowAnalysis<CopyAssignments>
    {
        protected LoadInstruction[] copyAssignments;

        private Func<IInstruction, bool> IsCopy = ins => {

            return ins.IsCopy(out IVariable left, out IVariable right);
        };

        public CopyPropagationAnalysis(ControlFlowGraph cfg) : base(cfg)
        {
            copyAssignments = cfg.Nodes.SelectMany(n => n.Instructions).OfType<LoadInstruction>().Where(ins => IsCopy(ins)).ToArray();
        }

        protected override bool Compare(CopyAssignments oldValue, CopyAssignments newValue)
        {
            return oldValue.Equals(newValue);
        }

        protected void Flow(IInstruction ins, CopyAssignments state)
        {
            // A copy assignment x <- y is no longer valid if x or y is modified.

            if (ins.IsCopy(out IVariable left, out IVariable right) && ins is LoadInstruction copyAssignment)
            {
                var assignmentToLeft = state.GetAssignmentTo(left);
                if (assignmentToLeft != null)
                    state.Remove(state.Positions[assignmentToLeft]);

                state.Add(state.Positions[copyAssignment]);

                foreach (var idx in state.GetCopiesOf(left).Select(i => state.Positions[i]))
                    state.Remove(idx);
            }
            else
            {
                if (ins is DefinitionInstruction definitionInstruction && definitionInstruction.HasResult)
                {
                    var assignment = state.GetAssignmentTo(definitionInstruction.Result);
                    if (assignment != null)
                        state.Remove(state.Positions[assignment]);

                    var toRemove = state.GetCopiesOf(definitionInstruction.Result).Select(i => state.Positions[i]);
                    foreach (var idx in toRemove)
                        state.Remove(idx);
                }
            }
        }

        protected override CopyAssignments Flow(CFGNode node, CopyAssignments input)
        {
            CopyAssignments result = input.Clone();
            foreach (var ins in node.Instructions)
                Flow(ins, result);

            return result;
        }

        protected override CopyAssignments InitialValue(CFGNode node)
        {
            if (node.Kind == CFGNodeKind.Entry)
                return new CopyAssignments(copyAssignments, true);

            var result = new CopyAssignments(copyAssignments, false);
            result.AddAll();

            return result;
        }

        protected override CopyAssignments Join(CopyAssignments left, CopyAssignments right)
        {
            left.Intersect(right);
            return left.Clone();
        }
    }

    public class CopyPropagationTransformation : CopyPropagationAnalysis
    {
        public CopyPropagationTransformation(ControlFlowGraph cfg) : base(cfg)
        {
        }

        public void Transform(MethodBody methodBody)
        {
            LocallyCopyPropagate(methodBody);
            Analyze();
            GloballyCopyPropagate(methodBody);
        }

        private void LocallyCopyPropagate(MethodBody methodBody)
        {
            foreach (var n in cfg.Nodes)
            {
                var inputState = new CopyAssignments(copyAssignments, true);
                foreach (var ins in n.Instructions)
                {
                    UpdateInstructionOperands(ins, inputState);
                    Flow(ins, inputState);
                }
            }

            RemoveUnusedDefinitions(methodBody);
        }

        private void GloballyCopyPropagate(MethodBody methodBody)
        {
            foreach (var n in cfg.Nodes)
            {
                var orginalInputState = Result[n.Id].Input;
                if (orginalInputState == null)
                    continue;

                var inputState = orginalInputState.Clone();

                foreach (var ins in n.Instructions)
                {
                    UpdateInstructionOperands(ins, inputState);
                    Flow(ins, inputState);
                }
            }

            RemoveUnusedDefinitions(methodBody);
        }

        private void UpdateInstructionOperands(IInstruction ins, CopyAssignments validAssignments)
        {
            foreach (var usedVar in ins.UsedVariables)
            {
                var replacement = validAssignments.GetAssignmentTo(usedVar);
                if (replacement != null)
                {
                    ins.Replace(usedVar, (IVariable)replacement.Operand);
                }
            }
        }

        private void RemoveUnusedDefinitions(MethodBody body)
        {
            ReachingDefinitionsAnalysis reachingDefinitionsAnalysis = new ReachingDefinitionsAnalysis(cfg);
            reachingDefinitionsAnalysis.Analyze();
            reachingDefinitionsAnalysis.ComputeDefUseAndUseDefChains();

            var toRemove = new Dictionary<IInstruction, NopInstruction>();
            for (int idx = 0; idx < body.Instructions.Count; idx++)
            {
                IInstruction ins = body.Instructions[idx];
                if (ins is LoadInstruction def)
                {
                    bool present = reachingDefinitionsAnalysis.DefinitionUses.TryGetValue(def, out List<IInstruction> uses);
                    if (!present || uses.Count == 0)
                    {
                        // this definition can be safely removed 
                        // add nop in case there is a branch targeting this instruction
                        var old = body.Instructions[idx];
                        var nop = new NopInstruction(ins.Offset);
                        body.Instructions[idx] = nop;
                        body.Instructions[idx].Label = ins.Label;

                        toRemove.Add(old, nop);
                    }
                }
            }

            foreach (var cfn in cfg.Nodes)
            {
                for (int i = 0; i < cfn.Instructions.Count; i++)
                {
                    IInstruction ins = cfn.Instructions[i];
                    if (toRemove.TryGetValue(ins, out NopInstruction nop))
                    {
                        cfn.Instructions[i] = nop;
                    }
                }
            }
        }
    }
}
