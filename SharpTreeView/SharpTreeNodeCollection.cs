// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;

namespace ICSharpCode.TreeView
{
	/// <summary>
	/// Collection that validates that inserted nodes do not have another parent.
	/// </summary>
	public sealed class SharpTreeNodeCollection : IList<SharpTreeNode>, INotifyCollectionChanged
	{
		private readonly SharpTreeNode parent;
		private List<SharpTreeNode> list = new List<SharpTreeNode>();
		private bool isRaisingEvent;

		public SharpTreeNodeCollection(SharpTreeNode parent)
		{
			this.parent = parent;
		}

		public event NotifyCollectionChangedEventHandler? CollectionChanged;

		private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			Debug.Assert(!isRaisingEvent);
			isRaisingEvent = true;
			try {
				parent.OnChildrenChanged(e);
				CollectionChanged?.Invoke(this, e);
			} finally {
				isRaisingEvent = false;
			}
		}

		private void ThrowOnReentrancy()
		{
			if (isRaisingEvent)
				throw new InvalidOperationException();
		}

		private void ThrowIfValueHasParent(SharpTreeNode node)
		{
			if (node.modelParent != null)
				throw new ArgumentException("The node already has a parent", nameof(node));
		}

		public SharpTreeNode this[int index] {
			get => list[index];
			set {
				ThrowOnReentrancy();
				var oldItem = list[index];
				if (oldItem != value) {
					ThrowIfValueHasParent(value);
					list[index] = value;
					OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, oldItem, index));
				}
			}
		}

		public int Count => list.Count;

		bool ICollection<SharpTreeNode>.IsReadOnly => false;

		public int IndexOf(SharpTreeNode node) => (node.modelParent == parent) ? list.IndexOf(node) : -1;

		public void Insert(int index, SharpTreeNode node)
		{
			ThrowOnReentrancy();
			ThrowIfValueHasParent(node);
			list.Insert(index, node);
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, node, index));
		}

		public void InsertRange(int index, IEnumerable<SharpTreeNode> nodes)
		{
			ThrowOnReentrancy();
			List<SharpTreeNode> newNodes = nodes.ToList();
			if (newNodes.Count != 0) {
				foreach (SharpTreeNode node in newNodes) {
					ThrowIfValueHasParent(node);
				}
				list.InsertRange(index, newNodes);
				OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newNodes, index));
			}
		}

		public void RemoveAt(int index)
		{
			ThrowOnReentrancy();
			var oldItem = list[index];
			list.RemoveAt(index);
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldItem, index));
		}

		public void RemoveRange(int index, int count)
		{
			ThrowOnReentrancy();
			if (count != 0) {
				var oldItems = list.GetRange(index, count);
				list.RemoveRange(index, count);
				OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldItems, index));
			}
		}

		public void Add(SharpTreeNode node)
		{
			ThrowOnReentrancy();
			ThrowIfValueHasParent(node);
			list.Add(node);
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, node, list.Count - 1));
		}

		public void AddRange(IEnumerable<SharpTreeNode> nodes)
		{
			InsertRange(this.Count, nodes);
		}

		public void Clear()
		{
			ThrowOnReentrancy();
			var oldList = list;
			list = new List<SharpTreeNode>();
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldList, 0));
		}

		public bool Contains(SharpTreeNode node) => IndexOf(node) >= 0;

		public void CopyTo(SharpTreeNode[] array, int arrayIndex) => list.CopyTo(array, arrayIndex);

		public bool Remove(SharpTreeNode item)
		{
			int pos = IndexOf(item);
			if (pos >= 0) {
				RemoveAt(pos);
				return true;
			} else {
				return false;
			}
		}

		public IEnumerator<SharpTreeNode> GetEnumerator() => list.GetEnumerator();

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => list.GetEnumerator();

		public void RemoveAll(Predicate<SharpTreeNode> match)
		{
			ThrowOnReentrancy();
			int firstToRemove = 0;
			for (int i = 0; i < list.Count; i++) {
				bool removeNode;
				isRaisingEvent = true;
				try {
					removeNode = match(list[i]);
				} finally {
					isRaisingEvent = false;
				}
				if (!removeNode) {
					if (firstToRemove < i) {
						RemoveRange(firstToRemove, i - firstToRemove);
						i = firstToRemove - 1;
					} else {
						firstToRemove = i + 1;
					}
					Debug.Assert(firstToRemove == i + 1);
				}
			}
			if (firstToRemove < list.Count) {
				RemoveRange(firstToRemove, list.Count - firstToRemove);
			}
		}
	}
}
