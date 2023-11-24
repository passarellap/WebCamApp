//-----------------------------------------------------------------------------
// Copyright (c) 2017 by Bystronic Laser AG, CH-3362 Niederönz
//-----------------------------------------------------------------------------
// $LastChangedRevision: 456 $
// $LastChangedDate: 2017-09-29 08:38:10 +0200 (Fr., 29 Sep 2017) $
// $Author: Land $
//-----------------------------------------------------------------------------

#region Namespaces
using System;
using System.Threading.Tasks;
using UnifiedAutomation.UaBase;
using UnifiedAutomation.UaClient;
#endregion

namespace Bystronic.IoT.OpcUaClientHelper
{
	public static class DataTypeManagerExtensions
	{
		#region Public Methods
		public static Task<GenericEncodeableObject> ParseValueAsync(this DataTypeManager dataTypeManager, ExtensionObject extensionObject)
		{
			var tcs = new TaskCompletionSource<GenericEncodeableObject>();

			dataTypeManager.BeginParseValue(extensionObject,
				asyncResult =>
				{
					try
					{
						var encodeableObject = dataTypeManager.EndParseValue(asyncResult);
						tcs.SetResult(encodeableObject);
					}
					catch (Exception ex)
					{
						tcs.SetException(ex);
					}
				},
				null);

			return tcs.Task;
		}
		#endregion
	}
}