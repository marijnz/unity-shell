using System;
using Mono.CSharp;

namespace UnityShell
{
	[Serializable]
	public class ShellEvaluator
	{
		Evaluator evaluator;

		public ShellEvaluator()
		{
			foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				try
				{
					Evaluator.ReferenceAssembly(assembly);
				}
				catch (Exception)
				{
					// ignored
				}
			}
			Evaluator.Run ("using UnityEngine; using UnityEditor; using System; using System.Collections.Generic;");
		}

		public object Evaluate(string command)
		{
			if(!command.EndsWith(";"))
			{
				command += ";";
			}

			object outResult;
			bool isResultSet;

			object result = Evaluator.Evaluate(command, out outResult, out isResultSet);

			if(isResultSet)
			{
				result = outResult;
			}
			else
			{
				result = "invalid input " + result;
			}
			return result;
		}
	}
}