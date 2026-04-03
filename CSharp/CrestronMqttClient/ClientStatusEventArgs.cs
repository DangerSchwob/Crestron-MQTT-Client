using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrestronMqttClient
{
    public class ClientStatusEventArgs : EventArgs
    {
        public ushort Status { get; set; }
    }
}
