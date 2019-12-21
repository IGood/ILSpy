using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace ICSharpCode.TreeView
{
	internal class SharpTreeViewItemAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider
	{
		internal SharpTreeViewItemAutomationPeer(SharpTreeViewItem owner)
			: base(owner)
		{
			SharpTreeViewItem.DataContextChanged += OnDataContextChanged;
			if (SharpTreeViewItem.DataContext is SharpTreeNode node) node.PropertyChanged += OnPropertyChanged;
		}

		private SharpTreeViewItem SharpTreeViewItem => (SharpTreeViewItem)base.Owner;

		protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.TreeItem;

		public override object GetPattern(PatternInterface patternInterface) => (patternInterface == PatternInterface.ExpandCollapse) ? this : base.GetPattern(patternInterface);

		public void Collapse() { }

		public void Expand() { }

		public ExpandCollapseState ExpandCollapseState {
			get {

				if (SharpTreeViewItem.DataContext is SharpTreeNode node && node.ShowExpander)
					return node.IsExpanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
				return ExpandCollapseState.LeafNode;
			}
		}

		private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName != nameof(SharpTreeNode.IsExpanded)) return;
			SharpTreeNode node = (SharpTreeNode)sender;
			if (node.Children.Count == 0) return;
			bool newValue = node.IsExpanded;
			bool oldValue = !newValue;
			RaisePropertyChangedEvent(
				ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
				oldValue ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed,
				newValue ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);
		}

		private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (e.OldValue is SharpTreeNode oldNode)
				oldNode.PropertyChanged -= OnPropertyChanged;
			if (e.NewValue is SharpTreeNode newNode)
				newNode.PropertyChanged += OnPropertyChanged;
		}
	}
}
