using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using TiaMcpV2.Core;

namespace TiaMcpV2.Services
{
    /// <summary>
    /// Communication protocol service:
    /// - Reference for all protocols (PROFINET, PROFIBUS, AS-i, IO-Link, Modbus, HART, MQTT, OPC UA)
    /// - SCL templates for comm blocks (TSEND_C/TRCV_C, TMAIL_C, MB_CLIENT, GET/PUT, OPC UA)
    /// - Redundancy patterns (MRP/MRPD, S2/R1)
    /// - Web Server / SNMP / SMTP setup guidance
    /// </summary>
    public class CommunicationProtocolService
    {
        private readonly BlockAutonomyService _blockAutonomy;
        private readonly ILogger<CommunicationProtocolService>? _logger;

        public CommunicationProtocolService(BlockAutonomyService blockAutonomy, ILogger<CommunicationProtocolService>? logger = null)
        {
            _blockAutonomy = blockAutonomy;
            _logger = logger;
        }

        public List<Dictionary<string, object?>> GetProtocolReference()
        {
            return new()
            {
                new() {
                    ["Protocol"] = "PROFINET RT",
                    ["Type"] = "Industrial Ethernet (Real-Time)",
                    ["Cycle"] = "1-512ms",
                    ["UseFor"] = "Standard I/O, drives, distributed I/O",
                    ["MaxDevices"] = "≤ 256 IO devices per controller",
                    ["RedundancyOptions"] = "MRP, MRPD, S2 system redundancy"
                },
                new() {
                    ["Protocol"] = "PROFINET IRT",
                    ["Type"] = "Isochronous Real-Time",
                    ["Cycle"] = "31µs - 4ms (deterministic)",
                    ["UseFor"] = "Synchronized motion control, μs precision"
                },
                new() {
                    ["Protocol"] = "PROFIBUS DP",
                    ["Type"] = "Serial fieldbus (RS485)",
                    ["BaudRate"] = "9.6 kbps - 12 Mbps",
                    ["UseFor"] = "Legacy systems, hazardous areas (PROFIBUS PA)",
                    ["MaxDevices"] = "≤ 126 per segment"
                },
                new() {
                    ["Protocol"] = "AS-Interface",
                    ["Type"] = "Sensor/actuator level fieldbus",
                    ["UseFor"] = "Simple binary I/O at sensor level",
                    ["MaxDevices"] = "62 slaves on yellow flat cable"
                },
                new() {
                    ["Protocol"] = "IO-Link",
                    ["Type"] = "Point-to-point sensor/actuator (3-wire)",
                    ["UseFor"] = "Smart sensors, parameterizable devices",
                    ["DataRate"] = "COM1 (4.8kbps), COM2 (38.4), COM3 (230.4)"
                },
                new() {
                    ["Protocol"] = "Modbus RTU",
                    ["Type"] = "Serial (RS232/485)",
                    ["UseFor"] = "Legacy device integration, third-party",
                    ["Implementation"] = "Modbus_Master / Modbus_Slave on CM PtP"
                },
                new() {
                    ["Protocol"] = "Modbus TCP",
                    ["Type"] = "TCP/IP",
                    ["UseFor"] = "VFD integration, third-party PLCs",
                    ["Implementation"] = "MB_CLIENT / MB_SERVER blocks"
                },
                new() {
                    ["Protocol"] = "HART",
                    ["Type"] = "Process instrumentation (4-20mA + digital)",
                    ["UseFor"] = "Smart transmitters, calibration over loop",
                    ["Implementation"] = "ET 200SP AI HART module"
                },
                new() {
                    ["Protocol"] = "MQTT",
                    ["Type"] = "Pub/Sub broker-based messaging",
                    ["UseFor"] = "IoT, cloud connectivity",
                    ["Implementation"] = "MQTT instructions (S7-1500 FW ≥ 2.8)"
                },
                new() {
                    ["Protocol"] = "OPC UA Server",
                    ["Type"] = "Industrial interoperability standard",
                    ["UseFor"] = "SCADA/MES/ERP integration",
                    ["Implementation"] = "Built-in S7-1500 (FW ≥ 2.0), license required for full"
                },
                new() {
                    ["Protocol"] = "OPC UA Client",
                    ["Type"] = "OPC UA consumer",
                    ["UseFor"] = "Read from external OPC UA servers",
                    ["Implementation"] = "OPC_UA_Connect, OPC_UA_NamespaceGetIndexList, etc."
                },
                new() {
                    ["Protocol"] = "Web Server",
                    ["Type"] = "HTTP/HTTPS",
                    ["UseFor"] = "Web pages, diagnostic, custom user pages",
                    ["Implementation"] = "Built-in S7-1500 web server with custom HTML"
                },
                new() {
                    ["Protocol"] = "SMTP",
                    ["Type"] = "Email send",
                    ["UseFor"] = "Alarm emails, reports",
                    ["Implementation"] = "TMAIL_C instruction"
                },
                new() {
                    ["Protocol"] = "FTP",
                    ["Type"] = "File transfer",
                    ["UseFor"] = "Recipe/data file exchange",
                    ["Implementation"] = "FTP_CMD"
                },
                new() {
                    ["Protocol"] = "SNMP",
                    ["Type"] = "Network management",
                    ["UseFor"] = "Network device monitoring",
                    ["Implementation"] = "Built-in SNMP agent on CP modules"
                },
                new() {
                    ["Protocol"] = "S7 Communication",
                    ["Type"] = "Siemens proprietary (TCP)",
                    ["UseFor"] = "PLC-to-PLC data exchange",
                    ["Implementation"] = "GET/PUT/BSEND/BRCV"
                }
            };
        }

        public List<Dictionary<string, object?>> GetRedundancyReference()
        {
            return new()
            {
                new() {
                    ["Type"] = "MRP (Media Redundancy Protocol)",
                    ["Mode"] = "Ring topology with manager + clients",
                    ["RecoveryTime"] = "≤ 200ms",
                    ["UseFor"] = "PROFINET ring redundancy, fault-tolerant network",
                    ["Configuration"] = "1 Manager, all others Clients"
                },
                new() {
                    ["Type"] = "MRPD (Media Redundancy with Planned Duplication)",
                    ["RecoveryTime"] = "0ms (bumpless)",
                    ["UseFor"] = "Highest network availability — packet duplication",
                    ["Requirement"] = "All devices must support MRPD"
                },
                new() {
                    ["Type"] = "PRP (Parallel Redundancy Protocol)",
                    ["RecoveryTime"] = "0ms (bumpless)",
                    ["UseFor"] = "Two parallel networks with double LAN",
                    ["Requirement"] = "RUGGEDCOM or PRP-capable devices"
                },
                new() {
                    ["Type"] = "S2 System Redundancy",
                    ["UseFor"] = "Two PROFINET controllers share IO devices",
                    ["Failover"] = "Switchover < 1 PROFINET cycle"
                },
                new() {
                    ["Type"] = "R1 Device Redundancy",
                    ["UseFor"] = "ET 200 station with redundant interface modules"
                },
                new() {
                    ["Type"] = "S7-1500R/H Hot-Standby",
                    ["UseFor"] = "Two CPUs running synchronously",
                    ["Failover"] = "Bumpless via fiber sync"
                }
            };
        }

        #region SCL Templates

        public string GenerateTcpClientFb(string blockName, string remoteIp, int port)
        {
            var ipParts = remoteIp.Split('.');
            return $@"FUNCTION_BLOCK ""{blockName}""
TITLE = 'TCP/IP Client (TSEND_C / TRCV_C)'
{{ S7_Optimized_Access := 'TRUE' }}
AUTHOR : 'TiaMcpV2'
FAMILY : 'Comm'
VERSION : 1.0
//Open TCP/IP communication client with auto-reconnect

   VAR_INPUT
      iEnable : Bool;
      iSendRequest : Bool;
   END_VAR

   VAR_IN_OUT
      ioSendBuffer : Array[0..255] of Byte;
      ioRecvBuffer : Array[0..255] of Byte;
   END_VAR

   VAR_OUTPUT
      oConnected : Bool;
      oSendDone : Bool;
      oReceived : Bool;
      oError : Bool;
      oErrorCode : Word;
      oBytesReceived : DInt;
   END_VAR

   VAR
      _TSEND : TSEND_C;
      _TRCV : TRCV_C;
      _connection : TCON_IP_v4;
      _sendReqPrev : Bool;
   END_VAR

BEGIN

REGION Connection parameters
    _connection.InterfaceId := 64;          // CPU PROFINET interface
    _connection.ID := 16#0001;
    _connection.ConnectionType := 16#0B;     // TCP/IP active
    _connection.ActiveEstablished := TRUE;
    _connection.RemoteAddress.ADDR[1] := {ipParts[0]};
    _connection.RemoteAddress.ADDR[2] := {ipParts[1]};
    _connection.RemoteAddress.ADDR[3] := {ipParts[2]};
    _connection.RemoteAddress.ADDR[4] := {ipParts[3]};
    _connection.RemotePort := {port};
    _connection.LocalPort := 0;
END_REGION

REGION Send (with auto-connect)
    _TSEND(REQ := iSendRequest AND NOT _sendReqPrev,
           CONT := iEnable,
           LEN := 256,
           CONNECT := _connection,
           DATA := ioSendBuffer);
    _sendReqPrev := iSendRequest;
    oSendDone := _TSEND.DONE;
    oConnected := _TSEND.STATUS = 16#7000 OR _TSEND.STATUS = 16#0000;
END_REGION

REGION Receive (continuous)
    _TRCV(EN_R := iEnable,
          CONT := iEnable,
          LEN := 256,
          CONNECT := _connection,
          DATA := ioRecvBuffer);
    oReceived := _TRCV.NDR;
    oBytesReceived := _TRCV.RCVD_LEN;
END_REGION

REGION Status
    oError := _TSEND.ERROR OR _TRCV.ERROR;
    IF _TSEND.ERROR THEN oErrorCode := _TSEND.STATUS;
    ELSIF _TRCV.ERROR THEN oErrorCode := _TRCV.STATUS;
    END_IF;
END_REGION

END_FUNCTION_BLOCK";
        }

        public string GenerateEmailFb(string blockName, string smtpServer, string fromAddress)
        {
            return $@"FUNCTION_BLOCK ""{blockName}""
TITLE = 'Email Sender (TMAIL_C)'
{{ S7_Optimized_Access := 'TRUE' }}
AUTHOR : 'TiaMcpV2'
FAMILY : 'Comm'
VERSION : 1.0
//SMTP email send for alarm notifications

   VAR_INPUT
      iSend : Bool;
      iSubject : String[80];
      iBody : String[200];
      iToAddress : String[80];
   END_VAR

   VAR_OUTPUT
      oDone : Bool;
      oError : Bool;
      oStatus : Word;
   END_VAR

   VAR
      _TMAIL : TMAIL_C;
      _mailParams : TMail_V4;
      _sendReqPrev : Bool;
   END_VAR

BEGIN

REGION Mail server parameters
    // SMTP server: {smtpServer}
    // From: {fromAddress}
    _mailParams.WATCH_DOG_TIME := T#10s;
    _mailParams.MAIL_SERVER_ADDRESS.ADDR[1] := 192;  // TODO: parse from {smtpServer}
    _mailParams.MAIL_SERVER_ADDRESS.ADDR[2] := 168;
    _mailParams.MAIL_SERVER_ADDRESS.ADDR[3] := 1;
    _mailParams.MAIL_SERVER_ADDRESS.ADDR[4] := 1;
    _mailParams.USERNAME := '';
    _mailParams.PASSWORD := '';
    _mailParams.FROM := '{fromAddress}';
    _mailParams.TO := iToAddress;
    _mailParams.SUBJECT := iSubject;
END_REGION

REGION Send
    _TMAIL(REQ := iSend AND NOT _sendReqPrev,
           TO_S := iToAddress,
           SUBJECT := iSubject,
           TEXT := iBody,
           MAIL_ADDR_PARAM := _mailParams);
    _sendReqPrev := iSend;
    oDone := _TMAIL.DONE;
    oError := _TMAIL.ERROR;
    oStatus := _TMAIL.STATUS;
END_REGION

END_FUNCTION_BLOCK";
        }

        public string GenerateOpcUaServerNote()
        {
            return @"// OPC UA Server is built into S7-1500 (FW >= 2.0)
// CONFIGURATION (TIA Portal, not SCL):
// 1. Open device properties → 'OPC UA' tab
// 2. Enable 'Activate OPC UA server'
// 3. Configure port (default 4840)
// 4. Set security policies (None / Basic128Rsa15 / Basic256 / Basic256Sha256)
// 5. Configure user authentication (Anonymous / User / Certificate)
// 6. In 'Server interfaces' add tags to expose:
//    - From PLC tag tables
//    - From data blocks (mark 'Accessible from HMI/OPC UA')
// 7. Optional: Use OPC UA Companion Specifications (PackML, EUROMAP, etc.)
//
// Apps: SCADA, MES, ERP can read/write via standard OPC UA clients.";
        }

        public string GenerateMqttFb(string blockName, string brokerIp, string topic)
        {
            return $@"FUNCTION_BLOCK ""{blockName}""
TITLE = 'MQTT Client (S7-1500 FW >= 2.8)'
{{ S7_Optimized_Access := 'TRUE' }}
AUTHOR : 'TiaMcpV2'
FAMILY : 'Comm'
VERSION : 1.0
//MQTT publisher/subscriber for IoT integration

   VAR_INPUT
      iEnable : Bool;
      iPublish : Bool;
      iPayload : String[254];
   END_VAR

   VAR_OUTPUT
      oConnected : Bool;
      oPublished : Bool;
      oReceived : Bool;
      oReceivedPayload : String[254];
      oError : Bool;
   END_VAR

   VAR
      // Note: In actual S7-1500, use the MQTT_PUBLISH and MQTT_SUBSCRIBE blocks
      // from the OUC library. This is a wrapper sketch.
      _BrokerIp : String[15] := '{brokerIp}';
      _Topic : String[80] := '{topic}';
      _publishReqPrev : Bool;
   END_VAR

BEGIN
    // TODO: Call MQTT_PUBLISH(REQ:=iPublish, ...) and MQTT_SUBSCRIBE(...)
    // Configure broker IP, port (default 1883 / 8883 for TLS), QoS, retain flag
    // Topic format example: '{topic}/+/data'
    oConnected := iEnable;
    oPublished := iPublish AND NOT _publishReqPrev;
    _publishReqPrev := iPublish;
END_FUNCTION_BLOCK";
        }

        #endregion
    }
}
