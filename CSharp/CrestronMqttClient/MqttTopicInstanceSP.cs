using MQTTnet.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrestronMqttClient
{
    public class MqttTopicInstanceSP : MqttTopicInstance
    {
        public ushort SignedSP
        {
            get { return (ushort)(Signed ? 1 : 0); }
            set { Signed = value == 1; }
        }

        public ushort RetainSP
        {
            get { return (ushort)(Retain ? 1 : 0); }
            set { Retain = value == 1; }
        }

        public ushort QoSLevelSP
        {
            get { return (ushort)QoSLevel; }
            set
            {
                if (Enum.IsDefined(typeof(MqttQualityOfServiceLevel), (Int32)value))
                    QoSLevel = (MqttQualityOfServiceLevel)((Int32)value);
            }
        }
    }
}
