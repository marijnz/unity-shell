using System;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace UnityShell
{
	public class UnityShellEditorWindow : EditorWindow
	{
		static class Styles
		{
			public static readonly GUIStyle textAreaStyle;

			static Texture2D _backgroundTexture;
			public static Texture2D backgroundTexture
			{
				get
				{
					if(_backgroundTexture == null)
					{
						_backgroundTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
						_backgroundTexture.SetPixel(0, 0, new Color(.18f, .18f, .18f));
						_backgroundTexture.Apply();
					}
					return _backgroundTexture;
				}
			}

			static Styles()
			{
				textAreaStyle = new GUIStyle(EditorStyles.textArea);
				textAreaStyle.padding = new RectOffset();

				var style = textAreaStyle.focused;
				style.background = backgroundTexture;
				style.textColor = new Color(.7f, .7f, .7f);

				textAreaStyle.focused = style;
				textAreaStyle.active = style;
				textAreaStyle.onActive = style;
				textAreaStyle.hover = style;
				textAreaStyle.normal = style;
				textAreaStyle.onNormal = style;
			}
		}

		[MenuItem("Window/UnityShell #%u")]
		static void CreateWindow()
		{
			GetWindow<UnityShellEditorWindow>("UnityShell");
		}

		const string ConsoleTextAreaControlName = "ConsoleTextArea";
		const string CommandName = "command > ";

		string text
		{
			get
			{
				return textEditor.text;
			}
			set
			{
				textEditor.text = value;
			}
		}

		[SerializeField]
		ShellEvaluator evaluator;

		[SerializeField]
		AutocompleteBox autocompleteBox;

		[SerializeField]
		AutocompleteProvider autocompleteProvider;

		[SerializeField]
		Vector2 scrollPos = Vector2.zero;

		[SerializeField]
		TextEditor textEditor;

		[SerializeField]
		List<string> inputHistory = new List<string>();

		bool requestMoveToCursorToEnd;
		bool requestFocusOnTextArea;

		bool requestRevertNewLine;

		string input = "";
		string lastWord = "";

		Vector2 lastCursorPos;

		int positionInHistory;

		string savedInput;

		void Awake()
		{
			ClearText();
			requestFocusOnTextArea = true;

			autocompleteProvider = new AutocompleteProvider();
			autocompleteBox = new AutocompleteBox();
		}

		void ClearText()
		{
			if(textEditor != null)
			{
				text = "";
			}
		}

		void OnEnable()
		{
			new Thread(() => evaluator = new ShellEvaluator()).Start();
			ScheduleMoveCursorToEnd();
			autocompleteBox.onConfirm += OnAutocompleteConfirm;
			autocompleteBox.Clear();
		}

		void OnAutocompleteConfirm(string confirmedInput)
		{
			text = text.Substring(0, text.Length - lastWord.Length);
			text += confirmedInput;
			lastWord = confirmedInput;
			requestRevertNewLine = true;
		}

		void OnInspectorUpdate()
		{
			Repaint();
		}

		void OnGUI()
		{
			textEditor = (TextEditor) GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
			if(text == "")
			{
				AppendStartCommand();
				ScheduleMoveCursorToEnd();
			}

			EnsureNotAboutToTypeAtInvalidPosition();
			autocompleteBox.HandleEvents();
			HandleHistory();
			DoAutoComplete();
			HandleRequests();
			DrawAll();
		}

		void HandleHistory()
		{
			var current = Event.current;
			if(current.type == EventType.KeyDown)
			{
				var changed = false;
				if(current.keyCode == KeyCode.DownArrow)
				{
					positionInHistory++;
					changed = true;
					current.Use();
				}
				if(current.keyCode == KeyCode.UpArrow)
				{
					positionInHistory--;
					changed = true;
					current.Use();
				}

				if(changed)
				{
					if(savedInput == null)
					{
						savedInput = input;
					}

					if(positionInHistory < 0) positionInHistory = 0;
					else if(positionInHistory >= inputHistory.Count)
					{
						ReplaceCurrentCommand(savedInput);
						positionInHistory = inputHistory.Count;
						savedInput = null;
					}
					else
					{
						ReplaceCurrentCommand(inputHistory[positionInHistory]);
					}
				}
			}
		}

		void ReplaceCurrentCommand(string replacement)
		{
			text = text.Substring(0, text.Length - input.Length);
			text += replacement;
			textEditor.MoveTextEnd();
		}

		void DoAutoComplete()
		{
			var newInput = GetInput();
			if (newInput != null && input != newInput && !requestRevertNewLine)
			{
				input = newInput;

				lastWord = input;
				var lastWordIndex = input.LastIndexOfAny(new[] {'(', ' '});
				if (lastWordIndex != -1)
				{
					lastWord = input.Substring(lastWordIndex + 1);
				}

				autocompleteProvider.SetInput(lastWord);
			}
		}

		string GetInput()
		{
			var commandStartIndex = text.LastIndexOf(CommandName, StringComparison.Ordinal);
			if (commandStartIndex != -1)
			{
				commandStartIndex += CommandName.Length;
				return text.Substring(commandStartIndex);
			}
			return null;
		}

		void HandleRequests()
		{
			var current = Event.current;
			if (requestMoveToCursorToEnd && current.type == EventType.Repaint)
			{
				textEditor.MoveTextEnd();
				requestMoveToCursorToEnd = false;
				Repaint();
			}
			else if (focusedWindow == this && requestFocusOnTextArea)
			{
				GUI.FocusControl(ConsoleTextAreaControlName);
				requestFocusOnTextArea = false;
				Repaint();
			}

			var cursorPos = textEditor.graphicalCursorPos;

			if (current.type == EventType.Repaint && cursorPos.y > lastCursorPos.y && requestRevertNewLine)
			{
				textEditor.Backspace();
				textEditor.MoveTextEnd();
				Repaint();
				requestRevertNewLine = false;
			}

			lastCursorPos = cursorPos;
		}

		void EnsureNotAboutToTypeAtInvalidPosition()
		{
			var current = Event.current;

			if(current.isKey && !current.command && !current.control)
			{
				var lastIndexCommand = text.LastIndexOf(CommandName, StringComparison.Ordinal) + CommandName.Length;

				var cursorIndex = textEditor.cursorIndex;
				if(current.keyCode == KeyCode.Backspace)
					cursorIndex--;

				if(cursorIndex < lastIndexCommand)
				{
					ScheduleMoveCursorToEnd();
					current.Use();
				}
			}
		}

		void DrawAll()
		{
			GUI.DrawTexture(new Rect(0, 0, maxSize.x, maxSize.y), Styles.backgroundTexture, ScaleMode.StretchToFill);
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
			{
				GUILayout.FlexibleSpace();

				if (GUILayout.Button("Clear", EditorStyles.toolbarButton))
				{
					ClearText();
				}
			}
			EditorGUILayout.EndHorizontal();

			scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
			{
				DrawConsole();
			}
			EditorGUILayout.EndScrollView();

			autocompleteBox.results = autocompleteProvider.completions;
			var pos = textEditor.graphicalCursorPos;
			var rect = new Rect(pos.x, pos.y, 300, 200);
			rect.y += 34;
			autocompleteBox.OnGUI(lastWord, rect);
		}

		void DrawConsole()
		{
			var current = Event.current;

			if(current.type == EventType.KeyDown)
			{
				ScrollDown();

				if(current.keyCode == KeyCode.Return && !current.shift)
				{
					textEditor.MoveTextEnd();
					try
					{
						var result = evaluator.Evaluate(input);
						Append(result);
						inputHistory.Add(input);
						positionInHistory = inputHistory.Count;
					}
					catch(Exception e)
					{
						Debug.LogException(e);
						Append(e.Message);
					}

					AppendStartCommand();

					current.Use();
				}
			}

			GUI.SetNextControlName(ConsoleTextAreaControlName);
			GUILayout.TextArea(text, Styles.textAreaStyle, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
		}

		void ScrollDown()
		{
			scrollPos.y = float.MaxValue;
		}

		void AppendStartCommand()
		{
			text += CommandName;
			ScheduleMoveCursorToEnd();
		}

		void ScheduleMoveCursorToEnd()
		{
			requestMoveToCursorToEnd = true;
			ScrollDown();
		}

		void Append(object result)
		{
			text += "\n" + result + "\n";
		}
	}
}




