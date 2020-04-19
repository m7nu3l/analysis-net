﻿// Copyright (c) Edgardo Zoppi.  All Rights Reserved.  Licensed under the MIT License.  See License.txt in the project root for license information.

using TacAnalyses.Model;
using Model.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TacAnalyses.Utils
{
	public class MethodAnalysisInfo : AnalysisInfo
	{
		public IMethodReference Method { get; private set; }

		public MethodAnalysisInfo(IMethodReference method)
		{
			this.Method = method;
		}
	}
}
