using Crestron.SimplSharp;
using Microsoft.SqlServer.Server;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Protocol;
using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CrestronMqttClient
{
    public class MqttClientHandler
    {
        IMqttClient client;
        ushort uniqueID;

        Dictionary<string, List<SubscribeArgs>> ReceiveEvents = new Dictionary<string, List<SubscribeArgs>>();
        Mutex ReceiveEventsMutex = new Mutex();

        static Dictionary<ushort, MqttClientHandler> MqttClientInstances = new Dictionary<ushort, MqttClientHandler>();
        static Mutex MqttClientInstancesMutex = new Mutex();

        public delegate void ConnectStatusDelegate(object sender, ClientStatusEventArgs args);
        public event ConnectStatusDelegate OnConnectionStatusChanged;

        public string Username { get; set; } = String.Empty;
        public string Password { get; set; } = String.Empty;
        public ushort ServerPort { get; set; } = 1883;
        public bool TLSEnabled { get; set; } = true;
        public bool CertificateValidation { get; set; } = true;

        public MqttClientHandler() 
        {
        }

        public void StartClient(ushort uniqueID, string serverAddress)
        {
            this.uniqueID = uniqueID;

            var mqttLogger = new MqttNetLogger();
            mqttLogger.IsEnabled = false;

            var factory = new MqttFactory(mqttLogger);
            client = factory.CreateMqttClient();

            var optionsBuilder = new MqttClientOptionsBuilder()
                .WithTcpServer(serverAddress, ServerPort);
            if (TLSEnabled)
                optionsBuilder.WithTlsOptions(o =>
                 {
                    o.UseTls();
                     if (!CertificateValidation)
                     {
                         o.WithIgnoreCertificateChainErrors();
                         o.WithAllowUntrustedCertificates();
                         o.WithIgnoreCertificateRevocationErrors();
                         o.WithCertificateValidationHandler((x) => { return true; });
                     }
                 });

            if (!String.IsNullOrEmpty(Username) || !String.IsNullOrEmpty(Password))
                optionsBuilder = optionsBuilder.WithCredentials(Username, Password);

            var options = optionsBuilder.Build();

            client.ConnectedAsync += e =>
            {
                try
                {
                    ReceiveEventsMutex.WaitOne();
                    foreach (var topic in ReceiveEvents)
                    {
                        Subscribe(topic.Key, topic.Value.OrderByDescending(x => x.QosLevel).First().QosLevel);    
                    }
                }
                finally
                {
                    ReceiveEventsMutex?.ReleaseMutex();
                }
                OnConnectionStatusChanged?.Invoke(this, new ClientStatusEventArgs() { Status = 1 });
                return Task.CompletedTask;
            };

            client.DisconnectedAsync += async e =>
            {
                OnConnectionStatusChanged?.Invoke(this, new ClientStatusEventArgs() { Status = 0 });
                await Task.Delay(TimeSpan.FromSeconds(5));

                try
                {
                    await client.ConnectAsync(options, CancellationToken.None);
                }
                catch
                {}
            };

            client.ApplicationMessageReceivedAsync += ApplicationMessageReceivedAsync;

            client.ConnectAsync(options);
            MqttClientInstanceAdd(uniqueID,this);
        }

        //public void StopClient()
        //{ 
        //    client.Dispose();
        //}

        internal void ReceiveAddEvent(string topic, SubscribeArgs args)
        {
            try
            {
                ReceiveEventsMutex.WaitOne();
                if (!ReceiveEvents.ContainsKey(topic))
                    ReceiveEvents.Add(topic, new List<SubscribeArgs>());

                ReceiveEvents[topic].Add(args);
                if (client.IsConnected && ReceiveEvents[topic].Count == 1)
                    Subscribe(topic, args.QosLevel);
            }
            finally
            {
                ReceiveEventsMutex.ReleaseMutex();
            }
        }

        //internal void ReceiveRemoveEvent(string topic, Action<MqttApplicationMessageReceivedEventArgs> callback)
        //{
        //    try
        //    {
        //        ReceiveEventsMutex.WaitOne();
        //        if (!ReceiveEvents.ContainsKey(topic))
        //            return;

        //        ReceiveEvents[topic].Remove(callback);
        //        if (ReceiveEvents[topic].Count == 0)
        //            ReceiveEvents.Remove(topic);
        //        client.UnsubscribeAsync(new MqttClientUnsubscribeOptionsBuilder().WithTopicFilter(topic).Build());
        //    }
        //    finally
        //    {
        //        ReceiveEventsMutex.ReleaseMutex();
        //    }
        //}

        private static void MqttClientInstanceAdd(ushort uniqueID, MqttClientHandler mqttClient)
        {
            try
            {
                MqttClientInstancesMutex.WaitOne();
                if (MqttClientInstances.ContainsKey(uniqueID))
                    return;
                MqttClientInstances.Add(uniqueID, mqttClient);
            }
            finally
            {
                MqttClientInstancesMutex.ReleaseMutex();
            }
        }

        public static MqttClientHandler MqttClientHandlerGet(ushort uniqueID)
        {
            try
            {
                MqttClientInstancesMutex.WaitOne();
                if (MqttClientInstances.ContainsKey(uniqueID))
                    return MqttClientInstances[uniqueID];
            }
            finally
            {
                MqttClientInstancesMutex.ReleaseMutex();
            }
            return null;
        }

        public void PublishMessage(MqttApplicationMessage message)
        {
            client.PublishAsync(message, CancellationToken.None);
        }

        private Task ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
        {
            if (!ReceiveEvents.ContainsKey(arg.ApplicationMessage.Topic))
                return Task.CompletedTask;

            foreach (var events in ReceiveEvents[arg.ApplicationMessage.Topic])
            { 
                events.Callback?.Invoke(arg, events);
            }
            return Task.CompletedTask;
        }

        private void Subscribe(string topic, MqttQualityOfServiceLevel qosLevel)
        {
            client.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(topic).WithQualityOfServiceLevel(qosLevel).Build());
        }
    }
}
