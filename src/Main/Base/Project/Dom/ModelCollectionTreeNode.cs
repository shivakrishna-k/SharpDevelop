﻿// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.Utils;
using ICSharpCode.TreeView;

namespace ICSharpCode.SharpDevelop.Dom
{
	public abstract class ModelCollectionTreeNode : SharpTreeNode
	{
		protected static readonly IComparer<SharpTreeNode> NodeTextComparer = KeyComparer.Create((SharpTreeNode n) => n.Text.ToString(), StringComparer.OrdinalIgnoreCase, StringComparer.OrdinalIgnoreCase);
		
		protected ModelCollectionTreeNode()
		{
			this.LazyLoading = true;
		}
		
		protected abstract IModelCollection<object> ModelChildren { get; }
		protected abstract IComparer<SharpTreeNode> NodeComparer { get; }
		
		protected virtual bool IsSpecialNode()
		{
			return false;
		}
		
		protected virtual void InsertSpecialNodes()
		{
			throw new NotSupportedException("this node is not a special node!");
		}
		
		protected virtual void DetachEventHandlers()
		{
			// If children loaded, also detach the collection change event handler
			if (!LazyLoading) {
				ModelChildren.CollectionChanged -= ModelChildrenCollectionChanged;
			}
		}
		
		protected override void OnIsVisibleChanged()
		{
			base.OnIsVisibleChanged();
			
			if (IsVisible) {
				if (!LazyLoading) {
					ModelChildren.CollectionChanged += ModelChildrenCollectionChanged;
					SynchronizeModelChildren();
				}
			} else {
				ModelChildren.CollectionChanged -= ModelChildrenCollectionChanged;
			}
		}
		
		#region Manage Children
		protected override void LoadChildren()
		{
			Children.Clear();
			if (IsSpecialNode())
				InsertSpecialNodes();
			InsertChildren(ModelChildren);
			ModelChildren.CollectionChanged += ModelChildrenCollectionChanged;
		}
		
		protected void InsertChildren(IEnumerable children)
		{
			foreach (object child in children) {
				var treeNode = SD.TreeNodeFactory.CreateTreeNode(child);
				if (treeNode != null)
					Children.OrderedInsert(treeNode, NodeComparer);
			}
		}
		
		void SynchronizeModelChildren()
		{
			HashSet<object> set = new HashSet<object>(ModelChildren);
			Children.RemoveAll(n => !set.Contains(n.Model));
			set.ExceptWith(Children.Select(n => n.Model));
			InsertChildren(set);
		}
		
		void ModelChildrenCollectionChanged(IReadOnlyCollection<object> removedItems, IReadOnlyCollection<object> addedItems)
		{
			if (!IsVisible) {
				SwitchBackToLazyLoading();
				return;
			}
			Children.RemoveAll(n => removedItems.Contains(n.Model));
			InsertChildren(addedItems);
		}
		
		void SwitchBackToLazyLoading()
		{
			ModelChildren.CollectionChanged -= ModelChildrenCollectionChanged;
			Children.Clear();
			LazyLoading = true;
		}
		#endregion
	}
	
	public class WaitForSolutionLoadedTreeNode : SharpTreeNode
	{
		public WaitForSolutionLoadedTreeNode()
		{
			this.LazyLoading = false;
		}
		
		public override object Text {
			get {
				return "Waiting for solution load to be completed...";
			}
		}
	}
}
