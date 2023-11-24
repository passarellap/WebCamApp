//-----------------------------------------------------------------------------
// Copyright (c) 2017 by Bystronic Laser AG, CH-3362 Niederönz
//-----------------------------------------------------------------------------
// $LastChangedRevision: 845 $
// $LastChangedDate: 2018-03-16 16:45:56 +0100 (Fr., 16 Mär 2018) $
// $Author: Land $
//-----------------------------------------------------------------------------

#region Namespaces
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using UnifiedAutomation.UaBase;
using UnifiedAutomation.UaClient;
#endregion

namespace Bystronic.IoT.OpcUaClientHelper
{
	public static class OpcUaClientExtensions
	{
		#region Public Methods
		public static InformationModel LoadInformationModel(this OpcUaClient opcUaClient)
		{
			opcUaClient.EnsureConnected();


			List<string> objectsToBeParsed = new List<string>();
			objectsToBeParsed.Add("PLC");


			if (opcUaClient == null)
				throw new ArgumentNullException(nameof(opcUaClient));

			opcUaClient.EnsureConnected();

			byte[] continuationPoint;
			var rootFolderDescriptions = opcUaClient.Session.Browse(ObjectIds.RootFolder, out continuationPoint);
			var objectsFolderDescription = rootFolderDescriptions.First(d => d.NodeId == ObjectIds.ObjectsFolder);

			var nodeIdentifierMap = new Dictionary<NodeId, NodeInfo>();
			var objectsFolderChildren = LoadNodeInfos(opcUaClient.Session, ObjectIds.ObjectsFolder, nodeIdentifierMap);

			var objectsFolderNodeId = objectsFolderDescription.NodeId.ToNodeId(opcUaClient.Session.NamespaceUris);
			var objectsFolderInfo = new NodeInfo(objectsFolderNodeId, objectsFolderDescription, objectsFolderChildren);

			nodeIdentifierMap.Add(objectsFolderNodeId, objectsFolderInfo);

			return new InformationModel(objectsFolderInfo, nodeIdentifierMap);
		}


		public static NodeInfoLite ReadNodeInfoLite(this OpcUaClient opcUaClient, NodeId nodeId, bool writeUserData = false)
		{
			NodeInfoLite nodeInfoLite = ReadNodeInfoLiteFather(opcUaClient, nodeId, writeUserData);

			//Children

			var nodeInfos = new List<NodeInfo>();

			var browseContext = new BrowseContext
			{
				BrowseDirection = BrowseDirection.Forward,
				ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
				IncludeSubtypes = true,
				NodeClassMask = (int)NodeClass.Object | (int)NodeClass.Variable,
				ResultMask = (uint)BrowseResultMask.All
			};

			List<ReferenceDescription> descriptions;

			byte[] continuationPoint;
			descriptions = opcUaClient.Session.Browse(nodeId, browseContext, out continuationPoint);


			foreach (var description in descriptions.Where(d => d.ReferenceTypeId != ReferenceTypeIds.HasNotifier))
			{

				var node = description.NodeId.ToNodeId(opcUaClient.Session.NamespaceUris);
				if (description.NodeId.Identifier.ToString().Contains("#"+ description.DisplayName.Text))
					nodeInfoLite.Children.Add(ReadNodeInfoLite(opcUaClient, node, false));
				else
					nodeInfoLite.Children.Add(ReadNodeInfoLite(opcUaClient, node, true));
			}


			return nodeInfoLite;
		}



		public static NodeInfoLite ReadNodeInfoLiteFather(this OpcUaClient opcUaClient, NodeId nodeId, bool writeUserData = false)
		{
						
			ReadValueIdCollection nodesToRead = new ReadValueIdCollection();

			ReadValueId readValueId = new ReadValueId()
			{
				NodeId = nodeId,
				AttributeId = Attributes.NodeClass
			};
			nodesToRead.Add(readValueId);


			readValueId = new ReadValueId()
			{
				NodeId = nodeId,
				AttributeId = Attributes.DataType
			};
			nodesToRead.Add(readValueId);

			readValueId = new ReadValueId()
			{
				NodeId = nodeId,
				AttributeId = Attributes.Value
			};
			nodesToRead.Add(readValueId);

			readValueId = new ReadValueId()
			{
				NodeId = nodeId,
				AttributeId = Attributes.BrowseName
			};
			nodesToRead.Add(readValueId);

			readValueId = new ReadValueId()
			{
				NodeId = nodeId,
				AttributeId = Attributes.DisplayName
			};
			nodesToRead.Add(readValueId);

			List<DataValue> results = opcUaClient.Session.Read(nodesToRead);


			NodeClass nodeClass = (NodeClass)results[0].Value;

			NodeInfoLite nodeInfoLite = new NodeInfoLite(nodeId);
			nodeInfoLite.NodeClass = nodeClass;
			
			nodeInfoLite.BrowseName = results[3].Value.ToString();
			nodeInfoLite.DisplayName = results[4].Value.ToString();

			if (writeUserData)
			{

				VariableInfo variableInfo = new VariableInfo();

				var dataTypeId = results[1].WrappedValue.ToNodeId();
				var builtinType = TypeUtils.GetBuiltInType(dataTypeId, opcUaClient.Session.Cache);


				variableInfo.BuiltInType = builtinType;

				GenericStructureDataType genericStructureDataType = null;
				if (builtinType == BuiltInType.ExtensionObject)
				{
					if (results[1].StatusCode.IsGood() && dataTypeId?.Identifier != null)
					{
						var genericType = opcUaClient.DataTypeManager.NewTypeFromDataType(results[1].WrappedValue.ToExpandedNodeId(), "Default Binary", false);
						genericStructureDataType = genericType;
					}
					else
					{
						// Ansonsten muss über den Variablen-Wert der Datentyp herausgefunden werden.
						var value = results[2];
						if (value.StatusCode.IsGood())
						{
							var extensionObject = value.WrappedValue.ToExtensionObject();
							if (extensionObject != null)
							{
								var genericObject = opcUaClient.DataTypeManager.ParseValue(extensionObject);
								genericStructureDataType = genericObject.TypeDefinition;
							}
						}
					}
				}

				variableInfo.GenericStructureDataType = genericStructureDataType;
				nodeInfoLite.UserData = variableInfo;
			}

			return nodeInfoLite;
		}







		public static async Task<InformationModel> LoadInformationModelAsync(this OpcUaClient opcUaClient)
		{
			if (opcUaClient == null)
				throw new ArgumentNullException(nameof(opcUaClient));

			await opcUaClient.EnsureConnectedAsync().ConfigureAwait(false);

			var rootFolderBrowseResult = await opcUaClient.Session.BrowseAsync(ObjectIds.RootFolder).ConfigureAwait(false);
			var objectsFolderDescription = rootFolderBrowseResult.References.First(d => d.NodeId == ObjectIds.ObjectsFolder);

			var nodeIdentifierMap = new Dictionary<NodeId, NodeInfo>();
			var objectsFolderChildren = await LoadNodeInfosAsync(opcUaClient.Session, ObjectIds.ObjectsFolder, nodeIdentifierMap).ConfigureAwait(false);

			var objectsFolderNodeId = objectsFolderDescription.NodeId.ToNodeId(opcUaClient.Session.NamespaceUris);
			var objectsFolder = new NodeInfo(objectsFolderNodeId, objectsFolderDescription, objectsFolderChildren);

			nodeIdentifierMap.Add(objectsFolderNodeId, objectsFolder);

			return new InformationModel(objectsFolder, nodeIdentifierMap);
		}

		public static object ConvertToObject(this OpcUaClient opcUaClient, NodeInfo nodeInfo, IList<DataValue> dataValues, int startIndex)
		{
			if (opcUaClient == null)
				throw new ArgumentNullException(nameof(opcUaClient));
			if (nodeInfo == null)
				throw new ArgumentNullException(nameof(nodeInfo));
			if (dataValues == null)
				throw new ArgumentNullException(nameof(dataValues));

			if (nodeInfo.NodeClass == NodeClass.Variable)
			{
				var value = ConvertToObject(opcUaClient, dataValues[startIndex]);
				return value;
			}

			var expando = new ExpandoObject();
			var namedValues = (IDictionary<string, object>)expando;

			var nodeInfoStack = new Stack<NodeInfo>();
			nodeInfoStack.Push(nodeInfo);

			var namedValuesStack = new Stack<IDictionary<string, object>>();
			namedValuesStack.Push(namedValues);

			while (nodeInfoStack.Count > 0)
			{
				var nextBrowseInfo = nodeInfoStack.Pop();
				var nextNamedValues = namedValuesStack.Pop();

				foreach (var child in nextBrowseInfo.Children)
				{
					if (child.NodeClass == NodeClass.Variable)
					{
						var dataValue = dataValues[startIndex++];
						nextNamedValues[child.BrowseName.Name] = ConvertToObject(opcUaClient, dataValue);
					}
					else
					{
						nodeInfoStack.Push(child);
						var childExpando = new ExpandoObject();
						var childNamedValues = (IDictionary<string, object>)childExpando;
						namedValuesStack.Push(childNamedValues);
						nextNamedValues[child.BrowseName.Name] = childExpando;
					}
				}
			}

			return expando;
		}

		public static object ConvertToObjectV2(this OpcUaClient opcUaClient, NodeInfoLite nodeInfo, IList<DataValue> dataValues, int startIndex)
		{
			if (opcUaClient == null)
				throw new ArgumentNullException(nameof(opcUaClient));
			if (nodeInfo == null)
				throw new ArgumentNullException(nameof(nodeInfo));
			if (dataValues == null)
				throw new ArgumentNullException(nameof(dataValues));

			if (nodeInfo.NodeClass == NodeClass.Variable)
			{
				var value = ConvertToObject(opcUaClient, dataValues[startIndex]);
				return value;
			}

			var expando = new ExpandoObject();
			var namedValues = (IDictionary<string, object>)expando;

			var nodeInfoStack = new Stack<NodeInfoLite>();
			nodeInfoStack.Push(nodeInfo);

			var namedValuesStack = new Stack<IDictionary<string, object>>();
			namedValuesStack.Push(namedValues);

			while (nodeInfoStack.Count > 0)
			{
				var nextBrowseInfo = nodeInfoStack.Pop();
				var nextNamedValues = namedValuesStack.Pop();

				foreach (var child in nextBrowseInfo.Children)
				{
					if (child.NodeClass == NodeClass.Variable)
					{
						var dataValue = dataValues[startIndex++];
						nextNamedValues[child.DisplayName.Text] = ConvertToObject(opcUaClient, dataValue);
					}
					else
					{
						nodeInfoStack.Push(child);
						var childExpando = new ExpandoObject();
						var childNamedValues = (IDictionary<string, object>)childExpando;
						namedValuesStack.Push(childNamedValues);
						nextNamedValues[child.DisplayName.Text] = childExpando;
					}
				}
			}

			return expando;
		}

		public static async Task<object> ConvertToObjectAsync(this OpcUaClient opcUaClient, NodeInfo nodeInfo, IList<DataValue> dataValues, int startIndex)
		{
			if (opcUaClient == null)
				throw new ArgumentNullException(nameof(opcUaClient));
			if (nodeInfo == null)
				throw new ArgumentNullException(nameof(nodeInfo));
			if (dataValues == null)
				throw new ArgumentNullException(nameof(dataValues));

			if (nodeInfo.NodeClass == NodeClass.Variable)
			{
				var value = await ConvertToObjectAsync(opcUaClient, dataValues[startIndex]).ConfigureAwait(false);
				return value;
			}

			var expando = new ExpandoObject();
			var namedValues = (IDictionary<string, object>)expando;

			var nodeInfoStack = new Stack<NodeInfo>();
			nodeInfoStack.Push(nodeInfo);

			var namedValuesStack = new Stack<IDictionary<string, object>>();
			namedValuesStack.Push(namedValues);

			while (nodeInfoStack.Count > 0)
			{
				var nextBrowseInfo = nodeInfoStack.Pop();
				var nextNamedValues = namedValuesStack.Pop();

				foreach (var child in nextBrowseInfo.Children)
				{
					if (child.NodeClass == NodeClass.Variable)
					{
						var dataValue = dataValues[startIndex++];
						nextNamedValues[child.BrowseName.Name] = await ConvertToObjectAsync(opcUaClient, dataValue).ConfigureAwait(false);
					}
					else
					{
						nodeInfoStack.Push(child);
						var childExpando = new ExpandoObject();
						var childNamedValues = (IDictionary<string, object>)childExpando;
						namedValuesStack.Push(childNamedValues);
						nextNamedValues[child.BrowseName.Name] = childExpando;
					}
				}
			}

			return expando;
		}
		#endregion

		#region Private Methods
		private static IReadOnlyCollection<NodeInfo> LoadNodeInfos(Session session, NodeId node, IDictionary<NodeId, NodeInfo> nodeIdMap)
		{
			var nodeInfos = new List<NodeInfo>();

			var browseContext = new BrowseContext
			{
				BrowseDirection = BrowseDirection.Forward,
				ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
				IncludeSubtypes = true,
				NodeClassMask = (int)NodeClass.Object | (int)NodeClass.Variable,
				ResultMask = (uint)BrowseResultMask.All
			};

			List<ReferenceDescription> descriptions;
			try
			{
				byte[] continuationPoint;
				descriptions = session.Browse(node, browseContext, out continuationPoint);
			}
			catch
			{
				return nodeInfos;
			}

			foreach (var description in descriptions.Where(d => d.ReferenceTypeId != ReferenceTypeIds.HasNotifier))
			{
				int i;
				if (description.ToString() == "PLC")
					i = 1;

				if (description.ToString().Contains("EURange"))
					i = 2;

				var nodeId = description.NodeId.ToNodeId(session.NamespaceUris);
				NodeInfo nodeInfo;

				if (!nodeIdMap.TryGetValue(nodeId, out nodeInfo))
				{
					var children = LoadNodeInfos(session, description.NodeId.ToNodeId(session.NamespaceUris), nodeIdMap);
					nodeInfo = new NodeInfo(description.NodeId.ToNodeId(session.NamespaceUris), description, children);
				}

				nodeInfos.Add(nodeInfo);

				if (!nodeIdMap.ContainsKey(nodeId))
				{
					nodeIdMap.Add(nodeId, nodeInfo);
				}
			}

			return nodeInfos;
		}

		private static async Task<IReadOnlyCollection<NodeInfo>> LoadNodeInfosAsync(Session session, NodeId node, IDictionary<NodeId, NodeInfo> nodeIdMap)
		{
			var nodeInfos = new List<NodeInfo>();

			var browseContext = new BrowseContext
			{
				BrowseDirection = BrowseDirection.Forward,
				ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
				IncludeSubtypes = true,
				NodeClassMask = (int)NodeClass.Object | (int)NodeClass.Variable,
				ResultMask = (uint)BrowseResultMask.All
			};

            UnifiedAutomation.UaBase.BrowseResult browseResult;

			try
			{
				browseResult = await session.BrowseAsync(node, browseContext, null).ConfigureAwait(false);
			}
			catch
			{
				return nodeInfos;
			}

			foreach (var description in browseResult.References.Where(d => d.ReferenceTypeId != ReferenceTypeIds.HasNotifier))
			{
				var nodeId = description.NodeId.ToNodeId(session.NamespaceUris);
				NodeInfo nodeInfo;

				if (!nodeIdMap.TryGetValue(nodeId, out nodeInfo))
				{
					var children = await LoadNodeInfosAsync(session, description.NodeId.ToNodeId(session.NamespaceUris), nodeIdMap).ConfigureAwait(false);
					nodeInfo = new NodeInfo(description.NodeId.ToNodeId(session.NamespaceUris), description, children);
				}

				nodeInfos.Add(nodeInfo);

				if (!nodeIdMap.ContainsKey(nodeId))
				{
					nodeIdMap.Add(nodeId, nodeInfo);
				}
			}

			return nodeInfos;
		}

		private static object ConvertToObject(this OpcUaClient opcUaClient, DataValue value)
		{
			if (StatusCode.IsBad(value.StatusCode))
			{
				return null;
			}

			if (value.WrappedValue.DataType != BuiltInType.ExtensionObject)
			{
				return GetClrValue(value.Value);
			}

			if (!value.WrappedValue.IsArray)
			{
				var extensionObject = value.WrappedValue.ToExtensionObject();
				if (extensionObject.Body.GetType() != typeof(byte[]))
				{
					return extensionObject.Body;
				}
				var genericObject = opcUaClient.ParseValue(extensionObject);
				return GetGenericObjectValues(genericObject);
			}

			var extensionObjectArray = value.WrappedValue.ToExtensionObjectArray();
			var valuesArray = new object[extensionObjectArray.Length];

			for (var i = 0; i < valuesArray.Length; ++i)
			{
				if (extensionObjectArray[i].Body.GetType() != typeof(byte[]))
				{
					valuesArray[i] = extensionObjectArray[i].Body;
				}
				else
				{
					var genericObject = opcUaClient.ParseValue(extensionObjectArray[i]);
					valuesArray[i] = GetGenericObjectValues(genericObject);
				}
			}

			return valuesArray;
		}

		private static async Task<object> ConvertToObjectAsync(this OpcUaClient opcUaClient, DataValue value)
		{
			if (StatusCode.IsBad(value.StatusCode))
			{
				return null;
			}

			if (value.WrappedValue.DataType != BuiltInType.ExtensionObject)
			{
				return GetClrValue(value.Value);
			}

			if (!value.WrappedValue.IsArray)
			{
				var extensionObject = value.WrappedValue.ToExtensionObject();
				if (extensionObject.Body.GetType() != typeof(byte[]))
				{
					return extensionObject.Body;
				}
				var genericObject = await opcUaClient.ParseValueAsync(extensionObject).ConfigureAwait(false);
				return GetGenericObjectValues(genericObject);
			}

			var extensionObjectArray = value.WrappedValue.ToExtensionObjectArray();
			var valuesArray = new object[extensionObjectArray.Length];

			for (var i = 0; i < valuesArray.Length; ++i)
			{
				if (extensionObjectArray[i].Body.GetType() != typeof(byte[]))
				{
					valuesArray[i] = extensionObjectArray[i].Body;
				}
				else
				{
					var genericObject = await opcUaClient.ParseValueAsync(extensionObjectArray[i]).ConfigureAwait(false);
					valuesArray[i] = GetGenericObjectValues(genericObject);
				}
			}

			return valuesArray;
		}

		private static object GetGenericObjectValues(GenericEncodeableObject genericObject)
		{
			var expando = new ExpandoObject();
			var namedValues = (IDictionary<string, object>)expando;

			for (var i = 0; i < genericObject.TypeDefinition.Count; ++i)
			{
				object value;

				var extensionObject = genericObject[i].Value as ExtensionObject;
				if (extensionObject != null)
				{
					value = GetGenericObjectValues((GenericEncodeableObject)extensionObject.Body);
				}
				else
				{
					var extensionObjectArray = genericObject[i].Value as ExtensionObject[];
					if (extensionObjectArray != null)
					{
						var valueArray = new object[extensionObjectArray.Length];
						for (var j = 0; j < extensionObjectArray.Length; ++j)
						{
							valueArray[j] = GetGenericObjectValues((GenericEncodeableObject)extensionObjectArray[j].Body);
						}
						value = valueArray;
					}
					else
					{
						value = GetClrValue(genericObject[i].Value);
					}
				}

				namedValues[genericObject.TypeDefinition[i].Name] = value;
			}

			return expando;
		}

		private static object GetClrValue(object value)
		{
			if (value is Uuid)
			{
				return ((Uuid)value).Guid;
			}

			return value;
		}
		#endregion
	}
}