//-----------------------------------------------------------------------------
// Copyright (c) 2017 by Bystronic Laser AG, CH-3362 Niederönz
//-----------------------------------------------------------------------------
// $LastChangedRevision: 691 $
// $LastChangedDate: 2017-12-20 11:28:36 +0100 (Mi., 20 Dez 2017) $
// $Author: Land $
//-----------------------------------------------------------------------------

#region Namespaces
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnifiedAutomation.UaBase;
using UnifiedAutomation.UaClient;
using TypeInfo = UnifiedAutomation.UaBase.TypeInfo;
#endregion

namespace Bystronic.IoT.OpcUaClientHelper
{
	public class OpcUaDataClient : IDisposable
	{
		#region Fields
		private readonly OpcUaClient _opcUaClient;
		private InformationModel _informationModel;
		private readonly Dictionary<NodeId, NodeId> _registeredNodeMap;
		private bool _clearRegisteredNodeMap;
		private Dictionary<NodeId, NodeInfoLite> _nodeInfos = new Dictionary<NodeId, NodeInfoLite>();
		#endregion

		#region Constructors
		public OpcUaDataClient(Session session, string endpointUrl)
		{
			if (session == null)
				throw new ArgumentNullException(nameof(session));
			if (endpointUrl == null)
				throw new ArgumentNullException(nameof(endpointUrl));

			_opcUaClient = new OpcUaClient(session, endpointUrl);
			_registeredNodeMap = new Dictionary<NodeId, NodeId>();

			session.ConnectionStatusUpdate += OnConnectionStatusUpdate;
		}
		#endregion

		#region Properties
		public Session Session => _opcUaClient.Session;
		public string EndpointUrl => _opcUaClient.EndpointUrl;
		public bool IsConnected => _opcUaClient.IsConnected;
		#endregion

		#region Public Methods
		public void EnsureConnected()
		{
			_opcUaClient.EnsureConnected();
		}

		public Task EnsureConnectedAsync()
		{
			return _opcUaClient.EnsureConnectedAsync();
		}

		public object ReadData(NodeId nodeId)
		{
			if (nodeId == null)
				throw new ArgumentNullException(nameof(nodeId));
			var nodeInfo = GetNodeInfo(nodeId);
			return ReadDataCore(nodeInfo);
		}


		public object ReadDataV2(NodeId nodeId)
		{
			if (nodeId == null)
				throw new ArgumentNullException(nameof(nodeId));
			var nodeInfo = GetNodeInfoV2(nodeId);
			return ReadDataCoreV2(nodeInfo);
		}

		public object ReadData(string variableName)
		{
			if (variableName == null)
				throw new ArgumentNullException(nameof(variableName));
			var nodeInfo = GetNodeInfo(variableName);
			return ReadDataCore(nodeInfo);
		}

		public async Task<object> ReadDataAsync(NodeId nodeId)
		{
			if (nodeId == null)
				throw new ArgumentNullException(nameof(nodeId));
			var nodeInfo = await GetNodeInfoAsync(nodeId).ConfigureAwait(false);
			return await ReadDataCoreAsync(nodeInfo).ConfigureAwait(false);
		}

		public async Task<object> ReadDataAsync(string variableName)
		{
			if (variableName == null)
				throw new ArgumentNullException(nameof(variableName));
			var nodeInfo = await GetNodeInfoAsync(variableName).ConfigureAwait(false);
			return await ReadDataCoreAsync(nodeInfo).ConfigureAwait(false);
		}

		public void WriteData(NodeId nodeId, object value)
		{
			if (nodeId == null)
				throw new ArgumentNullException(nameof(nodeId));
		
			var nodeInfoLite = GetNodeInfoV2(nodeId);
			//var nodeInfo = GetNodeInfo(nodeId);
			WriteDataCoreV2(nodeInfoLite, value);
		}


		public void WriteData(NodeId nodeId, object value, int arrayIndex)
		{
			if (nodeId == null)
				throw new ArgumentNullException(nameof(nodeId));

			var nodeInfoLite = GetNodeInfoV2(nodeId);
			//var nodeInfo = GetNodeInfo(nodeId);
			WriteDataCoreV2(nodeInfoLite, value, arrayIndex);
		}


		public void WriteData(List<NodeId> nodeIds, List<object> values)
		{
			if (nodeIds == null)
				throw new ArgumentNullException(nameof(nodeIds));

			List<NodeInfoLite> nodeInfos = new List<NodeInfoLite>();
			foreach(NodeId nodeId in nodeIds)
				nodeInfos.Add(GetNodeInfoV2(nodeId));

			WriteDataCoreV2(nodeInfos, values);
		}

		public void WriteData(string variableName, object value)
		{
			if (variableName == null)
				throw new ArgumentNullException(nameof(variableName));
			var nodeInfo = GetNodeInfo(variableName);
			WriteDataCore(nodeInfo, value);
		}

		public async Task WriteDataAsync(NodeId nodeId, object value)
		{
			if (nodeId == null)
				throw new ArgumentNullException(nameof(nodeId));
			var nodeInfo = await GetNodeInfoAsync(nodeId).ConfigureAwait(false);
			await WriteDataCoreAsync(nodeInfo, value).ConfigureAwait(false);
		}

		public async Task WriteDataAsync(string variableName, object value)
		{
			if (variableName == null)
				throw new ArgumentNullException(nameof(variableName));
			var nodeInfo = await GetNodeInfoAsync(variableName).ConfigureAwait(false);
			await WriteDataCoreAsync(nodeInfo, value).ConfigureAwait(false);
		}
		#endregion

		#region IDisposable
		public void Dispose()
		{
			_opcUaClient.Dispose();
			Session.ConnectionStatusUpdate -= OnConnectionStatusUpdate;
		}
		#endregion

		#region Event Handlers
		private void OnConnectionStatusUpdate(Session sender, ServerConnectionStatusUpdateEventArgs e)
		{
			if (e.Status == ServerConnectionStatus.SessionAutomaticallyRecreated)
			{
				_clearRegisteredNodeMap = true;
			}
		}
		#endregion

		#region Overrides
		public override string ToString()
		{
			return EndpointUrl;
		}
		#endregion

		#region Private Methods
		public NodeInfo GetNodeInfo(NodeId nodeId)
		{
			LoadInformationModel();

			var nodeInfo = _informationModel.GetNodeInfo(nodeId);


			if (nodeInfo == null)
			{			
				throw new InvalidOperationException($"NodeId '{nodeId}' does not exist on endpoint '{EndpointUrl}'.");
			}

			return nodeInfo;
		}

		public void RegisterNodeId(NodeId nodeId)
		{
			NodeInfoLite nodeInfoAux;
			_nodeInfos.TryGetValue(nodeId, out nodeInfoAux);

			if (nodeInfoAux == null)
			{
				_opcUaClient.EnsureConnected();

				nodeInfoAux = _opcUaClient.ReadNodeInfoLite(nodeId, true);

				if (nodeInfoAux == null)
					throw new InvalidOperationException($"NodeId '{nodeId}' does not exist on endpoint '{EndpointUrl}'.");

				_nodeInfos[nodeId] = nodeInfoAux;
			}
		}


		public NodeInfoLite GetNodeInfoV2(NodeId nodeId)
		{

			NodeInfoLite nodeInfoAux;
			_nodeInfos.TryGetValue(nodeId, out nodeInfoAux);

			if (nodeInfoAux == null)
			{
				_opcUaClient.EnsureConnected();

				nodeInfoAux = _opcUaClient.ReadNodeInfoLite(nodeId, true);

				if (nodeInfoAux == null)
					throw new InvalidOperationException($"NodeId '{nodeId}' does not exist on endpoint '{EndpointUrl}'.");


				_nodeInfos[nodeId] = nodeInfoAux;

			}			

			return nodeInfoAux;
		}

		private NodeInfo GetNodeInfo(string variableName)
		{
			LoadInformationModel();

			var nodeInfo = _informationModel.GetNodeInfo(variableName);
			if (nodeInfo == null)
			{
				throw new InvalidOperationException($"Variable '{variableName}' does not exist on endpoint '{EndpointUrl}'.");
			}

			return nodeInfo;
		}

		private async Task<NodeInfo> GetNodeInfoAsync(NodeId nodeId)
		{
			await LoadInformationModelAsync().ConfigureAwait(false);

			var nodeInfo = _informationModel.GetNodeInfo(nodeId);
			if (nodeInfo == null)
			{
				throw new InvalidOperationException($"NodeId '{nodeId}' does not exist on endpoint '{EndpointUrl}'.");
			}

			return nodeInfo;
		}

		private async Task<NodeInfo> GetNodeInfoAsync(string variableName)
		{
			await LoadInformationModelAsync().ConfigureAwait(false);

			var nodeInfo = _informationModel.GetNodeInfo(variableName);
			if (nodeInfo == null)
			{
				throw new InvalidOperationException($"Variable '{variableName}' does not exist on endpoint '{EndpointUrl}'.");
			}

			return nodeInfo;
		}

		public void LoadInformationModel(bool forceLoad = false)
		{
			if (_informationModel == null || forceLoad)
			{
				_informationModel = _opcUaClient.LoadInformationModel();
			}
		}

		private async Task LoadInformationModelAsync()
		{
			if (_informationModel == null)
			{
				_informationModel = await _opcUaClient.LoadInformationModelAsync().ConfigureAwait(false);
			}
		}

		private IList<NodeId> RegisterNodes(IList<NodeId> nodes)
		{
			List<NodeId> unregisteredNodes;
			var registeredNodes = GetRegisteredNodes(nodes, out unregisteredNodes);

			if (unregisteredNodes.Count > 0)
			{
				registeredNodes = _opcUaClient.RegisterNodes(unregisteredNodes);
				UpdateRegisteredNodeMap(unregisteredNodes, registeredNodes);
				registeredNodes = GetRegisteredNodes(nodes, out unregisteredNodes);
			}

			return registeredNodes;
		}

		private async Task<IList<NodeId>> RegisterNodesAsync(IList<NodeId> nodes)
		{
			List<NodeId> unregisteredNodes;
			var registeredNodes = GetRegisteredNodes(nodes, out unregisteredNodes);

			if (unregisteredNodes.Count > 0)
			{
				registeredNodes = await _opcUaClient.RegisterNodesAsync(unregisteredNodes).ConfigureAwait(false);
				UpdateRegisteredNodeMap(unregisteredNodes, registeredNodes);
				registeredNodes = GetRegisteredNodes(nodes, out unregisteredNodes);
			}

			return registeredNodes;
		}

		private IList<NodeId> GetRegisteredNodes(IEnumerable<NodeId> nodes, out List<NodeId> unregisteredNodes)
		{
			unregisteredNodes = new List<NodeId>();
			var registeredNodes = new List<NodeId>();

			if (_clearRegisteredNodeMap)
			{
				_clearRegisteredNodeMap = false;
				_registeredNodeMap.Clear();
				unregisteredNodes.AddRange(nodes);
				return registeredNodes;
			}

			foreach (var nodeId in nodes)
			{
				NodeId registeredNode;
				if (_registeredNodeMap.TryGetValue(nodeId, out registeredNode))
				{
					registeredNodes.Add(registeredNode);
				}
				else
				{
					unregisteredNodes.Add(nodeId);
				}
			}

			return registeredNodes;
		}

		private void UpdateRegisteredNodeMap(IList<NodeId> unregisteredNodes, IList<NodeId> registeredNodes)
		{
			for (var i = 0; i < unregisteredNodes.Count; ++i)
			{
				_registeredNodeMap[unregisteredNodes[i]] = registeredNodes[i];
			}
		}

		private object ReadDataCore(NodeInfo nodeInfo)
		{
			var nodesToRead = nodeInfo.GetVariableNodeIds();
			if (nodesToRead.Count == 0)
			{
				return null;
			}

			var registeredNodes = RegisterNodes(nodesToRead);
			var dataValues = _opcUaClient.Read(registeredNodes);
			return _opcUaClient.ConvertToObject(nodeInfo, dataValues, 0);
		}

		private object ReadDataCoreV2(NodeInfoLite nodeInfo)
		{
			var nodesToRead = nodeInfo.GetVariableNodeIds();
			if (nodesToRead.Count == 0)
			{
				return null;
			}

			var registeredNodes = RegisterNodes(nodesToRead);
			var dataValues = _opcUaClient.Read(registeredNodes);
			return _opcUaClient.ConvertToObjectV2(nodeInfo, dataValues, 0);
		}

		private async Task<object> ReadDataCoreAsync(NodeInfo nodeInfo)
		{
			var nodesToRead = nodeInfo.GetVariableNodeIds();
			if (nodesToRead.Count == 0)
			{
				return Task.FromResult((object)null);
			}

			var registeredNodes = await RegisterNodesAsync(nodesToRead).ConfigureAwait(false);
			var dataValues = await _opcUaClient.ReadAsync(registeredNodes).ConfigureAwait(false);
			return await _opcUaClient.ConvertToObjectAsync(nodeInfo, dataValues, 0).ConfigureAwait(false);
		}

		private void WriteDataCore(NodeInfo nodeInfo, object value)
		{
			UpdateVariableInfos(nodeInfo);

			List<Variant> valuesToWrite;
			var nodesToWrite = GetWriteNodes(nodeInfo, value, out valuesToWrite);
			if (nodesToWrite.Count == 0)
			{
				return;
			}

			var registeredNodes = RegisterNodes(nodesToWrite);
			IList<StatusCode> lista = _opcUaClient.Write(registeredNodes, valuesToWrite);
		}


		private void WriteDataCoreV2(NodeInfoLite nodeInfo, object value, int arrayIndex = -1)
		{			

			List<Variant> valuesToWrite;
			var nodesToWrite = GetWriteNodesV2(nodeInfo, value, out valuesToWrite);
			if (nodesToWrite.Count == 0)
			{
				return;
			}

			var registeredNodes = RegisterNodes(nodesToWrite);
			IList<StatusCode> lista = _opcUaClient.Write(nodesToWrite, valuesToWrite, arrayIndex);
		}


		private void WriteDataCoreV2(List<NodeInfoLite> nodeInfos, List<object> values)
		{
			List<NodeId> nodesToWrite = new List<NodeId>();
			List<Variant> valuesToWrite = new List<Variant>();

			var nodesValues = nodeInfos.Zip(values, (n, v) => new { nodeInfo = n, value = v });
			foreach (var nv in nodesValues)
			{
				List<Variant> valuesToWriteLoc = new List<Variant>();
				nodesToWrite.AddRange(GetWriteNodesV2(nv.nodeInfo, nv.value, out valuesToWriteLoc));
				valuesToWrite.AddRange(valuesToWriteLoc);
			}			

			if (nodesToWrite.Count == 0)
			{
				return;
			}

			var registeredNodes = RegisterNodes(nodesToWrite);
			IList<StatusCode> lista = _opcUaClient.Write(nodesToWrite, valuesToWrite);
		}

		private async Task WriteDataCoreAsync(NodeInfo nodeInfo, object value)
		{
			await UpdateVariableInfosAsync(nodeInfo).ConfigureAwait(false);

			List<Variant> valuesToWrite;
			var nodesToWrite = GetWriteNodes(nodeInfo, value, out valuesToWrite);
			if (nodesToWrite.Count == 0)
			{
				return;
			}

			var registeredNodes = await RegisterNodesAsync(nodesToWrite).ConfigureAwait(false);
			await _opcUaClient.WriteAsync(registeredNodes, valuesToWrite).ConfigureAwait(false);
		}

		private static IList<NodeId> GetWriteNodes(NodeInfo rootNode, object rootValue, out List<Variant> valuesToWrite)
		{
			var nodesToWrite = new List<NodeId>();
			valuesToWrite = new List<Variant>();

			var nodeStack = new Stack<NodeInfo>();
			nodeStack.Push(rootNode);

			var valueStack = new Stack<object>();
			valueStack.Push(rootValue);

			while (nodeStack.Count > 0)
			{
				var node = nodeStack.Pop();
				var value = valueStack.Pop();

				if (node.NodeClass == NodeClass.Variable)
				{
					nodesToWrite.Add(node.NodeId);
					valuesToWrite.Add(GetWriteValue(value, (VariableInfo)node.UserData));
				}
				else if (value != null)
				{
					var namedValues = GetNamedValues(value);

					foreach (var namedValue in namedValues)
					{
						var childNode = node.Children.FirstOrDefault(child => child.BrowseName.Name == namedValue.Key);
						if (childNode != null)
						{
							nodeStack.Push(childNode);
							valueStack.Push(namedValue.Value);
						}
					}
				}
			}

			return nodesToWrite;
		}


		private static IList<NodeId> GetWriteNodesV2(NodeInfoLite rootNode, object rootValue, out List<Variant> valuesToWrite)
		{
			var nodesToWrite = new List<NodeId>();
			valuesToWrite = new List<Variant>();

			var nodeStack = new Stack<NodeInfoLite>();
			nodeStack.Push(rootNode);

			var valueStack = new Stack<object>();
			valueStack.Push(rootValue);

			while (nodeStack.Count > 0)
			{
				var node = nodeStack.Pop();
				var value = valueStack.Pop();

				if (node.NodeClass == NodeClass.Variable)
				{
					nodesToWrite.Add(node.NodeId);
					valuesToWrite.Add(GetWriteValue(value, (VariableInfo)node.UserData));
				}
				else if (value != null)
				{
					var namedValues = GetNamedValues(value);

					foreach (var namedValue in namedValues)
					{
						var childNode = node.Children.FirstOrDefault(child => child.BrowseName.Name == namedValue.Key || child.DisplayName.Text == namedValue.Key);
						if (childNode != null)
						{
							nodeStack.Push(childNode);
							valueStack.Push(namedValue.Value);
						}
					}
				}
			}

			return nodesToWrite;
		}

		private void UpdateVariableInfos(NodeInfo rootNode)
		{
			var variableNodes = rootNode.GetVariableNodes();

			foreach (var variableNode in variableNodes.Where(node => node.UserData == null))
			{
				var variableId = variableNode.NodeId;
				var dataTypeId = _opcUaClient.Read(variableId, Attributes.DataType).WrappedValue.ToNodeId();
				var builtinType = TypeUtils.GetBuiltInType(dataTypeId, _opcUaClient.Session.Cache);

				GenericStructureDataType genericStructureDataType = null;
				if (builtinType == BuiltInType.ExtensionObject)
				{
					genericStructureDataType = _opcUaClient.GetGenericStructureDataType(variableId);
				}

				variableNode.UserData = new VariableInfo
				{
					BuiltInType = builtinType,
					GenericStructureDataType = genericStructureDataType
				};
			}
		}

		private async Task UpdateVariableInfosAsync(NodeInfo rootNode)
		{
			var variableNodes = rootNode.GetVariableNodes();

			foreach (var variableNode in variableNodes.Where(node => node.UserData == null))
			{
				var variableId = variableNode.NodeId;
				var dataTypeId = (await _opcUaClient.ReadAsync(variableId, Attributes.DataType).ConfigureAwait(false)).WrappedValue.ToNodeId();
				var builtinType = TypeUtils.GetBuiltInType(dataTypeId, _opcUaClient.Session.Cache);

				GenericStructureDataType genericStructureDataType = null;
				if (builtinType == BuiltInType.ExtensionObject)
				{
					genericStructureDataType = await _opcUaClient.GetGenericStructureDataTypeAsync(variableId).ConfigureAwait(false);
				}

				variableNode.UserData = new VariableInfo
				{
					BuiltInType = builtinType,
					GenericStructureDataType = genericStructureDataType
				};
			}
		}

		private static Variant GetWriteValue(object value, VariableInfo variableInfo)
		{
			if (value == null || variableInfo.BuiltInType != BuiltInType.ExtensionObject)
			{

                switch(variableInfo.BuiltInType)
                {
                    case BuiltInType.Boolean:
                        value = Convert.ToBoolean(value);
                        break;
                    case BuiltInType.Byte:
                        value = Convert.ToByte(value);
                        break;
                    case BuiltInType.Double:
                        value = Convert.ToDouble(value);
                        break;                    
                    case BuiltInType.Int16:
                        value = Convert.ToInt16(value);
                        break;
                    case BuiltInType.Int32:
                        value = Convert.ToInt32(value);
                        break;
                    case BuiltInType.Int64:
                        value = Convert.ToInt64(value);
                        break;
                    case BuiltInType.UInt16:
                        value = Convert.ToUInt16(value);
                        break;
                    case BuiltInType.UInt32:
                        value = Convert.ToUInt32(value);
                        break;
                    case BuiltInType.UInt64:
                        value = Convert.ToUInt64(value);
                        break;
                }

                var res = new Variant(value, TypeInfo.Construct(value));
                return res;
			}

			var valueArray = value as object[];
			if (valueArray is null)
			{
				try
				{
					var list = value as IEnumerable<object>;
					valueArray = list.Cast<object>().ToArray();
				}
				catch(Exception e) { }
			}
			if (valueArray != null)
			{
				var genericObjects = new GenericEncodeableObject[valueArray.Length];
				for (var i = 0; i < valueArray.Length; ++i)
				{
					genericObjects[i] = new GenericEncodeableObject(variableInfo.GenericStructureDataType);
					genericObjects[i].ReadFromRealObject(valueArray[i], variableInfo.GenericStructureDataType);
				}
				return new Variant(genericObjects);
			}

			var genericObject = new GenericEncodeableObject(variableInfo.GenericStructureDataType);
			genericObject.ReadFromRealObject(value, variableInfo.GenericStructureDataType);
			return new Variant(genericObject);
		}

		

		private static IDictionary<string, object> GetNamedValues(object composite)
		{
			var namedValues = composite as IDictionary<string, object>;
			if (namedValues != null)
			{
				return namedValues;
			}

			namedValues = new Dictionary<string, object>();

			var properties = composite.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
			foreach (var property in properties)
			{
				var propertyValue = property.GetValue(composite);
				namedValues.Add(property.Name, propertyValue);
			}

			return namedValues;
		}
		#endregion


		#region Antil Library

		public object ReadNodeId<T>(NodeId nodeId)
		{

			EnsureConnected();

			//var jobj = ReadNodeId(nodeId, "varName", typeof(T));

			//var result = ReadData(nodeId);
			var result = ReadDataV2(nodeId);


			var jobj = Newtonsoft.Json.JsonConvert.SerializeObject(result);

			var obj = JsonConvert.DeserializeObject<T>(jobj);

			//var obj = ParseExpando<T>(result);

			

			return obj;

		}


		public object ParseExpando<T>(object result)
		{
			// get settable public properties of the type
			var props = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
				.Where(x => x.GetSetMethod() != null);

			// create an instance of the type
			var obj = Activator.CreateInstance(typeof(T));

			// set property values using reflection
			var values = (IDictionary<string, object>)result;
			foreach (var prop in props)
			{
				
				prop.SetValue(obj, values[prop.Name]);
				
			}


			return obj;
		}


		public JObject ReadNodeId(NodeId nodeId, string varName, System.Type datatype)
		{

			if (!IsConnected)
			{

				return null;
			}

			ReadValueIdCollection nodesToRead;
			ReadValueId readValueId;

			//Lettura valore          

			nodesToRead = new ReadValueIdCollection();


			QualifiedName dataEncoding = new QualifiedName(UnifiedAutomation.UaBase.BrowseNames.DefaultBinary);


			readValueId = new ReadValueId()
			{
				NodeId = nodeId,
				AttributeId = Attributes.Value
			};
			nodesToRead.Add(readValueId);

			List<DataValue> results = _opcUaClient.Session.Read(nodesToRead);

			DataValue value = results[0];

			try
			{
				ExtensionObject ext = (ExtensionObject)value.Value;
			}
			catch (Exception sdfsd) { }



			JObject jobj = new JObject();
			if (value.Value != null || value.StatusCode.IsGood())
			{
				jobj = ParseDataValue(value, varName);
				if (jobj[varName] == null)
					jobj[varName] = jobj;
			}
			else
			{
				JObject jobjAux = new JObject();
				var props = datatype.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
				foreach (var prop in props)
				{
					NodeId childNodeId = NodeId.Parse(nodeId.ToString() + "." + prop.Name);
					var jChild = ReadNodeId(childNodeId, prop.Name, prop.PropertyType);
					jobjAux[prop.Name] = jChild[prop.Name];
				}
				jobj[varName] = jobjAux;
			}

			return jobj;
		}

		JObject ParseDataValue(DataValue value, String varName)
		{
			JObject jobj = new JObject();
			ExtensionObject[] eee = new ExtensionObject[0];

			if (!value.WrappedValue.IsArray)
			{

				if (value.WrappedValue.TypeInfo.ToString() == "ExtensionObject")
				{
					eee = new ExtensionObject[1];
					eee[0] = (ExtensionObject)value.Value;
				}
				else
				{
					JValue jval = new JValue(value.Value);
					jobj[varName] = jval;

				}
			}
			else
			{
				if (value.WrappedValue.TypeInfo.BuiltInType == BuiltInType.ExtensionObject)
					eee = (ExtensionObject[])value.Value;
				else
					jobj[varName] = new JArray(value.Value);
			}


			if (eee.Length > 0)
			{

				GenericEncodeableObject[] resultEOarr = new GenericEncodeableObject[eee.Length];
				for (int i = 0; i < eee.Length; i++)
					resultEOarr[i] = _opcUaClient.DataTypeManager.ParseValue(eee[i]);
				jobj = ParseGeOarr(resultEOarr, varName);
			}

			return jobj;
		}

		public JObject ParseGeOarr(GenericEncodeableObject[] geoArr, String varName)
		{
			JObject jobj = new JObject();
			JObject jobjI = new JObject();
			int i;

			JArray ja = new JArray();

			int len = geoArr.Length;

			for (i = 0; i < len; i++)
			{
				var geo = geoArr[i];
				jobjI = ParseGeo(geo);


				if (len > 1)
					ja.Add(jobjI);
			}

			if (len > 1)
				jobj[varName] = ja;
			else
				jobj = jobjI;

			return jobj;

		}


		public JObject ParseGeo(GenericEncodeableObject geo)
		{
			JObject jobj = new JObject();
			JValue jval;
			int i, j;

			for (i = 0; i < geo.TypeDefinition.Count; i++)
			{
				if (geo.TypeDefinition[i].TypeDescription.TypeClass == GenericDataTypeClass.Simple || geo.TypeDefinition[i].TypeDescription.TypeClass == GenericDataTypeClass.Enumerated)
				{
					Variant fieldValue = geo[i];
					if (fieldValue.IsArray)
					{

						JArray jarr = new JArray();

						for (j = 0; j < fieldValue.ArrayLength; j++)
						{
							jval = new JValue(fieldValue.ToVariantArray()[j].Value);
							jarr.Add(jval);

						}
						jobj[geo.TypeDefinition[i].Name] = jarr;

					}
					else
					{
						if (geo.TypeDefinition[i].TypeDescription.ToString() == "Guid")
							jval = new JValue(geo[i].Value.ToString());
						else
							jval = new JValue(geo[i].Value);

						jobj[geo.TypeDefinition[i].Name] = jval;
					}
				}

				if (geo.TypeDefinition[i].TypeDescription.TypeClass == GenericDataTypeClass.Structured)
				{
					Variant fieldValue = geo[i];

					if (!fieldValue.IsArray)
					{
						GenericEncodeableObject geo2 = fieldValue.GetValue<GenericEncodeableObject>(null);
						JObject jobj2 = ParseGeo(geo2);
						jobj[geo.TypeDefinition[i].Name] = jobj2;
					}
					else
					{

						ExtensionObject[] eo2Arr = fieldValue.ToExtensionObjectArray();
						JArray jarr = new JArray();

						for (j = 0; j < fieldValue.ArrayLength; j++)
						{
							Variant fieldValue2 = eo2Arr[j];
							GenericEncodeableObject geo3 = fieldValue2.GetValue<GenericEncodeableObject>(null);
							JObject jobj3 = ParseGeo(geo3);
							jarr.Add(jobj3);

						}
						jobj[geo.TypeDefinition[i].Name] = jarr;
					}
				}
			}

			return jobj;
		}

		#endregion

	}
}