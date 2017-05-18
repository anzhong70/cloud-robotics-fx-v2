using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Data.SqlClient;
using CloudRoboticsUtil;


namespace CloudRoboticsFX
{
    /// <summary>
    /// Service Fabric ランタイムによって、このクラスのインスタンスがサービス レプリカごとに作成されます。
    /// </summary>
    internal sealed class CloudRoboFxSvc : StatefulService
    {
        /// <summary>
        /// Names of the dictionaries that hold the current offset value and partition epoch.
        /// </summary>
        private const string OffsetDictionaryName = "OffsetDictionary";
        private const string EpochDictionaryName = "EpochDictionary";

        /// <summary>
        /// Lock Object for multi-thread
        /// </summary>
        private static string thisLock = "{ThisObjectLock1}";
        private static string thisLock2 = "{ThisObjectLock2}";

        /// <summary>
        /// Application Exception
        /// </summary>
        private ApplicationException appException;

        /// <summary>
        /// Error & Info message trace
        /// </summary>
        private RbTraceLog2 rbTraceLog = null;
        private string rbTraceLevel = string.Empty;

        /// <summary>
        /// SQL Database Info for Cloud Robotics FX
        /// </summary>
        private string rbSqlConnectionString = string.Empty;
        private string rbEncPassPhrase = string.Empty;
        private int rbCacheExpiredTimeSec = 60;

        /// <summary>
        /// SQL Database Table Cache
        /// </summary>
        private Dictionary<string, object> rbCustomerRescCacheDic = new Dictionary<string, object>();
        private Dictionary<string, object> rbAppMasterCacheDic = new Dictionary<string, object>();
        private Dictionary<string, object> rbAppRouterCacheDic = new Dictionary<string, object>();
        private Dictionary<string, object> rbAppDllCacheInfoDic = new Dictionary<string, object>();

        /// <summary>
        /// App Domain for dll load
        /// </summary>
        private string appDomanNameBase = "AppDomain_P";
        private Dictionary<string, AppDomain> appDomainList = new Dictionary<string, AppDomain>();
        private string archivedDirectoryName = string.Empty;

        /// <summary>
        /// Reconfiguration may occur when the below error number returns from SQL Database.
        /// It is necessary to retry when we face the below error number.
        /// https://social.technet.microsoft.com/wiki/contents/articles/4235.retry-logic-for-transient-failures-in-windows-azure-sql-database.aspx
        /// </summary>
        // 20	    The instance of SQL Server does not support encryption.
        // 64	    An error occurred during login. 
        // 233	    Connection initialization error. 
        // 10053	A transport-level error occurred when receiving results from the server. 
        // 10054	A transport-level error occurred when sending the request to the server. 
        // 10060	Network or instance-specific error. 
        // 40143	Connection could not be initialized. 
        // 40197	The service encountered an error processing your request.
        // 40501	The server is busy. 
        // 40613	The database is currently unavailable.
        private List<int> sqlErrorListForRetry 
            = new List<int> { 20, 64, 233, 10053, 10054, 10060, 40143, 40197, 40501, 40613 };
        private int maxLoopCounter = 10;
        private int sleepInterval = 1000; //millisecond


        public CloudRoboFxSvc(StatefulServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Listeners for client request or user one should be created in this method. (HTTP, Service Remoting, WCF and etc.)
        /// This override is able to be omitted.
        /// </summary>
        /// <remarks>
        ///Please reffer to https://aka.ms/servicefabricservicecommunication 
        /// </remarks>
        /// <returns>Collection of Listeners</returns>
        //protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        //{
        //    return new ServiceReplicaListener[0];
        //}

        /// <summary>
        /// This is the main entry point of the service replica.
        /// This method will start if this service replica is promoted to primary and enabled write action
        /// </summary>
        /// <param name="cancellationToken">This method will be cancelled if Service Fabric needs to shutdown this service replica.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            // IoT Hub Connection Info in Setting.xml
            string iotHubConnectionString =
                this.Context.CodePackageActivationContext
                    .GetConfigurationPackageObject("Config")
                    .Settings
                    .Sections["IoTHub.ConnectionInfo"]
                    .Parameters["ConnectionString"]
                    .Value;
            string iotHubConsumerGroup =
                this.Context.CodePackageActivationContext
                    .GetConfigurationPackageObject("Config")
                    .Settings
                    .Sections["IoTHub.ConnectionInfo"]
                    .Parameters["ConsumerGroup"]
                    .Value;

            // SQL Database Connection Info in Setting.xml
            string sqlConnectionString =
                this.Context.CodePackageActivationContext
                    .GetConfigurationPackageObject("Config")
                    .Settings
                    .Sections["Sql.ConnectionInfo"]
                    .Parameters["ConnectionString"]
                    .Value;
            rbSqlConnectionString = sqlConnectionString;

            // Pass phrase & Cache expired time in Setting.xml
            string EncPassPhrase =
                this.Context.CodePackageActivationContext
                    .GetConfigurationPackageObject("Config")
                    .Settings
                    .Sections["RbSetting.Info"]
                    .Parameters["EncPassPhrase"]
                    .Value;
            rbEncPassPhrase = EncPassPhrase;
            string CacheExpiredTimeSec =
                this.Context.CodePackageActivationContext
                    .GetConfigurationPackageObject("Config")
                    .Settings
                    .Sections["RbSetting.Info"]
                    .Parameters["CacheExpiredTimeSec"]
                    .Value;
            rbCacheExpiredTimeSec = int.Parse(CacheExpiredTimeSec);

            // RbTraceLog Info in Setting.xml
            string rbTraceStorageAccount =
                this.Context.CodePackageActivationContext
                    .GetConfigurationPackageObject("Config")
                    .Settings
                    .Sections["RbTrace.Info"]
                    .Parameters["StorageAccount"]
                    .Value;
            string rbTraceStorageKey =
                this.Context.CodePackageActivationContext
                    .GetConfigurationPackageObject("Config")
                    .Settings
                    .Sections["RbTrace.Info"]
                    .Parameters["StorageKey"]
                    .Value;
            string rbTraceStorageConnString = string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}",
                                                            rbTraceStorageAccount, rbTraceStorageKey);
            string rbTraceStorageTableName =
                this.Context.CodePackageActivationContext
                    .GetConfigurationPackageObject("Config")
                    .Settings
                    .Sections["RbTrace.Info"]
                    .Parameters["StorageTableName"]
                    .Value;
            string traceLevel =
                this.Context.CodePackageActivationContext
                    .GetConfigurationPackageObject("Config")
                    .Settings
                    .Sections["RbTrace.Info"]
                    .Parameters["TraceLevel"]
                    .Value;
            rbTraceLevel = traceLevel;

            // RbC2dLog Info in Setting.xml
            string rbC2dLogEnabledString =
                this.Context.CodePackageActivationContext
                    .GetConfigurationPackageObject("Config")
                    .Settings
                    .Sections["RbC2dLog.Info"]
                    .Parameters["Enabled"]
                    .Value;
            bool rbC2dLogEnabled = false;
            if (rbC2dLogEnabledString.ToLower() == "true")
                rbC2dLogEnabled = true;
            string rbC2dLogEventHubConnString =
                this.Context.CodePackageActivationContext
                    .GetConfigurationPackageObject("Config")
                    .Settings
                    .Sections["RbC2dLog.Info"]
                    .Parameters["EventHubConnString"]
                    .Value;
            string rbC2dLogEventHubName =
                this.Context.CodePackageActivationContext
                    .GetConfigurationPackageObject("Config")
                    .Settings
                    .Sections["RbC2dLog.Info"]
                    .Parameters["EventHubName"]
                    .Value;

            // Initialize RbTraceLog
            rbTraceLog = new RbTraceLog2(rbTraceStorageConnString, rbTraceStorageTableName, 
                                            "CloudRoboticsFx2", this.Context.TraceId.ToString());

            // These Reliable Dictionaries are used to keep track of our position in IoT Hub.
            // If this service fails over, this will allow it to pick up where it left off in the event stream.
            IReliableDictionary<string, string> offsetDictionary =
                await this.StateManager.GetOrAddAsync<IReliableDictionary<string, string>>(OffsetDictionaryName);

            IReliableDictionary<string, long> epochDictionary =
                await this.StateManager.GetOrAddAsync<IReliableDictionary<string, long>>(EpochDictionaryName);

            // Each partition of this service corresponds to a partition in IoT Hub.
            // IoT Hub partitions are numbered 0..n-1, up to n = 32.
            // This service needs to use an identical partitioning scheme. 
            // The low key of every partition corresponds to an IoT Hub partition.
            Int64RangePartitionInformation partitionInfo = (Int64RangePartitionInformation)this.Partition.PartitionInfo;
            long servicePartitionKey = partitionInfo.LowKey;

            EventHubReceiver eventHubReceiver = null;
            MessagingFactory messagingFactory = null;

            try
            {
                // Trace to Storage Table
                if (rbTraceLevel == RbTraceType.Detail)
                {
                    rbTraceLog.WriteLog($"CloudRoboFxSvc has started. Partition({servicePartitionKey})");
                }

                // Get an EventHubReceiver and the MessagingFactory used to create it.
                // The EventHubReceiver is used to get events from IoT Hub.
                // The MessagingFactory is just saved for later so it can be closed before RunAsync exits.
                Tuple<EventHubReceiver, MessagingFactory> iotHubInfo =
                    await this.ConnectToIoTHubAsync(iotHubConnectionString, iotHubConsumerGroup, servicePartitionKey, epochDictionary, offsetDictionary);

                eventHubReceiver = iotHubInfo.Item1;
                messagingFactory = iotHubInfo.Item2;

                bool checkPoint = false;
                string previousOffset = string.Empty;

                // Loop of Processing EventData
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        using (EventData eventData = await eventHubReceiver.ReceiveAsync(TimeSpan.FromMilliseconds(100)))
                        {
                            // Check if eventData exists
                            if (eventData == null)
                            {
                                continue;
                            }
                            else
                            {
                                checkPoint = true;
                            }

                            // Loop of retry for reconfiguration on SQL Database
                            int loopCounter = 0;
                            while (true)
                            {
                                JObject jo_message = null;

                                // Routing switch
                                bool devRouting = false;
                                bool appRouting = false;

                                try
                                {

                                    // Receive a message and system properties
                                    string iothub_deviceId = (string)eventData.SystemProperties["iothub-connection-device-id"];
                                    DateTime iothub_enqueuedTimeUtc = (DateTime)eventData.SystemProperties["EnqueuedTimeUtc"];
                                    string text_message = Encoding.UTF8.GetString(eventData.GetBytes());

                                    // ETW Trace
                                    ServiceEventSource.Current.ServiceMessage(
                                            this.Context,
                                            "** Message received : {0}",
                                            text_message);
                                    // Trace to Storage Table
                                    if (rbTraceLevel == RbTraceType.Detail)
                                    {
                                        rbTraceLog.WriteLog(string.Format("CloudRoboFxSvc received a message. "
                                            + "Partition({0}), DeviceId({1}), Message:{2}",
                                              servicePartitionKey, iothub_deviceId, text_message));
                                    }

                                    // Check RbHeader
                                    if (text_message.IndexOf(RbFormatType.RbHeader) < 0)
                                    {
                                        rbTraceLog.WriteLog(string.Format(RbExceptionMessage.RbHeaderNotFound
                                            + "  Partition:{0}, DeviceId:{1}, Message:{2}",
                                                 servicePartitionKey, iothub_deviceId, text_message));
                                        goto TxCommitLabel;
                                    }

                                    // Check RbHeader simplly
                                    jo_message = JsonConvert.DeserializeObject<JObject>(text_message);
                                    var jo_rbh = (JObject)jo_message[RbFormatType.RbHeader];

                                    var v_rbhRoutingType = jo_rbh[RbHeaderElement.RoutingType];
                                    if (v_rbhRoutingType == null)
                                    {
                                        rbTraceLog.WriteError("W001", "** Message skipped because RoutingType is null **", jo_message);
                                        goto TxCommitLabel;
                                    }
                                    string s_rbhRoutingType = (string)v_rbhRoutingType;
                                    if (s_rbhRoutingType == RbRoutingType.LOG || s_rbhRoutingType == string.Empty)
                                    {
                                        // RoutingType == LOG -> only using IoT Hub with Stream Analytics  
                                        goto TxCommitLabel;
                                    }

                                    // Check RbHeader in detail
                                    RbHeaderBuilder hdBuilder = new RbHeaderBuilder(jo_message, iothub_deviceId);
                                    RbHeader rbh = hdBuilder.ValidateJsonSchema();

                                    // Check RoutingType (CALL, D2D, CONTROL)
                                    if (rbh.RoutingType == RbRoutingType.CALL)
                                    {
                                        appRouting = true;
                                    }
                                    else if (rbh.RoutingType == RbRoutingType.D2D)
                                    {
                                        devRouting = true;
                                        if (rbh.AppProcessingId != string.Empty)
                                        {
                                            appRouting = true;
                                        }
                                    }
                                    else if (rbh.RoutingType == RbRoutingType.CONTROL)
                                    {
                                        devRouting = false;
                                        appRouting = false;
                                    }
                                    else
                                    {
                                        rbTraceLog.WriteError("W002", "** Message skipped because of bad RoutingType **", jo_message);
                                        goto TxCommitLabel;
                                    }

                                    // Device Router builds RbHeader
                                    DeviceRouter dr = null;
                                    if (devRouting)
                                    {
                                        dr = new DeviceRouter(rbh, sqlConnectionString);
                                        rbh = dr.GetDeviceRouting();
                                        string new_header = JsonConvert.SerializeObject(rbh);
                                        jo_message[RbFormatType.RbHeader] = JsonConvert.DeserializeObject<JObject>(new_header);
                                    }
                                    else
                                    {
                                        rbh.TargetDeviceId = rbh.SourceDeviceId;
                                        rbh.TargetType = RbTargetType.Device;
                                    }

                                    // Application Routing
                                    JArray ja_messages = null;
                                    if (appRouting)
                                    {
                                        // Application Call Logic
                                        JObject jo_temp;
                                        string rbBodyString;
                                        try
                                        {
                                            jo_temp = (JObject)jo_message[RbFormatType.RbBody];
                                            rbBodyString = JsonConvert.SerializeObject(jo_temp);
                                        }
                                        catch(Exception ex)
                                        {
                                            rbTraceLog.WriteError("E003", $"** RbBody is not regular JSON format ** {ex.ToString()}", jo_message);
                                            goto TxCommitLabel;
                                        }

                                        try
                                        {
                                            ja_messages = CallApps(rbh, rbBodyString, servicePartitionKey.ToString());
                                        }
                                        catch (Exception ex)
                                        {
                                            rbTraceLog.WriteError("E001", $"** Error occured in CallApps ** {ex.ToString()}", jo_message);
                                            goto TxCommitLabel;
                                        }
                                    }
                                    else if (rbh.RoutingType != RbRoutingType.CONTROL)
                                    {
                                        ja_messages = new JArray();
                                        ja_messages.Add(jo_message);
                                    }

                                    // RoutingType="CONTROL" and AppProcessingId="ReqAppInfo" 
                                    if (rbh.RoutingType == RbRoutingType.CONTROL)
                                    {
                                        if (rbh.AppProcessingId == null)
                                        {
                                            rbTraceLog.WriteError("W003", "** Message skipped because AppProcessingId is null when CONTROL RoutingType **", jo_message);
                                            goto TxCommitLabel;
                                        }
                                        else if (rbh.AppProcessingId == RbControlType.ReqAppInfo)
                                        {
                                            try
                                            {
                                                ja_messages = CreateControlMessage(rbh);
                                            }
                                            catch (Exception ex)
                                            {
                                                rbTraceLog.WriteError("E002", $"** Error occured in CreateControlMessage ** {ex.ToString()}", jo_message);
                                                goto TxCommitLabel;
                                            }
                                        }
                                        else
                                        {
                                            rbTraceLog.WriteError("W004", "** Message skipped because of bad AppProcessingId when CONTROL RoutingType **", jo_message);
                                            goto TxCommitLabel;
                                        }
                                    }

                                    // Send C2D Message
                                    C2dMessageSender c2dsender = null;
                                    if (rbh.RoutingType == RbRoutingType.CALL
                                        || rbh.RoutingType == RbRoutingType.D2D
                                        || rbh.RoutingType == RbRoutingType.CONTROL)
                                    {
                                        c2dsender = new C2dMessageSender(ja_messages, iotHubConnectionString, sqlConnectionString);
                                        c2dsender.SendToDevice();
                                    }

                                    // C2D Message Logging to Event Hub
                                    if (rbC2dLogEnabled)
                                    {
                                        RbEventHubs rbEventHubs = new RbEventHubs(rbC2dLogEventHubConnString, rbC2dLogEventHubName);
                                        foreach (JObject jo in ja_messages)
                                        {
                                            string str_message = JsonConvert.SerializeObject(jo);
                                            rbEventHubs.SendMessage(str_message, iothub_deviceId);
                                        }
                                    }

                                    // Get out of retry loop because of normal completion
                                    break;

                                }
                                catch(Exception ex)
                                {
                                    rbTraceLog.WriteError("E003", $"** Critical error occured ** {ex.ToString()}", jo_message);

                                    bool continueLoop = false;

                                    if (ex != null && ex is SqlException)
                                    {
                                        foreach (SqlError error in (ex as SqlException).Errors)
                                        {
                                            if (sqlErrorListForRetry.Contains(error.Number))
                                            {
                                                continueLoop = true;
                                                break;  // Exit foreach loop
                                            }
                                        }

                                        if (continueLoop)
                                        {
                                            ++loopCounter;
                                            rbTraceLog.WriteLog($"Transaction retry has started. Count({loopCounter})");

                                            if (loopCounter > maxLoopCounter)
                                            {
                                                break;  // Get out of retry loop because counter reached max number
                                            }
                                            else
                                            {
                                                Thread.Sleep(sleepInterval);
                                            }
                                        }
                                        else
                                        {
                                            break;  // Get out of retry loop because of another sql error
                                        }
                                    }
                                    else
                                    {
                                        throw;
                                    }
                                }
                            }


                            // Label - Transaction Commit
                            TxCommitLabel:;

                            // Save the current Iot Hub data stream offset.
                            // This will allow the service to pick up from its current location if it fails over.
                            // Duplicate device messages may still be sent to the the tenant service 
                            // if this service fails over after the message is sent but before the offset is saved.
                            if (checkPoint)
                            {
                                ServiceEventSource.Current.ServiceMessage(
                                        this.Context,
                                        "Saving offset {0}",
                                        eventData.Offset);

                                using (ITransaction tx = this.StateManager.CreateTransaction())
                                {
                                    await offsetDictionary.SetAsync(tx, "offset", eventData.Offset);
                                    await tx.CommitAsync();
                                }

                                checkPoint = false;
                            }
                        }
                    }
                    catch (TimeoutException te)
                    {
                        // transient error. Retry.
                        ServiceEventSource.Current.ServiceMessage(this.Context, $"TimeoutException in RunAsync: {te.ToString()}");
                        rbTraceLog.WriteError("E004", $"** TimeoutException in RunAsync ** {te.ToString()}");
                    }
                    catch (FabricTransientException fte)
                    {
                        // transient error. Retry.
                        ServiceEventSource.Current.ServiceMessage(this.Context, $"FabricTransientException in RunAsync: {fte.ToString()}");
                        rbTraceLog.WriteError("E005", $"** FabricTransientException in RunAsync ** {fte.ToString()}");
                    }
                    catch (FabricNotPrimaryException)
                    {
                        // not primary any more, time to quit.

                        rbTraceLog.WriteError("E006", $"** FabricNotPrimaryException in RunAsync **");
                        return;
                    }
                    catch (Exception ex)
                    {
                        ServiceEventSource.Current.ServiceMessage(this.Context, ex.ToString());
                        rbTraceLog.WriteError("E007", $"** Critical error occured ** {ex.ToString()}");

                        throw;
                    }

                }
            }
            finally
            {
                if (messagingFactory != null)
                {
                    await messagingFactory.CloseAsync();
                }
            }
        }


        /// <summary>
        /// Creates an EventHubReceiver from the given connection sting and partition key.
        /// The Reliable Dictionaries are used to create a receiver from wherever the service last left off,
        /// or from the current date/time if it's the first time the service is coming up.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="servicePartitionKey"></param>
        /// <param name="epochDictionary"></param>
        /// <param name="offsetDictionary"></param>
        /// <returns></returns>
        private async Task<Tuple<EventHubReceiver, MessagingFactory>> ConnectToIoTHubAsync(
            string connectionString,
            string consumerGroup,
            long servicePartitionKey,
            IReliableDictionary<string, long> epochDictionary,
            IReliableDictionary<string, string> offsetDictionary)
        {

            // EventHubs doesn't support NetMessaging, so ensure the transport type is AMQP.
            ServiceBusConnectionStringBuilder connectionStringBuilder = new ServiceBusConnectionStringBuilder(connectionString);
            connectionStringBuilder.TransportType = TransportType.Amqp;

            // ETW Trace
            ServiceEventSource.Current.ServiceMessage(
                      this.Context,
                      "CloudRoboFxSvc connecting to IoT Hub at {0}",
                      String.Join(",", connectionStringBuilder.Endpoints.Select(x => x.ToString())));

            // A new MessagingFactory is created here so that each partition of this service will have its own MessagingFactory.
            // This gives each partition its own dedicated TCP connection to IoT Hub.
            MessagingFactory messagingFactory = MessagingFactory.CreateFromConnectionString(connectionStringBuilder.ToString());
            EventHubClient eventHubClient = messagingFactory.CreateEventHubClient("messages/events");
            EventHubRuntimeInformation eventHubRuntimeInfo = await eventHubClient.GetRuntimeInformationAsync();
            EventHubReceiver eventHubReceiver;

            // Get an IoT Hub partition ID that corresponds to this partition's low key.
            // This assumes that this service has a partition count 'n' that is equal to the IoT Hub partition count and a partition range of 0..n-1.
            // For example, given an IoT Hub with 32 partitions, this service should be created with:
            // partition count = 32
            // partition range = 0..31
            string eventHubPartitionId = eventHubRuntimeInfo.PartitionIds[servicePartitionKey];

            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                ConditionalValue<string> offsetResult = await offsetDictionary.TryGetValueAsync(tx, "offset", LockMode.Default);
                ConditionalValue<long> epochResult = await epochDictionary.TryGetValueAsync(tx, "epoch", LockMode.Update);

                long newEpoch = epochResult.HasValue
                    ? epochResult.Value + 1
                    : 0;

                if (offsetResult.HasValue)
                {
                    // continue where the service left off before the last failover or restart.
                    ServiceEventSource.Current.ServiceMessage(
                        this.Context,
                        "Creating EventHub listener on partition {0} with offset {1}",
                        eventHubPartitionId,
                        offsetResult.Value);

                    eventHubReceiver = await eventHubClient.GetConsumerGroup(consumerGroup).
                                                CreateReceiverAsync(eventHubPartitionId, offsetResult.Value, newEpoch);
                }
                else
                {
                    // first time this service is running so there is no offset value yet.
                    // start with the current time.
                    ServiceEventSource.Current.ServiceMessage(
                        this.Context,
                        "Creating EventHub listener on partition {0} with offset {1}",
                        eventHubPartitionId,
                        DateTime.UtcNow);

                    eventHubReceiver =
                        await
                            eventHubClient.GetConsumerGroup(consumerGroup)
                                .CreateReceiverAsync(eventHubPartitionId, DateTime.UtcNow, newEpoch);
                }

                // epoch is recorded each time the service fails over or restarts.
                await epochDictionary.SetAsync(tx, "epoch", newEpoch);
                await tx.CommitAsync();
            }

            return new Tuple<EventHubReceiver, MessagingFactory>(eventHubReceiver, messagingFactory);
        }

        JArray CallApps(RbHeader rbh, string rbBodyString, string partitionId)
        {
            // Get App Master Info
            RbAppMasterCache rbappmc = GetAppMasterInfo(rbh);

            // Get App Routing Info
            RbAppRouterCache rbapprc = GetAppRoutingInfo(rbh);

            JArrayString ja_messagesString = null;
            JArray ja_messages = null;
            string dllFilePath = string.Empty;

            IAppRouterDll routedAppDll = null;

            // Load DLL from BLOB
            string baseDirectory = string.Empty;
            string privateDllDirectory = string.Empty;
            string cachedFileName = string.Empty;
            string cachedFileNameWithoutExt = string.Empty;

            if (rbapprc.DevMode == "True")
            {
                string devdir = rbapprc.DevLocalDir;
                int pos = devdir.Length - 1;
                if (devdir.Substring(pos, 1) == @"\")
                {
                    dllFilePath = rbapprc.DevLocalDir + rbapprc.FileName;
                }
                else
                {
                    dllFilePath = rbapprc.DevLocalDir + @"\" + rbapprc.FileName;
                }

                baseDirectory = Path.GetDirectoryName(dllFilePath);
                privateDllDirectory = baseDirectory;
                cachedFileName = Path.GetFileName(dllFilePath);
                cachedFileNameWithoutExt = Path.GetFileNameWithoutExtension(dllFilePath);
            }
            else
            {
                CachedDllFileInfo cachedDllFileInfo = CopyBlobToLocalDir(rbappmc, rbapprc, partitionId);
                baseDirectory = cachedDllFileInfo.BaseDirectory;
                privateDllDirectory = cachedDllFileInfo.PrivateDllDirectory;
                cachedFileName = Path.GetFileName(cachedDllFileInfo.PrivateDllFilePath);
                cachedFileNameWithoutExt = Path.GetFileNameWithoutExtension(cachedDllFileInfo.PrivateDllFilePath);
            }

            //Dynamic load using AppDomain
            try
            {
                string appDomainName = appDomanNameBase + partitionId;
                AppDomain appDomain = null;
                if (appDomainList.ContainsKey(partitionId))
                {
                    appDomain = appDomainList[partitionId];
                }

                if (appDomain == null)
                {
                    appDomain = CreateAppDomain(appDomainName, baseDirectory, privateDllDirectory);
                    lock (thisLock2)
                    {
                        appDomainList[partitionId] = appDomain;
                    }
                }
                routedAppDll = appDomain.CreateInstanceAndUnwrap(cachedFileNameWithoutExt, rbapprc.ClassName) as IAppRouterDll;
            }
            catch (Exception ex)
            {
                rbTraceLog.WriteError("E011", ex.ToString());
                appException = new ApplicationException("Error ** Exception occured during creating AppDomain & Instance(App DLL)");
                throw appException;
            }

            // ProcessMessage
            try
            {
                rbh.ProcessingStack = rbapprc.FileName;
                ja_messagesString = routedAppDll.ProcessMessage(rbappmc, rbapprc, rbh, rbBodyString);
                ja_messages = ja_messagesString.ConvertToJArray();
            }
            catch (Exception ex)
            {
                rbTraceLog.WriteError("E012", ex.ToString());
                appException = new ApplicationException("Error ** Exception occured in routed App DLL");
                throw appException;
            }

            return ja_messages;
        }

        RbAppMasterCache GetAppMasterInfo(RbHeader rbh)
        {
            AppMaster am = null;
            bool am_action = true;
            RbAppMasterCache rbappmc = null;
            if (rbAppMasterCacheDic.ContainsKey(rbh.AppId))
            {
                rbappmc = (RbAppMasterCache)rbAppMasterCacheDic[rbh.AppId];
                if (rbappmc.CacheExpiredDatetime >= DateTime.Now)
                    am_action = false;
            }
            if (am_action)
            {
                am = new AppMaster(rbh.AppId, rbEncPassPhrase, rbSqlConnectionString, rbCacheExpiredTimeSec);
                rbappmc = am.GetAppMaster();
                if (rbappmc != null)
                {
                    lock (thisLock)
                    {
                        rbAppMasterCacheDic[rbh.AppId] = rbappmc;
                    }
                }
                else
                {
                    appException = new ApplicationException("Error ** GetAppMaster() returns Null Object");
                    throw appException;
                }
            }

            return rbappmc;
        }

        RbAppRouterCache GetAppRoutingInfo(RbHeader rbh)
        {
            AppRouter ar = null;
            bool ar_action = true;
            string cachekey = rbh.AppId + "_" + rbh.AppProcessingId;
            RbAppRouterCache rbapprc = null;
            if (rbAppRouterCacheDic.ContainsKey(cachekey))
            {
                rbapprc = (RbAppRouterCache)rbAppRouterCacheDic[cachekey];
                if (rbapprc.CacheExpiredDatetime >= DateTime.Now)
                    ar_action = false;
            }
            if (ar_action)
            {
                ar = new AppRouter(rbh.AppId, rbh.AppProcessingId, rbSqlConnectionString, rbCacheExpiredTimeSec);
                rbapprc = ar.GetAppRouting();
                if (rbapprc != null)
                {
                    lock (thisLock)
                    {
                        rbAppRouterCacheDic[cachekey] = rbapprc;
                    }
                }
                else
                {
                    appException = new ApplicationException("Error ** GetAppRouting() returns Null Object");
                    throw appException;
                }
            }

            return rbapprc;
        }

        CachedDllFileInfo CopyBlobToLocalDir(RbAppMasterCache rbappmc, RbAppRouterCache rbapprc, string partitionId)
        {
            //string curdir = Environment.CurrentDirectory;
            CachedDllFileInfo cachedDllFileInfo = new CachedDllFileInfo();
            string curdir = AppDomain.CurrentDomain.BaseDirectory;
            cachedDllFileInfo.BaseDirectory = curdir;
            cachedDllFileInfo.PrivateDllDirectory = Path.Combine(curdir, "P" + partitionId);

            string blobTargetFilePath = string.Empty;
            RbAppDllCacheInfo rbAppDllInfo = null;
            RbAppDllCacheInfo rbAppDllInfo_partition = null;
            bool loadAction = true;
            bool blobCopyAction = true;
            string partitionedFileNameKey = "P" + partitionId + "_" + rbapprc.FileName;

            // Check original DLL info
            if (rbAppDllCacheInfoDic.ContainsKey(rbapprc.FileName))
            {
                // Original DLL
                rbAppDllInfo = (RbAppDllCacheInfo)rbAppDllCacheInfoDic[rbapprc.FileName];
                blobTargetFilePath = Path.Combine(rbAppDllInfo.CacheDir, rbAppDllInfo.CachedFileName);

                // Use cached original DLL if Registered_Datetime not changed.
                if (rbAppDllInfo.AppId == rbapprc.AppId
                    && rbAppDllInfo.AppProcessingId == rbapprc.AppProcessingId
                    && rbAppDllInfo.Registered_DateTime == rbapprc.Registered_DateTime)
                {
                    blobCopyAction = false;
                }
            }

            // Check partitioned DLL info
            if (rbAppDllCacheInfoDic.ContainsKey(partitionedFileNameKey))
            {
                // DLL copied into each partition directory
                rbAppDllInfo_partition = (RbAppDllCacheInfo)rbAppDllCacheInfoDic[partitionedFileNameKey];
                cachedDllFileInfo.PrivateDllFilePath = Path.Combine(rbAppDllInfo_partition.CacheDir, rbAppDllInfo_partition.CachedFileName);

                // Use cached DLL copied into each partition directory if Registered_Datetime not changed.
                if (rbAppDllInfo_partition.AppId == rbapprc.AppId
                    && rbAppDllInfo_partition.AppProcessingId == rbapprc.AppProcessingId
                    && rbAppDllInfo_partition.Registered_DateTime == rbapprc.Registered_DateTime)
                {
                    loadAction = false;
                }
            }

            if (loadAction)
            {
                if (blobTargetFilePath != string.Empty)
                {
                    AppDomain appDomain = null;
                    if (appDomainList.ContainsKey(partitionId))
                    {
                        appDomain = appDomainList[partitionId];
                        AppDomain.Unload(appDomain);
                        lock (thisLock2)
                        {
                            appDomainList[partitionId] = null;
                        }
                    }

                    if (blobCopyAction)
                    {
                        // Move current DLL to archive directory
                        if (File.Exists(blobTargetFilePath))
                        {
                            string archivedDirectory = Path.Combine(curdir, archivedDirectoryName);
                            string archivedDllFilePath = archivedDirectory + @"\" + rbapprc.FileName
                                                       + ".bk" + DateTime.Now.ToString("yyyyMMddHHmmss");
                            if (!Directory.Exists(archivedDirectory))
                            {
                                Directory.CreateDirectory(archivedDirectory);
                            }
                            File.Move(blobTargetFilePath, archivedDllFilePath);
                        }
                    }
                }

                if (blobCopyAction)
                {
                    // Download DLL from BLOB
                    RbAzureStorage rbAzureStorage = new RbAzureStorage(rbappmc.StorageAccount, rbappmc.StorageKey);
                    rbAppDllInfo = new RbAppDllCacheInfo();
                    rbAppDllInfo.FileName = rbapprc.FileName;
                    rbAppDllInfo.CacheDir = Path.Combine(curdir, "cache");
                    if (!Directory.Exists(rbAppDllInfo.CacheDir))
                        Directory.CreateDirectory(rbAppDllInfo.CacheDir);
                    rbAppDllInfo.AppId = rbapprc.AppId;
                    rbAppDllInfo.AppProcessingId = rbapprc.AppProcessingId;
                    rbAppDllInfo.Registered_DateTime = rbapprc.Registered_DateTime;
                    //rbAppDllInfo.GenerateCachedFileName();
                    rbAppDllInfo.CachedFileName = rbAppDllInfo.FileName;
                    blobTargetFilePath = Path.Combine(rbAppDllInfo.CacheDir, rbAppDllInfo.CachedFileName);

                    using (var fileStream = File.OpenWrite(blobTargetFilePath))
                    {
                        rbAzureStorage.BlockBlobDownload(fileStream, rbapprc.BlobContainer, rbapprc.FileName);
                    }

                    // Update cache info if DLL download from BLOB is successful.
                    lock (thisLock2)
                    {
                        rbAppDllCacheInfoDic[rbapprc.FileName] = rbAppDllInfo;
                    }

                    // Logging
                    if (rbTraceLevel == RbTraceType.Detail)
                    {
                        rbTraceLog.WriteLog(string.Format("App DLL is copied from BLOB strage.  Dir:{0}, FileName:{1}",
                                     curdir, rbAppDllInfo.CachedFileName));
                    }
                }

                // Copy original DLL into partition directory
                rbAppDllInfo_partition = new RbAppDllCacheInfo();
                rbAppDllInfo_partition.FileName = rbapprc.FileName;
                rbAppDllInfo_partition.CacheDir = cachedDllFileInfo.PrivateDllDirectory;
                rbAppDllInfo_partition.AppId = rbapprc.AppId;
                rbAppDllInfo_partition.AppProcessingId = rbapprc.AppProcessingId;
                rbAppDllInfo_partition.Registered_DateTime = rbapprc.Registered_DateTime;
                rbAppDllInfo_partition.CachedFileName = rbAppDllInfo_partition.FileName;

                string sourceFilePath = Path.Combine(rbAppDllInfo.CacheDir, rbAppDllInfo.CachedFileName);
                string targetFilePath = Path.Combine(rbAppDllInfo_partition.CacheDir, rbAppDllInfo_partition.CachedFileName);
                cachedDllFileInfo.PrivateDllFilePath = targetFilePath;
                if (!Directory.Exists(rbAppDllInfo_partition.CacheDir))
                    Directory.CreateDirectory(rbAppDllInfo_partition.CacheDir);
                File.Copy(sourceFilePath, targetFilePath, true);

                // Update cache info if DLL copied successfully.
                lock (thisLock2)
                {
                    rbAppDllCacheInfoDic[partitionedFileNameKey] = rbAppDllInfo_partition;
                }

                // Logging
                if (rbTraceLevel == RbTraceType.Detail)
                {
                    rbTraceLog.WriteLog(string.Format("Original App DLL is copied into partition directory.  Dir:{0}, FileName:{1}, PartitionId:{2}",
                                 curdir, rbAppDllInfo.CachedFileName, partitionId));
                }
            }

            return cachedDllFileInfo;
        }

        private class CachedDllFileInfo
        {
            public string BaseDirectory { set; get; }
            public string PrivateDllDirectory { set; get; }
            public string PrivateDllFilePath { set; get; }
        }

        AppDomain CreateAppDomain(string appName, string baseDirectory, string privateDllDirectory)
        {
            AppDomainSetup setup = new AppDomainSetup();
            setup.ApplicationName = appName;
            setup.ApplicationBase = baseDirectory;       //AppDomain.CurrentDomain.BaseDirectory
            setup.PrivateBinPath = privateDllDirectory;
            setup.CachePath = Path.Combine(privateDllDirectory, "cache" + Path.DirectorySeparatorChar);
            setup.ShadowCopyFiles = "true";
            setup.ShadowCopyDirectories = privateDllDirectory;

            AppDomain appDomain = AppDomain.CreateDomain(appName, null, setup);

            return appDomain;
        }

        /// <summary>
        /// Create a control message which devices use initially.
        /// </summary>
        /// <param name="rbh"></param>
        /// <returns>JArray</returns>
        JArray CreateControlMessage(RbHeader rbh)
        {

            JArray ja_messages = new JArray();
            RbAppMasterCache rbappmc = GetAppMasterInfo(rbh);
            RbMessage message = new RbMessage();
            message.RbHeader = rbh;
            message.RbBody = JsonConvert.DeserializeObject<JObject>(rbappmc.AppInfoDevice);
            string json_message = JsonConvert.SerializeObject(message);
            JObject jo = (JObject)JsonConvert.DeserializeObject(json_message);
            ja_messages.Add(jo);

            return ja_messages;
        }
    }
}
