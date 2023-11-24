using Bystronic.DCLoadUnload.OpcUaApiModule.Classes;
using Bystronic.IoT.OpcUaClientHelper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OpcUaTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using UnifiedAutomation.UaBase;
using UnifiedAutomation.UaClient;

namespace WebCamApp
{
    public class WebCamPlcManager : IOpcUaActor, IHostedService
    {
        public event Action<bool> OnOpcUaConnectionStatusChanged;
        public event Action<OpcUaSubscriptionArgs> NewSubscriptionCreated;
        public event Action<OpcUaMethodArgs> MethodCalled;
        public event CallMethodSyncDelegate CallMethodSync;
        public event Action RefreshData;
        public event ReadNodeIdDelegate ReadNodeId;
        public event ReadListNodeIdDelegate ReadListNodeId;
        public event Action<OpcUaWriteArgs> WriteData;
        public event Action<List<OpcUaWriteArgs>> WriteDataList;
        private OpcUaConnectionParams _plcOpcUaConnectionParams;
        private List<OpcUaSubscription> _subscriptions = new List<OpcUaSubscription>();
        private OpcUaSubscription m_subscription = null;
        private OpcUaConnection _connection;
        private OpcUaDataClient _opcUaDataClient;
        private Session _session = new Session();

        public WebCamPlcManager()
        {
            _plcOpcUaConnectionParams = new OpcUaConnectionParams() { ServerAddress = "10.180.12.13:4840", ServerNamespace = 6 };
            IOpcUaSubscriber subscriber;
            _connection = new OpcUaConnection(_plcOpcUaConnectionParams);
            CreateClient();
            Connect();
            InitSubscriptions();
            CreateSubscription(new OpcUaSubscriptionArgs(new OpcUaSubscription("WebCam subscription", 700, _plcOpcUaConnectionParams), _plcOpcUaConnectionParams));
        }
        public async void CheckPhotoNeeded()
        {
            int memPrev = ReadMem(1650);
            await Task.Run(() =>
            {
                while (true)
                {
                    var mem = ReadMem(1650);
                    if (mem == 1)
                    {
                        if (memPrev != 1)
                            new Thread(() =>
                            {
                                Thread.CurrentThread.IsBackground = false;
                                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, (SendOrPostCallback)delegate
                                {
                                    AlarmReceived.Invoke();
                                }, null);
                            }).Start();
                    }
                    else if (mem == 0)
                    {
                        if (memPrev != 0)
                            new Thread(() =>
                        {

                            Thread.CurrentThread.IsBackground = false;
                            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, (SendOrPostCallback)delegate
                            {
                                DisabledButton.Invoke();
                            }, null);
                        }).Start();
                    }
                    memPrev = mem;
                    mem = 0;
                    Task.Delay(1000);
                }
            });
        }
        public int ReadMem(int mem)
        {
            //JObject result = null;
            var node = NodeId.Parse("ns=6;s=::AsGlobalPV:mem_table[" + mem + "].val");
            int nodeValue = (int)_opcUaDataClient.ReadNodeId<int>(node);
            //ReadNodeId?.Invoke("::AsGlobalPV:mem_table[" + mem + "].val", "mem" + mem, _plcOpcUaConnectionParams, out result);
            return nodeValue;
        }
        public int WriteMem(int mem,int val)
        {
            var node = NodeId.Parse("ns=6;s=::AsGlobalPV:mem_table[" + mem + "].val");
            _opcUaDataClient.WriteData(node, 0);
            return 1;
        }
        public event Action AlarmReceived;
        public event Action DisabledButton;
        private void CreateSubscription(OpcUaSubscriptionArgs args)
        {
            try
            {
                CreateSubscriptionTask(args);
            }
            catch (Exception e)
            {
                //_logger.LogError(e, "CreateSubscription error");
            }
        }
        public OpcUaConnectionParams GetConnectionParams()
        {
            return _plcOpcUaConnectionParams;
        }
        private event Action<OpcUaConnection> OpcUaConnectionEstablished;
        private event Action<OpcUaConnection> OpcUaConnectionBroken;
        public void InitSubscriptions()
        {
            OpcUaSubscription subscriptionWebCam = new OpcUaSubscription("WebCam subscription", 700, _plcOpcUaConnectionParams);
            _subscriptions.Add(subscriptionWebCam);
            OpcUaNodeGroup group = new OpcUaNodeGroup("group");
            group.Add(new OpcUaNode($"MemImageNeeded", $"::AsGlobalPV:mem_table[1650].val"));
            subscriptionWebCam.AddGroup(group);
            subscriptionWebCam.OnOpcUaNewDataReceived += OpcUaNewDataReceived;
            NewSubscriptionCreated?.Invoke(new OpcUaSubscriptionArgs(subscriptionWebCam, _plcOpcUaConnectionParams));
        }
        public void OnOpcUaConnectionBroken(OpcUaConnection connection){}
        public void OnOpcUaConnectionEstablished(OpcUaConnection connection){}
        public void OpcUaConnectionStatusChanged(bool status){}
        private void Connect()
        {
            _connection.session.ConnectionStatusUpdate += (sender, e) => OnConnectionStatusUpdate(sender, e, _connection);
            try
            {
                _session.Connect("opc.tcp://10.180.12.13:4840", SecuritySelection.None);
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
            }
        }
        public void CreateClient()
        {
            _session.UseDnsNameAndPortFromDiscoveryUrl = true;
            _session.AutomaticReconnect = true;
            _session.ConnectionStatusUpdate += (sender, e) => OnConnectionStatusUpdate(sender, e, _connection);
            _opcUaDataClient = new OpcUaDataClient(_session, "opc.tcp://10.180.12.13:4840");
        }
        private void OnConnectionStatusUpdate(Session session, ServerConnectionStatusUpdateEventArgs args, OpcUaConnection connection)
        {
            //_logger.LogInformation($"OPCUA connection {connection.address} status updated to {args.Status.ToString()}");

            if (args.Status == ServerConnectionStatus.Connected ||
                args.Status == ServerConnectionStatus.SessionAutomaticallyRecreated)
            {
                try
                {
                    OpcUaConnectionEstablished?.Invoke(connection);
                }
                catch (Exception e)
                {
                    e = e;
                }
            }
            if (args.Status == ServerConnectionStatus.Disconnected ||
                args.Status == ServerConnectionStatus.LicenseExpired ||
                args.Status == ServerConnectionStatus.ServerShutdown ||
                args.Status == ServerConnectionStatus.ServerShutdownInProgress ||
                args.Status == ServerConnectionStatus.ConnectionErrorClientReconnect ||
                args.Status == ServerConnectionStatus.ConnectionWarningWatchdogTimeout)
            {
                OpcUaConnectionBroken?.Invoke(connection);
            }
        }
        public void OpcUaNewDataReceived(List<OpcUaDataChangedArgs> ee)
        {
            foreach (OpcUaDataChangedArgs e in ee)
            {
                //_logger.LogInformation(@"{0}: new data received from OPCUA", e.VarName);
                int iVal;
                //TODO loggare i vari case
                switch (e.VarName)
                {
                    case "MemImageNeeded":
                        iVal = int.Parse(e.VarValue[e.VarName].ToString());
                        if (iVal == 1)
                            AlarmReceived.Invoke();
                        break;
                }
            }
        }
        private void Subscription_DataChanged(Subscription subscription, DataChangedEventArgs e)
        {
            // Need to make sure this method is called on the UI thread because it updates UI controls.
            //if (InvokeRequired)
            //{
            //    BeginInvoke(new DataChangedEventHandler(Subscription_DataChanged), subscription, e);
            //    return;
            //}

            try
            {
                // Check that the subscription has not changed.
                if (!Object.ReferenceEquals(m_subscription, subscription))
                {
                    return;
                }

                foreach (DataChange change in e.DataChanges)
                {
                    // Get text box for displaying value from user data
                    //TextBox textBox = change.MonitoredItem.UserData as TextBox;
                    var addr = change.MonitoredItem.NodeId.Identifier.ToString();

                    //var group = subscription.GroupsList.FirstOrDefault(g => g.NodesList.FirstOrDefault(nl => nl.NodeAddress == addr) != null);
                    //var varName = group.NodesList.FirstOrDefault(nl => nl.NodeAddress == addr).NodeName;

                    //if (varName != varName2)
                    //	varName = varName2;

                    DataValue value = change.Value;
                }
            }
            catch (Exception exception)
            {
                //ExceptionDlg.Show("Error in DataChanged callback", exception);
            }
        }
        public void CreateSubscriptionTask(OpcUaSubscriptionArgs args)
        {
            ILogger logger;
            //var connection = new OpcUaConnection(_plcOpcUaConnectionParams);

            OpcUaSubscription opcUaSubscription = args.Subscription;

            //if (_subscriptions.Exists(s => s.Name == opcUaSubscription.Name))
            //{
            //    var oldSubscription = _subscriptions.First(s => s.Name == opcUaSubscription.Name);
            //    oldSubscription.Subscription.Delete();
            //    _subscriptions.Remove(oldSubscription);
            //}
            var subscription = new Subscription(_connection.session)
            {
                PublishingInterval = opcUaSubscription.PublishingInterval,
                MaxKeepAliveTime = 5000,
            };
            subscription.Create(new RequestSettings() { OperationTimeout = 10000 });
            subscription.DataChanged += Subscription_DataChanged;
            opcUaSubscription.Subscription = subscription;
            RegisterSubscription(opcUaSubscription);
            _subscriptions.Add(opcUaSubscription);
            _subscriptions.ForEach(sub => _session.Subscriptions.Add(sub.Subscription)
            );

        }
        //private void Subscription_DataChanged(Subscription subscription, DataChangedEventArgs e)
        //{
        //    OpcUaSubscription sub = _opcUaSubscriptions.FirstOrDefault(s => s.Subscription == subscription);

        //    try
        //    {
        //        List<OpcUaDataChangedArgs> dataChanged = new List<OpcUaDataChangedArgs>();
        //        var connection = new OpcUaConnection(_plcOpcUaConnectionParams);

        //        // Update the value
        //        foreach (DataChange change in e.DataChanges)
        //        {
        //            var addr = change.MonitoredItem.NodeId.Identifier.ToString();

        //            var group = sub.GroupsList.FirstOrDefault(g => g.NodesList.FirstOrDefault(nl => nl.NodeAddress == addr) != null);
        //            var varName = group.NodesList.FirstOrDefault(nl => nl.NodeAddress == addr).NodeName;

        //            //if (varName != varName2)
        //            //	varName = varName2;

        //            DataValue value = change.Value;
        //            var jobj = ParseDataValue(value, varName, connection.dtm);
        //            //_dataDict[addr] = jobj;
        //            dataChanged.Add(new OpcUaDataChangedArgs(varName, addr, jobj));
        //        }

        //        sub?.OnOpcUaNewDataReceived?.Invoke(dataChanged);
        //    }
        //    catch (Exception eee)
        //    {
        //        _logger.LogError(eee, "SubscriptionDataChanged error for subscription " + sub.Name);
        //    }
        //}
        private int RegisterSubscription(OpcUaSubscription sub)
        {
            //var connection = new OpcUaConnection(_plcOpcUaConnectionParams);
            List<MonitoredItem> monitoredItems = new List<MonitoredItem>();
            foreach (OpcUaNodeGroup group in sub.GroupsList)
            {
                foreach (OpcUaNode node in group.NodesList)
                {
                    NodeId nodeid = new NodeId(node.NodeAddress, (ushort)_connection.nameSpace);

                    DataMonitoredItem item = new DataMonitoredItem(nodeid)
                    {
                        DiscardOldest = true,
                        QueueSize = 1,
                        SamplingInterval = -1
                    };
                    monitoredItems.Add(item);
                }
            }
            monitoredItems.Add(new DataMonitoredItem(new NodeId("::AsGlobalPV:mem_table[1650].val", 6)));
            if (monitoredItems.Count > 0)
                sub.Subscription.CreateMonitoredItems(monitoredItems);
            return 1;
        }
        //private void Subscription_DataChanged(Subscription subscription, DataChangedEventArgs e)
        //{
        //    Task.Run(() =>
        //    {
        //        //lock (_syncRoot)
        //        //{
        //        try
        //        {
        //            Subscription_DataChangedTask(subscription, e);
        //        }
        //        catch (Exception eee)
        //        {
        //        }
        //        //}
        //    });
        //}
        //private void Subscription_DataChangedTask(Subscription subscription, DataChangedEventArgs e)
        //{
        //    OpcUaSubscription sub = _subscriptions.FirstOrDefault(s => s.Subscription == subscription);

        //    try
        //    {
        //        List<OpcUaDataChangedArgs> dataChanged = new List<OpcUaDataChangedArgs>();
        //        var connection = new OpcUaConnection(_plcOpcUaConnectionParams);

        //        // Update the value
        //        foreach (DataChange change in e.DataChanges)
        //        {
        //            var addr = change.MonitoredItem.NodeId.Identifier.ToString();

        //            var group = sub.GroupsList.FirstOrDefault(g => g.NodesList.FirstOrDefault(nl => nl.NodeAddress == addr) != null);
        //            var varName = group.NodesList.FirstOrDefault(nl => nl.NodeAddress == addr).NodeName;

        //            //if (varName != varName2)
        //            //	varName = varName2;

        //            DataValue value = change.Value;
        //            //var jobj = ParseDataValue(value, varName, connection.dtm);
        //            JObject jobj = new JObject();
        //            dataChanged.Add(new OpcUaDataChangedArgs(varName, addr, jobj));
        //        }

        //        sub?.OnOpcUaNewDataReceived?.Invoke(dataChanged);
        //    }
        //    catch (Exception eee)
        //    {
        //    }
        //}
        //private int RegisterSubscription(OpcUaSubscription sub)
        //{
        //    var connection = new OpcUaConnection(_plcOpcUaConnectionParams);
        //    List<MonitoredItem> monitoredItems = new List<MonitoredItem>();
        //    foreach (OpcUaNodeGroup group in sub.GroupsList)
        //    {
        //        foreach (OpcUaNode node in group.NodesList)
        //        {
        //            NodeId nodeid = new NodeId(node.NodeAddress, (ushort)connection.nameSpace);

        //            DataMonitoredItem item = new DataMonitoredItem(nodeid)
        //            {
        //                DiscardOldest = true,
        //                QueueSize = 1,
        //                SamplingInterval = -1
        //            };
        //            monitoredItems.Add(item);
        //        }
        //    }
        //    if (monitoredItems.Count > 0)
        //        sub.Subscription.CreateMonitoredItems(monitoredItems);
        //    return 1;
        //}
        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}