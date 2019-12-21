// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System.Windows;
using System.Windows.Media;

namespace ICSharpCode.TreeView
{
	internal static class ExtensionMethods
	{
		public static T? FindAncestor<T>(this DependencyObject d) where T : DependencyObject
		{
			do {
				d = VisualTreeHelper.GetParent(d);
				if (d is T casted) {
					return casted;
				}
			} while (d != null);
			return null;
		}
	}
}
