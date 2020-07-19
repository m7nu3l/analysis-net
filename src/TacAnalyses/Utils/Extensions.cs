﻿// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Model.ThreeAddressCode.Values;
using Model.ThreeAddressCode.Expressions;
using Model.ThreeAddressCode.Instructions;
using TacAnalyses.Analyses;
using Model.Types;
using Model;
using TacAnalyses.Model;

namespace TacAnalyses.Utils
{
	public static class Extensions
	{
		public static bool DictionaryEquals<K,V>(this IDictionary<K,V> self, IDictionary<K,V> other, Func<V, V, bool> valueEquals = null)
		{
			if (object.ReferenceEquals(self, other)) return true;
			if (self.Count != other.Count) return false;

			if (valueEquals == null)
			{
				valueEquals = (a, b) => object.Equals(a, b);
			}

			foreach (var key in self.Keys)
			{
				var otherContainsKey = other.ContainsKey(key);
				if (!otherContainsKey) return false;
			}

			foreach (var entry in self)
			{
				var value = other[entry.Key];
				var valuesAreEquals = valueEquals(entry.Value, value);

				if (!valuesAreEquals) return false;
			}

			return true;
		}

		public static IDictionary<K, V> Union<K, V>(this IDictionary<K, V> self, IEnumerable<KeyValuePair<K, V>> other, Func<V, V, V> valueUnion)
		{
			var result = new Dictionary<K, V>(self);
			result.UnionWith(other, valueUnion);
			return result;
		}

		public static void UnionWith<K, V>(this IDictionary<K, V> self, IEnumerable<KeyValuePair<K, V>> other, Func<V, V, V> valueUnion)
		{
			foreach (var entry in other)
			{
				V value;

				if (self.TryGetValue(entry.Key, out value))
				{
					value = valueUnion(value, entry.Value);

					if (value != null)
					{
						self[entry.Key] = value;
					}
					else
					{
						self.Remove(entry.Key);
					}
				}
				else
				{
					self.Add(entry.Key, entry.Value);
				}
			}
		}

		public static IDictionary<K, V> Intersect<K, V>(this IDictionary<K, V> self, IEnumerable<KeyValuePair<K, V>> other, Func<V, V, V> valueIntersect)
		{
			var result = new Dictionary<K, V>();

			foreach (var entry in other)
			{
				V value;

				if (self.TryGetValue(entry.Key, out value))
				{
					value = valueIntersect(value, entry.Value);

					if (value != null)
					{
						result.Add(entry.Key, value);
					}
				}
			}

			return result;
		}

		public static void IntersectWith<K, V>(this IDictionary<K, V> self, IEnumerable<KeyValuePair<K, V>> other, Func<V, V, V> valueIntersect)
		{
			var keys = new HashSet<K>();

			foreach (var entry in other)
			{
				V value;

				if (self.TryGetValue(entry.Key, out value))
				{
					value = valueIntersect(value, entry.Value);

					if (value != null)
					{
						self[entry.Key] = value;
						keys.Add(entry.Key);
					}
					else
					{
						self.Remove(entry.Key);
					}
				}
			}

			var keysToRemove = self.Keys.Except(keys).ToArray();

			foreach (var key in keysToRemove)
			{
				self.Remove(key);
			}
		}

		//public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> elements)
		//{
		//	foreach (var element in elements)
		//	{
		//		collection.Add(element);
		//	}
		//}

		private static IEnumerable<KeyValuePair<TKey, TResultCollection>> Select<TKey, TValue, TCollection, TResultValue, TResultCollection>
			(this IEnumerable<KeyValuePair<TKey, TCollection>> self, Func<TKey, TValue, TResultValue> action)
			where TCollection : ICollection<TValue>, new()
			where TResultCollection : ICollection<TResultValue>, new()
		{
			var result = self.Select(e =>
			{
				var value = new TResultCollection();
				var values = e.Value.Select(v => action(e.Key, v));

				value.AddRange(values);
				return new KeyValuePair<TKey, TResultCollection>(e.Key, value);
			});

			return result;
		}

		public static IEnumerable<KeyValuePair<TKey, HashSet<TResultValue>>> Select<TKey, TValue, TResultValue>
			(this IEnumerable<KeyValuePair<TKey, HashSet<TValue>>> self, Func<TKey, TValue, TResultValue> action)
		{
			var result = self.Select<TKey, TValue, HashSet<TValue>, TResultValue, HashSet<TResultValue>>(action);
			return result;
		}

		public static IEnumerable<KeyValuePair<TKey, List<TResultValue>>> Select<TKey, TValue, TResultValue>
			(this IEnumerable<KeyValuePair<TKey, List<TValue>>> self, Func<TKey, TValue, TResultValue> action)
		{
			var result = self.Select<TKey, TValue, List<TValue>, TResultValue, List<TResultValue>>(action);
			return result;
		}

		private static TResult Select<TKey, TValue, TCollection, TResult, TResultValue, TResultCollection>
			(this Map<TKey, TValue, TCollection> self, Func<TKey, TValue, TResultValue> action)
			where TCollection : ICollection<TValue>, new()
			where TResultCollection : ICollection<TResultValue>, new()
			where TResult : Map<TKey, TResultValue, TResultCollection>, new()
		{
			var result = new TResult();

			foreach (var entry in self)
			{
				var values = entry.Value.Select(v => action(entry.Key, v));
				result.AddRange(entry.Key, values);
			}

			return result;
		}

		public static MapSet<TKey, TResultValue> Select<TKey, TValue, TResultValue>
			(this MapSet<TKey, TValue> self, Func<TKey, TValue, TResultValue> action)
		{
			var result = self.Select<TKey, TValue, HashSet<TValue>, MapSet<TKey, TResultValue>, TResultValue, HashSet<TResultValue>>(action);
			return result;
		}

		public static MapList<TKey, TResultValue> Select<TKey, TValue, TResultValue>
			(this MapList<TKey, TValue> self, Func<TKey, TValue, TResultValue> action)
		{
			var result = self.Select<TKey, TValue, List<TValue>, MapList<TKey, TResultValue>, TResultValue, List<TResultValue>>(action);
			return result;
		}

		public static MapSet<K, V> ToMapSet<K, V>(this IEnumerable<KeyValuePair<K, IEnumerable<V>>> elements)
		{
			var result = new MapSet<K, V>();

			foreach (var element in elements)
			{
				result.AddRange(element.Key, element.Value);
			}

			return result;
		}

		public static MapList<K, V> ToMapList<K, V>(this IEnumerable<KeyValuePair<K, IEnumerable<V>>> elements)
		{
			var result = new MapList<K, V>();

			foreach (var element in elements)
			{
				result.AddRange(element.Key, element.Value);
			}

			return result;
		}

		public static MapSet<K, V> ToMapSet<K, V>(this IEnumerable<V> elements, Func<V, K> keySelector)
		{
			var result = new MapSet<K, V>();

			foreach (var element in elements)
			{
				var key = keySelector(element);
				result.Add(key, element);
			}

			return result;
		}

		public static MapList<K, V> ToMapList<K, V>(this IEnumerable<V> elements, Func<V, K> keySelector)
		{
			var result = new MapList<K, V>();

			foreach (var element in elements)
			{
				var key = keySelector(element);
				result.Add(key, element);
			}

			return result;
		}

		public static Subset<T> ToSubset<T>(this T[] universe)
		{
			return new Subset<T>(universe, false);
		}

		public static Subset<T> ToEmptySubset<T>(this T[] universe)
		{
			return new Subset<T>(universe, true);
		}

		public static uint StartOffset(this IInstructionContainer block)
		{
			var instruction = block.Instructions.First();
			return instruction.Offset;
		}

		public static uint EndOffset(this IInstructionContainer block)
		{
			var instruction = block.Instructions.Last();
			return instruction.Offset;
		}

		public static ISet<IVariable> GetVariables(this IInstructionContainer block)
		{
			var result = from i in block.Instructions
						 from v in i.Variables
						 select v;

			//var result = block.Instructions.SelectMany(i => i.Variables);
			return new HashSet<IVariable>(result);
		}

		public static ISet<IVariable> GetModifiedVariables(this IInstructionContainer block)
		{
			var result = from i in block.Instructions
						 from v in i.ModifiedVariables
						 select v;

			//var result = block.Instructions.SelectMany(i => i.ModifiedVariables);
			return new HashSet<IVariable>(result);
		}

		public static ISet<IVariable> GetUsedVariables(this IInstructionContainer block)
		{
			var result = from i in block.Instructions
						 from v in i.UsedVariables
						 select v;

			//var result = block.Instructions.SelectMany(i => i.UsedVariables);
			return new HashSet<IVariable>(result);
		}

		public static ISet<IVariable> GetDefinedVariables(this IInstructionContainer block)
		{
			var result = from i in block.Instructions
						 let d = i as DefinitionInstruction
						 where d != null && d.HasResult
						 select d.Result;

			return new HashSet<IVariable>(result);
			//var result = new HashSet<IVariable>();

			//foreach (var instruction in block.Instructions)
			//{
			//    var definition = instruction as DefinitionInstruction;

			//    if (definition != null && definition.HasResult)
			//    {
			//        result.Add(definition.Result);
			//    }
			//}

			//return result;
		}

		public static ISet<IVariable> GetVariables(this CFGLoop loop)
		{
			var result = from n in loop.Body
						 from v in n.GetVariables()
						 select v;

			//var result = loop.Body.SelectMany(n => n.GetVariables());
			return new HashSet<IVariable>(result);
		}

		public static ISet<IVariable> GetModifiedVariables(this CFGLoop loop)
		{
			var result = from n in loop.Body
						 from v in n.GetModifiedVariables()
						 select v;

			//var result = loop.Body.SelectMany(n => n.GetModifiedVariables());
			return new HashSet<IVariable>(result);
		}

		public static ISet<IVariable> GetUsedVariables(this CFGLoop loop)
		{
			var result = from n in loop.Body
						 from v in n.GetUsedVariables()
						 select v;

			//var result = loop.Body.SelectMany(n => n.GetUsedVariables());
			return new HashSet<IVariable>(result);
		}

		public static ISet<IVariable> GetDefinedVariables(this CFGLoop loop)
		{
			var result = from n in loop.Body
						 from v in n.GetDefinedVariables()
						 select v;

			//var result = loop.Body.SelectMany(n => n.GetDefinedVariables());
			return new HashSet<IVariable>(result);
		}

		public static ISet<IVariable> GetVariables(this ControlFlowGraph cfg)
		{
			var result = from n in cfg.Nodes
						 from v in n.GetVariables()
						 select v;

			//var result = cfg.Nodes.SelectMany(n => n.GetVariables());
			return new HashSet<IVariable>(result);
		}

		public static ISet<CFGNode> GetExitNodes(this CFGLoop loop)
		{
			var result = from n in loop.Body
						 from m in n.Successors
						 where !loop.Body.Contains(m)
						 select n;

			//var result = loop.Body.Where(n => n.Successors.Any(m => !loop.Body.Contains(m)));
			return new HashSet<CFGNode>(result);
		}

		public static ISet<CFGNode> GetSuccessors(this CFGLoop loop)
		{
			var result = from n in loop.Body
						 from m in n.Successors
						 where !loop.Body.Contains(m)
						 select m;

			return new HashSet<CFGNode>(result);
		}

		public static IExpression ToExpression(this IValue value)
		{
			return value as IExpression;
		}

		public static IDictionary<IVariable, DefinitionInstruction> GetDefinitions(this ControlFlowGraph cfg)
		{
			// The CFG is expected to be in SSA form.
			var definitions = from node in cfg.Nodes
							  from ins in node.Instructions
							  let def = ins as DefinitionInstruction
							  where def != null && def.HasResult
							  select def;

			var result = definitions.ToDictionary(def => def.Result);
			return result;
		}

		public static IDictionary<IInstruction, CFGNode> GetContainingNodes(this ControlFlowGraph cfg)
		{
			var result = new Dictionary<IInstruction, CFGNode>();

			foreach (var node in cfg.Nodes)
			{
				foreach (var instruction in node.Instructions)
				{
					result.Add(instruction, node);
				}
			}

			return result;
		}

		//public static IExpression GetValueOriginal(this IDictionary<IVariable, IExpression> equalities, IVariable variable)
		//{
		//    var result = equalities.ContainsKey(variable) ? equalities[variable] : variable;
		//    return result;
		//}

		public static IExpression GetValue(this IDictionary<IVariable, IExpression> equalities, IVariable variable)
		{
			IExpression result = variable;

			while (variable != null && equalities.ContainsKey(variable))
			{
				result = equalities[variable];
				variable = result as IVariable;
			}

			return result;
		}

		public static IVariable GetOriginal(this IVariable variable)
		{
			while (variable is DerivedVariable)
			{
				var derived = variable as DerivedVariable;
				variable = derived.Original;
			}

			return variable;
		}

		public static bool IsTemporal(this IVariable variable)
		{
			var result = variable.GetOriginal() is TemporalVariable;
			return result;
		}

		public static bool ContainsAnyOriginal(this ISet<IVariable> set, IVariable variable)
		{
			var ok = set.Contains(variable);

			while (!ok && variable is DerivedVariable)
			{
				var derived = variable as DerivedVariable;
				variable = derived.Original;
				ok = set.Contains(variable);
			}

			return ok;
		}

		public static bool IsCopy(this IInstruction instruction, out IVariable left, out IVariable right)
		{
			var result = false;
			left = null;
			right = null;

			if (instruction is LoadInstruction)
			{
				var load = instruction as LoadInstruction;

				if (load.Operand is IVariable)
				{
					left = load.Result;
					right = load.Operand as IVariable;
					result = true;
				}
			}

			return result;
		}

		public static IExpression ReplaceVariables<T>(this IExpression expr, IDictionary<IVariable, T> equalities, bool replaceLocalVariables = false) where T : IExpression
		{
			foreach (var variable in expr.Variables)
			{
				if (replaceLocalVariables || variable.IsTemporal())
				{
					var hasValue = equalities.ContainsKey(variable);

					if (hasValue)
					{
						var value = equalities[variable];
						var isUnknown = value is UnknownValue;
						var isPhi = value is PhiExpression;
						var isMethodCall = value is MethodCallExpression;

						if (isUnknown || isPhi || isMethodCall)
							continue;

						expr = expr.Replace(variable, value);
					}
				}
			}

			return expr;
		}

		public static PTGNodeField ToPTGNodeField(this IFieldReference field)
		{
			return new PTGNodeField(field.Name, field.Type);
		}

		public static PTGNodeField ToPTGNodeField(this IFieldAccess access)
		{
			return new PTGNodeField(access.FieldName, access.Type);
		}

		public static PTGNodeField ToPTGNodeField(this ArrayElementAccess access)
		{
			return new PTGNodeField("[]", access.Type);
		}

		public static IVariable ToPTGGlobalVariable(this StaticFieldAccess access)
		{
			var name = access.Field.ContainingType.GetFullName().Replace(".", "_");
			name = string.Format("#{0}", name);
			return new LocalVariable(name) { Type = access.Field.ContainingType };
		}

		public static void RemoveTemporalVariables(this PointsToGraph ptg)
		{
			var variablesToRemove = ptg.Variables
									   .Where(v => v.IsTemporal())
									   .ToArray();

			foreach (var variable in variablesToRemove)
			{
				ptg.Remove(variable);
			}
		}

		public static void RemoveVariablesExceptParameters(this PointsToGraph ptg)
		{
			var variablesToRemove = ptg.Variables
						   .Where(v => !v.IsParameter)
						   .ToArray();

			foreach (var variable in variablesToRemove)
			{
				ptg.Remove(variable);
			}
		}

		public static ISet<PTGNode> GetTargets(this PointsToGraph ptg, IValue value)
		{
			ISet<PTGNode> result = null;

			if (value is IVariable)
			{
				var variable = value as IVariable;
				result = ptg.GetTargets(variable);
			}
			else if (value is IFieldAccess)
			{
				var access = value as IFieldAccess;
				result = ptg.GetTargets(access);
			}
			else if (value is ArrayElementAccess)
			{
				var access = value as ArrayElementAccess;
				result = ptg.GetTargets(access);
			}

			return result;
		}

		public static ISet<PTGNode> GetTargets(this PointsToGraph ptg, IFieldAccess access)
		{
			ISet<PTGNode> result = null;

			if (access is InstanceFieldAccess)
			{
				var instanceAccess = access as InstanceFieldAccess;
				result = ptg.GetTargets(instanceAccess);
			}
			else if (access is StaticFieldAccess)
			{
				var staticAccess = access as StaticFieldAccess;
				result = ptg.GetTargets(staticAccess);
			}

			return result;
		}

		public static ISet<PTGNode> GetTargets(this PointsToGraph ptg, InstanceFieldAccess access)
		{
			var field = access.ToPTGNodeField();
			var result = ptg.GetTargets(access.Instance, field);
			return result;
		}

		public static ISet<PTGNode> GetTargets(this PointsToGraph ptg, StaticFieldAccess access)
		{
			var variable = access.ToPTGGlobalVariable();
			var field = access.ToPTGNodeField();
			var result = ptg.GetTargets(variable, field);
			return result;
		}

		public static ISet<PTGNode> GetTargets(this PointsToGraph ptg, ArrayElementAccess access)
		{
			var field = access.ToPTGNodeField();
			var result = ptg.GetTargets(access.Array, field);
			return result;
		}

		#region Points-to graph may alias

		public static bool MayAlias(this PointsToGraph ptg, IVariable variable1, IVariable variable2)
		{
			var targets1 = ptg.GetTargets(variable1);
			var targets2 = ptg.GetTargets(variable2);
			var alias = targets1.Intersect(targets2);
			return alias.Any();
		}

		public static bool MayAlias(this PointsToGraph ptg, InstanceFieldAccess access, IVariable variable)
		{
			var targetsAccess = ptg.GetTargets(access);
			var targetsVariable = ptg.GetTargets(variable);
			var alias = targetsAccess.Intersect(targetsVariable);
			return alias.Any();
		}

		public static bool MayAlias(this PointsToGraph ptg, InstanceFieldAccess access1, InstanceFieldAccess access2)
		{
			var targets1 = ptg.GetTargets(access1);
			var targets2 = ptg.GetTargets(access2);
			var alias = targets1.Intersect(targets2);
			return alias.Any();
		}

		public static ISet<IVariable> GetAliases(this PointsToGraph ptg, IVariable variable)
		{
			var result = new HashSet<IVariable>() { variable };

			foreach (var node in ptg.GetTargets(variable))
			{
				if (!node.Equals(ptg.Null))
				{
					var nodeVariables = ptg.GetVariables(node);
					result.UnionWith(nodeVariables);
				}
			}

			return result;
		}

		#endregion

		#region Points-to graph reachability

		public static bool IsReachable(this PointsToGraph ptg, IVariable variable, PTGNode target)
		{
			var result = false;
			var visitedNodes = new HashSet<PTGNode>();
			var worklist = new Queue<PTGNode>();
			var nodes = ptg.GetTargets(variable);

			foreach (var node in nodes)
			{
				worklist.Enqueue(node);
				visitedNodes.Add(node);
			}

			while (worklist.Any())
			{
				var node = worklist.Dequeue();

				if (node.Equals(ptg.Null))
				{
					continue;
				}

				if (node.Equals(target))
				{
					result = true;
					break;
				}

				var nodeTargets = ptg.GetTargets(node);

				foreach (var targets in nodeTargets.Values)
				{
					foreach (var nodeTarget in targets)
					{
						if (!visitedNodes.Contains(nodeTarget))
						{
							worklist.Enqueue(nodeTarget);
							visitedNodes.Add(nodeTarget);
						}
					}
				}
			}

			return result;
		}

		public static ISet<PTGNode> GetReachableNodes(this PointsToGraph ptg, bool alsoFromGlobals = true)
		{
			var roots = ptg.Variables;

			if (!alsoFromGlobals)
			{
				// TODO: Replace v.Name.StartsWith("#") by a better way to recognize global variables!
				roots = roots.Where(v => !v.Name.StartsWith("#"));
			}

			var result = ptg.GetReachableNodes(roots);
			return result;
		}

		public static ISet<PTGNode> GetReachableNodes(this PointsToGraph ptg, IVariable variable)
		{
			var roots = variable.ToEnumerable();
			var result = ptg.GetReachableNodes(roots);
			return result;
		}

		public static ISet<PTGNode> GetReachableNodes(this PointsToGraph ptg, IEnumerable<IVariable> roots)
		{
			var visitedNodes = new HashSet<PTGNode>();
			var worklist = new Queue<PTGNode>();

			foreach (var root in roots)
			{
				var nodes = ptg.GetTargets(root);

				foreach (var node in nodes)
				{
					if (!visitedNodes.Contains(node))
					{
						worklist.Enqueue(node);
						visitedNodes.Add(node);
					}
				}
			}

			while (worklist.Any())
			{
				var node = worklist.Dequeue();

				//yield return node;

				if (node.Equals(ptg.Null))
				{
					continue;
				}

				var nodeTargets = ptg.GetTargets(node);

				foreach (var targets in nodeTargets.Values)
				{
					foreach (var nodeTarget in targets)
					{
						if (!visitedNodes.Contains(nodeTarget))
						{
							worklist.Enqueue(nodeTarget);
							visitedNodes.Add(nodeTarget);
						}
					}
				}
			}

			return visitedNodes;
		}

		public static void CollectGarbage(this PointsToGraph ptg)
		{
			var reachableNodes = ptg.GetReachableNodes();
			// Don't collect the special null node
			reachableNodes.Add(ptg.Null);
			var unreachableNodes = ptg.Nodes.Except(reachableNodes).ToArray();

			foreach (var node in unreachableNodes)
			{
				ptg.Remove(node);
			}
		}

		#endregion

		public static void Inline(this MethodBody callerBody, MethodCallInstruction methodCall, MethodBody calleeBody)
		{
			// TODO: Fix local variables (and parameters) name clashing.
			// TODO: If callee is a generic method we need to specialize
			// the generic parameters with the corresponding arguments.

			var index = callerBody.Instructions.IndexOf(methodCall);
			callerBody.Instructions.RemoveAt(index);

			IInstruction nextInstruction = null;

			if (callerBody.Instructions.Count > index)
			{
				// The caller method has more instructions after the method call
				nextInstruction = callerBody.Instructions[index];
			}			

			for (var i = 0; i < calleeBody.Parameters.Count; ++i)
			{
				var parameter = calleeBody.Parameters[i];
				var argument = methodCall.Arguments[i];
				var copy = new LoadInstruction(methodCall.Offset, parameter, argument);

				copy.Label = string.Format("{0}_{1}", methodCall.Label, copy.Label);
				callerBody.Instructions.Insert(index, copy);
				index++;
			}

			var lastCalleeInstructionIndex = calleeBody.Instructions.Count - 1;

			for (var i = 0; i < calleeBody.Instructions.Count; ++i)
			{
				var instruction = calleeBody.Instructions[i];

				if (instruction is ReturnInstruction)
				{
					var ret = instruction as ReturnInstruction;

					if (ret.HasOperand && methodCall.HasResult)
					{
						// Copy the return value of the callee to the result variable of the method call
						var copy = new LoadInstruction(ret.Offset, methodCall.Result, ret.Operand);

						copy.Label = string.Format("{0}_{1}", methodCall.Label, copy.Label);
						callerBody.Instructions.Insert(index, copy);
						index++;
					}

					if (nextInstruction != null && i < lastCalleeInstructionIndex)
					{
						// Jump to the instruction after the method call
						var branch = new UnconditionalBranchInstruction(ret.Offset, nextInstruction.Offset);

						branch.Label = string.Format("{0}_{1}", methodCall.Label, branch.Label);
						callerBody.Instructions.Insert(index, branch);
						index++;
					}
				}
				else
				{
					// TODO: Fix! We should clone the instruction
					// so the original is not modified
					// and calleeBody remains unchanged

					if (instruction is BranchInstruction)
					{
						var branch = instruction as BranchInstruction;
						branch.Target = string.Format("{0}_{1}", methodCall.Label, branch.Target);
					}
					else if (instruction is SwitchInstruction)
					{
						var branch = instruction as SwitchInstruction;

						for (var j = 0; j < branch.Targets.Count; ++j)
						{
							var target = branch.Targets[j];
							branch.Targets[j] = string.Format("{0}_{1}", methodCall.Label, target);
						}
					}

					instruction.Label = string.Format("{0}_{1}", methodCall.Label, instruction.Label);
					callerBody.Instructions.Insert(index, instruction);
					index++;
				}
			}
		}

		public static InputOutputInfo GetInputOutputInfo(this IInstructionContainer region, ISet<IVariable> liveVariablesAtExit)
		{
			var variables = region.GetVariables();
			var definedVariables = region.GetDefinedVariables();
			var inputs = variables.Except(definedVariables);

			var outputs = from output in definedVariables
						  where liveVariablesAtExit.Contains(output)
						  select output;

			var results = from ins in region.Instructions
						  let ret = ins as ReturnInstruction
						  where ret != null && ret.HasOperand
						  select ret.Operand;

			var result = new InputOutputInfo(inputs, outputs, results);
			return result;
		}

		public static InputOutputInfo GetInputOutputInfo(this CFGLoop loop, DataFlowAnalysisResult<Subset<IVariable>>[] livenessInfo)
		{
			var variables = loop.GetVariables();
			var definedVariables = loop.GetDefinedVariables();
			var inputs = variables.Except(definedVariables);

			//var outputs = from node in loop.GetExitNodes()
			//			  let livenessNodeInfo = livenessInfo[node.Id]
			//			  let liveVariablesAtExit = livenessNodeInfo.Output.ToSet()
			//			  from output in definedVariables
			//			  where liveVariablesAtExit.Contains(output)
			//			  select output;

			var outputs = from node in loop.GetSuccessors()
						  let livenessNodeInfo = livenessInfo[node.Id]
						  let liveVariablesAtExit = livenessNodeInfo.Input.ToSet()
						  from output in definedVariables
						  where liveVariablesAtExit.Contains(output)
						  select output;

			var results = from node in loop.Body
						  from ins in node.Instructions
						  let ret = ins as ReturnInstruction
						  where ret != null && ret.HasOperand
						  select ret.Operand;

			var result = new InputOutputInfo(inputs, outputs, results);
			return result;
		}

		public static InputOutputInfo GetInputOutputInfo(this ControlFlowGraph cfg)
		{
			var inputs = from variable in cfg.GetVariables()
						 where variable.IsParameter
						 select variable;

			// At the exit node of the CFG all variables are dead.
			var outputs = Enumerable.Empty<IVariable>();

			var results = from node in cfg.Nodes
						  from ins in node.Instructions
						  let ret = ins as ReturnInstruction
						  where ret != null && ret.HasOperand
						  select ret.Operand;

			var result = new InputOutputInfo(inputs, outputs, results);
			return result;
		}

		public static EscapeInfo GetEscapeInfo(this IMethodReference method, ProgramAnalysisInfo programInfo, InputOutputInfo ioInfo, IEnumerable<IInstruction> instructions)
		{
			var methodInfo = programInfo[method];
			var pti = methodInfo.Get<InterPointsToInfo>(InterPointsToAnalysis.INFO_IPTA_RESULT);
			var ptg = pti.Output;

			// TODO: Replace v.Name.StartsWith("#") by a better way to recognize global variables!
			var globals = ptg.Variables.Where(v => v.Name.StartsWith("#"));

			var allocatedNodes = from ins in instructions
								 let def = ins as DefinitionInstruction
								 where (def is CreateObjectInstruction || def is CreateArrayInstruction) &&
										def.HasResult && def.Result.Type.TypeKind != TypeKind.ValueType
								 let targets = ptg.GetTargets(def.Result)
								 from node in targets
								 select node;

			var cg = programInfo.Get<CallGraph>(InterPointsToAnalysis.INFO_CG);

			var calleesEscapingNodes = from ins in instructions
									   where ins is MethodCallInstruction
									   let def = ins as DefinitionInstruction
									   let inv = cg.GetInvocation(method, def.Label)
									   from callee in inv.PossibleCallees
									   let calleeInfo = programInfo[callee]
									   let escInfo = calleeInfo.Get<EscapeInfo>(EscapeAnalysis.INFO_ESC)
									   from node in escInfo.EscapingNodes
									   select node;

			var escapingVariables = ioInfo.Inputs.Union(ioInfo.Outputs).Union(ioInfo.Results).Union(globals);
			var newNodes = calleesEscapingNodes.Union(allocatedNodes).ToSet();
			var escapeInfo = new EscapeInfo();

			foreach (var variable in escapingVariables)
			{
				if (variable.Type.TypeKind == TypeKind.ValueType) continue;

				var nodes = from node in ptg.GetReachableNodes(variable)
							where node.Kind != PTGNodeKind.Null && node.Kind != PTGNodeKind.Global &&
								  newNodes.Contains(node)
							select node;

				escapeInfo.EscapeChannels.AddRange(variable, nodes);
			}

			// TODO: Fill EscapeInfo.ExternalNodes and EscapeInfo.CapturedNodes.

			return escapeInfo;
		}

		public static IBasicType SolveGenericParameterReferences(this IBasicType basicType, MethodCallInstruction instruction)
		{
			// This function is similar to IBasicType SolveGenericParameterReferences(this IBasicType target, IBasicType source)
			// Here we assume that if there is any generic parameter reference in basicType can be resolved in the scope of the instruction.
			// ie. basicType is the return type of the called method.

			IList<IType> solvedArguments = new List<IType>();
			foreach (var arg in basicType.GenericArguments)
			{
				IType solvedArg;
				if (arg is IGenericParameterReference genericParam)
				{
					IList<IType> container = genericParam.Kind == GenericParameterKind.Method ? instruction.Method.GenericArguments : instruction.Method.ContainingType.GenericArguments;
					solvedArg = container.ElementAt(genericParam.Index);
				}
				else
				{
					solvedArg = arg;
				}
				solvedArguments.Add(solvedArg);
			}

			var newType = basicType.Instantiate(solvedArguments);
			return newType;
		}

		public static IType SolveGenericParameterReferencesInArrayType(this ArrayType array, MethodCallInstruction instruction)
		{
			if (array.ElementsType is IBasicType basicElementsType)
			{
				// should we consider other implementations of IGenericReference as base case?
				var solved = basicElementsType.SolveGenericParameterReferences(instruction);
				var ret = new ArrayType(solved, array.Rank);
				ret.Attributes.AddRange(array.Attributes);
				return ret;
			}
			else if (array.ElementsType is ArrayType arrayElementsType)
			{
				// I think we should recurse on its ElementsType using this function
				// I prefer to wait to have a failing example before coding it.
				throw new NotSupportedException();
			}
			else
			{
				throw new NotSupportedException();
			}
		}

		public static IBasicType SolveGenericParameterReferences(this IBasicType target, IBasicType source)
		{
			// Is there any generic parameter reference in target that points to any generic argument in source?
			// class ArrayList<T> : IList<T> (this is our definition in C#).
			// In CIL, IList<T> is IList<!0> where !0 points to the first T in ArrayList<T>
			// If we are manipulating ArrayList<int>, we would need to solve IList<!0> as IList<int>.
			
			if (target.GenericArguments.Count == 0)
				return target;

			IList<IType> genericArguments = new List<IType>();
			foreach (var ga in target.GenericArguments)
			{
				if (ga is IGenericParameterReference gp)
				{
					if (gp.GenericContainer.Equals(source.GenericType))
					{
						genericArguments.Add(source.GenericArguments.ElementAt(gp.Index));
					}
				}
				else
				{
					genericArguments.Add(ga);
				}
			}

			return target.GenericType.Instantiate(genericArguments);
		}
	}
}
