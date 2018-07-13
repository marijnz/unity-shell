using System;
using System.Linq;
using System.Threading;
using Mono.CSharp;

namespace UnityShell
{
	[Serializable]
	public class ShellEvaluator
	{
		private Evaluator evaluator;
		public string[] completions;

		private int handleCount;
		
#if NET_4_6 || NET_STANDARD_2_0
		public Evaluator evaluator;
#endif

		public ShellEvaluator()
		{
			new Thread(InitializeEvaluator).Start();
		}
		
		private void InitializeEvaluator()
		{
#if NET_4_6 || NET_STANDARD_2_0
			evaluator = new Evaluator(new CompilerContext(new CompilerSettings(), new ConsoleReportPrinter()));
			AppDomain.CurrentDomain.GetAssemblies().ToList().ForEach(asm => {
				try
				{
					evaluator.ReferenceAssembly(asm);
				}
				catch { }
			});
			evaluator.Run("using UnityEngine; using UnityEditor; using System; using System.Collections.Generic;");
#else
			AppDomain.CurrentDomain.GetAssemblies().ToList().ForEach(asm => {
				try
				{
					Evaluator.ReferenceAssembly(asm);
				}
				catch { }
			});
			Evaluator.Run("using UnityEngine; using UnityEditor; using System; using System.Collections.Generic;");
#endif
		}

		public void SetInput(string input)
		{
#if NET_4_6 || NET_STANDARD_2_0
			if (evaluator == null)
			{
				return;
			}
#endif
			
			handleCount++;
			new Thread(() =>
			{
				int handle = handleCount;

				if (!string.IsNullOrEmpty(input))
				{
					string prefix;
#if NET_4_6 || NET_STANDARD_2_0
					var result = evaluator.GetCompletions(input, out prefix);
#else
					var result = Evaluator.GetCompletions(input, out prefix);
#endif

					// Avoid old threads overriding with old results
					if (handle == handleCount)
					{
						completions = result;
						if (completions == null)
						{
							return;
						}
						for (var i = 0; i < completions.Length; i++)
						{
							completions[i] = input + completions[i];
						}

						if (completions.Length == 1 && completions[0].Trim() == input.Trim())
						{
							completions = new string[0];
						}
					}
				}
				else
				{
					completions = new string[0];
				}
			}).Start();
		}
		
		public object Evaluate(string command)
		{
#if NET_4_6 || NET_STANDARD_2_0
			if (evaluator == null)
			{
				return null;
			}
#endif

			if (!command.EndsWith(";"))
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
			{
				result = "Executed code successfully";
			}
			return result;
		}
	}
}