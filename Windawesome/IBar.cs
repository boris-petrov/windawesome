﻿using System;
using System.Drawing;

namespace Windawesome
{
	public interface IBar
	{
		void InitializeBar(Windawesome windawesome);
		void Dispose();

		IntPtr Handle { get; }

		Monitor Monitor { get; }

		int GetBarHeight();

		void OnClientSizeChanging(Rectangle newRect);

		void Show();
		void Hide();

		void Refresh();
	}
}
