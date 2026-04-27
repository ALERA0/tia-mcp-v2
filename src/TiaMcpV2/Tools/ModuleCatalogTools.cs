using System.ComponentModel;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using TiaMcpV2.Core;
using TiaMcpV2.Helpers;
using TiaMcpV2.Models;
using TiaMcpV2.Services;

namespace TiaMcpV2.Tools
{
    [McpServerToolType]
    public static class ModuleCatalogTools
    {
        [McpServerTool(Name = "search_modules"), Description("Search the module catalog for I/O modules, communication modules, network devices. query examples: 'AI 8', 'DI 16', 'Counter', 'IO-Link', 'SIWAREX', 'CP 1543', 'CM PtP', 'SCALANCE X'. Returns full specs including supported signal types and configurable parameters.")]
        public static string SearchModules(string query)
        {
            try
            {
                var results = ServiceAccessor.ModuleCatalog.Search(query);
                return JsonHelper.ToJson(new { Success = true, Count = results.Count, Modules = results });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "get_modules_by_category"), Description("List modules of a specific category. Categories: DI, DQ, DI/DQ, AI, AQ, F-DI, F-DQ, Counter, Timer, Communication, Weighing, InterfaceModule, Network.")]
        public static string GetModulesByCategory(string category)
        {
            try
            {
                var results = ServiceAccessor.ModuleCatalog.GetByCategory(category);
                return JsonHelper.ToJson(new { Success = true, Category = category, Count = results.Count, Modules = results });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "recommend_io_module"), Description(@"Recommend the best I/O module for a requirement. Pass JSON like:
{
  ""Category"": ""AI"",
  ""MinChannels"": 8,
  ""SignalType"": ""RTD"",
  ""PreferredSubCategory"": ""HF""
}
Categories: DI, DQ, AI, AQ, F-DI, F-DQ. SubCategories: BA (Basic), ST (Standard), HF (High Feature), HS (High Speed), RLY (Relay).")]
        public static string RecommendIoModule(string requirementsJson)
        {
            try
            {
                var req = JsonSerializer.Deserialize<ModuleCatalogService.IoModuleRequirements>(requirementsJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (req == null)
                    return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = "Invalid JSON" });

                var results = ServiceAccessor.ModuleCatalog.RecommendIoModule(req);
                return JsonHelper.ToJson(new { Success = true, Count = results.Count, TopRecommendations = results.Take(5).ToList() });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "get_module_parameters"), Description("Get the list of configurable parameters for a module by order number. Returns parameter names that can be set via configure_module / configure_analog_input / etc.")]
        public static string GetModuleParameters(string orderNumber)
        {
            try
            {
                var module = ServiceAccessor.ModuleCatalog.GetByOrderNumber(orderNumber);
                if (module == null)
                    return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = $"Module not found: {orderNumber}" });

                return JsonHelper.ToJson(new
                {
                    Success = true,
                    OrderNumber = module.OrderNumber,
                    Description = module.Description,
                    Category = module.Category,
                    ChannelCount = module.ChannelCount,
                    Parameters = module.Parameters ?? Array.Empty<string>(),
                    SupportedSignalTypes = module.SupportedSignalTypes ?? Array.Empty<string>()
                });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }
    }

    [McpServerToolType]
    public static class ModuleConfigTools
    {
        [McpServerTool(Name = "configure_module"), Description(@"Configure parameters on a module. Pass JSON dictionary of parameter→value pairs. Use 'Channel[N].Param' for channel-specific parameters, or just 'Param' for module-wide. Example: {""InputDelay"":""3ms"",""DiagnosticInterrupt"":true,""Channel[0].MeasurementType"":""Voltage"",""Channel[0].MeasurementRange"":""+/-10V""}")]
        public static string ConfigureModule(string deviceItemPath, string parametersJson)
        {
            try
            {
                var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(parametersJson);
                if (parameters == null)
                    return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = "Invalid JSON" });

                var result = ServiceAccessor.ModuleConfig.ConfigureModule(deviceItemPath, parameters);
                return JsonHelper.ToJson(new { Success = true, Result = result });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "configure_analog_input"), Description(@"Configure an Analog Input module. Pass JSON: {
  ""ChannelIndex"": 0,
  ""MeasurementType"": ""Voltage""|""Current""|""RTD""|""Thermocouple""|""Resistance"",
  ""MeasurementRange"": ""+/-10V""|""0..10V""|""4..20mA""|""0..20mA""|""PT100""|""K"",
  ""FilterLevel"": ""None""|""Weak""|""Medium""|""Strong"",
  ""WireBreakDetection"": true,
  ""DiagnosticInterruptEnable"": true,
  ""UpperAlarmLimit"": 95.0,
  ""LowerAlarmLimit"": 5.0,
  ""RtdType"": ""Pt100""|""Pt1000"",
  ""ThermocoupleType"": ""K""|""J""|""T"",
  ""ConnectionType"": ""2-wire""|""3-wire""|""4-wire""
}")]
        public static string ConfigureAnalogInput(string moduleItemPath, string configJson)
        {
            try
            {
                var config = JsonSerializer.Deserialize<ModuleConfigService.AnalogInputConfig>(configJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (config == null)
                    return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = "Invalid JSON" });

                ServiceAccessor.ModuleConfig.ConfigureAnalogInput(moduleItemPath, config);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Configured AI module: {moduleItemPath}" });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "configure_digital_input"), Description(@"Configure a Digital Input module. JSON: {
  ""InputDelay"": ""0.05ms""|""0.1ms""|""0.5ms""|""3ms""|""20ms"",
  ""DiagnosticInterruptEnable"": true,
  ""HardwareInterruptEnable"": false,
  ""NoSensorSupplyDiagnostic"": true,
  ""WireBreakDiagnostic"": true
}")]
        public static string ConfigureDigitalInput(string moduleItemPath, string configJson)
        {
            try
            {
                var config = JsonSerializer.Deserialize<ModuleConfigService.DigitalInputConfig>(configJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (config == null)
                    return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = "Invalid JSON" });
                ServiceAccessor.ModuleConfig.ConfigureDigitalInput(moduleItemPath, config);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Configured DI module: {moduleItemPath}" });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "configure_digital_output"), Description(@"Configure a Digital Output module. JSON: {
  ""DiagnosticInterruptEnable"": true,
  ""ShortCircuitDiagnostic"": true,
  ""WireBreakDiagnostic"": false,
  ""ReactionToCpuStop"": ""TurnOff""|""ApplySubstitute""|""KeepLastValue"",
  ""SubstituteValue"": false
}")]
        public static string ConfigureDigitalOutput(string moduleItemPath, string configJson)
        {
            try
            {
                var config = JsonSerializer.Deserialize<ModuleConfigService.DigitalOutputConfig>(configJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (config == null)
                    return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = "Invalid JSON" });
                ServiceAccessor.ModuleConfig.ConfigureDigitalOutput(moduleItemPath, config);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Configured DQ module: {moduleItemPath}" });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "configure_counter_module"), Description(@"Configure a Counter (TM Count) or Position (TM PosInput) module. JSON: {
  ""OperatingMode"": ""Counting""|""Measurement""|""Position"",
  ""CountingDirection"": ""Up""|""Down""|""Bidirectional"",
  ""SignalType"": ""Pulse_24V""|""Encoder_24V""|""RS422""|""SSI"",
  ""SignalEvaluation"": ""Single""|""Double""|""Quadruple"",
  ""FilterFrequencyHz"": 50000,
  ""HardwareInterruptEnable"": true,
  ""EncoderType"": ""Incremental""|""Absolute""|""SSI"",
  ""EncoderResolution"": 1024
}")]
        public static string ConfigureCounter(string moduleItemPath, string configJson)
        {
            try
            {
                var config = JsonSerializer.Deserialize<ModuleConfigService.CounterConfig>(configJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (config == null)
                    return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = "Invalid JSON" });
                ServiceAccessor.ModuleConfig.ConfigureCounter(moduleItemPath, config);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Configured Counter module: {moduleItemPath}" });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "configure_io_link_port"), Description(@"Configure an IO-Link Master port. JSON: {
  ""PortMode"": ""Disabled""|""DI""|""DQ""|""IO-Link Auto""|""IO-Link Manual"",
  ""BaudRate"": ""COM1""|""COM2""|""COM3"",
  ""CycleTimeUs"": 2000,
  ""InputLengthBytes"": 8,
  ""OutputLengthBytes"": 4,
  ""VendorId"": ""0x123"",
  ""DeviceId"": ""0x456""
}")]
        public static string ConfigureIoLinkPort(string moduleItemPath, int portIndex, string configJson)
        {
            try
            {
                var config = JsonSerializer.Deserialize<ModuleConfigService.IoLinkPortConfig>(configJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (config == null)
                    return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = "Invalid JSON" });
                ServiceAccessor.ModuleConfig.ConfigureIoLinkPort(moduleItemPath, portIndex, config);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Configured IO-Link port {portIndex} on {moduleItemPath}" });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "configure_safety_module"), Description(@"Configure F-DI / F-DQ safety module. JSON: {
  ""ShortCircuitTestEnable"": true,
  ""DiscrepancyTimeMs"": 100,
  ""ChannelEvaluation"": ""1oo1""|""1oo2"",
  ""ParameterSignature"": ""abc123"",
  ""ReactionTimeMs"": 50
}")]
        public static string ConfigureSafetyModule(string moduleItemPath, string configJson)
        {
            try
            {
                var config = JsonSerializer.Deserialize<ModuleConfigService.SafetyModuleConfig>(configJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (config == null)
                    return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = "Invalid JSON" });
                ServiceAccessor.ModuleConfig.ConfigureSafetyModule(moduleItemPath, config);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Configured safety module: {moduleItemPath}" });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }
    }

    [McpServerToolType]
    public static class DistributedIoTools
    {
        [McpServerTool(Name = "setup_distributed_io_station"), Description(@"ONE-SHOT distributed I/O station setup (ET 200SP/MP/AL/eco/pro). Adds head module, connects to PROFINET, sets IP, configures MRP, plugs all modules with parameters. Pass JSON: {
  ""HeadModuleTypeIdentifier"": ""OrderNumber:6ES7 155-6AU01-0BN0"",
  ""StationName"": ""ET200SP_Station1"",
  ""SubnetName"": ""PN/IE_1"",
  ""IpAddress"": ""192.168.0.10"",
  ""ProfinetDeviceName"": ""et200sp-station1"",
  ""IoSystemName"": ""PROFINET IO-System"",
  ""MrpRole"": ""Client"",
  ""Modules"": [
    {""TypeIdentifier"": ""OrderNumber:6ES7 131-6BH00-0BA0"", ""Name"": ""DI_1"", ""Parameters"": {""InputDelay"":""3ms""}},
    {""TypeIdentifier"": ""OrderNumber:6ES7 132-6BH00-0BA0"", ""Name"": ""DQ_1""}
  ],
  ""AddServerModule"": true
}")]
        public static string SetupDistributedIoStation(string configJson)
        {
            try
            {
                var req = JsonSerializer.Deserialize<DistributedIoService.DistributedIoSetupRequest>(configJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (req == null)
                    return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = "Invalid JSON" });

                var result = ServiceAccessor.DistributedIo.SetupDistributedIoStation(req);
                return JsonHelper.ToJson(result);
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "configure_shared_device"), Description("Enable Shared Device on an ET 200 station — multiple controllers can access the same I/O modules. controllerPathsJson is a JSON array of PLC paths.")]
        public static string ConfigureSharedDevice(string stationPath, string controllerPathsJson)
        {
            try
            {
                var controllers = JsonSerializer.Deserialize<List<string>>(controllerPathsJson) ?? new List<string>();
                ServiceAccessor.DistributedIo.ConfigureSharedDevice(stationPath, controllers);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Enabled SharedDevice on {stationPath}" });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "list_distributed_stations"), Description("List all distributed I/O stations (ET 200SP/MP/AL/pro/eco) in the project with their network info, IP addresses and module counts.")]
        public static string ListDistributedStations()
        {
            try
            {
                var stations = ServiceAccessor.DistributedIo.GetDistributedStations();
                return JsonHelper.ToJson(new { Success = true, Count = stations.Count, Stations = stations });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "get_et200_head_modules"), Description("Get available ET 200 head modules (IM 155-x for ET 200SP, IM 155-5 for ET 200MP, IM 157-1 for ET 200AL, IM 154-1 for ET 200pro).")]
        public static string GetEt200HeadModules()
        {
            try
            {
                var modules = ServiceAccessor.ModuleCatalog.GetByCategory("InterfaceModule");
                return JsonHelper.ToJson(new { Success = true, Count = modules.Count, HeadModules = modules });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }
    }
}
