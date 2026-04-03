using Crestron.SimplSharp;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using MQTTnet.Server;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CrestronMqttClient
{
    public class MqttTopicInstance
    {
        MqttClientHandler client;

        public EMessageType MessageType { get; set; }
        public ushort DecimalPlaces { get; set; }
        public bool Signed { get; set; }
        public bool Retain { get; set; }
        public MqttQualityOfServiceLevel QoSLevel { get; set; }
        public char ParsingSeperatorChar { get; set; } = '§';

        public delegate void ModuleInitializeDelegate(object sender, EventArgs args);
        public event ModuleInitializeDelegate OnModuleInitializeDone;

        public delegate void ReceiveMessageDelegate(object sender, MessageEventArgs args);
        public event ReceiveMessageDelegate OnReceiveMessage;

        public MqttTopicInstance() 
        { }

        public void Initialize(ushort uniqueID)
        {
            Task.Run(() =>
            {
                while (true)
                {
                    client = MqttClientHandler.MqttClientHandlerGet(uniqueID);
                    if (client != null)
                    {
                        OnModuleInitializeDone?.Invoke(this,new EventArgs());
                        break;
                    }
                    Thread.Sleep(2000);
                }
            });
        }

        private void ReceiveMessage(MqttApplicationMessageReceivedEventArgs messageArgs, SubscribeArgs subscribeArgs)
        {
            var message = Encoding.ASCII.GetString(messageArgs.ApplicationMessage.PayloadSegment.ToArray());
            if (!String.IsNullOrEmpty(subscribeArgs.ParsingInformation))
            {
                message = ParsingData(message, subscribeArgs.ParsingInformation);
                if(String.IsNullOrEmpty(message))
                    return;
            }

            var messageObject = new MessageEventArgs()
            {
                Topic = messageArgs.ApplicationMessage.Topic,
                ParsingInformation = subscribeArgs.ParsingInformation,
                Message = message,
                MessageType = MessageType
            };

            if (MessageType == EMessageType.UShort)
            {
                messageObject.MessageUShort = GetUShortValue(messageObject.Message, DecimalPlaces);
            }

            OnReceiveMessage?.Invoke(this, messageObject);
        }

        public void RegisterTopic(string topic)
        {
            if (client == null)
                return;

            string parsingInformation = String.Empty;
            string topicWithoutParsingInformation = topic;

            if (topic.Contains(ParsingSeperatorChar))
            {
               var data = topic.Split(new[] { ParsingSeperatorChar }, 2, StringSplitOptions.None);
                if (data.Length != 2)
                    return;
                topicWithoutParsingInformation = data[0];
                parsingInformation = ParsingSeperatorChar+data[1];
            }

            client.ReceiveAddEvent(topicWithoutParsingInformation, new SubscribeArgs() { Callback = ReceiveMessage, QosLevel = QoSLevel, ParsingInformation = parsingInformation } );
        }

        public void PublishMessage(string topic, string message)
        {
            if (client == null)
                return;

            var applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(message)
                .WithRetainFlag(Retain)
                .WithQualityOfServiceLevel(QoSLevel)
            .Build();

            client.PublishMessage(applicationMessage);
        }

        public void PublishMessageUShort(string topic, ushort message)
        {
            var messageString = "";
            if (Signed)
                messageString = (((Int16)message)/Math.Pow(10,DecimalPlaces)).ToString(CultureInfo.InvariantCulture);
            else
                messageString = (message / Math.Pow(10, DecimalPlaces)).ToString(CultureInfo.InvariantCulture);

            PublishMessage(topic, messageString);
        }

        private ushort GetUShortValue(string message, ushort decimalPlaces)
        {
            try
            {
                var floatValue = double.Parse(message) * Math.Pow(10, decimalPlaces);

                ushort ushortValue = 0;
                if (floatValue < 0)
                    ushortValue = (ushort)Convert.ToInt16(floatValue);
                else
                    ushortValue = Convert.ToUInt16(floatValue);

                return ushortValue;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private string ParsingData(string message, string parsingInformation)
        {
            try
            {
                if (String.IsNullOrEmpty(parsingInformation))
                    return String.Empty;

                var dataTmp = parsingInformation.Substring(1).Split(new[] { ParsingSeperatorChar }, 2, StringSplitOptions.None);
                if (dataTmp.Length != 2)
                    return String.Empty;

                var data = (DataType: dataTmp[0], ParsingString: dataTmp[1]);

                switch (data.DataType)
                {
                    case "JSON":
                        {
                            JObject jobject = JObject.Parse(message);
                            var value = jobject.SelectToken(data.ParsingString);                             

                            if (value is null || value.Type == JTokenType.Null || value.Type == JTokenType.Undefined)
                                return String.Empty;

                            switch (value.Type)
                            {
                                case JTokenType.Integer:
                                    return value.Value<long>().ToString(CultureInfo.InvariantCulture);

                                case JTokenType.Float:
                                    return value.Value<double>().ToString(CultureInfo.InvariantCulture);

                                case JTokenType.Boolean:
                                case JTokenType.String:
                                    return value.ToString();

                                case JTokenType.Object:
                                case JTokenType.Array:
                                    return String.Empty;

                                default:
                                    return String.Empty;
                            }

                        }
                    default:
                        {
                            return String.Empty;
                        }
                }
            }
            catch (Exception ex)
            {
                CrestronConsole.PrintLine(ex.ToString());
            }

            return String.Empty;


        }
    }
}
