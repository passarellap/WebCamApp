using Bystronic.IoT.OpcUaClientHelper;
using Microsoft.Extensions.Logging;
using OpcUaTypes;
using UnifiedAutomation.UaClient;

namespace Bystronic.DCLoadUnload.OpcUaApiModule.Classes
{
	public class OpcUaConnection
	{
		public Session session { get; set; }
		public DataTypeManager dtm { get; set; }
        public OpcUaDataClient opcUaDataClient { get; set; }
        public string address { get; set; }
		public ushort nameSpace { get;set;}		
		public OpcUaConnectionParams connectionParams { get; set; }
        private ILogger _logger;
        public OpcUaConnection(OpcUaConnectionParams param)
		{
			this.address = param.ServerAddress;
			this.nameSpace = param.ServerNamespace;
			connectionParams = param;
			session = new Session(UnifiedAutomation.UaBase.ApplicationInstanceBase.Default);
            session.UseDnsNameAndPortFromDiscoveryUrl = true;
			session.AutomaticReconnect = true;
			session.WatchdogTimeout = 30000; //default 10000
			session.WatchdogCycleTime = 15000; //default 5000
		}
		public void Connect()
		{
            opcUaDataClient = new OpcUaDataClient(session, address);
            while (session.ConnectionStatus != ServerConnectionStatus.Connected)
			{
				try
				{
					session.Connect("opc.tcp://"+address, SecuritySelection.None);
				}
				catch (Exception e)
				{
					Thread.Sleep(5000);
				}
			}
			
			dtm = new DataTypeManager(session);

			//_logger.LogInformation($"Finish connect to {address} with timeout {session.WatchdogTimeout} and watchdog cycle time {session.WatchdogCycleTime}");

		}
		public void Disconnect()
		{
			session.Disconnect();
			session.Dispose();
		}
		public bool IsConnected()
		{
			return session.ConnectionStatus == ServerConnectionStatus.Connected;
		}
	}
}