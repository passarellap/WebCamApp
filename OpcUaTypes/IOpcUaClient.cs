using System;
using Bystronic.DCLoadUnload.OpcUaApiModule.Classes;
using OpcUaTypes;
using UnifiedAutomation.UaClient;

namespace OpcUaTypes
{
	public interface IOpcUaClient
	{
		void OnOpcUaConnectionBroken(OpcUaConnection connection);
		void OnOpcUaConnectionEstablished(OpcUaConnection connection);

		OpcUaConnectionParams GetConnectionParams();

	}
}
