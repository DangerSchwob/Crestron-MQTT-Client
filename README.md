
# Crestron MQTT Client

This project provides functionality to connect a Crestron 4‑Series control system to an MQTT broker for subscribing to or publishing data on MQTT topics.

The implementation is written in **.NET Framework 4.8** and serves as a wrapper around the **MQTTnet** library:  
👉 https://github.com/dotnet/MQTTnet

---

## Overview

The solution consists of two main SIMPL Windows module types:

### **1. `MqttClientInstance`**
Represents a connection to an MQTT broker.  
You can use multiple instances when:
- Connecting to several brokers  
- Using different credentials  

Else you can use a single module for all your topic modules.

### **2. `MqttTopicInstance`**
Represents an MQTT topic you want to **publish to** or **subscribe from**.

The `uniqueID` parameter is used to associate each topic module with a specific client module.  
Multiple topic modules can be bound to the same client instance.

Each module includes a help file explaining all parameters in detail.

---

## Topic Module Types

Although MQTT transmits everything as **strings**, topic modules are provided in three variants for convenience:

- **String**
- **Bool**
- **UShort**

These modules support data conversion to/from the appropriate type for use in SIMPL Windows programs.

---

## JSON Parsing for Subscriptions

Many MQTT devices publish **JSON payloads**, which can be difficult to parse directly in SIMPL Windows.

The subscription topic modules include a **parsing feature** that lets you extract a specific JSON value using a **select token** (https://www.newtonsoft.com/json/help/html/selecttoken.htm).

### Example  
Incoming MQTT payload on topic: `device/report`:

```json
{
  "address": {
    "abandoned": true,
    "ZIP": 12345,
    "city": "Anytown"
  }
}
```

To extract the `ZIP` element, you can set the topic field to:

```
device/report§JSON§address.ZIP
```

This will parse the JSON and output only the value `12345`.

The demo programming also shows how to use this feature

---

## Disclaimer

- This programming is **not created by Crestron Electronics**.  
- This programming is **not supported** by Crestron Electronics’ support team.  
- Crestron Electronics is **not liable for any damage** caused by this programming.

