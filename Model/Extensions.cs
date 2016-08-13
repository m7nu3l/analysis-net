// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using Model.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
	public static class Extensions
	{
		public static void AddRange<T>(this ICollection<T> self, IEnumerable<T> elements)
		{
			foreach (var element in elements)
			{
				self.Add(element);
			}
		}

		public static void RemoveAll<T>(this ICollection<T> self, IEnumerable<T> elements)
		{
			foreach (var element in elements)
			{
				self.Remove(element);
			}
		}

		public static void SetRange<K, V>(this IDictionary<K, V> self, IEnumerable<KeyValuePair<K, V>> elements)
		{
			foreach (var element in elements)
			{
				self[element.Key] = element.Value;
			}
		}

		public static IEnumerable<T> ToEnumerable<T>(this T item)
		{
			yield return item;
		}

		public static UnknownValueException<T> ToUnknownValueException<T>(this T self)
			where T : struct
		{
			return new UnknownValueException<T>(self);
		}

        public static  string GetContainingTypes(this ITypeDefinition containingType)
        {
            containingType = containingType.ContainingType;
            if (containingType == null)
                return null;

            var containingTypes = new StringBuilder();
            while (containingType!=null)
            {
                containingTypes.Insert(0, containingType.Name);
                containingType = containingType.ContainingType;
            }
            return containingTypes.ToString();
        }

        public static string GetFullNameWithAssembly(this IBasicType type)
		{
			var containingAssembly = string.Empty;
			var containingNamespace = string.Empty;

			if (type.ContainingAssembly != null)
			{
				containingAssembly = string.Format("[{0}]", type.ContainingAssembly.Name);
			}

			if (type.ContainingNamespace != null)
			{
				containingNamespace = string.Format("{0}.", type.ContainingNamespace);
			}

			return string.Format("{0}{1}{2}", containingAssembly, containingNamespace, type.GenericName);
		}
        public static string GetFullName(this IBasicType type)
        {
            var containingNamespace = string.Empty;
            var containingTypes = string.Empty;

            if (type.ContainingNamespace != null)
            {
                containingNamespace = string.Format("{0}.", type.ContainingNamespace);
            }
            if(!String.IsNullOrEmpty(type.ContainingTypes))
            {
                containingTypes = type.ContainingTypes + ".";
            }

            return string.Format("{0}{1}{2}", containingNamespace, containingTypes, type.GenericName);
        }
    }
}
