using System;
using System.Collections.Generic;
using OpcUaTypes;

namespace OpcUaTypes
{
	public interface IOpcUaWriter
	{
		event Action<OpcUaWriteArgs> WriteData;
		event Action<List<OpcUaWriteArgs>> WriteDataList;
	}
}
