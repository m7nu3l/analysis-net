// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test
{
	class Node
	{
		public object Value;
		public Node Next;

		public Node(object value)
		{
			this.Value = value;
		}
	}

	class ExamplesPointsTo
	{
        class Second
        {
            string field2;
            public class Third
            {
                string field3;
            }
        }
		public void Example1()
		{
			var node = new Node(1);
			node.Next = new Node(2);

            var c2 = new Second();
            var c3 = new Second.Third();

			var first = node;
			var second = first.Next;
			var third = second.Next;
			third.Next = null;
		}

		public void Example2(bool a, bool b)
		{
			var node = new Node(1);

			if (a) node.Next = new Node(2);
			else if (b) node.Next = new Node(3);
			else node.Next = node;
		}

		public void Example3()
		{
			var current = new Node(1);

			for (var i = 0; i < 5; ++i)
			{
				current.Next = new Node(i + 1);
				current = current.Next;
			}
		}
	}
}
