using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpcUaTypes
{
	public delegate void ReadNodeIdDelegate(string nodeId, string varName, OpcUaConnectionParams connectionParams, out JObject val);
	public delegate void ReadListNodeIdDelegate(Dictionary<string, string> nodeNames, OpcUaConnectionParams connectionParams, out List<JObject> vals);
    public delegate void CallMethodSyncDelegate(OpcUaMethodArgs args, OpcUaConnectionParams connectionParams, out JObject result);

}
