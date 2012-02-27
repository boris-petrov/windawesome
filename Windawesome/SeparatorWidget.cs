using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Windawesome
{
	public sealed class SeparatorWidget : IFixedWidthWidget
	{
		private Label label;
		private bool isLeft;
		private readonly string separator;
		private readonly Color backgroundColor;
		private readonly Color foregroundColor;

		public SeparatorWidget(string separator = "|", Color? backgroundColor = null, Color? foregroundColor = null)
		{
			this.separator = separator;

			this.backgroundColor = backgroundColor ?? Color.White;
			this.foregroundColor = foregroundColor ?? Color.Black;
		}

		#region IWidget Members

		void IWidget.StaticInitializeWidget(Windawesome windawesome)
		{
		}

		void IWidget.InitializeWidget(Bar bar)
		{
			label = bar.CreateLabel(separator, 0);
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
