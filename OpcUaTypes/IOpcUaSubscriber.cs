using System;
using System.Collections.Generic;
using OpcUaTypes;
using OpcUaTypes;

namespace OpcUaTypes
{
	public interface IOpcUaSubscriber
	{	

		event Action<OpcUaSubscriptionArgs> NewSubscriptionCreated;
		event Action RefreshData;

		void InitSubscriptions();
		//void OpcUaNewDataReceived(List<OpcUaDataChangedArgs> args);

	}
}
