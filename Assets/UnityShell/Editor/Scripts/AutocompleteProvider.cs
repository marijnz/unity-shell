using System;
using System.Threading;
using Mono.CSharp;

namespace UnityShell
{
	[Serializable]
	public class AutocompleteProvider
	{
		public string[] completions;

		int handleCount;

		public void SetInput(string input)
		{
			handleCount++;
			new Thread((threadStart) =>
			{
				int handle = handleCount;

				if(!string.IsNullOrEmpty(input))
				{
					string prefix;
#if NET_4_6 || NET_STANDARD_2_0
					var evaluator = new Evaluator(new CompilerContext(new CompilerSettings(), new ConsoleReportPrinter()));
					var result = evaluator.GetCompletions(input, out prefix);
#else
					var result = Evaluator.GetCompletions(input, out prefix);
#endif

					// Avoid old threads overriding with old results
					if(handle == handleCount)
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

						if(completions.Length == 1 && completions[0].Trim() == input.Trim())
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
	}
}