using System;
using Newtonsoft.Json.Linq;
using OpcUaTypes;

namespace OpcUaTypes
{
	public interface IOpcUaReader
	{
		event ReadNodeIdDelegate ReadNodeId;
		event ReadListNodeIdDelegate ReadListNodeId;
	}
}
