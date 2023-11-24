using System;
using OpcUaTypes;

namespace OpcUaTypes
{
    public class OpcUaSubscriptionArgs : EventArgs
    {
        public OpcUaSubscriptionArgs(OpcUaSubscription subscription, OpcUaConnectionParams connectionParams)
        {
            Subscription = subscription;
            ConnectionParams = connectionParams;

        }

        public OpcUaSubscription Subscription { get; }
        public OpcUaConnectionParams ConnectionParams { get; }
    }
}
