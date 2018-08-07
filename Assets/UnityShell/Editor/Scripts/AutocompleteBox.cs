using System;
using UnityEditor;
using UnityEngine;

namespace UnityShell
{
	[Serializable]
	public class AutocompleteBox
	{
		private static class Styles
		{
			public const float resultHeight = 20f;
			public const float resultsBorderWidth = 2f;
			public const float resultsMargin = 15f;
			public const float resultsLabelOffset = 2f;

			public static readonly GUIStyle entryEven;
			public static readonly GUIStyle entryOdd;
			public static readonly GUIStyle labelStyle;
			public static readonly GUIStyle resultsBorderStyle;
			public static readonly GUIStyle sliderStyle;

			static Styles()
			{
				entryOdd = new GUIStyle("CN EntryBackOdd");
				entryEven = new GUIStyle("CN EntryBackEven");
				resultsBorderStyle = new GUIStyle("hostview");

				labelStyle = new GUIStyle(EditorStyles.label)
				{
					alignment = TextAnchor.MiddleLeft,
					richText = true
				};

				sliderStyle = new GUIStyle("MiniSliderVertical");
			}
		}

		private const int HintToNextCompletionHeight = 7;

		public Action<string> onConfirm;
		public int maxResults = 10;

		[SerializeField]
		public string[] results = new string[0];

		[SerializeField]
		private Vector2 scrollPos;

		[SerializeField]
		private int selectedIndex = -1;

		[SerializeField]
		private int visualIndex = -1;

		private bool showResults;

		private string searchString;

		public void Clear()
		{
			searchString = "";
			showResults = false;
		}

		public void OnGUI(string result, Rect rect)
		{
			if (results == null)
			{
				results = new string[0];
			}

			if (result != searchString)
			{
				selectedIndex = 0;
				visualIndex = 0;
				showResults = true;
			}
			searchString = result;

			DrawResults(rect);
		}

		public void HandleEvents()
		{
			if (results.Length == 0)
			{
				return;
			}

			var current = Event.current;

			if (current.type == EventType.KeyDown)
			{
				switch (current.keyCode)
				{
				case KeyCode.Escape:
					showResults = false;
					break;
				case KeyCode.UpArrow:
					current.Use();
					selectedIndex--;
					break;
				case KeyCode.DownArrow:
					current.Use();
					selectedIndex++;
					break;
				case KeyCode.Return:
					if (selectedIndex >= 0)
					{
						current.Use();
						OnConfirm(results[selectedIndex]);
					}
					break;
				}

				if (selectedIndex >= results.Length)
				{
					selectedIndex = 0;
				}
				else if (selectedIndex < 0)
				{
					selectedIndex = results.Length - 1;
				}
			}
		}

		private void DrawResults(Rect drawRect)
		{
			if (results.Length <= 0 || !showResults)
			{
				return;
			}

			var current = Event.current;
			drawRect.height = Styles.resultHeight * Mathf.Min(maxResults, results.Length);
			drawRect.x = Styles.resultsMargin;
			drawRect.width -= Styles.resultsMargin * 2;

			drawRect.height += Styles.resultsBorderWidth;

			var backgroundRect = drawRect;
			if (results.Length > maxResults)
			{
				backgroundRect.height += HintToNextCompletionHeight + Styles.resultsBorderWidth;
			}

			GUI.color = new Color(0.78f, 0.78f, 0.78f);
			GUI.Label(backgroundRect, "", Styles.resultsBorderStyle);
			GUI.color = Color.white;

			var elementRect = drawRect;
			elementRect.x += Styles.resultsBorderWidth;
			elementRect.width -= Styles.resultsBorderWidth * 2;
			elementRect.height = Styles.resultHeight;

			var scrollViewRect = drawRect;
			scrollViewRect.height = Styles.resultHeight * results.Length;

			var clipRect = drawRect;
			clipRect.height += HintToNextCompletionHeight; // to hint for more

			UpdateVisualIndex(clipRect);

			var posRect = new Rect();
			posRect.yMin = selectedIndex * Styles.resultHeight;
			posRect.yMax = selectedIndex * Styles.resultHeight + Styles.resultHeight;

			GUI.BeginClip(clipRect);
			{
				elementRect.x = Styles.resultsBorderWidth;
				elementRect.y = 0;

				if (results.Length > maxResults)
				{
					elementRect.y = -visualIndex * Styles.resultHeight;

					var maxPos = GetTotalResultsShown(clipRect) * Styles.resultHeight - HintToNextCompletionHeight;

					if (-elementRect.y > maxPos)
					{
						elementRect.y = -maxPos;
					}
				}

				for (var i = 0; i < results.Length; i++)
				{
					if (current.type == EventType.Repaint)
					{
						var style = i % 2 == 0 ? Styles.entryOdd : Styles.entryEven;

						style.Draw(elementRect, false, false, i == selectedIndex, false);

						var labelRect = elementRect;
						labelRect.x += Styles.resultsLabelOffset;
						GUI.Label(labelRect, results[i], Styles.labelStyle);
					}
					elementRect.y += Styles.resultHeight;
				}

				if (results.Length > maxResults)
				{
					DrawScroll(clipRect);
				}
			}
			GUI.EndClip();
		}

		private void DrawScroll(Rect clipRect)
		{
			var scrollRect = clipRect;
			scrollRect.x += scrollRect.width - 30;
			scrollRect.y = 0;

			var resultsShown = GetTotalResultsShown(clipRect);

			scrollRect.height = ((float) maxResults / resultsShown * clipRect.height);

			scrollRect.y = ((float) visualIndex / (resultsShown)) * (clipRect.height - scrollRect.height);

			GUI.Box(scrollRect, GUIContent.none, Styles.sliderStyle);
		}

		private int GetTotalResultsShown(Rect clipRect)
		{
			// Actual scrolling is a bit less as there's also the view itself in which is not scrolled,
			// when moving down initially, for example.
			int resultsShown = results.Length;
			resultsShown -= (int) (clipRect.height / Styles.resultHeight);
			return resultsShown;
		}

		private void UpdateVisualIndex(Rect clipRect)
		{
			var ySelectedPos = selectedIndex * Styles.resultHeight;
			var yVisualPos = visualIndex * Styles.resultHeight;

			var totalHeight = results.Length * Styles.resultHeight;

			var max = Mathf.Min(clipRect.height, clipRect.height);
			var min = Math.Min(0, totalHeight - ySelectedPos);

			var diffMax = ySelectedPos - (yVisualPos + max) + Styles.resultHeight;
			var diffMin = (yVisualPos + min) - ySelectedPos;

			if (diffMax > 0)
			{
				visualIndex += Mathf.CeilToInt(diffMax / Styles.resultHeight);
			}
			else if (diffMin > 0)
			{
				visualIndex -= Mathf.CeilToInt(diffMin / Styles.resultHeight);
			}
		}

		private void OnConfirm(string result)
		{
			if (onConfirm != null)
			{
				onConfirm(result);
			}
			RepaintFocusedWindow();
			showResults = false;
			searchString = result;
		}

		private static void RepaintFocusedWindow()
		{
			if (EditorWindow.focusedWindow != null)
			{
				EditorWindow.focusedWindow.Repaint();
			}
		}
	}
}