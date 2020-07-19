using Model;
using Model.ThreeAddressCode.Instructions;
using Model.ThreeAddressCode.Values;
using Model.ThreeAddressCode.Visitor;
using Model.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using TacAnalyses.Model;
using TacAnalyses.Utils;

namespace TacAnalyses.Analyses
{
    // Based on "Efficient local type inference" (https://dl.acm.org/doi/10.1145/1449764.1449802)
    public class LocalTypeInferenceAnalysis
    {
        private static bool IsInteger(IType type)
        {
            return type.Equals(PlatformType.Int32) || type.Equals(PlatformType.Int16) || type.Equals(PlatformType.Int8) || type.Equals(PlatformType.Int64) ||
                    type.Equals(PlatformType.UInt32) || type.Equals(PlatformType.UInt16) || type.Equals(PlatformType.UInt8) || type.Equals(PlatformType.UInt64) ||
                    type.Equals(PlatformType.Char) || type.Equals(PlatformType.Boolean) || type.Equals(PlatformType.UIntPtr) || type.Equals(PlatformType.IntPtr);
        }

        private static IType GetEvaluationStackType(IType type)
        {
            if (type is FakeIntegerType)
                return PlatformType.Int32;

            if (type.Equals(PlatformType.Boolean) ||
                type.Equals(PlatformType.Char) ||
                type.Equals(PlatformType.Int8) ||
                type.Equals(PlatformType.Int16) ||
                type.Equals(PlatformType.Int32) ||
                type.Equals(PlatformType.UInt16) ||
                type.Equals(PlatformType.UInt32))
            {
                return PlatformType.Int32;
            }

            if (type.Equals(PlatformType.UInt64))
            {
                return PlatformType.Int64;
            }

            if (type.Equals(PlatformType.UIntPtr))
            {
                return PlatformType.IntPtr;
            }

            return type;
        }

        private IDictionary<Typing, QueuedSet<DefinitionInstruction>> worklists
            = new Dictionary<Typing, QueuedSet<DefinitionInstruction>>();

        private ISet<Typing> unfinishedTypings = new HashSet<Typing>();
        private ISet<Typing> typings = new HashSet<Typing>();

        private MethodBody methodBody;
        private IDictionary<IVariable, ISet<DefinitionInstruction>> uses;
        private ClassHierarchy classHierarchy;
        private MethodDefinition methodDefinition;
        private bool analyzed;

        public LocalTypeInferenceAnalysis(MethodDefinition methodDefinition, MethodBody methodBody, ClassHierarchy classHierarchy)
        {
            this.methodBody = methodBody;
            InitializeUses(methodBody.Instructions);
            this.classHierarchy = classHierarchy;
            this.methodDefinition = methodDefinition;
        }

        private Typing GetUnfinishedTyping()
        {
            var enumerator = unfinishedTypings.GetEnumerator();

            if (enumerator.MoveNext())
            {
                var r = enumerator.Current;
                unfinishedTypings.Remove(r);
                return r;
            }

            return null;
        }

        private void AddTyping(Typing typing)
        {
            if (!worklists[typing].IsEmpty())
                unfinishedTypings.Add(typing);
            else
                typings.Add(typing);
        }

        private void InitializeUses(IList<IInstruction> instructions)
        {
            uses = new Dictionary<IVariable, ISet<DefinitionInstruction>>();

            foreach (var ins in instructions.OfType<DefinitionInstruction>())
            {
                if (!ins.HasResult)
                    continue;

                var operands = ins.UsedVariables;
                foreach (var operand in operands)
                {
                    if (!uses.TryGetValue(operand, out ISet<DefinitionInstruction> definitions))
                    {
                        definitions = new HashSet<DefinitionInstruction>();
                        uses[operand] = definitions;
                    }
                    definitions.Add(ins);
                }
            }
        }

        private void SetInitialConstraints(Typing typing)
        {
            // set return type of each returned variable based on method definition.
            var returnInstructions = methodBody.Instructions.OfType<ReturnInstruction>().Where(r => r.HasOperand);
            foreach (var retIns in returnInstructions)
                typing.SetType(retIns.Operand, methodDefinition.ReturnType);

            // type each parameter based on method definition.
            foreach (var param in methodDefinition.Parameters)
            {
                int index = methodDefinition.IsStatic ? param.Index : param.Index + 1;
                var variable = methodBody.Parameters[index];
                typing.SetType(variable, param.Type);
            }

            // set 'this' type based on method definition
            if (!methodDefinition.IsStatic)
            {
                typing.SetType(methodBody.Parameters.First(), methodBody.Parameters.First().Type);
            }

            // get type constraint from the instruction
            var initObjectInstructions = methodBody.Instructions.OfType<InitializeObjectInstruction>();
            foreach (var initObjIns in initObjectInstructions)
            {
                typing.SetType(initObjIns.TargetAddress, new PointerType(initObjIns.ObjectType));
            }

            // When pointer types are used as parameters they are strictly required
            // If a parameter is a short and you push an int, that is OK. It is not the case for pointers.
            var methodCallInstructions = methodBody.Instructions.OfType<MethodCallInstruction>();
            foreach (var methodCall in methodCallInstructions)
            {
                foreach (var param in methodCall.Method.Parameters)
                {
                    if (param.Type is PointerType)
                    {
                        int index = methodCall.Method.IsStatic ? param.Index : param.Index + 1;
                        var variable = methodCall.Arguments[index];
                        typing.SetType(variable, param.Type);
                    }
                }
            }

            var variableReferences = methodBody.Instructions.OfType<LoadInstruction>().Where(load => load.Operand is Reference reference
                                                                                                && reference.Value is IVariable
                                                                                                && !typing.GetType(load.Result).Equals(UnknownType.Value));
            foreach (var load in variableReferences)
            {
                // some load instructions processed here are triggered by the previous code on pointer method arguments.
                var reference = load.Operand as Reference;
                var v = reference.Value as IVariable;
                var result = load.Result;

                IType resultType = typing.GetType(result);
                PointerType pointerType = resultType as PointerType;
                if (pointerType.TargetType.Equals(UnknownType.Value))
                    throw new NotSupportedException();

                typing.SetType(v, pointerType.TargetType);
            }
        }
        public ICollection<Typing> Analyze()
        {
            var typing = Typing.GetBottomTyping(methodBody.LocalVariables.Union(methodBody.Parameters).ToList());
            SetInitialConstraints(typing);

            // void method calls are considered definition instructions, we don't want them
            var definitions = methodBody.Instructions.OfType<DefinitionInstruction>().Where(def => def.HasResult).ToList();
            worklists[typing] = new QueuedSet<DefinitionInstruction>(definitions);
            AddTyping(typing);

            while ((typing = GetUnfinishedTyping()) != null)
            {
                var worklist = worklists[typing];
                var assignment = worklist.RemoveFirst();

                var currentTypeForVariable = typing.GetType(assignment.Result);
                var inferredTypeForExpression = Evaluate(typing, assignment);
                var lca = LeastCommonAncestors(currentTypeForVariable, inferredTypeForExpression);
                Contract.Assert(lca.Count != 0);
                foreach (var t in lca)
                {
                    if (!currentTypeForVariable.Equals(t))
                    {
                        typing = new Typing(typing);
                        typing.SetType(assignment.Result, t);

                        worklists[typing] = new QueuedSet<DefinitionInstruction>(worklist);
                        worklists[typing].AddLast(Depends(assignment.Result));
                    }

                    AddTyping(typing);
                }
            }

            Typing ty = typings.First();

            if (methodBody.LocalVariables.Any(v => ty.GetType(v).Equals(UnknownType.Value)))
                throw new NotSupportedException("Untyped variable found.");

            analyzed = true;

            return typings;
        }

        public void Transform()
        {
            if (!analyzed)
            {
                throw new NotSupportedException("You must call Analyze() first.");
            }

            // It is not a bug to have more than one typing.
            // Although, I haven't yet really thought about it.
            var typing = typings.First();

            // Promote fake integers
            foreach (var var in methodBody.LocalVariables)
            {
                if (typing.GetType(var) is FakeIntegerType fakeInteger)
                {
                    switch (fakeInteger.Size)
                    {
                        case FakeIntegerType.SizeInBits.I1:
                            typing.SetType(var, PlatformType.Boolean);
                            break;
                        case FakeIntegerType.SizeInBits.I8:
                            typing.SetType(var, PlatformType.Int8);
                            break;
                        case FakeIntegerType.SizeInBits.I16:
                            typing.SetType(var, PlatformType.Int16);
                            break;
                    }
                }
            }

            // If we call again SetInitialConstraints they may chagne variable types and that may break some other uses.
            // SetInitialConstraints(typing);

            foreach (var var in methodBody.LocalVariables)
            {
                var.Type = typing.GetType(var);
            }

            // The following transformation make adhoc transformation that shouldn't conflict with anything else.
            // They prevent changing variable types.

            var signCastTransformation = new ExplicitSignOrNarrowingCastTransformation();
            signCastTransformation.Transform(methodBody);

            var fieldCasts = new CastInstanceFieldAcceses(classHierarchy);
            fieldCasts.Transform(methodBody);

            var branchCasts = new CastConditionalBranchInstructions();
            branchCasts.Transform(methodBody);

            var callerCasts = new CastCaller(classHierarchy);
            callerCasts.Transform(methodBody);

            var returnCasts = new CastReturn(classHierarchy);
            returnCasts.Transform(methodBody, methodDefinition);
        }

        // Returns all assignments where 'var' is on the right side.
        private ISet<DefinitionInstruction> Depends(IVariable var)
        {
            if (uses.TryGetValue(var, out ISet<DefinitionInstruction> result))
                return result;
            return new HashSet<DefinitionInstruction>();
        }

        private IType Evaluate(Typing t, DefinitionInstruction definitionInstruction)
        {
            // Evaluator evaluates the RHS of the assignment (definitionInstruction)
            var evaluator = new Evaluator()
            {
                Typing = t,
            };
            definitionInstruction.Accept(evaluator);
            return evaluator.Result;
        }

        private ISet<IType> LeastCommonAncestors(IType t1, IType t2)
        {
            if (t1.Equals(t2))
                return new HashSet<IType>() { t2 };

            if (UnknownType.Value.Equals(t1))
                return new HashSet<IType>() { t2 };

            if (UnknownType.Value.Equals(t2))
                return new HashSet<IType>() { t1 };

            // At this point, t1 or t2 can't be UnknownType.Value
            FakeIntegerType fake1 = t1 as FakeIntegerType;
            FakeIntegerType fake2 = t2 as FakeIntegerType;

            if (fake1 != null || fake2 != null)
                return LeastCommonAncestorsFakeIntegerType(t1, t2);

            PointerType ptrType1 = t1 as PointerType;
            PointerType ptrType2 = t2 as PointerType;

            if (ptrType1 != null || ptrType2 != null)
                return LeastCommonAncestorsPointerType(t1, t2);

            if (IsInteger(t1) && IsInteger(t2))
            {
                if (GetEvaluationStackType(t1).Equals(GetEvaluationStackType(t2)))
                {
                    return new HashSet<IType>() { GetEvaluationStackType(t1) };
                }
                else
                    // maybe this is not a bug
                    throw new NotSupportedException("Not expected case in LCA");
            }

            if (t1.Equals(PlatformType.Object) || t2.Equals(PlatformType.Object))
                return new HashSet<IType>() { PlatformType.Object };

            if (t1 is IBasicType basicType1 && t2 is IBasicType basicType2)
            {
                var typeDef1 = basicType1.ResolvedType;
                var typeDef2 = basicType2.ResolvedType;
                if (typeDef1 != null && typeDef2 != null)
                {
                    return classHierarchy.LeastCommonAncestors(basicType1, basicType2).Cast<IType>().ToSet();
                }
                else if (typeDef1 == null && typeDef2 != null)
                {
                    var subtypes2 = classHierarchy.GetAllAncestors(basicType2);
                    if (subtypes2.Contains(basicType1))
                        return new HashSet<IType>() { basicType1 };
                    return new HashSet<IType>() { PlatformType.Object };
                } else if (typeDef1 != null && typeDef2 == null)
                {
                    var subtypes1 = classHierarchy.GetAllAncestors(basicType1);
                    if (subtypes1.Contains(basicType2))
                        return new HashSet<IType>() { basicType2 };
                    return new HashSet<IType>() { PlatformType.Object };
                }
                else if (typeDef1 == null && typeDef2 == null)
                {
                    return new HashSet<IType>() { PlatformType.Object };
                }
            }

            throw new NotSupportedException();
        }

        private ISet<IType> LeastCommonAncestorsPointerType(IType t1, IType t2)
        {
            PointerType ptrType1 = t1 as PointerType;
            PointerType ptrType2 = t2 as PointerType;

            if (ptrType1 != null && ptrType2 != null)
            {
                if (ptrType1.TargetType is FakeIntegerType ||
                    ptrType2.TargetType is FakeIntegerType)
                {
                    var targetType = LeastCommonAncestorsFakeIntegerType(ptrType1.TargetType, ptrType2.TargetType).First();
                    return new HashSet<IType>() { new PointerType(targetType) };
                }

                if (ptrType1.TargetType.Equals(UnknownType.Value))
                    return new HashSet<IType>() { new PointerType(ptrType2) };

                if (ptrType2.TargetType.Equals(UnknownType.Value))
                    return new HashSet<IType>() { new PointerType(ptrType1) };

                if (ptrType1.TypeKind == TypeKind.ReferenceType &&
                    ptrType2.TypeKind == TypeKind.ReferenceType)
                {
                    var lca = LeastCommonAncestors(ptrType1.TargetType, ptrType2.TargetType);
                    return lca.Select(t => new PointerType(t)).OfType<IType>().ToSet();
                }
            }

            throw new NotImplementedException();
        }

        private ISet<IType> LeastCommonAncestorsFakeIntegerType(IType t1, IType t2)
        {
            // At this point, t1 or t2 can't be UnknownType.Value
            FakeIntegerType fake1 = t1 as FakeIntegerType;
            FakeIntegerType fake2 = t2 as FakeIntegerType;

            if (fake1 != null && fake2 != null)
            {
                var elem = fake1.Size >= fake2.Size ? fake1 : fake2;
                return new HashSet<IType>() { elem };
            }

            FakeIntegerType remainingFakeType = fake1 == null ? fake2 : fake1;
            if (remainingFakeType != null)
            {
                IType remainingNonFakeType = remainingFakeType != t1 ? t1 : t2;
                switch (remainingFakeType.Size)
                {
                    case FakeIntegerType.SizeInBits.I1:
                        if (PlatformType.Boolean.Equals(remainingNonFakeType) ||
                            PlatformType.Int8.Equals(remainingNonFakeType) ||
                            PlatformType.Int32.Equals(remainingNonFakeType) ||
                            PlatformType.Int16.Equals(remainingNonFakeType))
                            return new HashSet<IType>() { remainingNonFakeType };
                        break;
                    case FakeIntegerType.SizeInBits.I8:
                        if (PlatformType.Int8.Equals(remainingNonFakeType) ||
                            PlatformType.Int32.Equals(remainingNonFakeType) ||
                            PlatformType.Int16.Equals(remainingNonFakeType))
                            return new HashSet<IType>() { remainingNonFakeType };
                        break;
                    case FakeIntegerType.SizeInBits.I16:
                        if (PlatformType.Int32.Equals(remainingNonFakeType) ||
                            PlatformType.Int16.Equals(remainingNonFakeType))
                            return new HashSet<IType>() { remainingNonFakeType };
                        break;
                }
            }

            throw new NotImplementedException();
        }

        public class Typing
        {
            public static Typing GetBottomTyping(IList<IVariable> variables)
            {
                var r = new Typing();
                foreach (var v in variables)
                    r.SetType(v, UnknownType.Value);

                return r;
            }

            public Typing() {
                typing = new Dictionary<IVariable, IType>();
            }

            public Typing(Typing t)
            {
                typing = new Dictionary<IVariable, IType>(t.typing);
            }

            private Dictionary<IVariable, IType> typing;

            public IType GetType(IVariable var) { return typing[var]; }
            public void SetType(IVariable var, IType t) { typing[var] = t; }

            public override int GetHashCode()
            {
                return typing.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (obj is Typing t)
                    return t.typing.Equals(typing);

                return false;
            }
        }

        private class Evaluator : InstructionVisitor
        {
            public IType Result;
            public Typing Typing;

            public override void Visit(BinaryInstruction instruction) {
                switch (instruction.Operation)
                {
                    case BinaryOperation.Add:
                    case BinaryOperation.Div:
                    case BinaryOperation.Mul:
                    case BinaryOperation.Rem:
                    case BinaryOperation.Sub:
                        Result = GetBinaryNumericOpTypeResult(Typing.GetType(instruction.LeftOperand), Typing.GetType(instruction.RightOperand));
                        return;

                    case BinaryOperation.Shl:
                    case BinaryOperation.Shr:
                        Result = GetShiftOpTypeResult(Typing.GetType(instruction.LeftOperand), Typing.GetType(instruction.RightOperand));
                        return;
                }

                throw new NotImplementedException();
            }
            public override void Visit(UnaryInstruction instruction) { throw new NotImplementedException(); }
            public override void Visit(LoadInstruction instruction) {
                Result = Evaluate(instruction.Operand);
            }
            public override void Visit(CatchInstruction instruction) { throw new NotImplementedException(); }
            public override void Visit(ConvertInstruction instruction) {
                if (instruction.Operation == ConvertOperation.Box)
                    Result = PlatformType.Object;
                else if (instruction.Operation == ConvertOperation.Cast ||
                    instruction.Operation == ConvertOperation.Unbox ||
                    instruction.Operation == ConvertOperation.Conv)
                    Result = instruction.ConversionType;
                else
                    throw new NotImplementedException();
            }
            public override void Visit(SizeofInstruction instruction) { throw new NotImplementedException(); }
            public override void Visit(LoadTokenInstruction instruction) { throw new NotImplementedException(); }
            public override void Visit(MethodCallInstruction instruction) {
                /*
                    You have class A<T> with method T Get().
                    In IL a method call to A<int32>::Get() looks like: callvirt instance !0 class A`1<int32>::Get()
                    We want the int32 (generic argument) not the generic reference as the return type . 
                    The returned value can also be used as the return value of the current method.
                    If the current method's return type is int32, then !0 is not compatible.
                     
                int Foo(){
                    A<int32> a = new A<int32>();
                    int b = a.Get();
                    return b;
                }
                */

                if (instruction.Method.ReturnType is IGenericParameterReference genericParamRef)
                {
                    if (genericParamRef.Kind == GenericParameterKind.Type)
                        Result = instruction.Method.ContainingType.GenericArguments.ElementAt(genericParamRef.Index);
                    else
                        Result = instruction.Method.GenericArguments.ElementAt(genericParamRef.Index);
                }
                else if (instruction.Method.ReturnType is IBasicType basicType && basicType.GenericArguments.Count > 0)
                {
                    IBasicType newType = basicType.SolveGenericParameterReferences(instruction);//SolveGenericParameterReferences(instruction, basicType);
                    Result = newType;
                }
                else if (instruction.Method.ReturnType is ArrayType arrayType)
                {
                    Result = arrayType.SolveGenericParameterReferencesInArrayType(instruction);
                }
                else
                    Result = instruction.Method.ReturnType;
            }

            public override void Visit(IndirectMethodCallInstruction instruction) { throw new NotImplementedException(); }
            public override void Visit(CreateObjectInstruction instruction) {
                Result = instruction.AllocationType;
            }
            public override void Visit(CreateArrayInstruction instruction) {
                Result = new ArrayType(instruction.ElementType, instruction.Rank);
            }
            public override void Visit(PhiInstruction instruction) { throw new NotImplementedException(); }

            private IType Evaluate(IValue value)
            {
                if (value is IVariable variable)
                {
                    return Typing.GetType(variable);
                }
                else if (value is Constant constant)
                {
                    var stackType = GetEvaluationStackType(constant.Type);
                    if (stackType.Equals(PlatformType.Int32))
                    {
                        int val;
                        if (constant.Value is sbyte sbyteValue)
                        {
                            val = sbyteValue;
                        }
                        else if (constant.Value is Int16 int16Value)
                        {
                            val = int16Value;
                        }
                        else if (constant.Value is int intValue)
                        {
                            val = intValue;
                        }
                        else
                            throw new NotSupportedException();

                        if (val == 0 || val == 1)
                            return FakeIntegerType.I1;

                        if (SByte.MinValue <= val && SByte.MaxValue >= val)
                            return FakeIntegerType.I8;

                        if (Int16.MinValue <= val && Int16.MaxValue >= val)
                            return FakeIntegerType.I16;

                        if (Int32.MinValue <= val && Int32.MaxValue >= val)
                            return PlatformType.Int32;
                    }

                    return stackType;

                }
                else if (value is IFieldAccess fieldAccess)
                {
                    return fieldAccess.Type;
                } else if (value is Reference reference)
                {
                    if (reference.Value is IVariable referencedVar)
                    {
                        var referencedVarType = Typing.GetType(referencedVar);
                        return new PointerType(referencedVarType);
                    } else if (reference.Value is ArrayElementAccess arrayElementAccess)
                    {
                        var referencedArrayType = Typing.GetType(arrayElementAccess.Array);
                        if (referencedArrayType is UnknownType)
                            return new PointerType(UnknownType.Value);

                        ArrayType arrayType = referencedArrayType as ArrayType;
                        return new PointerType(arrayType.ElementsType);
                    }
                        // I guess these other cases can be determined by the metadata
                        // ie. field types and can be trusted
                        return reference.Type;
                } else if (value is Dereference dereference)
                {
                    IVariable r = dereference.Reference;
                    IType refType = Typing.GetType(r);
                    if (refType is PointerType pointerType)
                        return pointerType.TargetType;
                    return UnknownType.Value;
                } else if (value is IFunctionReference functionReference)
                {
                    return new FunctionPointerType(functionReference.Method);
                } else if (value is ArrayElementAccess arrayElementAccess)
                {
                    IType type = Typing.GetType(arrayElementAccess.Array);
                    if (type is UnknownType)
                        return type;

                    // I guess it must always be ArrayType.
                    ArrayType arrayType = type as ArrayType;
                    return arrayType.ElementsType;
                }
                else
                {
                    throw new NotImplementedException();
                }

                return UnknownType.Value;
            }

            // Based on Table III.6: Shift Operations (ecma cil)
            private IType GetShiftOpTypeResult(IType toBeShifted, IType shiftBy)
            {
                var sToBeShifted = GetEvaluationStackType(toBeShifted);
                var sShiftBy = GetEvaluationStackType(shiftBy);

                if (PlatformType.Int32.Equals(sToBeShifted))
                {
                    if (PlatformType.Int32.Equals(sShiftBy))
                        return sShiftBy;

                    if (PlatformType.IntPtr.Equals(sShiftBy))
                        return sShiftBy;
                }

                if (PlatformType.Int64.Equals(sToBeShifted))
                {
                    if (PlatformType.Int32.Equals(sShiftBy))
                        return sToBeShifted;

                    if (PlatformType.IntPtr.Equals(sShiftBy))
                        return sToBeShifted;
                }

                if (PlatformType.IntPtr.Equals(sToBeShifted))
                {
                    if (PlatformType.Int32.Equals(sShiftBy))
                        return sToBeShifted;

                    if (PlatformType.IntPtr.Equals(sShiftBy))
                        return sToBeShifted;
                }

                return UnknownType.Value;
            }

            // Based on Table III.2: Binary Numeric Operations (ecma cil)
            // We are not handling unverifiable cases
            private IType GetBinaryNumericOpTypeResult(IType left, IType right)
            {
                var sLeft = GetEvaluationStackType(left);
                var sRight = GetEvaluationStackType(right);

                if (PlatformType.Int32.Equals(sLeft))
                {
                    if (PlatformType.Int32.Equals(sRight))
                        return PlatformType.Int32;

                    if (right.Equals(PlatformType.IntPtr))
                        return right;
                }

                if (PlatformType.Int64.Equals(sLeft) || PlatformType.Int64.Equals(sRight))
                {
                    return PlatformType.Int64;
                }

                if (PlatformType.IntPtr.Equals(sLeft))
                {
                    if (PlatformType.Int32.Equals(sRight))
                        return left;

                    if (PlatformType.IntPtr.Equals(sRight))
                        return left;
                }

                return UnknownType.Value;
            }

        }
        private class CastConditionalBranchInstructions{
            public void Transform(MethodBody methodBody)
            {
                var conditionalBranches = methodBody.Instructions.OfType<ConditionalBranchInstruction>().Where(i => IsTarget(i)).ToList();
                foreach (var branch in conditionalBranches)
                {
                    Constant constant = branch.RightOperand as Constant;
                    constant.Type = branch.LeftOperand.Type;
                    constant.Value = null;
                }
            }

            private bool IsTarget(ConditionalBranchInstruction branch)
            {
                IVariable left = branch.LeftOperand;
                IInmediateValue right = branch.RightOperand;

                bool isZero = right is Constant constant && constant.Value is int intVal && intVal == 0;
                bool isReferenceType = left.Type.TypeKind == TypeKind.ReferenceType;

                return isZero && isReferenceType;
            }
        }

        private class CastInstanceFieldAcceses
        {
            private ClassHierarchy classHierarchy;
            public CastInstanceFieldAcceses(ClassHierarchy classHierarchy)
            {
                this.classHierarchy = classHierarchy;
            }

            int i = 0;
            public void Transform(MethodBody methodBody)
            {
                InstructionInserter instructionInserter = methodBody.GetInstructionInserter();

                var loadTargets = methodBody.Instructions.OfType<LoadInstruction>().Where(load => load.Operand is InstanceFieldAccess fa && IsCastNeeded(fa)).ToList();

                foreach (var target in loadTargets)
                {
                    CastInstanceFieldAccess(target.Operand as InstanceFieldAccess, target, instructionInserter);
                }

                var storeTargets = methodBody.Instructions.OfType<StoreInstruction>().Where(store => store.Result is InstanceFieldAccess fa && IsCastNeeded(fa)).ToList();
                foreach (var target in storeTargets)
                {
                    CastInstanceFieldAccess(target.Result as InstanceFieldAccess, target, instructionInserter);
                }
            }

            private bool IsCastNeeded(InstanceFieldAccess fieldAccess)
            {
                var instance = fieldAccess.Instance;
                var field = fieldAccess.Field;

                var instanceType = instance.Type;
                var containingType = field.Type;

                // possible this must be changed.
                if (instanceType is PointerType)
                    return false;

                if (instanceType.Equals(containingType))
                    return false;

                var instanceTypeBasicType = instanceType as IBasicType;

                if (instanceTypeBasicType != null) {
                    var resolvedType = instanceTypeBasicType.ResolvedType;
  
                    if (resolvedType != null)
                    {
                        // If it is an interface it can't have fields.
                        // Although, the type inference analysis by using LCA it may choose a variable to be typed as an interface.
                        return resolvedType.Kind == TypeDefinitionKind.Interface;
                    }

                    if (resolvedType == null) // not defined in the assembly
                        return true;
                }
                
                return false;
            }

            // The idea is to cast cases where instance.Type is an interface which doesn't have the field

            private void CastInstanceFieldAccess(InstanceFieldAccess fieldAccess, IInstruction target, InstructionInserter instructionInserter)
            {
                var instance = fieldAccess.Instance;
                var field = fieldAccess.Field;

                var casted = new LocalVariable("new_casted_transform" + i, false);
                casted.Type = field.ContainingType;
                var cast = new ConvertInstruction(0, casted, instance, ConvertOperation.Cast, field.ContainingType);
                instructionInserter.AddBefore(target, cast);
                fieldAccess.Replace(instance, casted);
                i++;
            }
        }

        // The idea is to make sure that narrowing between int8, int16 and int32 (including unsigned versions)
        // is explicit in our three-address code.
        // Similarly, for sign loss between the same set of types.
        private class ExplicitSignOrNarrowingCastTransformation
        {
            public void Transform(MethodBody methodBody)
            {
                var targets = methodBody.Instructions.OfType<LoadInstruction>().Where(load => load.Operand is IVariable).ToList();
                var inserter = methodBody.GetInstructionInserter();

                foreach (var t in targets)
                {
                    var result = t.Result;
                    var operand = t.Operand as IVariable;

                    if (result.Type.Equals(PlatformType.Boolean) && IsTargetIntegerType(operand.Type))
                    {
                        ReplaceByConvert(t, inserter);
                        continue;
                    }

                    if (IsTargetIntegerType(operand.Type) && IsTargetIntegerType(result.Type))
                    {
                        if (GetSigness(result.Type) != GetSigness(operand.Type) || 
                            GetSizeInBits(result.Type) < GetSizeInBits(operand.Type))
                        {
                            ReplaceByConvert(t, inserter);
                            continue;
                        }
                    }
                }
            }

            private void ReplaceByConvert(LoadInstruction t, InstructionInserter inserter)
            {
                var result = t.Result;
                var operand = t.Operand as IVariable;

                var cast = new ConvertInstruction(t.Offset, result, operand, ConvertOperation.Conv, result.Type);
                inserter.Replace(t, cast);
            }
            enum Signess
            {
                Signed,
                Unsigned,
                NotDefined
            }

            private bool IsTargetIntegerType(IType type)
            {
                if (type.Equals(PlatformType.Int8) || type.Equals(PlatformType.Int16) || type.Equals(PlatformType.Int32) ||
                    type.Equals(PlatformType.UInt8) || type.Equals(PlatformType.UInt16) || type.Equals(PlatformType.UInt32))
                    return true;

                return false;
            }

            private Signess GetSigness(IType type)
            {
                if (type.Equals(PlatformType.Int8) || type.Equals(PlatformType.Int16) || type.Equals(PlatformType.Int32))
                    return Signess.Signed;
                if (type.Equals(PlatformType.UInt8) || type.Equals(PlatformType.UInt16) || type.Equals(PlatformType.UInt32))
                    return Signess.Unsigned;
                return Signess.NotDefined;
            }

            private int GetSizeInBits(IType type)
            {
                if (type.Equals(PlatformType.Int8) || type.Equals(PlatformType.UInt8))
                {
                    return 8;
                }

                if (type.Equals(PlatformType.Int16) || type.Equals(PlatformType.UInt16))
                {
                    return 16;
                }

                if (type.Equals(PlatformType.Int32) || type.Equals(PlatformType.UInt32))
                {
                    return 32;
                }
                return -1;
            }
        }

        private class FakeIntegerType : IType
        {
            public enum SizeInBits
            {
                I1,
                I8,
                I16,
            }

            public static FakeIntegerType I1 { get; } = new FakeIntegerType(SizeInBits.I1);

            public static FakeIntegerType I8 { get; } = new FakeIntegerType(SizeInBits.I8);

            public static FakeIntegerType I16 { get; } = new FakeIntegerType(SizeInBits.I16);

            public SizeInBits Size { get; private set; }

            private FakeIntegerType(SizeInBits size) {
                this.Size = size;
            }

            public TypeKind TypeKind
            {
                get { return TypeKind.ValueType; }
            }

            public ISet<CustomAttribute> Attributes
            {
                get { return null; }
            }

            public override string ToString()
            {
                return "Fake" + Size;
            }
        }

        private class QueuedSet<E>
        {
            private HashSet<E> hs = new HashSet<E>();
            private LinkedList<E> ll = new LinkedList<E>();

            public QueuedSet() {}

            public QueuedSet(IList<E> os)
            {
                foreach (E o in os)
                {
                    this.ll.AddLast(o);
                    this.hs.Add(o);
                }
            }
            
            public QueuedSet(QueuedSet<E> qs)
            {
                foreach (E o in qs.ll)
                {
                    this.ll.AddLast(o);
                    this.hs.Add(o);
                }
            }
            public bool IsEmpty()
            {
                return this.ll.Count == 0;
            }
            public bool AddLast(E o)
            {
                bool r = this.hs.Contains(o);
                if (!r)
                {
                    this.ll.AddLast(o);
                    this.hs.Add(o);
                }
                return r;
            }
            public int AddLast(ICollection<E> os)
            {
                int r = 0;
                foreach (E o in os)
                {
                    if (this.AddLast(o))
                    {
                        r++;
                    }
                }
                return r;
            }
            public E RemoveFirst()
            {
                E r = ll.First.Value;
                this.ll.RemoveFirst();
                this.hs.Remove(r);
                return r;
            }
        }

        private class CastReturn
        {
            private ClassHierarchy classHierarchy;

            public CastReturn(ClassHierarchy classHierarchy)
            {
                this.classHierarchy = classHierarchy;
            }

            private bool IsCastNeeded(ReturnInstruction returnInstruction, IType expectedType)
            {
                var variable = returnInstruction.Operand;
                var variableType = variable.Type;

                if (variableType.Equals(expectedType))
                    return false;

                // not sure about this
                if (expectedType is PlatformType)
                    return false;

                if (variableType is IBasicType variableTypeBasic)
                    return !classHierarchy.GetAllAncestors(variableTypeBasic).Contains(expectedType);
                
                return false;
            }

            public void Transform(MethodBody methodBody, MethodDefinition methodDefinition)
            {
                var targets = methodBody.Instructions.OfType<ReturnInstruction>().Where(ins => ins.HasOperand && IsCastNeeded(ins, methodDefinition.ReturnType)).ToList();
                var inserter = targets.Count > 0 ? methodBody.GetInstructionInserter() : null;

                int i = 0;
                foreach (var target in targets)
                {
                    var result = new LocalVariable("returnCast" + i);
                    result.Type = methodDefinition.ReturnType;
                    var cast = new ConvertInstruction(0, result, target.Operand, ConvertOperation.Cast, result.Type);
                    inserter.AddBefore(target, cast);
                    target.Replace(target.Operand, result);
                    methodBody.LocalVariables.Add(result);
                    i++;
                }
            }
        }

        private class CastCaller
        {
            private ClassHierarchy classHierarchy;

            public CastCaller(ClassHierarchy classHierarchy)
            {
                this.classHierarchy = classHierarchy;
            }

            private bool IsCastNeeded(IType callerType, IType expectedType, IMethodReference calledMethod)
            {
                // not sure about it.
                if (calledMethod.Name.Equals(".ctor"))
                    return false;

                // not sure about this case
                if (callerType is IGenericParameterReference)
                    return true;

                IBasicType basicCaller = callerType as IBasicType;
                
                // the caller is defined in the loaded assemblies
                if (basicCaller != null && basicCaller.ResolvedType != null)
                    return !classHierarchy.GetAllAncestors(basicCaller).Contains(expectedType);

                // the caller is not in the loaded assemblies 
                if (basicCaller != null && basicCaller.ResolvedType == null)
                    return true;

                return false;
            }

            private LocalVariable Cast(IVariable caller, IType expectedType, IInstruction call, InstructionInserter inserter, ref int i)
            {
                var result = new LocalVariable("callerCast" + i);
                result.Type = expectedType;
                var cast = new ConvertInstruction(0, result, caller, ConvertOperation.Cast, expectedType);
                inserter.AddBefore(call, cast);
                call.Replace(caller, result);
                i++;
                return result;
            }
            public void Transform(MethodBody methodBody)
            {
                var calls = methodBody.Instructions.OfType<MethodCallInstruction>().Where(m => !m.Method.IsStatic).ToList();

                var inserter = methodBody.GetInstructionInserter();
                int i = 0;
                foreach (var call in calls)
                {
                    // call expectedType::method(caller)
                    var caller = call.Arguments.First();
                    var callerType = caller.Type;
                    var expectedType = call.Method.ContainingType;

                    if (IsCastNeeded(callerType, expectedType, call.Method))
                    {
                        methodBody.LocalVariables.Add(Cast(caller, expectedType, call, inserter, ref i));
                    }
                }
            }
        }
    }
}
