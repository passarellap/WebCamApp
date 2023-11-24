using System;
using OpcUaTypes;

namespace OpcUaTypes
{
	public interface IOpcUaCaller
	{
		event Action<OpcUaMethodArgs> MethodCalled;
        event CallMethodSyncDelegate CallMethodSync;
    }
}
