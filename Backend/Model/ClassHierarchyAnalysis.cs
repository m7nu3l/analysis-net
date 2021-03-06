﻿// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.Cci;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Backend.Model
{
	public class ClassHierarchyAnalysis
	{
		#region class ClassHierarchyInfo

		private class ClassHierarchyInfo
		{
			public ITypeReference Type { get; private set; }
			public ISet<ITypeDefinition> Subtypes { get; private set; }

			public ClassHierarchyInfo(ITypeReference type)
			{
				this.Type = type;
				this.Subtypes = new HashSet<ITypeDefinition>();
			}
		}

		#endregion

		private IMetadataHost host;
		private IDictionary<ITypeReference, ClassHierarchyInfo> types;
		private bool analyzed;

		public ClassHierarchyAnalysis(IMetadataHost host)
		{
			this.host = host;
			this.types = new Dictionary<ITypeReference, ClassHierarchyInfo>(new TypeDefinitionComparer());
		}

		public IEnumerable<ITypeReference> Types
		{
			get { return types.Keys; }
		}

		public IEnumerable<ITypeDefinition> GetSubtypes(ITypeReference type)
		{
			ClassHierarchyInfo info;
			var result = Enumerable.Empty<ITypeDefinition>();

			if (types.TryGetValue(type, out info))
			{
				result = info.Subtypes;
			}

			return result;
		}

		public IEnumerable<ITypeDefinition> GetAllSubtypes(ITypeReference type)
		{
			var result = new HashSet<ITypeDefinition>();
			var worklist = new HashSet<ITypeDefinition>();

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

		public void Analyze()
		{
			if (analyzed) return;
			analyzed = true;

			var definedTypes = host.LoadedUnits.OfType<IModule>().ToList()
				.SelectMany(a => a.GetAllTypes())
				.OfType<INamedTypeDefinition>()
				.ToList();

			foreach (var type in definedTypes)
			{
                var genericType = (type is IGenericTypeInstance) ? (type as IGenericTypeInstance).GenericType.ResolvedType : type as ITypeDefinition;

                Analyze(genericType);
			}
		}

		private void Analyze(ITypeDefinition type)
		{
			if (type.IsClass)
			{
				AnalyzeClass(type);
			}
			else if (type.IsStruct)
			{
				AnalyzeStruct(type);
			}
			else if (type.IsInterface)
			{
				AnalyzeInterface(type);
			}
			else if (type.IsEnum || type.IsDelegate)
			{
				// Nothing
			}
			else
			{
				throw new Exception("Unknown type kind");
			}
		}

		private void AnalyzeClass(ITypeDefinition type)
		{
			GetOrAddInfo(type);

			if (type.BaseClasses.Any())
			{
				var baseInfo = GetOrAddInfo(type.BaseClasses.Single());
				baseInfo.Subtypes.Add(type);
			}

			foreach (var interfaceref in type.Interfaces)
			{
				var interfaceInfo = GetOrAddInfo(interfaceref);
				interfaceInfo.Subtypes.Add(type);
			}
		}

		private void AnalyzeStruct(ITypeDefinition type)
		{
			GetOrAddInfo(type);

			foreach (var interfaceref in type.Interfaces)
			{
				var interfaceInfo = GetOrAddInfo(interfaceref);
				interfaceInfo.Subtypes.Add(type);
			}
		}

		private void AnalyzeInterface(ITypeDefinition type)
		{
			GetOrAddInfo(type);

			foreach (var interfaceref in type.Interfaces)
			{
				var interfaceInfo = GetOrAddInfo(interfaceref);
				interfaceInfo.Subtypes.Add(type);
			}
		}

		private ClassHierarchyInfo GetOrAddInfo(ITypeReference type1)
		{
			ClassHierarchyInfo result;
            var type = (type1.ResolvedType is IGenericTypeInstance) ? (type1.ResolvedType as IGenericTypeInstance).GenericType.ResolvedType : type1.ResolvedType as ITypeDefinition;

            if (!types.TryGetValue(type, out result))
			{
				result = new ClassHierarchyInfo(type);
				types.Add(type, result);
			}

			return result;
		}
	}
}
