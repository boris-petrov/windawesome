using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Windawesome
{
	public sealed class CurrentlyPlayingWidget : IFixedWidthWidget
	{
		private Bar bar;

		private Label label;
		private bool isLeft;
		private readonly string windowClassName;
		private readonly Color backgroundColor;
		private readonly Color foregroundColor;

		public CurrentlyPlayingWidget(string windowClassName = "{97E27FAA-C0B3-4b8e-A693-ED7881E99FC1}",
			Color? backgroundColor = null, Color? foregroundColor = null)
		{
			this.windowClassName = windowClassName;

			this.backgroundColor = backgroundColor ?? Color.White;
			this.foregroundColor = foregroundColor ?? Color.Black;
		}

		private void WindawesomeOnWindowTitleOrIconChanged(Workspace workspace, Window window, string newText, Bitmap newIcon)
		{
			if (window.className == windowClassName)
			{
				var oldLeft  = label.Left;
				var oldRight = label.Right;

				var oldWidth = label.Width;
				var newWidth = TextRenderer.MeasureText(newText, label.Font).Width;

				label.Text  = newText;
				label.Width = newWidth;

				if (oldWidth != newWidth)
				{
					this.RepositionControls(oldLeft, oldRight);
					bar.DoFixedWidthWidgetWidthChanged(this);
				}
			}
		}

		#region IWidget Members

		void IWidget.StaticInitializeWidget(Windawesome windawesome)
		{
		}

		void IWidget.InitializeWidget(Bar bar)
		{
			this.bar = bar;

			Windawesome.WindowTitleOrIconChanged += WindawesomeOnWindowTitleOrIconChanged;

			var hWnd = NativeMethods.FindWindow(windowClassName, null);
			var text = NativeMethods.GetText(hWnd);
			label = bar.CreateLabel(text, 0);
			label.BackColor = backgroundColor;
			label.ForeColor = foregroundColor;
			label.TextAlign = ContentAlignment.MiddleCenter;
		}

		IEnumerable<Control> IFixedWidthWidget.GetInitialControls(bool isLeft)
		{
			this.isLeft = isLeft;

			return new[] { label };
		}

		public void RepositionControls(int left, int right)
		{
			this.label.Location = this.isLeft ? new Point(left, 0) : new Point(right - this.label.Width, 0);
		}

		int IWidget.GetLeft()
		{
			return label.Left;
		}

		int IWidget.GetRight()
		{
			return label.Right;
		}

		void IWidget.StaticDispose()
		{
		}

		void IWidget.Dispose()
		{
		}

		void IWidget.Refresh()
		{
		}

		#endregion
	}
}
