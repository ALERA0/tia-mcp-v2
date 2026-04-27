using System.ComponentModel;
using ModelContextProtocol.Server;
using System;
using TiaMcpV2.Core;
using TiaMcpV2.Helpers;
using TiaMcpV2.Models;

namespace TiaMcpV2.Tools
{
    [McpServerToolType]
    public static class SafetyProgrammingTools
    {
        [McpServerTool(Name = "get_f_instructions"), Description("Get reference of all F-CPU safety instructions: ESTOP1, EDM, SFDOOR, FDBACK, MUTING, TWO_H_EN, ACK_GL, ACK_REI, ENABLE_SWITCH. Returns purpose, inputs, outputs, related standards (ISO 13850, EN 574).")]
        public static string GetFInstructions()
        {
            try
            {
                var refs = ServiceAccessor.SafetyProgramming.GetFInstructionReference();
                return JsonHelper.ToJson(new { Success = true, Instructions = refs });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "get_safety_workflow"), Description("Get the complete F-program development workflow: SRS planning, hardware setup, F-programming, signature, validation, TÜV documentation, acceptance test. Returns 10-step workflow + standards (ISO 13849, IEC 61508, IEC 62061, ISO 13850) + best practices.")]
        public static string GetSafetyWorkflow()
        {
            try
            {
                var workflow = ServiceAccessor.SafetyProgramming.GetSafetyWorkflow();
                return JsonHelper.ToJson(new { Success = true, Workflow = workflow });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "create_f_runtime_block"), Description("Create an F-OB safety runtime block with ESTOP1, SFDOOR, MUTING templates already integrated. References DB_FSafety for F-data. Imports directly into TIA Portal F-runtime group.")]
        public static string CreateFRuntimeBlock(string softwarePath, string groupPath, string blockName)
        {
            try
            {
                var code = ServiceAccessor.SafetyProgramming.GenerateFobTemplate(blockName);
                ServiceAccessor.BlockAutonomy.WriteBlockFromScl(softwarePath, groupPath, code, blockName);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Created F-runtime block: {blockName}" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "create_f_data_block"), Description("Create an F-DB (safety data block) with all common safety variables (E-Stop chain, doors, light curtain, ack signals).")]
        public static string CreateFDataBlock(string softwarePath, string groupPath, string blockName)
        {
            try
            {
                var code = ServiceAccessor.SafetyProgramming.GenerateFDbTemplate(blockName);
                ServiceAccessor.BlockAutonomy.WriteBlockFromScl(softwarePath, groupPath, code, blockName);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Created F-DB: {blockName}" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }
    }

    [McpServerToolType]
    public static class HmiProgrammingTools
    {
        [McpServerTool(Name = "get_wincc_variants"), Description("Get reference of all WinCC HMI runtime variants: Basic, Comfort, Advanced, Professional, Unified. Returns tag limits, screen counts, scripting support, recommended use case.")]
        public static string GetWinCCVariants()
        {
            try
            {
                var variants = ServiceAccessor.HmiProgramming.GetWinCCVariants();
                return JsonHelper.ToJson(new { Success = true, Variants = variants });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "get_alarm_classes"), Description("Get HMI alarm class reference: Errors (red), Warnings (yellow), System (blue), Info (green), Diagnostics (orange). Use these as the standard alarm class structure.")]
        public static string GetAlarmClasses()
        {
            try
            {
                var classes = ServiceAccessor.HmiProgramming.GetAlarmClassReference();
                return JsonHelper.ToJson(new { Success = true, AlarmClasses = classes });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "create_faceplate_udt"), Description("Create a Faceplate UDT for HMI binding. objectType: 'Motor' (status/command/process), 'Valve' (open/close/position), 'Analog' (value/alarms/sim), 'PID' (sp/pv/output/tuning), 'Alarm' (alarm record). Imports as UDT ready to bind in WinCC faceplate.")]
        public static string CreateFaceplateUdt(string softwarePath, string groupPath, string objectType, string udtName)
        {
            try
            {
                var code = ServiceAccessor.HmiProgramming.GenerateFaceplateUdt(objectType, udtName);
                ServiceAccessor.BlockAutonomy.WriteBlockFromScl(softwarePath, groupPath, code, udtName);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Created {objectType} faceplate UDT: {udtName}" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "create_recipe_db"), Description("Create a Recipe DB with metadata structure (10 recipes default). Includes setpoints, durations, quality limits, modification tracking, active recipe selection. Customize the parameters as needed after import.")]
        public static string CreateRecipeDb(string softwarePath, string groupPath, string dbName, int recipeCount)
        {
            try
            {
                var count = recipeCount > 0 ? recipeCount : 10;
                var code = ServiceAccessor.HmiProgramming.GenerateRecipeDb(dbName, count);
                ServiceAccessor.BlockAutonomy.WriteBlockFromScl(softwarePath, groupPath, code, dbName);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Created recipe DB: {dbName} ({count} recipes)" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "get_area_pointers"), Description("Get HMI area pointer reference for bidirectional PLC↔HMI communication: Job mailbox, Project ID, Screen number, Coordination, Date/Time, Trends, Data record (recipe), User version.")]
        public static string GetAreaPointers()
        {
            try
            {
                var info = ServiceAccessor.HmiProgramming.GetAreaPointerReference();
                return JsonHelper.ToJson(new { Success = true, AreaPointers = info });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }
    }

    [McpServerToolType]
    public static class CommunicationProtocolTools
    {
        [McpServerTool(Name = "get_protocol_reference"), Description("Get reference of all industrial communication protocols supported by S7-1500: PROFINET RT/IRT, PROFIBUS DP, AS-i, IO-Link, Modbus RTU/TCP, HART, MQTT, OPC UA Server/Client, Web Server, SMTP, FTP, SNMP, S7 Communication.")]
        public static string GetProtocolReference()
        {
            try
            {
                var protocols = ServiceAccessor.CommProtocol.GetProtocolReference();
                return JsonHelper.ToJson(new { Success = true, Protocols = protocols });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "get_redundancy_reference"), Description("Get reference of network and CPU redundancy options: MRP (200ms), MRPD (0ms), PRP, S2 system redundancy, R1 device redundancy, S7-1500R/H hot-standby. Returns recovery times, requirements, use cases.")]
        public static string GetRedundancyReference()
        {
            try
            {
                var redundancy = ServiceAccessor.CommProtocol.GetRedundancyReference();
                return JsonHelper.ToJson(new { Success = true, Redundancy = redundancy });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "create_tcp_client_fb"), Description("Create a TCP/IP client FB using TSEND_C/TRCV_C with auto-connect. Pass remoteIp ('192.168.0.100') and port. Generated FB has send/receive buffers, connected/error status, automatic reconnection.")]
        public static string CreateTcpClientFb(string softwarePath, string groupPath, string blockName, string remoteIp, int port)
        {
            try
            {
                var code = ServiceAccessor.CommProtocol.GenerateTcpClientFb(blockName, remoteIp, port);
                ServiceAccessor.BlockAutonomy.WriteBlockFromScl(softwarePath, groupPath, code, blockName);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Created TCP client FB: {blockName} → {remoteIp}:{port}" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "create_email_fb"), Description("Create an email sender FB using TMAIL_C for SMTP alarm notifications. Pass smtpServer IP and fromAddress. Inputs: subject, body, to address. Outputs: done, error, status.")]
        public static string CreateEmailFb(string softwarePath, string groupPath, string blockName, string smtpServer, string fromAddress)
        {
            try
            {
                var code = ServiceAccessor.CommProtocol.GenerateEmailFb(blockName, smtpServer, fromAddress);
                ServiceAccessor.BlockAutonomy.WriteBlockFromScl(softwarePath, groupPath, code, blockName);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Created email FB: {blockName}" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "create_mqtt_fb"), Description("Create an MQTT client FB (S7-1500 FW >= 2.8). Pass broker IP and topic. Sketches the structure — actual MQTT_PUBLISH/MQTT_SUBSCRIBE calls need filling in based on broker.")]
        public static string CreateMqttFb(string softwarePath, string groupPath, string blockName, string brokerIp, string topic)
        {
            try
            {
                var code = ServiceAccessor.CommProtocol.GenerateMqttFb(blockName, brokerIp, topic);
                ServiceAccessor.BlockAutonomy.WriteBlockFromScl(softwarePath, groupPath, code, blockName);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Created MQTT FB: {blockName}" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "get_opc_ua_setup_guide"), Description("Get guide for setting up OPC UA Server on S7-1500. Returns step-by-step configuration: enable server, port, security policies, user auth, tag exposure, companion specifications.")]
        public static string GetOpcUaSetupGuide()
        {
            try
            {
                var guide = ServiceAccessor.CommProtocol.GenerateOpcUaServerNote();
                return JsonHelper.ToJson(new { Success = true, Guide = guide });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }
    }
}
