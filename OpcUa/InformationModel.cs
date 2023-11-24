//-----------------------------------------------------------------------------
// Copyright (c) 2017 by Bystronic Laser AG, CH-3362 Niederönz
//-----------------------------------------------------------------------------
// $LastChangedRevision: 726 $
// $LastChangedDate: 2018-02-12 13:27:10 +0100 (Mo., 12 Feb 2018) $
// $Author: Land $
//-----------------------------------------------------------------------------

#region Namespaces
using System;
using System.Collections.Generic;
using UnifiedAutomation.UaBase;

#endregion

namespace Bystronic.IoT.OpcUaClientHelper
{
	public sealed class InformationModel
	{
		#region Fields
		private readonly IDictionary<NodeId, NodeInfo> _nodeIdMap;
		private readonly IDictionary<string, NodeInfo> _variableNameMap;
		#endregion

		#region Constructors
		public InformationModel(NodeInfo objectsFolder, IDictionary<NodeId, NodeInfo> nodeIdMap)
		{
			if (objectsFolder == null)
				throw new ArgumentNullException(nameof(objectsFolder));
			if (nodeIdMap == null)
				throw new ArgumentNullException(nameof(nodeIdMap));

			ObjectsFolder = objectsFolder;
			_nodeIdMap = nodeIdMap;

			_variableNameMap = new Dictionary<string, NodeInfo>((int)(_nodeIdMap.Count * 1.5));
			BuildVariableNameMap();
		}
		#endregion

		#region Properties
		public NodeInfo ObjectsFolder { get; }
		public int TotalNodes => _nodeIdMap.Count;
		#endregion

		#region Public Methods
		public NodeInfo GetNodeInfo(NodeId nodeId)
		{
			NodeInfo nodeInfo;
			_nodeIdMap.TryGetValue(nodeId, out nodeInfo);
			return nodeInfo;
		}

		public NodeInfo GetNodeInfo(string variableName)
		{
			NodeInfo nodeInfo;
			_variableNameMap.TryGetValue(variableName, out nodeInfo);
			return nodeInfo;
		}
		#endregion

		#region Private Methods
		private void BuildVariableNameMap()
		{
			var nodeStack = new Stack<NodeInfo>();
			var nameStack = new Stack<string>();

			nodeStack.Push(ObjectsFolder);
			nameStack.Push(null);

			while (nodeStack.Count > 0)
			{
				var node = nodeStack.Pop();
				var name = nameStack.Pop();

				foreach (var child in node.Children)
				{
					var variableName = name != null ? name + "." + child.BrowseName.Name : child.BrowseName.Name;
					_variableNameMap[variableName] = child;
					if (child.Children.Count > 0)
					{
						nodeStack.Push(child);
						nameStack.Push(variableName);
					}
				}
			}
		}
		#endregion
	}
}