// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System.Windows;
using System.Windows.Controls;

namespace ICSharpCode.TreeView
{
	public class SharpGridView : GridView
	{
		public static ResourceKey ItemContainerStyleKey { get; } = new ComponentResourceKey(typeof(SharpTreeView), "GridViewItemContainerStyleKey");

		protected override object ItemContainerDefaultStyleKey => ItemContainerStyleKey;
	}
}
