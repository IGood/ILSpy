﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;

namespace ICSharpCode.TreeView
{
	public class SharpTreeView : ListView
	{
		static SharpTreeView()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(SharpTreeView),
													 new FrameworkPropertyMetadata(typeof(SharpTreeView)));

			SelectionModeProperty.OverrideMetadata(typeof(SharpTreeView),
												   new FrameworkPropertyMetadata(SelectionMode.Extended));

			AlternationCountProperty.OverrideMetadata(typeof(SharpTreeView),
													  new FrameworkPropertyMetadata(2));

			DefaultItemContainerStyleKey = new ComponentResourceKey(typeof(SharpTreeView), "DefaultItemContainerStyleKey");

			VirtualizingStackPanel.VirtualizationModeProperty.OverrideMetadata(typeof(SharpTreeView),
																			   new FrameworkPropertyMetadata(VirtualizationMode.Recycling));

			RegisterCommands();
		}

		public static ResourceKey DefaultItemContainerStyleKey { get; }

		public SharpTreeView()
		{
			SetResourceReference(ItemContainerStyleProperty, DefaultItemContainerStyleKey);
		}

		public static readonly DependencyProperty RootProperty =
			DependencyProperty.Register(nameof(Root), typeof(SharpTreeNode), typeof(SharpTreeView));

		public SharpTreeNode Root {
			get => (SharpTreeNode)GetValue(RootProperty);
			set => SetValue(RootProperty, value);
		}

		public static readonly DependencyProperty ShowRootProperty =
			DependencyProperty.Register(nameof(ShowRoot), typeof(bool), typeof(SharpTreeView),
										new FrameworkPropertyMetadata(true));

		public bool ShowRoot {
			get => (bool)GetValue(ShowRootProperty);
			set => SetValue(ShowRootProperty, value);
		}

		public static readonly DependencyProperty ShowRootExpanderProperty =
			DependencyProperty.Register(nameof(ShowRootExpander), typeof(bool), typeof(SharpTreeView),
										new FrameworkPropertyMetadata(false));

		public bool ShowRootExpander {
			get => (bool)GetValue(ShowRootExpanderProperty);
			set => SetValue(ShowRootExpanderProperty, value);
		}

		public static readonly DependencyProperty AllowDropOrderProperty =
			DependencyProperty.Register(nameof(AllowDropOrder), typeof(bool), typeof(SharpTreeView));

		public bool AllowDropOrder {
			get => (bool)GetValue(AllowDropOrderProperty);
			set => SetValue(AllowDropOrderProperty, value);
		}

		public static readonly DependencyProperty ShowLinesProperty =
			DependencyProperty.Register(nameof(ShowLines), typeof(bool), typeof(SharpTreeView),
										new FrameworkPropertyMetadata(true));

		public bool ShowLines {
			get => (bool)GetValue(ShowLinesProperty);
			set => SetValue(ShowLinesProperty, value);
		}

		public static bool GetShowAlternation(DependencyObject obj) => (bool)obj.GetValue(ShowAlternationProperty);

		public static void SetShowAlternation(DependencyObject obj, bool value) => obj.SetValue(ShowAlternationProperty, value);

		public static readonly DependencyProperty ShowAlternationProperty =
			DependencyProperty.RegisterAttached("ShowAlternation", typeof(bool), typeof(SharpTreeView),
												new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			if (e.Property == RootProperty ||
				e.Property == ShowRootProperty ||
				e.Property == ShowRootExpanderProperty) {
				Reload();
			}
		}

		private TreeFlattener? flattener;
		private bool updatesLocked;

		public IDisposable LockUpdates() => new UpdateLock(this);

		private class UpdateLock : IDisposable
		{
			private readonly SharpTreeView instance;

			public UpdateLock(SharpTreeView instance)
			{
				this.instance = instance;
				this.instance.updatesLocked = true;
			}

			public void Dispose() => this.instance.updatesLocked = false;
		}

		private void Reload()
		{
			flattener?.Stop();
			if (Root != null) {
				if (!(ShowRoot && ShowRootExpander)) {
					Root.IsExpanded = true;
				}
				flattener = new TreeFlattener(Root, ShowRoot);
				flattener.CollectionChanged += flattener_CollectionChanged;
				this.ItemsSource = flattener;
			}
		}

		private void flattener_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			// Deselect nodes that are being hidden, if any remain in the tree
			if (!updatesLocked && e.Action == NotifyCollectionChangedAction.Remove && Items.Count > 0) {
				var selectedOldItems = e.OldItems.Cast<SharpTreeNode>().Where(node => node.IsSelected).ToList();
				if (selectedOldItems.Count > 0) {
					var list = SelectedItems.Cast<SharpTreeNode>().Except(selectedOldItems).ToList();
					UpdateFocusedNode(list, Math.Max(0, e.OldStartingIndex - 1));
				}
			}
		}

		private void UpdateFocusedNode(List<SharpTreeNode>? newSelection, int topSelectedIndex)
		{
			if (updatesLocked) return;
			SetSelectedItems(newSelection ?? Enumerable.Empty<SharpTreeNode>());
			if (SelectedItem == null) {
				// if we removed all selected nodes, then move the focus to the node 
				// preceding the first of the old selected nodes
				SelectedIndex = topSelectedIndex;
				if (SelectedItem != null)
					FocusNode((SharpTreeNode)SelectedItem);
			}
		}

		protected override DependencyObject GetContainerForItemOverride() => new SharpTreeViewItem();

		protected override bool IsItemItsOwnContainerOverride(object item) => item is SharpTreeViewItem;

		protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
		{
			base.PrepareContainerForItemOverride(element, item);
			SharpTreeViewItem container = (SharpTreeViewItem)element;
			container.ParentTreeView = this;
			// Make sure that the line renderer takes into account the new bound data
			container.NodeView?.LinesRenderer.InvalidateVisual();
		}

		private bool doNotScrollOnExpanding;

		/// <summary>
		/// Handles the node expanding event in the tree view.
		/// This method gets called only if the node is in the visible region (a SharpTreeNodeView exists).
		/// </summary>
		internal void HandleExpanding(SharpTreeNode node)
		{
			if (doNotScrollOnExpanding)
				return;
			SharpTreeNode lastVisibleChild = node;
			while (true) {
				SharpTreeNode? tmp = lastVisibleChild.Children.LastOrDefault(c => c.IsVisible);
				if (tmp != null) {
					lastVisibleChild = tmp;
				} else {
					break;
				}
			}
			if (lastVisibleChild != node) {
				// Make the the expanded children are visible; but don't scroll down
				// too much (keep node itself visible)
				base.ScrollIntoView(lastVisibleChild);
				// For some reason, this only works properly when delaying it...
				Dispatcher.InvokeAsync(() => base.ScrollIntoView(node), DispatcherPriority.Loaded);
			}
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			SharpTreeViewItem? container = e.OriginalSource as SharpTreeViewItem;
			switch (e.Key) {
				case Key.Left:
					if (container != null && ItemsControl.ItemsControlFromItemContainer(container) == this) {
						if (container.Node.IsExpanded) {
							container.Node.IsExpanded = false;
						} else if (container.Node.Parent != null) {
							this.FocusNode(container.Node.Parent);
						}
						e.Handled = true;
					}
					break;
				case Key.Right:
					if (container != null && ItemsControl.ItemsControlFromItemContainer(container) == this) {
						if (!container.Node.IsExpanded && container.Node.ShowExpander) {
							container.Node.IsExpanded = true;
						} else if (container.Node.Children.Count > 0) {
							// jump to first child:
							container.MoveFocus(new TraversalRequest(FocusNavigationDirection.Down));
						}
						e.Handled = true;
					}
					break;
				case Key.Return:
				case Key.Space:
					if (container != null && Keyboard.Modifiers == ModifierKeys.None && this.SelectedItems.Count == 1 && this.SelectedItem == container.Node) {
						container.Node.ActivateItem(e);
					}
					break;
				case Key.Add:
					if (container != null && ItemsControl.ItemsControlFromItemContainer(container) == this) {
						container.Node.IsExpanded = true;
						e.Handled = true;
					}
					break;
				case Key.Subtract:
					if (container != null && ItemsControl.ItemsControlFromItemContainer(container) == this) {
						container.Node.IsExpanded = false;
						e.Handled = true;
					}
					break;
				case Key.Multiply:
					if (container != null && ItemsControl.ItemsControlFromItemContainer(container) == this) {
						container.Node.IsExpanded = true;
						ExpandRecursively(container.Node);
						e.Handled = true;
					}
					break;
				case Key.Back:
					if (IsTextSearchEnabled) {
						var instance = SharpTreeViewTextSearch.GetInstance(this);
						instance.RevertLastCharacter();
						e.Handled = true;
					}
					break;
			}
			if (!e.Handled)
				base.OnKeyDown(e);
		}

		protected override void OnTextInput(TextCompositionEventArgs e)
		{
			if (!string.IsNullOrEmpty(e.Text) && IsTextSearchEnabled && (e.OriginalSource == this || ItemsControl.ItemsControlFromItemContainer(e.OriginalSource as DependencyObject) == this)) {
				var instance = SharpTreeViewTextSearch.GetInstance(this);
				instance.Search(e.Text);
				e.Handled = true;
			} else {
				base.OnTextInput(e);
			}
		}

		private void ExpandRecursively(SharpTreeNode node)
		{
			if (node.CanExpandRecursively) {
				node.IsExpanded = true;
				foreach (SharpTreeNode child in node.Children) {
					ExpandRecursively(child);
				}
			}
		}

		/// <summary>
		/// Scrolls the specified node in view and sets keyboard focus on it.
		/// </summary>
		public void FocusNode(SharpTreeNode node)
		{
			ScrollIntoView(node);
			// WPF's ScrollIntoView() uses the same if/dispatcher construct, so we call OnFocusItem() after the item was brought into view.
			if (this.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated) {
				TryFocusContainer();
			} else {
				this.Dispatcher.InvokeAsync(TryFocusContainer, DispatcherPriority.Loaded);
			}

			void TryFocusContainer()
			{
				if (this.ItemContainerGenerator.ContainerFromItem(node) is FrameworkElement element)
					element.Focus();
			}
		}

		public void ScrollIntoView(SharpTreeNode node)
		{
			doNotScrollOnExpanding = true;
			foreach (SharpTreeNode ancestor in node.Ancestors())
				ancestor.IsExpanded = true;
			doNotScrollOnExpanding = false;
			base.ScrollIntoView(node);
		}

		protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer() => new SharpTreeViewAutomationPeer(this);

		#region Track selection

		protected override void OnSelectionChanged(SelectionChangedEventArgs e)
		{
			foreach (SharpTreeNode? node in e.RemovedItems) {
				node!.IsSelected = false;
			}
			foreach (SharpTreeNode? node in e.AddedItems) {
				node!.IsSelected = true;
			}
			base.OnSelectionChanged(e);
		}

		#endregion

		#region Drag and Drop
		protected override void OnDragEnter(DragEventArgs e) => OnDragOver(e);

		protected override void OnDragOver(DragEventArgs e)
		{
			e.Effects = DragDropEffects.None;

			if (Root != null && !ShowRoot) {
				e.Handled = true;
				Root.CanDrop(e, Root.Children.Count);
			}
		}

		protected override void OnDrop(DragEventArgs e)
		{
			e.Effects = DragDropEffects.None;

			if (Root != null && !ShowRoot) {
				e.Handled = true;
				Root.InternalDrop(e, Root.Children.Count);
			}
		}

		internal void HandleDragEnter(SharpTreeViewItem item, DragEventArgs e) => HandleDragOver(item, e);

		internal void HandleDragOver(SharpTreeViewItem item, DragEventArgs e)
		{
			HidePreview();

			var target = GetDropTarget(item, e);
			if (target != null) {
				e.Handled = true;
				ShowPreview(target.Item, target.Place);
			}
		}

		internal void HandleDrop(SharpTreeViewItem item, DragEventArgs e)
		{
			try {
				HidePreview();

				var target = GetDropTarget(item, e);
				if (target != null) {
					e.Handled = true;
					target.Node.InternalDrop(e, target.Index);
				}
			} catch (Exception ex) {
				Debug.WriteLine(ex);
				throw;
			}
		}

		internal void HandleDragLeave(SharpTreeViewItem item, DragEventArgs e)
		{
			HidePreview();
			e.Handled = true;
		}

		private class DropTarget
		{
			public DropTarget(SharpTreeViewItem item, DropPlace place, SharpTreeNode node, int index)
			{
				Item = item;
				Place = place;
				Node = node;
				Index = index;
			}

			public readonly SharpTreeViewItem Item;
			public readonly DropPlace Place;
			public double Y;
			public readonly SharpTreeNode Node;
			public readonly int Index;
		}

		private DropTarget? GetDropTarget(SharpTreeViewItem item, DragEventArgs e)
		{
			double y = e.GetPosition(item).Y;
			return BuildDropTargets(item, e).FirstOrDefault(target => target.Y >= y);
		}

		private List<DropTarget> BuildDropTargets(SharpTreeViewItem item, DragEventArgs e)
		{
			var result = new List<DropTarget>();
			var node = item.Node;

			if (AllowDropOrder) {
				TryAddDropTarget(result, item, DropPlace.Before, e);
			}

			TryAddDropTarget(result, item, DropPlace.Inside, e);

			if (AllowDropOrder) {
				if (node.IsExpanded && node.Children.Count > 0) {
					var firstChildItem = (SharpTreeViewItem)ItemContainerGenerator.ContainerFromItem(node.Children[0]);
					TryAddDropTarget(result, firstChildItem, DropPlace.Before, e);
				} else {
					TryAddDropTarget(result, item, DropPlace.After, e);
				}
			}

			double h = item.ActualHeight;
			double y1 = 0.2 * h;
			double y2 = h / 2;
			double y3 = h - y1;

			if (result.Count == 2) {
				if (result[0].Place == DropPlace.Inside &&
					result[1].Place != DropPlace.Inside) {
					result[0].Y = y3;
				} else if (result[0].Place != DropPlace.Inside &&
						   result[1].Place == DropPlace.Inside) {
					result[0].Y = y1;
				} else {
					result[0].Y = y2;
				}
			} else if (result.Count == 3) {
				result[0].Y = y1;
				result[1].Y = y3;
			}
			if (result.Count > 0) {
				result[result.Count - 1].Y = h;
			}
			return result;
		}

		private void TryAddDropTarget(List<DropTarget> targets, SharpTreeViewItem item, DropPlace place, DragEventArgs e)
		{
			GetNodeAndIndex(item, place, out SharpTreeNode? node, out int index);
			if (node != null) {
				e.Effects = DragDropEffects.None;
				if (node.CanDrop(e, index)) {
					targets.Add(new DropTarget(item, place, node, index));
				}
			}
		}

		private void GetNodeAndIndex(SharpTreeViewItem item, DropPlace place, out SharpTreeNode? node, out int index)
		{
			node = null;
			index = 0;

			if (place == DropPlace.Inside) {
				node = item.Node;
				index = node.Children.Count;
			} else if (place == DropPlace.Before) {
				if (item.Node.Parent != null) {
					node = item.Node.Parent;
					index = node.Children.IndexOf(item.Node);
				}
			} else {
				if (item.Node.Parent != null) {
					node = item.Node.Parent;
					index = node.Children.IndexOf(item.Node) + 1;
				}
			}
		}

		private SharpTreeNodeView? previewNodeView;
		private InsertMarker? insertMarker;
		private DropPlace previewPlace;

		private enum DropPlace
		{
			Before, Inside, After
		}

		private void ShowPreview(SharpTreeViewItem item, DropPlace place)
		{
			previewNodeView = item.NodeView;
			previewPlace = place;

			if (place == DropPlace.Inside) {
				previewNodeView.TextBackground = SystemColors.HighlightBrush;
				previewNodeView.Foreground = SystemColors.HighlightTextBrush;
			} else {
				if (insertMarker == null) {
					var adornerLayer = AdornerLayer.GetAdornerLayer(this);
					var adorner = new GeneralAdorner(this);
					insertMarker = new InsertMarker();
					adorner.Child = insertMarker;
					adornerLayer.Add(adorner);
				}

				insertMarker.Visibility = Visibility.Visible;

				var p1 = previewNodeView.TransformToVisual(this).Transform(new Point());
				var p = new Point(p1.X + previewNodeView.CalculateIndent() + 4.5, p1.Y - 3);

				if (place == DropPlace.After) {
					p.Y += previewNodeView.ActualHeight;
				}

				insertMarker.Margin = new Thickness(p.X, p.Y, 0, 0);

				SharpTreeNodeView? secondNodeView = null;
				int index = flattener!.IndexOf(item.Node);

				if (place == DropPlace.Before) {
					if (index > 0) {
						secondNodeView = ((SharpTreeViewItem)ItemContainerGenerator.ContainerFromIndex(index - 1)).NodeView;
					}
				} else if (index + 1 < flattener.Count) {
					secondNodeView = ((SharpTreeViewItem)ItemContainerGenerator.ContainerFromIndex(index + 1)).NodeView;
				}

				double w = p1.X + previewNodeView.ActualWidth - p.X;

				if (secondNodeView != null) {
					var p2 = secondNodeView.TransformToVisual(this).Transform(new Point());
					w = Math.Max(w, p2.X + secondNodeView.ActualWidth - p.X);
				}

				insertMarker.Width = w + 10;
			}
		}

		private void HidePreview()
		{
			if (previewNodeView != null) {
				previewNodeView.ClearValue(SharpTreeNodeView.TextBackgroundProperty);
				previewNodeView.ClearValue(SharpTreeNodeView.ForegroundProperty);
				if (insertMarker != null) {
					insertMarker.Visibility = Visibility.Collapsed;
				}
				previewNodeView = null;
			}
		}
		#endregion

		#region Cut / Copy / Paste / Delete Commands

		private static void RegisterCommands()
		{
			CommandManager.RegisterClassCommandBinding(typeof(SharpTreeView),
													   new CommandBinding(ApplicationCommands.Cut, HandleExecuted_Cut, HandleCanExecute_Cut));

			CommandManager.RegisterClassCommandBinding(typeof(SharpTreeView),
													   new CommandBinding(ApplicationCommands.Copy, HandleExecuted_Copy, HandleCanExecute_Copy));

			CommandManager.RegisterClassCommandBinding(typeof(SharpTreeView),
													   new CommandBinding(ApplicationCommands.Paste, HandleExecuted_Paste, HandleCanExecute_Paste));

			CommandManager.RegisterClassCommandBinding(typeof(SharpTreeView),
													   new CommandBinding(ApplicationCommands.Delete, HandleExecuted_Delete, HandleCanExecute_Delete));
		}

		private static void HandleExecuted_Cut(object sender, ExecutedRoutedEventArgs e) { }

		private static void HandleCanExecute_Cut(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = false;

		private static void HandleExecuted_Copy(object sender, ExecutedRoutedEventArgs e) { }

		private static void HandleCanExecute_Copy(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = false;

		private static void HandleExecuted_Paste(object sender, ExecutedRoutedEventArgs e) { }

		private static void HandleCanExecute_Paste(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = false;

		private static void HandleExecuted_Delete(object sender, ExecutedRoutedEventArgs e)
		{
			SharpTreeView treeView = (SharpTreeView)sender;
			treeView.updatesLocked = true;
			int selectedIndex = -1;
			try {
				foreach (SharpTreeNode node in treeView.GetTopLevelSelection().ToArray()) {
					if (selectedIndex == -1)
						selectedIndex = treeView.flattener!.IndexOf(node);
					node.Delete();
				}
			} finally {
				treeView.updatesLocked = false;
				treeView.UpdateFocusedNode(null, Math.Max(0, selectedIndex - 1));
			}
		}

		private static void HandleCanExecute_Delete(object sender, CanExecuteRoutedEventArgs e)
		{
			SharpTreeView treeView = (SharpTreeView)sender;
			e.CanExecute = treeView.GetTopLevelSelection().All(node => node.CanDelete());
		}

		/// <summary>
		/// Gets the selected items which do not have any of their ancestors selected.
		/// </summary>
		public IEnumerable<SharpTreeNode> GetTopLevelSelection()
		{
			var selection = this.SelectedItems.Cast<SharpTreeNode>();
			var selectionHash = new HashSet<SharpTreeNode>(selection);
			return selection.Where(item => item.Ancestors().All(a => !selectionHash.Contains(a)));
		}

		#endregion

		public void SetSelectedNodes(IEnumerable<SharpTreeNode> nodes) => this.SetSelectedItems(nodes.ToList());
	}
}
