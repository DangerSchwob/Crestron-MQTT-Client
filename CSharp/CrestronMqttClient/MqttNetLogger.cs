using Crestron.SimplSharp;
using MQTTnet.Diagnostics;
using System;

namespace CrestronMqttClient
{
    internal class MqttNetLogger : IMqttNetLogger
    {
        public bool IsEnabled { get; set; } = false;

        public void Publish(MqttNetLogLevel logLevel, string source, string message, object[] parameters, Exception exception)
        {
            CrestronConsole.PrintLine($"[{logLevel}] " +
                                  $"{source} - " +
                                  $"{message}");
            if(!(exception is null))
                CrestronConsole.PrintLine($"{exception}");
        }
    }
}