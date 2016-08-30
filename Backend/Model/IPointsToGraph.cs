using System;
using System.Collections.Generic;
using Backend.Utils;
using Model.ThreeAddressCode.Values;
using Model.Types;

//namespace Backend.Model
//{
//    public interface IPointsToGraph
//    {
//        IEnumerable<PTGNode> Nodes { get; }
//        IEnumerable<IVariable> Roots { get; }

//        void Add(PTGNode node);
//        void Add(IVariable variable);
//        void AddEdge(PTGNode source, IFieldReference field, PTGNode target);
//        void CleanUnreachableNodes();
//        SimplePointsToGraph Clone();
//        bool Contains(PTGNode node);
//        bool Contains(IVariable variable);
//        ISet<IVariable> GetAliases(IVariable v);
//        ISet<PTGNode> GetTargets(PTGNode source, IFieldReference field);
//        ISet<PTGNode> GetTargets(IVariable variable, IFieldReference field);
//        ISet<PTGNode> GetTargets(IVariable variable, bool failIfNotExists = false);
//        bool GraphEquals(object obj);
//        bool GraphLessEquals(SimplePointsToGraph other);
//        SimplePointsToGraph Join(SimplePointsToGraph ptg);
//        bool MayReacheableFromVariable(IVariable v1, IVariable v2);
//        MapSet<IVariable, PTGNode> NewFrame();
//        MapSet<IVariable, PTGNode> NewFrame(IEnumerable<KeyValuePair<IVariable, IVariable>> binding);
//        void PointsTo(IVariable variable, PTGNode target);
//        void PointsTo(IVariable variable, IEnumerable<PTGNode> targets);
//        void PointsTo(PTGNode source, IFieldReference field, PTGNode target);
//        bool Reachable(IVariable v1, PTGNode n);
//        IEnumerable<PTGNode> ReachableNodes(IEnumerable<PTGNode> roots, Predicate<Tuple<PTGNode, IFieldReference>> filter = null);
//        IEnumerable<PTGNode> ReachableNodesFromVariables();
//        void RemoveRootEdges(IVariable variable);
//        void RemoveTargets(PTGNode source, IFieldReference field);
//        void RestoreFrame(bool cleanUnreachable = true);
//        void RestoreFrame(IVariable retVariable, IVariable dest, bool cleanUnreachable = true);
//        void Union(SimplePointsToGraph ptg);
//    }
//}