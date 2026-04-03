using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrestronMqttClient
{
    public class MqttClientHandlerSP : MqttClientHandler
    {
        public ushort TLSEnabledSP 
        {
            get => TLSEnabled ? (ushort)1 : (ushort)0;
            set
            { 
                TLSEnabled = value==1; 
            }
        }

        public ushort CertificateValidationSP
        {
            get => CertificateValidation ? (ushort)1 : (ushort)0;
            set
            {
                CertificateValidation = value == 1;
            }
        }
    }
}
