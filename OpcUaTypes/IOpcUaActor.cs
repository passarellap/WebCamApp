using Bystronic.DCLoadUnload.OpcUaApiModule.Classes;
using UnifiedAutomation.UaClient;

namespace OpcUaTypes
{
	public interface IOpcUaActor
	{
		event Action<bool> OnOpcUaConnectionStatusChanged;
		event Action<OpcUaSubscriptionArgs> NewSubscriptionCreated;
		event Action<OpcUaMethodArgs> MethodCalled;
        event CallMethodSyncDelegate CallMethodSync;
        event Action RefreshData;
		event ReadNodeIdDelegate ReadNodeId;
		event ReadListNodeIdDelegate ReadListNodeId;
		void InitSubscriptions();
		void OpcUaNewDataReceived(List<OpcUaDataChangedArgs> e);
		void OpcUaConnectionStatusChanged(bool status);
		event Action<OpcUaWriteArgs> WriteData;
		event Action<List<OpcUaWriteArgs>> WriteDataList;
		OpcUaConnectionParams GetConnectionParams();
        void OnOpcUaConnectionEstablished(OpcUaConnection connection);
        void OnOpcUaConnectionBroken(OpcUaConnection connection);
    }
}
