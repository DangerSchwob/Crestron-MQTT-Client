using MQTTnet.Client;
using MQTTnet.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrestronMqttClient
{
    internal class SubscribeArgs
    {
        public Action<MqttApplicationMessageReceivedEventArgs, SubscribeArgs> Callback { get; set; }
        public MqttQualityOfServiceLevel QosLevel { get; set; }

        public string ParsingInformation { get; set; }
    }
}
