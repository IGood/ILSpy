// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace ICSharpCode.TreeView
{
	[TemplatePart(Name = PART_Spacer, Type = typeof(FrameworkElement))]
	[TemplatePart(Name = PART_LinesRenderer, Type = typeof(LinesRenderer))]
	[TemplatePart(Name = PART_Expander, Type = typeof(ToggleButton))]
	[TemplatePart(Name = PART_TextEditorContainer, Type = typeof(Border))]
	public class SharpTreeNodeView : Control
	{
		private const string PART_Spacer = nameof(PART_Spacer);
		private const string PART_LinesRenderer = nameof(PART_LinesRenderer);
		private const string PART_Expander = nameof(PART_Expander);
		private const string PART_TextEditorContainer = nameof(PART_TextEditorContainer);

		static SharpTreeNodeView()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(SharpTreeNodeView), new FrameworkPropertyMetadata(typeof(SharpTreeNodeView)));
		}

		public static readonly DependencyProperty TextBackgroundProperty =
			DependencyProperty.Register(nameof(TextBackground), typeof(Brush), typeof(SharpTreeNodeView));

		public Brush TextBackground {
			get => (Brush)GetValue(TextBackgroundProperty);
			set => SetValue(TextBackgroundProperty, value);
		}

		public SharpTreeNode Node => (SharpTreeNode)DataContext;

		public SharpTreeViewItem ParentItem { get; private set; }

		public static readonly DependencyProperty CellEditorProperty =
			DependencyProperty.Register(nameof(CellEditor), typeof(Control), typeof(SharpTreeNodeView), new FrameworkPropertyMetadata());

		public Control CellEditor {
			get => (Control)GetValue(CellEditorProperty);
			set => SetValue(CellEditorProperty, value);
		}

		public SharpTreeView ParentTreeView => ParentItem.ParentTreeView;

		internal LinesRenderer LinesRenderer { get; private set; }

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			LinesRenderer = (LinesRenderer)GetTemplateChild(PART_LinesRenderer);
			UpdateTemplate();
		}

		protected override void OnVisualParentChanged(DependencyObject oldParent)
		{
			base.OnVisualParentChanged(oldParent);
			ParentItem = this.FindAncestor<SharpTreeViewItem>()!;
			ParentItem.NodeView = this;
		}

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			if (e.Property == DataContextProperty) {
				UpdateDataContext(e.OldValue as SharpTreeNode, e.NewValue as SharpTreeNode);
			}
		}

		private void UpdateDataContext(SharpTreeNode? oldNode, SharpTreeNode? newNode)
		{
			if (newNode != null) {
				newNode.PropertyChanged += Node_PropertyChanged;
				if (Template != null) {
					UpdateTemplate();
				}
			}
			if (oldNode != null) {
				oldNode.PropertyChanged -= Node_PropertyChanged;
			}
		}

		private void Node_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(SharpTreeNode.IsEditing)) {
				OnIsEditingChanged();
			} else if (e.PropertyName == nameof(SharpTreeNode.IsLast)) {
				if (ParentTreeView.ShowLines) {
					foreach (var child in Node.VisibleDescendantsAndSelf()) {
						var container = ParentTreeView.ItemContainerGenerator.ContainerFromItem(child) as SharpTreeViewItem;
						container?.NodeView?.LinesRenderer.InvalidateVisual();
					}
				}
			} else if (e.PropertyName == nameof(SharpTreeNode.IsExpanded)) {
				if (Node.IsExpanded)
					ParentTreeView.HandleExpanding(Node);
			}
		}

		private void OnIsEditingChanged()
		{
			var textEditorContainer = (Border)GetTemplateChild(PART_TextEditorContainer);
			if (Node.IsEditing) {
				if (CellEditor == null)
					textEditorContainer.Child = new EditTextBox(ParentItem);
				else
					textEditorContainer.Child = CellEditor;
			} else {
				textEditorContainer.Child = null;
			}
		}

		private void UpdateTemplate()
		{
			var spacer = (FrameworkElement)GetTemplateChild(PART_Spacer);
			spacer.Width = CalculateIndent();

			var expander = (ToggleButton)GetTemplateChild(PART_Expander);
			if (ParentTreeView.Root == Node && !ParentTreeView.ShowRootExpander) {
				expander.Visibility = Visibility.Collapsed;
			} else {
				expander.ClearValue(VisibilityProperty);
			}
		}

		internal double CalculateIndent()
		{
			int result = 19 * Node.Level;
			if (ParentTreeView.ShowRoot) {
				if (!ParentTreeView.ShowRootExpander) {
					if (ParentTreeView.Root != Node) {
						result -= 15;
					}
				}
			} else {
				result -= 19;
			}
			if (result < 0) {
				Debug.WriteLine("SharpTreeNodeView.CalculateIndent() on node without correctly-set level");
				return 0;
			}
			return result;
		}
	}
}
