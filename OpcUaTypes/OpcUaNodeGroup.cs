using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpcUaTypes
{
    public class OpcUaNodeGroup
    {
        public List<OpcUaNode> NodesList;
        public string GroupName;
        private int count;

        public OpcUaNodeGroup(string name)
        {
            NodesList = new List<OpcUaNode>();
            GroupName = name;
        }

        public int Count { get => NodesList.Count; }

        public void Add(OpcUaNode node)
        {
            NodesList.Add(node);
        }

        public void Add(List<OpcUaNode> list)
        {
            NodesList.AddRange(list);
        }

        public void Clear()
        {
            NodesList.Clear();
        }
    }
}
