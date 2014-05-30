using System;
using System.Collections.Generic;

namespace Windawesome
{
	public class InputLanguageChangerPlugin : IPlugin
	{
		private readonly HashSet<string> classNamesSet;
		private readonly Dictionary<IntPtr, IntPtr> inputLanguages;
		private IntPtr currentForeground;

		public InputLanguageChangerPlugin(IEnumerable<string> windowClassNames)
		{
			classNamesSet = new HashSet<string>(windowClassNames);
			inputLanguages = new Dictionary<IntPtr, IntPtr>(5);

			Workspace.WindowActivatedEvent += OnWindowActivatedEvent;
		}

		private void OnWindowActivatedEvent(IntPtr hWnd)
		{
			if (currentForeground != IntPtr.Zero)
			{
				SaveLayoutForWindow(currentForeground);
			}

			var className = NativeMethods.GetWindowClassName(hWnd);

			if (classNamesSet.Contains(className))
			{
				IntPtr keyboardLayout;
				if (inputLanguages.TryGetValue(hWnd, out keyboardLayout))
				{
					NativeMethods.SendNotifyMessage(hWnd, NativeMethods.WM_INPUTLANGCHANGEREQUEST,
						UIntPtr.Zero, keyboardLayout);
				}
				else
				{
					SaveLayoutForWindow(hWnd);
				}

				currentForeground = hWnd;
			}
			else
			{
				currentForeground = IntPtr.Zero;
			}
		}

		private void SaveLayoutForWindow(IntPtr window)
		{
			var keyboardLayout = NativeMethods.GetKeyboardLayout(
				NativeMethods.GetWindowThreadProcessId(window, IntPtr.Zero));

			inputLanguages[window] = keyboardLayout;
		}

		#region IPlugin Members

		void IPlugin.InitializePlugin(Windawesome windawesome)
		{
		}

		void IPlugin.Dispose()
		{
		}

		#endregion
	}
}
