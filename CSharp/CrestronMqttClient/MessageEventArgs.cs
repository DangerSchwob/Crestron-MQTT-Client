using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrestronMqttClient
{
    public class MessageEventArgs : EventArgs
    {
        public EMessageType MessageType { get; set; }
        public string Topic { get; set; }
        public string ParsingInformation { get; set; }
        public string Message { get; set; }
        public bool MessageBool { get; set; }
        public ushort MessageUShort { get; set; }
    }
}
