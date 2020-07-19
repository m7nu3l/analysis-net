using Model;
using Model.ThreeAddressCode.Instructions;
using Model.Types;
using System;
using System.Collections.Generic;

namespace Model
{
    public class InstructionInserter
    {
        // This will modify the current method body and clean unused labels.
        // It helps to reduce complexity while adding an instruction.
        // If it has a label it is because it is being targeted.
        internal InstructionInserter(MethodBody methodBody)
        {
            this.methodBody = methodBody;

            methodBody.RemoveUnusedLabels();
        }
        private MethodBody methodBody;
        public void AddBefore(IInstruction target, IInstruction newInstruction)
        {
            // In middle insertions can be very expensive as MethodBody uses an array list.
            // In the future, we may have to reconsider which data structure to use.

            var index = methodBody.Instructions.IndexOf(target);
            if (index == -1)
                throw new NotSupportedException("The target instruction it is not in the method body.");

            methodBody.Instructions.Insert(index, newInstruction);

            // Update the label.
            if (target.Label != null)
            {
                // Since we used RemoveUnusedLabels an instruction has a non null Label if it is targeted.
                newInstruction.Label = target.Label;
                target.Label = null;
            }

        }

        public void Replace(IInstruction target, IInstruction newInst)
        {
            newInst.Label = target.Label;
            var index = methodBody.Instructions.IndexOf(target);
            if (index == -1)
                throw new NotSupportedException("The target instruction it is not in the method body.");

            methodBody.Instructions.Insert(index, newInst);
            methodBody.Instructions.RemoveAt(index + 1);
        }

    }
}
