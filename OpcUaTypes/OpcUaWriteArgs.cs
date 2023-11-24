using System;
using System.Collections.Generic;
using OpcUaTypes;
using UnifiedAutomation.UaBase;

namespace OpcUaTypes
{
    public class OpcUaWriteArgs : EventArgs
    {
		public OpcUaWriteArgs(string nodeAddress, object value, OpcUaConnectionParams connectionParams)
		{
			this.nodeAddress = nodeAddress;
			this.value = value;
			this.arrayIndex = -1;
			ConnectionParams = connectionParams;
		}

		public OpcUaWriteArgs(string nodeAddress, object value, int arrayIndex, OpcUaConnectionParams connectionParams)
		{
			this.nodeAddress = nodeAddress;
			this.value = value;
			this.arrayIndex = arrayIndex;
			ConnectionParams = connectionParams;
		}

		public string nodeAddress { get; }
	    public object value { get; }
		public int arrayIndex { get; }
		public OpcUaConnectionParams ConnectionParams { get; }
	    
    }
}
