// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace ICSharpCode.TreeView
{
	public class GeneralAdorner : Adorner
	{
		public GeneralAdorner(UIElement target) : base(target) { }

		private FrameworkElement? child;

		public FrameworkElement? Child {
			get => child;
			set {
				if (child != value) {
					RemoveVisualChild(child);
					RemoveLogicalChild(child);
					child = value;
					AddLogicalChild(value);
					AddVisualChild(value);
					InvalidateMeasure();
				}
			}
		}

		protected override int VisualChildrenCount => (child != null) ? 1 : 0;

		protected override Visual? GetVisualChild(int index) => child;

		protected override Size MeasureOverride(Size constraint)
		{
			if (child != null) {
				child.Measure(constraint);
				return child.DesiredSize;
			}
			return default;
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			child?.Arrange(new Rect(finalSize));
			return finalSize;
		}
	}
}
