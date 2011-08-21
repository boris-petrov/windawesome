﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Windawesome
{
	public class WorkspacesWidget : IFixedWidthWidget
	{
		private Bar bar;
		private Label[] workspaceLabels;
		private readonly Color[] normalForegroundColor;
		private readonly Color[] normalBackgroundColor;
		private readonly Color highlightedForegroundColor;
		private readonly Color highlightedBackgroundColor;
		private readonly Color highlightedInactiveForegroundColor;
		private readonly Color highlightedInactiveBackgroundColor;
		private readonly Color flashingForegroundColor;
		private readonly Color flashingBackgroundColor;
		private int left, right;
		private bool isLeft;
		private bool isShown;
		private readonly bool flashWorkspaces;
		private readonly Dictionary<IntPtr, Workspace> flashingWindows;

		private static Windawesome windawesome;
		private static Config config;
		private static Timer flashTimer;
		private static HashSet<Workspace> flashingWorkspaces;

		public WorkspacesWidget(Color[] normalForegroundColor = null, Color[] normalBackgroundColor = null,
			Color? highlightedForegroundColor = null, Color? highlightedBackgroundColor = null,
			Color? highlightedInactiveForegroundColor = null, Color? highlightedInactiveBackgroundColor = null,
			Color? flashingForegroundColor = null, Color? flashingBackgroundColor = null, bool flashWorkspaces = true)
		{
			this.normalForegroundColor = normalForegroundColor ?? new[]
				{
					Color.FromArgb(0x00, 0x00, 0x00),
					Color.FromArgb(0x00, 0x00, 0x00),
					Color.FromArgb(0x00, 0x00, 0x00),
					Color.FromArgb(0x00, 0x00, 0x00),
					Color.FromArgb(0x00, 0x00, 0x00),
					Color.FromArgb(0xFF, 0xFF, 0xFF),
					Color.FromArgb(0xFF, 0xFF, 0xFF),
					Color.FromArgb(0xFF, 0xFF, 0xFF),
					Color.FromArgb(0xFF, 0xFF, 0xFF),
					Color.FromArgb(0xFF, 0xFF, 0xFF),
				};
			this.normalBackgroundColor = normalBackgroundColor ?? new[]
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
					Color.FromArgb(0x18, 0x18, 0x18),
				};
			this.highlightedForegroundColor = highlightedForegroundColor ?? Color.FromArgb(0xFF, 0xFF, 0xFF);
			this.highlightedBackgroundColor = highlightedBackgroundColor ?? Color.FromArgb(0x33, 0x99, 0xFF);
			this.highlightedInactiveForegroundColor = highlightedInactiveForegroundColor ?? Color.White;
			this.highlightedInactiveBackgroundColor = highlightedInactiveBackgroundColor ?? Color.Green;
			this.flashingForegroundColor = flashingForegroundColor ?? Color.White;
			this.flashingBackgroundColor = flashingBackgroundColor ?? Color.Red;
			this.flashWorkspaces = flashWorkspaces;
			if (flashWorkspaces)
			{
				flashingWindows = new Dictionary<IntPtr, Workspace>(3);

				if (flashTimer == null)
				{
					flashTimer = new Timer { Interval = 500 };
					flashingWorkspaces = new HashSet<Workspace>();
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
			if (workspace.IsCurrentWorkspace && isShown)
			{
				workspaceLabel.BackColor = highlightedBackgroundColor;
				workspaceLabel.ForeColor = highlightedForegroundColor;
			}
			else if (workspace.IsWorkspaceVisible && isShown)
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
			if (isShown)
			{
				SetWorkspaceLabelColor(workspace);
			}
		}

		private void OnWindowFlashing(LinkedList<Tuple<Workspace, Window>> list)
		{
			if (list.First.Value.Item2.hWnd != NativeMethods.GetForegroundWindow() && NativeMethods.IsWindow(list.First.Value.Item2.hWnd) &&
				!flashingWindows.ContainsKey(list.First.Value.Item2.hWnd))
			{
				flashingWindows[list.First.Value.Item2.hWnd] = list.First.Value.Item1;
				flashingWorkspaces.Add(list.First.Value.Item1);
				if (flashingWorkspaces.Count == 1)
				{
					flashTimer.Start();
				}
			}
		}

		private void OnTimerTick(object sender, EventArgs e)
		{
			if (isShown)
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
		}

		private void StopFlashingApplication(IntPtr hWnd)
		{
			Workspace workspace;
			if (flashingWindows.TryGetValue(hWnd, out workspace))
			{
				flashingWindows.Remove(hWnd);
				if (flashingWindows.Values.All(w => w != workspace))
				{
					SetWorkspaceLabelColor(workspace);
					flashingWorkspaces.Remove(workspace);
					if (flashingWorkspaces.Count == 0)
					{
						flashTimer.Stop();
					}
				}
			}
		}

		private void OnBarShown()
		{
			isShown = true;
			SetWorkspaceLabelColor(bar.Monitor.CurrentVisibleWorkspace);
		}

		private void OnBarHidden()
		{
			isShown = false;

			if (flashWorkspaces)
			{
				flashingWindows.Values.ForEach(SetWorkspaceLabelColor);
			}
			SetWorkspaceLabelColor(bar.Monitor.CurrentVisibleWorkspace);
		}

		#region IWidget Members

		void IWidget.StaticInitializeWidget(Windawesome windawesome, Config config)
		{
			WorkspacesWidget.windawesome = windawesome;
			WorkspacesWidget.config = config;
		}

		void IWidget.InitializeWidget(Bar bar)
		{
			this.bar = bar;

			if (flashWorkspaces)
			{
				flashTimer.Tick += OnTimerTick;
				Workspace.WorkspaceApplicationRemoved += (_, w) => StopFlashingApplication(w.hWnd);
				Workspace.WindowActivatedEvent += StopFlashingApplication;
				Workspace.WorkspaceApplicationRestored += (_, w) => StopFlashingApplication(w.hWnd);
				Windawesome.WindowFlashing += OnWindowFlashing;
			}

			isShown = false;

			bar.BarShown += OnBarShown;
			bar.BarHidden += OnBarHidden;

			workspaceLabels = new Label[config.Workspaces.Length];

			Workspace.WorkspaceApplicationAdded += (ws, _) => SetWorkspaceLabelColor(ws);
			Workspace.WorkspaceApplicationRemoved += (ws, _) => SetWorkspaceLabelColor(ws);

			Workspace.WorkspaceDeactivated += OnWorkspaceChangedFromTo;
			Workspace.WorkspaceActivated += OnWorkspaceChangedFromTo;
			Workspace.WorkspaceShown += OnWorkspaceChangedFromTo;
			Workspace.WorkspaceHidden += OnWorkspaceChangedFromTo;

			for (var i = 0; i < config.Workspaces.Length; i++)
			{
				var workspace = config.Workspaces[i];
				var name = (i + 1) + (workspace.name == "" ? "" : ":" + workspace.name);

				var label = bar.CreateLabel(" " + name + " ", 0);
				label.TextAlign = ContentAlignment.MiddleCenter;
				label.Click += OnWorkspaceLabelClick;
				workspaceLabels[i] = label;
				SetWorkspaceLabelColor(workspace);
			}
		}

		IEnumerable<Control> IWidget.GetControls(int left, int right)
		{
			isLeft = right == -1;

			this.RepositionControls(left, right);

			return workspaceLabels;
		}

		public void RepositionControls(int left, int right)
		{
			if (isLeft)
			{
				this.left = left;
				foreach (var label in workspaceLabels)
				{
					label.Location = new Point(left, 0);
					left += label.Width;
				}
				this.right = left;
			}
			else
			{
				this.right = right;
				foreach (var label in workspaceLabels.Reverse())
				{
					right -= label.Width;
					label.Location = new Point(right, 0);
				}
				this.left = right;
			}
		}

		int IWidget.GetLeft()
		{
			return left;
		}

		int IWidget.GetRight()
		{
			return right;
		}

		void IWidget.StaticDispose()
		{
		}

		void IWidget.Dispose()
		{
		}

		void IWidget.Refresh()
		{
			// remove all flashing windows that no longer exist
			flashingWindows.Keys.Unless(NativeMethods.IsWindow).ForEach(StopFlashingApplication);
		}

		#endregion
	}
}
