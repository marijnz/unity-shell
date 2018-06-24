using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Mono.CSharp;
using UnityEngine;
using Debug = UnityEngine.Debug;

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
			new Thread(() =>
			{
				int handle = handleCount;

				if(!string.IsNullOrEmpty(input))
				{
					string prefix;
					var result = Evaluator.GetCompletions(input, out prefix);

					// Avoid old threads overriding with old results
					if(handle == handleCount)
					{
						completions = result;
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