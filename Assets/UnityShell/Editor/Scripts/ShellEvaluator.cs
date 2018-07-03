using System;
using Mono.CSharp;

namespace UnityShell
{
	[Serializable]
	public class ShellEvaluator
	{
#if NET_4_6 || NET_STANDARD_2_0
		Evaluator evaluator = new Evaluator(new CompilerContext(new CompilerSettings(), new ConsoleReportPrinter()));
#endif

		public ShellEvaluator()
		{
			foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				try
				{
#if NET_4_6 || NET_STANDARD_2_0
					evaluator.ReferenceAssembly(assembly);
#else
					Evaluator.ReferenceAssembly(assembly);
#endif
				}
				catch (Exception)
				{
					// ignored
				}
			}
#if NET_4_6 || NET_STANDARD_2_0
			evaluator.Run ("using UnityEngine; using UnityEditor; using System; using System.Collections.Generic;");
#else
			Evaluator.Run ("using UnityEngine; using UnityEditor; using System; using System.Collections.Generic;");
#endif
		}

		public object Evaluate(string command)
		{
			if(!command.EndsWith(";"))
			{
				command += ";";
			}

#if NET_4_6 || NET_STANDARD_2_0
			var compilationResult = evaluator.Compile(command);
#else
			var compilationResult = Evaluator.Compile(command);
#endif
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