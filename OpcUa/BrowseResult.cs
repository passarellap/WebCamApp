//-----------------------------------------------------------------------------
// Copyright (c) 2017 by Bystronic Laser AG, CH-3362 Niederönz
//-----------------------------------------------------------------------------
// $LastChangedRevision: 456 $
// $LastChangedDate: 2017-09-29 08:38:10 +0200 (Fr., 29 Sep 2017) $
// $Author: Land $
//-----------------------------------------------------------------------------

#region Namespaces
using System.Collections.Generic;
using UnifiedAutomation.UaBase;
#endregion

namespace Bystronic.IoT.OpcUaClientHelper
{
	public sealed class BrowseResult
	{
		#region Constructor
		public BrowseResult(IList<ReferenceDescription> referenceDescriptions, byte[] continuationPoint)
		{
			ReferenceDescriptions = referenceDescriptions;
			ContinuationPoint = continuationPoint;
		}
		#endregion

		#region Properties
		public IList<ReferenceDescription> ReferenceDescriptions { get; }
		public byte[] ContinuationPoint { get; }
		#endregion
	}
}