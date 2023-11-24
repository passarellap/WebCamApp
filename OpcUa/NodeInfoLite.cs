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
	public sealed class NodeInfoLite
	{
		#region Constructors
		public NodeInfoLite(NodeId nodeId)//, ReferenceDescription description, IReadOnlyCollection<NodeInfo> children)
		{			

			NodeId = nodeId;
			this.Children = new List<NodeInfoLite>();
		
		}
		#endregion

		#region Properties
		public NodeId NodeId { get; }

		public NodeClass NodeClass { get; set; }

		public QualifiedName BrowseName { get; set; }

		public LocalizedText DisplayName { get; set; }

		//public ExpandedNodeId TypeDefinition { get; }

		public object UserData { get; set; }

		public IList<NodeInfoLite> Children { get; }
		#endregion


		#region Public methods


		public IList<NodeInfoLite> GetVariableNodes()
		{
			if (NodeClass == NodeClass.Variable)
			{
				return new List<NodeInfoLite> { this };
			}

			var variableNodes = new List<NodeInfoLite>();
			var nodeInfoStack = new Stack<NodeInfoLite>();

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


	}
}