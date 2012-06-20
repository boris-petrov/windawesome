using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Windawesome
{
	public sealed class WorkspacesWidget : IFixedWidthWidget
	{
		private Label[] workspaceLabels;
		private readonly Color[] normalForegroundColor;
		private readonly Color[] normalBackgroundColor;
		private readonly Color highlightedForegroundColor;
		private readonly Color highlightedBackgroundColor;
		private readonly Color highlightedInactiveForegroundColor;
		private readonly Color highlightedInactiveBackgroundColor;
		private readonly Color flashingForegroundColor;
		private readonly Color flashingBackgroundColor;
		private bool isLeft;
		private readonly bool flashWorkspaces;

		private static Windawesome windawesome;
		private static Timer flashTimer;
		private static Dictionary<IntPtr, Workspace> flashingWindows;
		private static HashSet<Workspace> flashingWorkspaces;

		private delegate void WorkFlashingStopped(Workspace workspace);
		private static event WorkFlashingStopped OnWorkspaceFlashingStopped;

		public WorkspacesWidget(IEnumerable<Color> normalForegroundColor = null, IEnumerable<Color> normalBackgroundColor = null,
			Color? highlightedForegroundColor = null, Color? highlightedBackgroundColor = null,
			Color? highlightedInactiveForegroundColor = null, Color? highlightedInactiveBackgroundColor = null,
			Color? flashingForegroundColor = null, Color? flashingBackgroundColor = null, bool flashWorkspaces = true)
		{
			this.normalForegroundColor = normalForegroundColor != null ? normalForegroundColor.ToArray() : new[]
				{
					Color.Black, Color.Black, Color.Black, Color.Black, Color.Black,
					Color.White, Color.White, Color.White, Color.White, Color.White
				};
			this.normalBackgroundColor = normalBackgroundColor != null ? normalBackgroundColor.ToArray() : new[]
				{
					Color.FromArgb(0xF0, 0xF0, 0xF0),
					Color.FromArgb(0xD8, 0xD8, 0xD8),
					Color.FromArgb(0xC0, 0xC0, 0xC0),
					Color.FromArgb(0xA8, 0xA8, 0xA8),
					Color.FromArgb(0x90, 0x90, 0x90),
					Color.FromArgb(0x78, 0x78, 0x78),
					Color.FromArgb(0x60, 0x60, 0x60),
					Color.FromArgb(0x48, 0x48, 0x48),
					Color.FromArgb(0x30, 0x30, 0x30),
					Color.FromArgb(0x18, 0x18, 0x18)
				};
			this.highlightedForegroundColor = highlightedForegroundColor ?? Color.White;
			this.highlightedBackgroundColor = highlightedBackgroundColor ?? Color.FromArgb(0x33, 0x99, 0xFF);
			this.highlightedInactiveForegroundColor = highlightedInactiveForegroundColor ?? Color.White;
			this.highlightedInactiveBackgroundColor = highlightedInactiveBackgroundColor ?? Color.Green;
			this.flashingForegroundColor = flashingForegroundColor ?? Color.White;
			this.flashingBackgroundColor = flashingBackgroundColor ?? Color.Red;
			this.flashWorkspaces = flashWorkspaces;
			if (flashWorkspaces)
			{
				if (flashTimer == null)
				{
					flashTimer = new Timer { Interval = 500 };
					flashingWindows = new Dictionary<IntPtr, Workspace>(3);
					flashingWorkspaces = new HashSet<Workspace>();

					Workspace.WorkspaceWindowRemoved += (_, w) => StopFlashingApplication(w.hWnd);
					Workspace.WindowActivatedEvent += StopFlashingApplication;
					Workspace.WorkspaceWindowRestored += (_, w) => StopFlashingApplication(w.hWnd);
					Windawesome.WindowFlashing += OnWindowFlashing;
				}
			}
		}

		private void OnWorkspaceLabelClick(object sender, EventArgs e)
		{
			windawesome.SwitchToWorkspace(Array.IndexOf(workspaceLabels, sender as Label) + 1);
		}

		private void SetWorkspaceLabelColor(Workspace workspace)
		{
			var workspaceLabel = workspaceLabels[workspace.id - 1];
			if (workspace.IsCurrentWorkspace)
			{
				workspaceLabel.BackColor = highlightedBackgroundColor;
				workspaceLabel.ForeColor = highlightedForegroundColor;
			}
			else if (workspace.IsWorkspaceVisible)
			{
				workspaceLabel.BackColor = highlightedInactiveBackgroundColor;
				workspaceLabel.ForeColor = highlightedInactiveForegroundColor;
			}
			else
			{
				var count = workspace.GetWindowsCount();
				if (count > 9)
				{
					count = 9;
				}
				workspaceLabel.BackColor = normalBackgroundColor[count];
				workspaceLabel.ForeColor = normalForegroundColor[count];
			}
		}

		private void OnWorkspaceChangedFromTo(Workspace workspace)
		{
			SetWorkspaceLabelColor(workspace);
		}

		private static void OnWindowFlashing(IntPtr hWnd, LinkedList<Tuple<Workspace, Window>> list)
		{
			if (list != null)
			{
				if (NativeMethods.IsWindow(hWnd) && !flashingWindows.ContainsKey(hWnd))
				{
					var foregroundWindow = NativeMethods.GetForegroundWindow();

					if (Utilities.DoForSelfAndOwnersWhile(foregroundWindow, h => h != hWnd) == IntPtr.Zero)
					{
						var workspace = list.First.Value.Item1;

						flashingWindows[hWnd] = workspace;
						flashingWorkspaces.Add(workspace);
						if (flashingWorkspaces.Count == 1)
						{
							flashTimer.Start();
						}
					}
				}
			}
		}

		private static void StopFlashingApplication(IntPtr hWnd)
		{
			Workspace workspace;
			if (flashingWindows.TryGetValue(hWnd, out workspace))
			{
				flashingWindows.Remove(hWnd);
				if (flashingWindows.Values.All(w => w != workspace))
				{
					OnWorkspaceFlashingStopped(workspace);
					flashingWorkspaces.Remove(workspace);
					if (flashingWorkspaces.Count == 0)
					{
						flashTimer.Stop();
					}
				}
			}
		}

		private void OnTimerTick(object sender, EventArgs e)
		{
			foreach (var flashingWorkspace in flashingWorkspaces)
			{
				if (workspaceLabels[flashingWorkspace.id - 1].BackColor == flashingBackgroundColor)
				{
					SetWorkspaceLabelColor(flashingWorkspace);
				}
				else
				{
					workspaceLabels[flashingWorkspace.id - 1].BackColor = flashingBackgroundColor;
					workspaceLabels[flashingWorkspace.id - 1].ForeColor = flashingForegroundColor;
				}
			}
		}

		#region IWidget Members

		void IWidget.StaticInitializeWidget(Windawesome windawesome)
		{
			WorkspacesWidget.windawesome = windawesome;
		}

		void IWidget.InitializeWidget(Bar bar)
		{
			if (flashWorkspaces)
			{
				flashTimer.Tick += OnTimerTick;
				OnWorkspaceFlashingStopped += SetWorkspaceLabelColor;
			}

			bar.BarShown += () => flashTimer.Start();
			bar.BarHidden += () => flashTimer.Stop();

			workspaceLabels = new Label[windawesome.config.Workspaces.Length];

			Workspace.WorkspaceWindowAdded += (ws, _) => SetWorkspaceLabelColor(ws);
			Workspace.WorkspaceWindowRemoved += (ws, _) => SetWorkspaceLabelColor(ws);

			Workspace.WorkspaceDeactivated += OnWorkspaceChangedFromTo;
			Workspace.WorkspaceActivated += OnWorkspaceChangedFromTo;
			Workspace.WorkspaceShown += OnWorkspaceChangedFromTo;
			Workspace.WorkspaceHidden += OnWorkspaceChangedFromTo;

			for (var i = 0; i < windawesome.config.Workspaces.Length; i++)
			{
				var workspace = windawesome.config.Workspaces[i];
				var name = workspace.name ?? (i + 1).ToString();

				var label = bar.CreateLabel(" " + name + " ", 0);
				label.TextAlign = ContentAlignment.MiddleCenter;
				label.Click += OnWorkspaceLabelClick;
				workspaceLabels[i] = label;
				SetWorkspaceLabelColor(workspace);
			}
		}

		IEnumerable<Control> IFixedWidthWidget.GetInitialControls(bool isLeft)
		{
			this.isLeft = isLeft;

			return workspaceLabels;
		}

		public void RepositionControls(int left, int right)
		{
			if (isLeft)
			{
				foreach (var label in workspaceLabels)
				{
					label.Location = new Point(left, 0);
					left += label.Width;
				}
			}
			else
			{
				foreach (var label in NativeMethods.Reverse(workspaceLabels))
				{
					right -= label.Width;
					label.Location = new Point(right, 0);
				}
			}
		}

		int IWidget.GetLeft()
		{
			return workspaceLabels.First().Left;
		}

		int IWidget.GetRight()
		{
			return workspaceLabels.Last().Right;
		}

		void IWidget.StaticDispose()
		{
		}

		void IWidget.Dispose()
		{
		}

		void IWidget.Refresh()
		{
			// remove all flashing windows
			flashingWindows.Keys.ToArray().ForEach(StopFlashingApplication);
		}

		#endregion
	}
}
