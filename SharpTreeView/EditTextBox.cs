// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ICSharpCode.TreeView
{
	internal class EditTextBox : TextBox
	{
		static EditTextBox()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(EditTextBox), new FrameworkPropertyMetadata(typeof(EditTextBox)));
		}

		public EditTextBox(SharpTreeViewItem item)
		{
			Item = item;
			Loaded += delegate { Init(); };
		}

		public SharpTreeViewItem Item { get; }

		public SharpTreeNode Node => Item.Node;

		private void Init()
		{
			Text = Node.LoadEditText();
			Focus();
			SelectAll();
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.Key == Key.Enter) {
				Commit();
			} else if (e.Key == Key.Escape) {
				Node.IsEditing = false;
			}
		}

		protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
		{
			if (Node.IsEditing) {
				Commit();
			}
		}

		private bool committing;

		private void Commit()
		{
			if (!committing) {
				committing = true;

				Node.IsEditing = false;
				if (!Node.SaveEditText(Text)) {
					Item.Focus();
				}
				Node.RaisePropertyChanged(nameof(SharpTreeNode.Text));

				//if (Node.SaveEditText(Text)) {
				//    Node.IsEditing = false;
				//    Node.RaisePropertyChanged("Text");
				//}
				//else {
				//    Init();
				//}

				committing = false;
			}
		}
	}
}
