//-----------------------------------------------------------------------------
// Copyright (c) 2017 by Bystronic Laser AG, CH-3362 Niederönz
//-----------------------------------------------------------------------------
// $LastChangedRevision: 456 $
// $LastChangedDate: 2017-09-29 08:38:10 +0200 (Fr., 29 Sep 2017) $
// $Author: Land $
//-----------------------------------------------------------------------------

#region Namespaces
using UnifiedAutomation.UaBase;
#endregion

namespace Bystronic.IoT.OpcUaClientHelper
{
	public sealed class NodeIdValuePair
	{
		#region Constructors
		public NodeIdValuePair()
		{
		}

		public NodeIdValuePair(NodeId nodeId, Variant value)
		{
			NodeId = nodeId;
			Value = value;
		}
		#endregion

		#region Properties
		public NodeId NodeId { get; set; }
		public Variant Value { get; set; }
		#endregion
	}
}