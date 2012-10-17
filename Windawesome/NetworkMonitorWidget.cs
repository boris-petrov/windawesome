using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Drawing;
using System.Windows.Forms;

namespace Windawesome
{
	public sealed class NetworkMonitorWidget : IFixedWidthWidget
	{
	private Bar bar;

	private Label label;
	private bool isLeft;
	private readonly Timer updateTimer;
	private readonly Color backgroundColor;
	private readonly Color foregroundColor;
	private long oldReceived;
	private long oldSent;
	private NetworkInterface networkInterface;
	private int updateDuration;

	public NetworkMonitorWidget(int interfaceId = 0, int updateTime = 1000,
		Color? backgroundColor = null, Color? foregroundColor = null)
	{
		this.backgroundColor = backgroundColor ?? Color.White;
		this.foregroundColor = foregroundColor ?? Color.Black;

        if (!NetworkInterface.GetIsNetworkAvailable())
        {
            return;
        }

		updateDuration = updateTime;
		updateTimer = new Timer { Interval = updateTime };
		updateTimer.Tick += OnTimerTick;

		networkInterface = NetworkInterface.GetAllNetworkInterfaces()[interfaceId];
		oldReceived = networkInterface.GetIPv4Statistics().BytesReceived;
		oldSent = networkInterface.GetIPv4Statistics().BytesSent;
	}

	private void OnTimerTick(object sender, EventArgs e)
	{
		var oldLeft = label.Left;
		var oldRight = label.Right;

		var curReceived = networkInterface.GetIPv4Statistics().BytesReceived;
		var downSpeed = ((float) (curReceived - oldReceived)) / updateDuration * 1000 / 1024;
		var curSent = networkInterface.GetIPv4Statistics().BytesSent;
		var upSpeed = ((float) (curSent - oldSent)) / updateDuration * 1000 / 1024;

		var oldWidth = label.Width;
		label.Text = string.Format("\u2193{0}{1} \u2191{2}{3}", ((downSpeed > 999) ? downSpeed / 1024 : downSpeed).ToString("0.0"),
            ((downSpeed > 999) ? "M" : "k"), ((upSpeed > 999) ? upSpeed / 1024 : upSpeed).ToString("0.0"), ((upSpeed > 999) ? "M" : "k"));
		var newWidth = TextRenderer.MeasureText(label.Text, label.Font).Width;
		label.Width = newWidth;

		if (oldWidth != newWidth)
		{
		    this.RepositionControls(oldLeft, oldRight);
	        bar.DoFixedWidthWidgetWidthChanged(this);
		}

		oldSent = curSent;
		oldReceived = curReceived;
	}

	#region IWidget Members

	void IWidget.StaticInitializeWidget(Windawesome windawesome)
	{
	}

	void IWidget.InitializeWidget(Bar bar)
	{
		this.bar = bar;

		label = bar.CreateLabel("", 0);
		label.BackColor = backgroundColor;
		label.ForeColor = foregroundColor;
		label.TextAlign = ContentAlignment.MiddleCenter;

		bar.BarShown += () => updateTimer.Start();
		bar.BarHidden += () => updateTimer.Stop();
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
