// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Model;
using Model.Types;
using System.Collections.Generic;
using System.Linq;
using TacAnalyses.Utils;

namespace TacAnalyses.Model
{
	public class ClassHierarchy
	{
		#region class ClassHierarchyInfo

		private class ClassHierarchyInfo
		{
			public IBasicType Type { get; private set; }
			public ISet<TypeDefinition> Subtypes { get; private set; }
			// Direct Ancesotrs (parent class and implemented interfaces)
			public ISet<IBasicType> Ancestors { get; private set; }

			public ClassHierarchyInfo(IBasicType type)
			{
				this.Type = type;
				this.Subtypes = new HashSet<TypeDefinition>();
				this.Ancestors = new HashSet<IBasicType>();
			}
		}

		#endregion

		private IDictionary<IBasicType, ClassHierarchyInfo> types;
		private bool analyzed;

		public ClassHierarchy()
		{
			this.types = new Dictionary<IBasicType, ClassHierarchyInfo>(BasicTypeDefinitionComparer.Default);
		}

		public IEnumerable<IBasicType> Types
		{
			get { return types.Keys; }
		}

		public ISet<IBasicType> LeastCommonAncestors(IBasicType basicTypeA, IBasicType basicTypeB)
        {
			// Source wikipedia
			// Bender et al. (2005) gave an equivalent definition, where the lowest common ancestors of x and y are the nodes of out-degree zero in the subgraph of G induced by the set of common ancestors of x and y.
			var aAncestors = GetAllAncestors(basicTypeA).ToSet();
			aAncestors.Add(basicTypeA);

			var bAncestors = GetAllAncestors(basicTypeB).ToSet();
			bAncestors.Add(basicTypeB);

			// aAncestors holds the nodes of the induced subgraph
			aAncestors.IntersectWith(bAncestors);

			var result = new HashSet<IBasicType>();
			foreach (var node in aAncestors)
            {
				var nodeAncestors = GetAncestors(node);
				nodeAncestors.IntersectWith(aAncestors);
				if (nodeAncestors.Count == 0)
					result.Add(node);
            }

			return result;
		}

		public ISet<IBasicType> GetAllAncestors(IBasicType type)
		{
			var result = new HashSet<IBasicType>();
			var worklist = new HashSet<IBasicType>();

			var ancestors = GetAncestors(type);

			worklist.UnionWith(ancestors);

			while (worklist.Count > 0)
			{
				var ancestor = worklist.First();
				worklist.Remove(ancestor);

				var isNewAncestor = result.Add(ancestor);

				if (isNewAncestor)
				{
					ancestors = GetAncestors(ancestor);
					worklist.UnionWith(ancestors);
				}
			}

			return result;
		}
		
		public ISet<IBasicType> GetAncestors(IBasicType type)
		{
			ClassHierarchyInfo info;
			ISet<IBasicType> result = new HashSet<IBasicType>();

			if (types.TryGetValue(type, out info))
			{
				result = info.Ancestors;
			}

			if (type.GenericArguments.Count > 0)
				result = result.Select(a => a.SolveGenericParameterReferences(type)).ToSet();

			return result;
		}

		public IEnumerable<TypeDefinition> GetSubtypes(IBasicType type)
		{
			ClassHierarchyInfo info;
			var result = Enumerable.Empty<TypeDefinition>();

			if (types.TryGetValue(type, out info))
			{
				result = info.Subtypes;
			}

			return result;
		}

		public IEnumerable<TypeDefinition> GetAllSubtypes(IBasicType type)
		{
			var result = new HashSet<TypeDefinition>();
			var worklist = new HashSet<TypeDefinition>();

			var subtypes = GetSubtypes(type);
			worklist.UnionWith(subtypes);

			while (worklist.Count > 0)
			{
				var subtype = worklist.First();
				worklist.Remove(subtype);

				var isNewSubtype = result.Add(subtype);

				if (isNewSubtype)
				{
					subtypes = GetSubtypes(subtype);
					worklist.UnionWith(subtypes);
				}
			}

			return result;
		}

		public void Analyze(Host host)
		{
			if (analyzed) return;
			analyzed = true;

			var definedTypes = from a in host.Assemblies
							   from t in a.RootNamespace.GetAllTypes()
							   select t;

			foreach (var type in definedTypes)
			{
				Analyze(type);
			}
		}

		public void Analyze(Assembly assembly)
		{
			if (analyzed) return;
			analyzed = true;

			var definedTypes = assembly.RootNamespace.GetAllTypes();

			foreach (var type in definedTypes)
			{
				Analyze(type);
			}
		}

		private void Analyze(TypeDefinition type)
		{
			var typeInfo = GetOrAddInfo(type);

			if (type.Base != null)
			{
				var baseInfo = GetOrAddInfo(type.Base);
				baseInfo.Subtypes.Add(type);
				typeInfo.Ancestors.Add(type.Base);
			}

			foreach (var interfaceref in type.Interfaces)
			{
				var interfaceInfo = GetOrAddInfo(interfaceref);
				interfaceInfo.Subtypes.Add(type);
				typeInfo.Ancestors.Add(interfaceref);
			}
		}

		private ClassHierarchyInfo GetOrAddInfo(IBasicType type)
		{
			ClassHierarchyInfo result;

			if (!types.TryGetValue(type, out result))
			{
				result = new ClassHierarchyInfo(type);
				types.Add(type, result);
			}

			return result;
		}
	}
}
