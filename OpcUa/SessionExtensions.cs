//-----------------------------------------------------------------------------
// Copyright (c) 2017 by Bystronic Laser AG, CH-3362 Niederönz
//-----------------------------------------------------------------------------
// $LastChangedRevision: 648 $
// $LastChangedDate: 2017-12-13 13:23:32 +0100 (Mi., 13 Dez 2017) $
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
	public static class SessionExtensions
	{
		#region Public Methods
		public static Task ConnectAsync(this Session session,
			string discoveryUrl,
			SecuritySelection securitySelection,
			string transportProfileUri,
			RetryInitialConnect retryInitialConnect,
			RequestSettings requestSettings)
		{
			var tcs = new TaskCompletionSource<bool>();

			session.BeginConnect(discoveryUrl, securitySelection, transportProfileUri, retryInitialConnect, requestSettings,
				asyncResult =>
				{
					try
					{
						session.EndConnect(asyncResult);
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

		public static Task DisconnectAsync(this Session session,
			SubscriptionCleanupPolicy cleanupPolicy,
			RequestSettings requestSettings)
		{
			var tcs = new TaskCompletionSource<bool>();

			session.BeginDisconnect(cleanupPolicy, requestSettings,
				asyncResult =>
				{
					try
					{
						session.EndDisconnect(asyncResult);
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

		public static async Task<CallResult> CallAsync(this Session session,
			NodeId objectToCall,
			NodeId methodToCall,
			List<Variant> inputArguments,
			RequestSettings requestSettings)
		{
			var tcs = new TaskCompletionSource<CallResult>();

			session.BeginCall(objectToCall, methodToCall, inputArguments, requestSettings,
				asyncResult =>
				{
					try
					{
						List<StatusCode> inputArgumentErrors;
						List<Variant> outputArguments;
						var statusCode = session.EndCall(asyncResult, out inputArgumentErrors, out outputArguments);
						var result = new CallResult
						{
							StatusCode = statusCode,
							InputArgumentErrors = inputArgumentErrors,
							OutputArguments = outputArguments
						};
						tcs.SetResult(result);
					}
					catch (Exception ex)
					{
						tcs.SetException(ex);
					}
				},
				null);

			return await tcs.Task;
		}

		public static async Task<IList<DataValue>> ReadAsync(this Session session,
			IList<ReadValueId> readValueIds,
			uint maxAge,
			TimestampsToReturn timestamps,
			RequestSettings requestSettings)
		{
			var tcs = new TaskCompletionSource<IList<DataValue>>();

			session.BeginRead(readValueIds, maxAge, timestamps, requestSettings,
				asyncResult =>
				{
					try
					{
						var values = session.EndRead(asyncResult);
						tcs.SetResult(values);
					}
					catch (Exception ex)
					{
						tcs.SetException(ex);
					}
				}, null);

			return await tcs.Task;
		}

		public static async Task<List<StatusCode>> WriteAsync(this Session session,
			IList<WriteValue> valuesToWrite,
			RequestSettings requestSettings)
		{
			var tcs = new TaskCompletionSource<List<StatusCode>>();

			session.BeginWrite(valuesToWrite, requestSettings,
				asyncResult =>
				{
					try
					{
						var statusCodes = session.EndWrite(asyncResult);
						tcs.SetResult(statusCodes);
					}
					catch (Exception ex)
					{
						tcs.SetException(ex);
					}
				}, null);

			return await tcs.Task;
		}

		public static async Task<IList<NodeId>> RegisterNodesAsync(this Session session,
			IList<NodeId> nodesToRegister,
			RequestSettings requestSettings)
		{
			var tcs = new TaskCompletionSource<List<NodeId>>();

			session.BeginRegisterNodes(nodesToRegister, requestSettings,
				asyncResult =>
				{
					try
					{
						var nodeIds = session.EndRegisterNodes(asyncResult);
						tcs.SetResult(nodeIds);
					}
					catch (Exception ex)
					{
						tcs.SetException(ex);
					}
				}, null);

			return await tcs.Task;
		}

		public static async Task<BrowseResult> BrowseAsync(this Session session,
			NodeId nodeToBrowse)
		{
			var tcs = new TaskCompletionSource<BrowseResult>();

			session.BeginBrowse(nodeToBrowse,
				asyncResult =>
				{
					try
					{
						byte[] continuationPoint;
						var referenceDescriptions = session.EndBrowse(asyncResult, out continuationPoint);
						tcs.SetResult(new BrowseResult(referenceDescriptions, continuationPoint));
					}
					catch (Exception ex)
					{
						tcs.SetException(ex);
					}
				}, null);

			return await tcs.Task;
		}

		public static async Task<BrowseResult> BrowseAsync(this Session session,
			NodeId nodeToBrowse,
			BrowseContext browseContext,
			RequestSettings requestSettings)
		{
			var tcs = new TaskCompletionSource<BrowseResult>();

			session.BeginBrowse(nodeToBrowse, browseContext, requestSettings,
				asyncResult =>
				{
					try
					{
						byte[] continuationPoint;
						var referenceDescriptions = session.EndBrowse(asyncResult, out continuationPoint);
						tcs.SetResult(new BrowseResult(referenceDescriptions, continuationPoint));
					}
					catch (Exception ex)
					{
						tcs.SetException(ex);
					}
				}, null);

			return await tcs.Task;
		}

		public static async Task<BrowseResult> BrowseNextAsync(this Session session,
			byte[] continuationPoint,
			RequestSettings requestSettings)
		{
			var tcs = new TaskCompletionSource<BrowseResult>();

			session.BeginBrowseNext(continuationPoint, requestSettings,
				asyncResult =>
				{
					try
					{
						byte[] continuationPoint2;
						var referenceDescriptions = session.EndBrowse(asyncResult, out continuationPoint2);
						tcs.SetResult(new BrowseResult(referenceDescriptions, continuationPoint));
					}
					catch (Exception ex)
					{
						tcs.SetException(ex);
					}
				}, null);

			return await tcs.Task;
		}
		#endregion

		#region CallResult Class
		public class CallResult
		{
			#region Properties
			public StatusCode StatusCode { get; set; }
			public IList<StatusCode> InputArgumentErrors { get; set; }
			public IList<Variant> OutputArguments { get; set; }
			#endregion
		}
		#endregion
	}
}