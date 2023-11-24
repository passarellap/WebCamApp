//-----------------------------------------------------------------------------
// Copyright (c) 2017 by Bystronic Laser AG, CH-3362 Niederönz
//-----------------------------------------------------------------------------
// $LastChangedRevision: 686 $
// $LastChangedDate: 2017-12-19 11:05:51 +0100 (Di., 19 Dez 2017) $
// $Author: Land $
//-----------------------------------------------------------------------------

#region Namespaces
using System;
using System.Collections.Generic;
using System.Linq;
using UnifiedAutomation.UaBase;
#endregion

namespace Bystronic.IoT.OpcUaClientHelper
{
	public sealed class NodeInfo
	{
		#region Constructors
		public NodeInfo(NodeId nodeId, ReferenceDescription description, IReadOnlyCollection<NodeInfo> children)
		{
			if (description == null)
				throw new ArgumentNullException(nameof(description));
			if (children == null)
				throw new ArgumentNullException(nameof(children));
			NodeId = nodeId;
			NodeClass = description.NodeClass;
			TypeDefinition = description.TypeDefinition;
			BrowseName = description.BrowseName;
			DisplayName = description.DisplayName;
			Children = children;
		}
		#endregion

		#region Properties
		public NodeId NodeId { get; }

		public NodeClass NodeClass { get; }

		public QualifiedName BrowseName { get; }

		public LocalizedText DisplayName { get; }

		public ExpandedNodeId TypeDefinition { get; }

		public object UserData { get; set; }

		public IReadOnlyCollection<NodeInfo> Children { get; }
		#endregion

		#region Public Methods
		public IList<NodeInfo> GetVariableNodes()
		{
			if (NodeClass == NodeClass.Variable)
			{
				return new List<NodeInfo> { this };
			}

			var variableNodes = new List<NodeInfo>();
			var nodeInfoStack = new Stack<NodeInfo>();

			if (Children.Count > 0)
			{
				nodeInfoStack.Push(this);
			}

			while (nodeInfoStack.Count > 0)
			{
				var browseInfo = nodeInfoStack.Pop();

				foreach (var child in browseInfo.Children)
				{
					if (child.NodeClass == NodeClass.Variable)
					{
						variableNodes.Add(child);
					}
					else if (child.Children.Count > 0)
					{
						nodeInfoStack.Push(child);
					}
				}
			}

			return variableNodes;
		}

		public IList<NodeId> GetVariableNodeIds()
		{
			return GetVariableNodes().Select(node => node.NodeId).ToList();
		}
		#endregion

		#region Overrides
		public override string ToString()
		{
			return BrowseName.Name;
		}
		#endregion
	}
}