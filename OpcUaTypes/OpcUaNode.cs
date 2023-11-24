using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpcUaTypes
{
    public class OpcUaNode
    {
        public string NodeAddress;
        public string NodeName;

        public OpcUaNode(string name, string addr)
        {
            NodeName = name;
            NodeAddress = addr;
        }
    }
}
