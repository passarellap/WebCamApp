using System;
using System.Collections.Generic;
using OpcUaTypes;
using UnifiedAutomation.UaClient;

namespace OpcUaTypes
{
    public class OpcUaSubscription
    {
        public Subscription Subscription;

        public string Name { get; set; }

        public double PublishingInterval { get; }

        public Action<List<OpcUaDataChangedArgs>> OnOpcUaNewDataReceived { get; set; }

        public List<OpcUaNodeGroup> GroupsList;

        public OpcUaConnectionParams ConnectionParams;

        public OpcUaSubscription(string name, double publishingInterval, OpcUaConnectionParams connectionParams)
        {

            PublishingInterval = publishingInterval;
            Name = name;

            GroupsList = new List<OpcUaNodeGroup>();

            ConnectionParams = connectionParams;

        }

        public void AddGroup(OpcUaNodeGroup group)
        {
            GroupsList.Add(group);
        }
    }
}
