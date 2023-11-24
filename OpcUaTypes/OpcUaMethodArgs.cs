using System;
using System.Collections.Generic;
using OpcUaTypes;
using UnifiedAutomation.UaBase;

namespace OpcUaTypes
{
    public class OpcUaMethodArgs : EventArgs
    {
        public OpcUaMethod Method { get; }
        public List<Variant> InputArguments { get; }
        public OpcUaConnectionParams ConnectionParams { get; }

        public OpcUaMethodArgs(OpcUaMethod method, List<Variant> inputArguments, OpcUaConnectionParams connectionParams)
        {
            Method = method;
            InputArguments = inputArguments;
            ConnectionParams = connectionParams;

        }
    }
}
