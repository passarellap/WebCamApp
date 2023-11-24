//-----------------------------------------------------------------------------
// Copyright (c) 2017 by Bystronic Laser AG, CH-3362 Niederönz
//-----------------------------------------------------------------------------
// $LastChangedRevision: 912 $
// $LastChangedDate: 2018-04-16 15:39:03 +0200 (Mo., 16 Apr 2018) $
// $Author: Land $
//-----------------------------------------------------------------------------

#region Namespaces
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnifiedAutomation.UaBase;
using UnifiedAutomation.UaClient;
#endregion

namespace Bystronic.IoT.OpcUaClientHelper
{
	public class OpcUaClient : IDisposable
	{
		#region Fields
		private readonly Subscription _subscription;
		private readonly DataTypeManager _dataTypeManager;
		private int _namespaceIndex = -1;
		private readonly Dictionary<NodeId, DataMonitoredItem> _dataMonitoredItemMap;
		#endregion

		#region Constructors
		public OpcUaClient(Session session, string endpointUrl)
			: this(session, endpointUrl, null, null)
		{
		}
		public OpcUaClient(Session session, string endpointUrl, RequestSettings requestSettings)
			: this(session, endpointUrl, null, requestSettings)
		{
		}
		public OpcUaClient(Session session, string endpointUrl, string namespaceUri)
			: this(session, endpointUrl, namespaceUri, null)
		{
		}
		public OpcUaClient(Session session, string endpointUrl, string namespaceUri, RequestSettings requestSettings)
		{
			if (session == null)
				throw new ArgumentNullException(nameof(session));
			if (endpointUrl == null)
				throw new ArgumentNullException(nameof(endpointUrl));

			Session = session;
			EndpointUrl = endpointUrl;
			NamespaceUri = namespaceUri;
			RequestSettings = requestSettings;

			_subscription = new Subscription(session);
			_subscription.DataChanged += OnSubscriptionDataChanged;

			_dataTypeManager = new DataTypeManager(session);
			_dataMonitoredItemMap = new Dictionary<NodeId, DataMonitoredItem>();
		}
		#endregion

		#region Properties
		public Session Session { get; }
		public string EndpointUrl { get; }
		public string NamespaceUri { get; }
		public RequestSettings RequestSettings { get; }
		public bool IsConnected => Session.ConnectionStatus == ServerConnectionStatus.Connected;
		public ICollection<DataMonitoredItem> DataMonitoredItems => _dataMonitoredItemMap.Values;
		public DataTypeManager DataTypeManager => _dataTypeManager;
		public Subscription Subscription => _subscription;
		#endregion

		#region Public Methods
		public void EnsureConnected()
		{
			switch (Session.ConnectionStatus)
			{
				case ServerConnectionStatus.Connected:
					return;
				case ServerConnectionStatus.Disconnected:
					BeforeConnect();
					Session.Connect(EndpointUrl, SecuritySelection.None, null, RetryInitialConnect.No, RequestSettings);
					AfterConnect();
					return;
			}
			throw new StatusException(new StatusCode(StatusCodes.BadNotConnected));
		}

		public async Task EnsureConnectedAsync()
		{
			switch (Session.ConnectionStatus)
			{
				case ServerConnectionStatus.Connected:
					return;
				case ServerConnectionStatus.Disconnected:
					BeforeConnect();
					await Session.ConnectAsync(EndpointUrl, SecuritySelection.None, null, RetryInitialConnect.No, RequestSettings).ConfigureAwait(false);
					AfterConnect();
					return;
			}
			throw new StatusException(new StatusCode(StatusCodes.BadNotConnected));
		}

		public GenericStructureDataType GetGenericStructureDataType(string id)
		{
			var namespaceIndex = GetNamespaceIndex();
			var nodeId = new NodeId(id, namespaceIndex);
			return GetGenericStructureDataType(nodeId);
		}
		public async Task<GenericStructureDataType> GetGenericStructureDataTypeAsync(string id)
		{
			var namespaceIndex = await GetNamespaceIndexAsync().ConfigureAwait(false);
			var nodeId = new NodeId(id, namespaceIndex);
			return await GetGenericStructureDataTypeAsync(nodeId).ConfigureAwait(false);
		}
		public GenericStructureDataType GetGenericStructureDataType(NodeId nodeId)
		{
			// Zuerst prüfen, ob die Variable einen gültigen Datentyp referenziert.
			// Dies ist normalerweise immer der Fall, aber es gibt auch OPC UA Server,
			// welche dies aus unbekannten Gründen nicht tun.
			var dataType = Read(nodeId, Attributes.DataType);

			if (dataType.StatusCode.IsGood() && dataType.WrappedValue.ToNodeId()?.Identifier != null)
			{
				var genericType = _dataTypeManager.NewTypeFromDataType(dataType.WrappedValue.ToExpandedNodeId(), "Default Binary", false);
				return genericType;
			}
			// Ansonsten muss über den Variablen-Wert der Datentyp herausgefunden werden.
			var value = Read(nodeId);
			if (value.StatusCode.IsGood())
			{
				var extensionObject = value.WrappedValue.ToExtensionObject();
				if (extensionObject != null)
				{
					var genericObject = ParseValue(extensionObject);
					return genericObject.TypeDefinition;
				}
			}
			throw new StatusException(new StatusCode(StatusCodes.BadDataUnavailable));
		}
		public async Task<GenericStructureDataType> GetGenericStructureDataTypeAsync(NodeId nodeId)
		{
			// Zuerst prüfen, ob die Variable einen gültigen Datentyp referenziert.
			// Dies ist normalerweise immer der Fall, aber es gibt auch OPC UA Server,
			// welche dies aus unbekannten Gründen nicht tun.
			var dataType = await ReadAsync(nodeId, Attributes.DataType).ConfigureAwait(false);

			if (dataType.StatusCode.IsGood() && dataType.WrappedValue.ToNodeId()?.Identifier != null)
			{
				var genericType = _dataTypeManager.NewTypeFromDataType(dataType.WrappedValue.ToExpandedNodeId(), "Default Binary", false);
				return genericType;
			}

			// Ansonsten muss über den Variablen-Wert der Datentyp herausgefunden werden.
			var value = await ReadAsync(nodeId).ConfigureAwait(false);
			if (value.StatusCode.IsGood())
			{
				var extensionObject = value.WrappedValue.ToExtensionObject();
				if (extensionObject != null)
				{
					var genericObject = await ParseValueAsync(extensionObject).ConfigureAwait(false);
					return genericObject.TypeDefinition;
				}
			}

			throw new StatusException(new StatusCode(StatusCodes.BadDataUnavailable));
		}

		public GenericEncodeableObject ParseValue(ExtensionObject extensionObject)
		{
			var encodeableObject = _dataTypeManager.ParseValue(extensionObject);
			return encodeableObject;
		}

		public async Task<GenericEncodeableObject> ParseValueAsync(ExtensionObject extensionObject)
		{
			var encodeableObject = await _dataTypeManager.ParseValueAsync(extensionObject).ConfigureAwait(false);
			return encodeableObject;
		}

		public IList<NodeId> RegisterNodes(IEnumerable<string> ids)
		{
			var namespaceIndex = GetNamespaceIndex();
			var nodeIds = ids.Select(id => new NodeId(id, namespaceIndex)).ToList();
			return RegisterNodes(nodeIds);
		}

		public async Task<IList<NodeId>> RegisterNodesAsync(IEnumerable<string> ids)
		{
			var namespaceIndex = await GetNamespaceIndexAsync().ConfigureAwait(false);
			var nodeIds = ids.Select(id => new NodeId(id, namespaceIndex)).ToList();
			return await RegisterNodesAsync(nodeIds).ConfigureAwait(false);
		}

		public IList<NodeId> RegisterNodes(IList<NodeId> nodeIds)
		{
			EnsureConnected();
			var registeredNodes = Session.RegisterNodes(nodeIds, RequestSettings);
			return registeredNodes;
		}

		public async Task<IList<NodeId>> RegisterNodesAsync(IList<NodeId> nodeIds)
		{
			await EnsureConnectedAsync();
			var registeredNodes = await Session.RegisterNodesAsync(nodeIds, RequestSettings).ConfigureAwait(false);
			return registeredNodes;
		}

		public DataValue Read(string id,
			uint attributeId = Attributes.Value, uint maxAge = 0, TimestampsToReturn timestamps = TimestampsToReturn.Neither)
		{
			return Read(new[] { id }, attributeId, maxAge, timestamps)[0];
		}

		public async Task<DataValue> ReadAsync(string id,
			uint attributeId = Attributes.Value, uint maxAge = 0, TimestampsToReturn timestamps = TimestampsToReturn.Neither)
		{
			return (await ReadAsync(new[] { id }, attributeId, maxAge, timestamps).ConfigureAwait(false))[0];
		}

		public DataValue Read(NodeId nodeId,
			uint attributeId = Attributes.Value, uint maxAge = 0, TimestampsToReturn timestamps = TimestampsToReturn.Neither)
		{
			return Read(new[] { nodeId }, attributeId, maxAge, timestamps)[0];
		}

		public async Task<DataValue> ReadAsync(NodeId nodeId,
			uint attributeId = Attributes.Value, uint maxAge = 0, TimestampsToReturn timestamps = TimestampsToReturn.Neither)
		{
			return (await ReadAsync(new[] { nodeId }, attributeId, maxAge, timestamps).ConfigureAwait(false))[0];
		}

		public IList<DataValue> Read(IEnumerable<string> ids,
			uint attributeId = Attributes.Value, uint maxAge = 0, TimestampsToReturn timestamps = TimestampsToReturn.Neither)
		{
			var namespaceIndex = GetNamespaceIndex();
			var nodeIds = ids.Select(id => new NodeId(id, namespaceIndex));
			return Read(nodeIds, attributeId, maxAge, timestamps);
		}

		public async Task<IList<DataValue>> ReadAsync(IEnumerable<string> ids,
			uint attributeId = Attributes.Value, uint maxAge = 0, TimestampsToReturn timestamps = TimestampsToReturn.Neither)
		{
			var namespaceIndex = await GetNamespaceIndexAsync().ConfigureAwait(false);
			var nodeIds = ids.Select(id => new NodeId(id, namespaceIndex));
			return await ReadAsync(nodeIds, attributeId, maxAge, timestamps).ConfigureAwait(false);
		}

		public IList<DataValue> Read(IEnumerable<NodeId> nodeIds,
			uint attributeId = Attributes.Value, uint maxAge = 0, TimestampsToReturn timestamps = TimestampsToReturn.Neither)
		{
			EnsureConnected();
			var readValueIds = ToReadValueIds(nodeIds, attributeId);
			var values = Session.Read(readValueIds, maxAge, timestamps, RequestSettings);
			return values;
		}

		public async Task<IList<DataValue>> ReadAsync(IEnumerable<NodeId> nodeIds,
			uint attributeId = Attributes.Value, uint maxAge = 0, TimestampsToReturn timestamps = TimestampsToReturn.Neither)
		{
			await EnsureConnectedAsync().ConfigureAwait(false);
			var readValueIds = ToReadValueIds(nodeIds, attributeId);
			var values = await Session.ReadAsync(readValueIds, maxAge, timestamps, RequestSettings).ConfigureAwait(false);
			return values;
		}

		public StatusCode Write(string id, Variant value)
		{
			return Write(new[] { id }, new[] { value })[0];
		}

		public async Task<StatusCode> WriteAsync(string id, Variant value)
		{
			return (await WriteAsync(new[] { id }, new[] { value }).ConfigureAwait(false))[0];
		}

		public StatusCode Write(NodeId nodeId, Variant value)
		{
			return Write(new[] { nodeId }, new[] { value })[0];
		}

		public async Task<StatusCode> WriteAsync(NodeId nodeId, Variant value)
		{
			return (await WriteAsync(new[] { nodeId }, new[] { value }).ConfigureAwait(false))[0];
		}

		public IList<StatusCode> Write(IEnumerable<string> ids, IEnumerable<Variant> values)
		{
			var namespaceIndex = GetNamespaceIndex();
			var nodeIds = ids.Select(id => new NodeId(id, namespaceIndex));
			return Write(nodeIds, values);
		}

		public async Task<IList<StatusCode>> WriteAsync(IEnumerable<string> ids, IEnumerable<Variant> values)
		{
			var namespaceIndex = await GetNamespaceIndexAsync().ConfigureAwait(false);
			var nodeIds = ids.Select(id => new NodeId(id, namespaceIndex));
			return await WriteAsync(nodeIds, values).ConfigureAwait(false);
		}

		public IList<StatusCode> Write(IEnumerable<NodeId> nodeIds, IEnumerable<Variant> values, int arrayIndex = -1)
		{
			EnsureConnected();
			var valuesToWrite = ToWriteValues(nodeIds, values, arrayIndex);
            var statusCodes = Session.Write(valuesToWrite, RequestSettings);
			return statusCodes;
		}

		public async Task<IList<StatusCode>> WriteAsync(IEnumerable<NodeId> nodeIds, IEnumerable<Variant> values)
		{
			await EnsureConnectedAsync().ConfigureAwait(false);
			var valuesToWrite = ToWriteValues(nodeIds, values);
			var statusCodes = await Session.WriteAsync(valuesToWrite, RequestSettings).ConfigureAwait(false);
			return statusCodes;
		}

		public IList<StatusCode> Write(ICollection<NodeIdValuePair> pairs)
		{
			return Write(pairs.Select(p => p.NodeId), pairs.Select(p => p.Value));
		}

		public async Task<IList<StatusCode>> WriteAsync(ICollection<NodeIdValuePair> pairs)
		{
			return await WriteAsync(pairs.Select(p => p.NodeId), pairs.Select(p => p.Value)).ConfigureAwait(false);
		}

		public SessionExtensions.CallResult CallMethod(string instanceName, string methodName)
		{
			return CallMethod(instanceName, methodName, new List<Variant>());
		}

		public async Task<CallMethodResult> CallMethodAsync(string instanceName, string methodName)
		{
			return await CallMethodAsync(instanceName, methodName, new List<Variant>()).ConfigureAwait(false);
		}

		public SessionExtensions.CallResult CallMethod(string instanceName, string methodName, List<Variant> inputArguments)
		{
			var namespaceIndex = GetNamespaceIndex();
			var objectToCall = new NodeId(instanceName, namespaceIndex);
			var methodToCall = new NodeId(instanceName + "." + methodName, namespaceIndex);
			return CallMethod(objectToCall, methodToCall, inputArguments);
		}

		public async Task<CallMethodResult> CallMethodAsync(string instanceName, string methodName, List<Variant> inputArguments)
		{
			var namespaceIndex = await GetNamespaceIndexAsync().ConfigureAwait(false);
			var objectToCall = new NodeId(instanceName, namespaceIndex);
			var methodToCall = new NodeId(instanceName + "." + methodName, namespaceIndex);
			return await CallMethodAsync(objectToCall, methodToCall, inputArguments).ConfigureAwait(false);
		}

		public SessionExtensions.CallResult CallMethod(NodeId objectToCall, NodeId methodToCall)
		{
			return CallMethod(objectToCall, methodToCall, new List<Variant>());
		}

		public async Task<CallMethodResult> CallMethodAsync(NodeId objectToCall, NodeId methodToCall)
		{
			return await CallMethodAsync(objectToCall, methodToCall, new List<Variant>()).ConfigureAwait(false);
		}

		public SessionExtensions.CallResult CallMethod(NodeId objectToCall, NodeId methodToCall, List<Variant> inputArguments)
		{
			EnsureConnected();

			List<StatusCode> inputArgumentErrors;
			List<Variant> outputArguments;

			var statusCode = Session.Call(objectToCall, methodToCall, inputArguments, RequestSettings, out inputArgumentErrors, out outputArguments);

			return new SessionExtensions.CallResult
			{
				StatusCode = statusCode,
				InputArgumentErrors = inputArgumentErrors,
				OutputArguments = outputArguments
			};
		}

		public async Task<CallMethodResult> CallMethodAsync(NodeId objectToCall, NodeId methodToCall, List<Variant> inputArguments)
		{
			await EnsureConnectedAsync().ConfigureAwait(false);
			var callResult = await Session.CallAsync(objectToCall, methodToCall, inputArguments, RequestSettings).ConfigureAwait(false);
			return callResult;
		}

		public void CreateSubscription()
		{
			_subscription.Create(RequestSettings);
		}

		public async Task CreateSubscriptionAsync()
		{
			await _subscription.CreateAsync(RequestSettings).ConfigureAwait(false);
		}

		public void DeleteSubscription()
		{
			_subscription.Delete(RequestSettings);
		}

		public async Task DeleteSubscriptionAsync()
		{
			await _subscription.DeleteAsync(RequestSettings).ConfigureAwait(false);
		}

		public StatusCode AddDataMonitoredNode(string id)
		{
			return AddDataMonitoredNodes(new[] { id })[0];
		}

		public async Task<StatusCode> AddDataMonitoredNodeAsync(string id)
		{
			return (await AddDataMonitoredNodesAsync(new[] { id }).ConfigureAwait(false))[0];
		}

		public StatusCode AddDataMonitoredNode(NodeId nodeId)
		{
			return AddDataMonitoredNodes(new[] { nodeId })[0];
		}

		public async Task<StatusCode> AddDataMonitoredNodeAsync(NodeId nodeId)
		{
			return (await AddDataMonitoredNodesAsync(new[] { nodeId }).ConfigureAwait(false))[0];
		}

		public IList<StatusCode> AddDataMonitoredNodes(IEnumerable<string> ids)
		{
			var namespaceIndex = GetNamespaceIndex();
			var nodeIds = ids.Select(id => new NodeId(id, namespaceIndex));
			return AddDataMonitoredNodes(nodeIds);
		}

		public async Task<IList<StatusCode>> AddDataMonitoredNodesAsync(IEnumerable<string> ids)
		{
			var namespaceIndex = await GetNamespaceIndexAsync().ConfigureAwait(false);
			var nodeIds = ids.Select(id => new NodeId(id, namespaceIndex));
			return await AddDataMonitoredNodesAsync(nodeIds).ConfigureAwait(false);
		}

		public IList<StatusCode> AddDataMonitoredNodes(IEnumerable<NodeId> nodeIds)
		{
			var monitoredItems = AddDataMonitoredItems(nodeIds);
			var statusCodes = _subscription.CreateMonitoredItems(monitoredItems, RequestSettings);
			return statusCodes;
		}

		public async Task<IList<StatusCode>> AddDataMonitoredNodesAsync(IEnumerable<NodeId> nodeIds)
		{
			var monitoredItems = AddDataMonitoredItems(nodeIds);
			var statusCodes = await _subscription.CreateMonitoredItemsAsync(monitoredItems, RequestSettings).ConfigureAwait(false);
			return statusCodes;
		}

		public StatusCode RemoveDataMonitoredNode(string id)
		{
			return RemoveDataMonitoredNodes(new[] { id })[0];
		}

		public async Task<StatusCode> RemoveDataMonitoredNodeAsync(string id)
		{
			return (await RemoveDataMonitoredNodesAsync(new[] { id }).ConfigureAwait(false))[0];
		}

		public StatusCode RemoveDataMonitoredNode(NodeId nodeId)
		{
			return RemoveDataMonitoredNodes(new[] { nodeId })[0];
		}

		public async Task<StatusCode> RemoveDataMonitoredNodeAsync(NodeId nodeId)
		{
			return (await RemoveDataMonitoredNodesAsync(new[] { nodeId }).ConfigureAwait(false))[0];
		}

		public IList<StatusCode> RemoveDataMonitoredNodes(IEnumerable<string> ids)
		{
			var namespaceIndex = GetNamespaceIndex();
			var nodeIds = ids.Select(id => new NodeId(id, namespaceIndex));
			return RemoveDataMonitoredNodes(nodeIds);
		}

		public async Task<IList<StatusCode>> RemoveDataMonitoredNodesAsync(IEnumerable<string> ids)
		{
			var namespaceIndex = await GetNamespaceIndexAsync().ConfigureAwait(false);
			var nodeIds = ids.Select(id => new NodeId(id, namespaceIndex));
			return await RemoveDataMonitoredNodesAsync(nodeIds).ConfigureAwait(false);
		}

		public IList<StatusCode> RemoveDataMonitoredNodes(IEnumerable<NodeId> nodeIds)
		{
			var monitoredItems = RemoveDataMonitoredItems(nodeIds);
			var statusCodes = _subscription.DeleteMonitoredItems(monitoredItems, RequestSettings);
			return statusCodes;
		}

		public async Task<IList<StatusCode>> RemoveDataMonitoredNodesAsync(IEnumerable<NodeId> nodeIds)
		{
			var monitoredItems = RemoveDataMonitoredItems(nodeIds);
			var statusCodes = await _subscription.DeleteMonitoredItemsAsync(monitoredItems, RequestSettings).ConfigureAwait(false);
			return statusCodes;
		}

		public DataValue GetDataMonitoredNodeValue(string id)
		{
			DataMonitoredItem item;
			return _dataMonitoredItemMap.TryGetValue(new NodeId(id, (ushort)_namespaceIndex), out item)
				? (DataValue)item.UserData
				: new DataValue(new StatusCode(StatusCodes.BadNoEntryExists));
		}

		public DataValue GetDataMonitoredNodeValue(NodeId nodeId)
		{
			DataMonitoredItem item;
			return _dataMonitoredItemMap.TryGetValue(nodeId, out item)
				? (DataValue)item.UserData
				: new DataValue(new StatusCode(StatusCodes.BadNoEntryExists));
		}

		public IList<DataValue> GetDataMonitoredNodeValues(IEnumerable<string> ids)
		{
			return ids.Select(GetDataMonitoredNodeValue).ToList();
		}

		public IList<DataValue> GetDataMonitoredNodeValues(IEnumerable<NodeId> nodeIds)
		{
			return nodeIds.Select(GetDataMonitoredNodeValue).ToList();
		}
		#endregion

		#region IDisposable
		public void Dispose()
		{
			_dataTypeManager.Dispose();
		}
		#endregion

		#region Event Handlers
		private void OnSubscriptionDataChanged(Subscription subscription, DataChangedEventArgs e)
		{
			foreach (var dataChange in e.DataChanges)
			{
				dataChange.MonitoredItem.UserData = dataChange.Value;
			}
		}
		#endregion

		#region Overrides
		public override string ToString()
		{
			return string.IsNullOrEmpty(NamespaceUri) ? EndpointUrl : EndpointUrl + $" ({NamespaceUri})";
		}
		#endregion

		#region Private Methods
		private ushort GetNamespaceIndex()
		{
			if (string.IsNullOrEmpty(NamespaceUri))
			{
				return 0;
			}

			EnsureConnected();
			return FindNamespaceIndex();
		}

		private async Task<ushort> GetNamespaceIndexAsync()
		{
			if (string.IsNullOrEmpty(NamespaceUri))
			{
				return 0;
			}

			await EnsureConnectedAsync().ConfigureAwait(false);
			return FindNamespaceIndex();
		}

		private ushort FindNamespaceIndex()
		{
			if (_namespaceIndex < 0 && Session.NamespaceUris != null)
			{
				for (var i = 0; i < Session.NamespaceUris.Count; ++i)
				{
					if (string.Equals(Session.NamespaceUris[i], NamespaceUri, StringComparison.InvariantCultureIgnoreCase))
					{
						_namespaceIndex = i;
						break;
					}
				}
			}

			if (_namespaceIndex >= 0)
			{
				return (ushort)_namespaceIndex;
			}

			var message = $"Namespace '{NamespaceUri}' not found in server namespace table.";
			throw new InvalidOperationException(message);
		}

		private static List<ReadValueId> ToReadValueIds(IEnumerable<NodeId> nodeIds, uint attributeId)
		{
			var readValueIds = nodeIds.Select(nodeId =>
				new ReadValueId
				{
					NodeId = nodeId,
					AttributeId = attributeId
				}).ToList();
			return readValueIds;
		}

		private static List<WriteValue> ToWriteValues(IEnumerable<NodeId> nodeIds, IEnumerable<Variant> values, int arrayIndex = -1)
		{
			List<WriteValue> valuesToWrite;
			if (arrayIndex >= 0)
			{
				var range = ""+arrayIndex;// + ":" + arrayIndex;
				valuesToWrite = nodeIds.Select(id =>
					new WriteValue
					{
						NodeId = id,
						AttributeId = Attributes.Value,
						IndexRange = range
						
					}).ToList();

			}
			else
			{
				valuesToWrite = nodeIds.Select(id =>
					new WriteValue
					{
						NodeId = id,
						AttributeId = Attributes.Value
					}).ToList();
			}

			var valueList = values.ToList();

			for (var i = 0; i < valuesToWrite.Count; ++i)
			{
				valuesToWrite[i].Value = i < valueList.Count ? new DataValue { WrappedValue = valueList[i] } : null;
			}

			return valuesToWrite;
		}

		private List<MonitoredItem> AddDataMonitoredItems(IEnumerable<NodeId> nodeIds)
		{
			var monitoredItems = new List<MonitoredItem>();

			foreach (var nodeId in nodeIds)
			{
				if (_dataMonitoredItemMap.ContainsKey(nodeId))
				{
					continue;
				}

				var item = new DataMonitoredItem(nodeId)
				{
					UserData = new DataValue(new StatusCode(StatusCodes.Bad))
				};

				_dataMonitoredItemMap.Add(nodeId, item);
				monitoredItems.Add(item);
			}

			return monitoredItems;
		}

		private List<MonitoredItem> RemoveDataMonitoredItems(IEnumerable<NodeId> nodeIds)
		{
			var monitoredItems = new List<MonitoredItem>();

			foreach (var nodeId in nodeIds)
			{
				DataMonitoredItem item;
				if (!_dataMonitoredItemMap.TryGetValue(nodeId, out item))
				{
					continue;
				}

				_dataMonitoredItemMap.Remove(nodeId);
				monitoredItems.Add(item);
			}

			return monitoredItems;
		}

		private void BeforeConnect()
		{
			Session.AutomaticReconnect = true;
			Session.UseDnsNameAndPortFromDiscoveryUrl = true;
		}

		private void AfterConnect()
		{
			if (Session.SubscriptionCount > 0)
			{
				_subscription.Recreate(RequestSettings);
			}
		}
		#endregion
	}
}