// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System.Windows;
using System.Windows.Controls;

namespace ICSharpCode.TreeView
{
	public class InsertMarker : Control
	{
		static InsertMarker()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(InsertMarker), new FrameworkPropertyMetadata(typeof(InsertMarker)));
		}
	}
}
