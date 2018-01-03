// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Cci;
using Backend;
using System.IO;

namespace TinyBCT
{
	class Program
	{
        public static StreamWriter streamWriter;
        static void Main(string[] args)
		{
			const string root = @"..\..\..";
			//const string root = @"C:"; // casa
			//const string root = @"C:\Users\Edgar\Projects"; // facu

			const string input = root + @"\Test\bin\Debug\Test.dll";

			using (var host = new PeReader.DefaultHost())
			using (var assembly = new Assembly(host))
			{
				assembly.Load(input);

				Types.Initialize(host);

                streamWriter = new StreamWriter(@"C:\result.bpl");
                // prelude
                streamWriter.WriteLine("type Ref;");

                var visitor = new MethodVisitor(host, assembly.PdbReader);
				visitor.Traverse(assembly.Module);
                streamWriter.Close();
			}

			System.Console.WriteLine("Done!");
			System.Console.ReadKey();
		}
	}
}
