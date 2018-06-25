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

			var compilationResult = Evaluator.Compile(command);
			if (compilationResult == null)
			{
				return "Compilation failed";
			}

			object result = null;
			compilationResult(ref result);

			if (result == null)
				result = "Executed code successfully";
			return result;
		}
	}
}