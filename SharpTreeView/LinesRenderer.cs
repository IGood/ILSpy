// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace ICSharpCode.TreeView
{
	internal class LinesRenderer : FrameworkElement
	{
		static LinesRenderer()
		{
			pen = new Pen(Brushes.LightGray, 1);
			pen.Freeze();
		}

		private static readonly Pen pen;

		protected override void OnRender(DrawingContext dc)
		{
			var nodeView = TemplatedParent as SharpTreeNodeView;
			SharpTreeNode? node = nodeView?.Node;
			if (node == null) {
				// This seems to happen sometimes with DataContext==DisconnectedItem,
				// though I'm not sure why WPF would call OnRender() on a disconnected node
				Debug.WriteLine($"LinesRenderer.OnRender() called with DataContext={nodeView?.DataContext}");
				return;
			}
			double indent = nodeView!.CalculateIndent();
			var p = new Point(indent + 4.5, 0);

			if (!node.IsRoot || nodeView.ParentTreeView.ShowRootExpander) {
				dc.DrawLine(pen, new Point(p.X, ActualHeight / 2), new Point(p.X + 10, ActualHeight / 2));
			}

			if (node.IsRoot) return;

			if (!node.IsLast) {
				dc.DrawLine(pen, p, new Point(p.X, ActualHeight));
			} else {
				dc.DrawLine(pen, p, new Point(p.X, ActualHeight / 2));
			}
			var current = node;
			while (true) {
				p.X -= 19;
				if (p.X < 0) break;
				current = current.Parent!;
				if (!current.IsLast) {
					dc.DrawLine(pen, p, new Point(p.X, ActualHeight));
				}
			}
		}
	}
}
