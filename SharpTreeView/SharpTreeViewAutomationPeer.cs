using System.Windows.Automation.Peers;

namespace ICSharpCode.TreeView
{
	internal class SharpTreeViewAutomationPeer : FrameworkElementAutomationPeer
	{
		internal SharpTreeViewAutomationPeer(SharpTreeView owner) : base(owner) { }
		//private SharpTreeView  SharpTreeView { get { return (SharpTreeView)base.Owner; } }
		protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Tree;
	}
}
