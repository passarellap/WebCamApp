//-----------------------------------------------------------------------------
// Copyright (c) 2017 by Bystronic Laser AG, CH-3362 Niederönz
//-----------------------------------------------------------------------------
// $LastChangedRevision: 456 $
// $LastChangedDate: 2017-09-29 08:38:10 +0200 (Fr., 29 Sep 2017) $
// $Author: Land $
//-----------------------------------------------------------------------------

#region Namespaces
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnifiedAutomation.UaBase;
using UnifiedAutomation.UaClient;
#endregion

namespace Bystronic.IoT.OpcUaClientHelper
{
	public static class SubscriptionExtensions
	{
		#region Public Methods
		public static Task CreateAsync(this Subscription subscription,
			RequestSettings requestSettings)
		{
			var tcs = new TaskCompletionSource<bool>();

			subscription.BeginCreate(requestSettings,
				asyncResult =>
				{
					try
					{
						subscription.EndCreate(asyncResult);
						tcs.SetResult(true);
					}
					catch (Exception ex)
					{
						tcs.SetException(ex);
					}
				},
				null);

			return tcs.Task;
		}

		public static Task DeleteAsync(this Subscription subscription,
			RequestSettings requestSettings)
		{
			var tcs = new TaskCompletionSource<bool>();

			subscription.BeginDelete(requestSettings,
				asyncResult =>
				{
					try
					{
						subscription.EndDelete(asyncResult);
						tcs.SetResult(true);
					}
					catch (Exception ex)
					{
						tcs.SetException(ex);
					}
				},
				null);

			return tcs.Task;
		}

		public static Task<List<StatusCode>> CreateMonitoredItemsAsync(this Subscription subscription,
			IList<MonitoredItem> monitoredItems,
			RequestSettings requestSettings)
		{
			var tcs = new TaskCompletionSource<List<StatusCode>>();

			subscription.BeginCreateMonitoredItems(monitoredItems, requestSettings,
				asyncResult =>
				{
					try
					{
						var statusCodes = subscription.EndCreateMonitoredItems(asyncResult);
						tcs.SetResult(statusCodes);
					}
					catch (Exception ex)
					{
						tcs.SetException(ex);
					}
				},
				null);

			return tcs.Task;
		}

		public static Task<List<StatusCode>> DeleteMonitoredItemsAsync(this Subscription subscription,
			IList<MonitoredItem> monitoredItems,
			RequestSettings requestSettings)
		{
			var tcs = new TaskCompletionSource<List<StatusCode>>();

			subscription.BeginDeleteMonitoredItems(monitoredItems, requestSettings,
				asyncResult =>
				{
					try
					{
						var statusCodes = subscription.EndDeleteMonitoredItems(asyncResult);
						tcs.SetResult(statusCodes);
					}
					catch (Exception ex)
					{
						tcs.SetException(ex);
					}
				},
				null);

			return tcs.Task;
		}

		public static Task<List<StatusCode>> ModifyMonitoredItemsAsync(this Subscription subscription,
			IList<MonitoredItem> monitoredItems,
			RequestSettings requestSettings)
		{
			var tcs = new TaskCompletionSource<List<StatusCode>>();

			subscription.BeginModifyMonitoredItems(monitoredItems, requestSettings,
				asyncResult =>
				{
					try
					{
						var statusCodes = subscription.EndModifyMonitoredItems(asyncResult);
						tcs.SetResult(statusCodes);
					}
					catch (Exception ex)
					{
						tcs.SetException(ex);
					}
				},
				null);

			return tcs.Task;
		}

		public static Task<NotificationMessage> RepublishAsync(this Subscription subscription,
			uint retransmitSequenceNumber,
			RequestSettings requestSettings)
		{
			var tcs = new TaskCompletionSource<NotificationMessage>();

			subscription.BeginRepublish(retransmitSequenceNumber, requestSettings,
				asyncResult =>
				{
					try
					{
						var message = subscription.EndRepublish(asyncResult);
						tcs.SetResult(message);
					}
					catch (Exception ex)
					{
						tcs.SetException(ex);
					}
				},
				null);

			return tcs.Task;
		}

		public static Task<List<StatusCode>> SetMonitoringModeAsync(this Subscription subscription,
			MonitoringMode monitoringMode,
			IList<MonitoredItem> monitoredItems,
			RequestSettings requestSettings)
		{
			var tcs = new TaskCompletionSource<List<StatusCode>>();

			subscription.BeginSetMonitoringMode(monitoringMode, monitoredItems, requestSettings,
				asyncResult =>
				{
					try
					{
						var statusCodes = subscription.EndSetMonitoringMode(asyncResult);
						tcs.SetResult(statusCodes);
					}
					catch (Exception ex)
					{
						tcs.SetException(ex);
					}
				},
				null);

			return tcs.Task;
		}

		public static Task<SetTriggeringResult> SetTriggeringAsync(this Subscription subscription,
			MonitoredItem triggeringItem,
			IList<MonitoredItem> linksToAdd,
			IList<MonitoredItem> linksToRemove,
			RequestSettings requestSettings)
		{
			var tcs = new TaskCompletionSource<SetTriggeringResult>();

			subscription.BeginSetTriggering(triggeringItem, linksToAdd, linksToRemove, requestSettings,
				asyncResult =>
				{
					try
					{
						var result = subscription.EndSetTriggering(asyncResult);
						tcs.SetResult(result);
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