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
    public static class HmiSetupTools
    {
        [McpServerTool(Name = "setup_hmi_panel"), Description(@"ONE-SHOT HMI panel setup: add panel, connect to PROFINET, set IP and PROFINET name, create initial screens. Pass JSON: {
  ""PanelTypeIdentifier"": ""OrderNumber:6AV2 124-1DC01-0AX0"",
  ""PanelName"": ""HMI_Main"",
  ""SubnetName"": ""PN/IE_1"",
  ""IpAddress"": ""192.168.0.30"",
  ""ProfinetDeviceName"": ""hmi-main"",
  ""PlcDeviceName"": ""PLC_1"",
  ""InitialScreens"": [""Overview"", ""Motors"", ""Alarms"", ""Recipes"", ""Trends""]
}
Supported panel categories: Basic (KTP400/700/900/1200), Comfort (TP/KP 700-2200), Unified Comfort (MTP700-2200), Mobile (KTP F Mobile), IPC.")]
        public static string SetupHmiPanel(string configJson)
        {
            try
            {
                var req = JsonSerializer.Deserialize<HmiSetupService.HmiSetupRequest>(configJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (req == null)
                    return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = "Invalid JSON" });

                var result = ServiceAccessor.HmiSetup.SetupHmiPanel(req);
                return JsonHelper.ToJson(result);
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "list_hmi_panels"), Description("List all HMI panels in the project (KTP, TP, KP, MTP, Mobile, IPC) with IP addresses, screen counts, and connection status.")]
        public static string ListHmiPanels()
        {
            try
            {
                var panels = ServiceAccessor.HmiSetup.GetHmiPanels();
                return JsonHelper.ToJson(new { Success = true, Count = panels.Count, Panels = panels });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "search_hmi_panels"), Description("Search HMI panel catalog. query: 'KTP', 'TP1500', 'Comfort', 'Unified', 'Mobile', 'IPC'. Returns order numbers and descriptions for use with setup_hmi_panel.")]
        public static string SearchHmiPanels(string query)
        {
            try
            {
                var modules = ServiceAccessor.ModuleCatalog.GetByCategory("HMI");
                if (!string.IsNullOrEmpty(query))
                {
                    var q = query.ToUpperInvariant();
                    modules = modules.Where(m =>
                        m.Description.ToUpperInvariant().Contains(q) ||
                        (m.SubCategory ?? "").ToUpperInvariant().Contains(q) ||
                        (m.Tags?.Any(t => t.ToUpperInvariant().Contains(q)) == true)
                    ).ToList();
                }
                return JsonHelper.ToJson(new { Success = true, Count = modules.Count, Panels = modules });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }
    }

    [McpServerToolType]
    public static class SafetyHardwareTools
    {
        [McpServerTool(Name = "recommend_safety_devices"), Description(@"Recommend safety devices (E-Stop buttons, safety relays, light curtains, door switches, two-hand controls) for given hazards. Pass JSON: {
  ""Hazards"": [""E-Stop"", ""Safety door"", ""Light curtain"", ""Two-hand control"", ""Cable-pull""],
  ""RequiredSIL"": ""SIL3"",
  ""RequiredPL"": ""PLe"",
  ""Application"": ""press machine""
}
Returns SIRIUS 3SK relays, 3SU buttons, 3SE switches, FS400 light curtains.")]
        public static string RecommendSafetyDevices(string requirementsJson)
        {
            try
            {
                var req = JsonSerializer.Deserialize<SafetyHardwareService.SafetyRequirement>(requirementsJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (req == null)
                    return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = "Invalid JSON" });

                var result = ServiceAccessor.SafetyHardware.RecommendSafetyDevices(req);
                return JsonHelper.ToJson(new { Success = true, Recommendations = result });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "generate_safety_wiring_plan"), Description(@"Generate F-DI/F-DQ wiring plan for safety devices with PROFIsafe address allocation. Pass JSON array of devices: [
  {""DeviceName"":""E-Stop_1"",""DeviceType"":""E-Stop"",""Hazard"":""Operator emergency"",""InputChannels"":2,""OutputChannels"":0,""RequiredSIL"":""SIL3""},
  {""DeviceName"":""Door_1"",""DeviceType"":""DoorSwitch"",""InputChannels"":2,""OutputChannels"":1},
  {""DeviceName"":""LightCurtain_1"",""DeviceType"":""LightCurtain"",""InputChannels"":2,""OutputChannels"":0}
]")]
        public static string GenerateSafetyWiringPlan(string assignmentsJson, int fDiStartByte, int fDqStartByte)
        {
            try
            {
                var assignments = JsonSerializer.Deserialize<List<SafetyHardwareService.SafetyDeviceAssignment>>(
                    assignmentsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (assignments == null)
                    return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = "Invalid JSON" });

                var result = ServiceAccessor.SafetyHardware.GenerateSafetyWiringPlan(assignments, fDiStartByte, fDqStartByte);
                return JsonHelper.ToJson(new { Success = true, Plan = result });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "generate_safety_template"), Description("Generate SCL safety function templates using built-in F-system blocks. templateType: 'ESTOP1' (emergency stop), 'TWO_HAND' (two-hand control EN 574), 'MUTING' (light curtain muting), 'ENABLE_SWITCH' (3-position enable), 'DOOR_LOCK' (safety door with guard locking). Returns SCL code ready for write_block import.")]
        public static string GenerateSafetyTemplate(string templateType, string blockName)
        {
            try
            {
                var code = ServiceAccessor.SafetyHardware.GenerateSafetyTemplate(templateType, blockName, null);
                return JsonHelper.ToJson(new { Success = true, BlockName = blockName, TemplateType = templateType, SclCode = code });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "search_safety_hardware"), Description("Search safety hardware catalog. query: 'E-Stop', 'light curtain', 'door switch', 'two-hand', 'cable-pull', 'safety relay', '3SK1', '3SK2', '3SU', '3SE', 'enabling switch'. Returns SIRIUS, FS400, SAFETY catalog entries.")]
        public static string SearchSafetyHardware(string query)
        {
            try
            {
                var modules = ServiceAccessor.ModuleCatalog.GetByCategory("Safety");
                if (!string.IsNullOrEmpty(query))
                {
                    var q = query.ToUpperInvariant();
                    modules = modules.Where(m =>
                        m.Description.ToUpperInvariant().Contains(q) ||
                        (m.SubCategory ?? "").ToUpperInvariant().Contains(q) ||
                        (m.Tags?.Any(t => t.ToUpperInvariant().Contains(q)) == true)
                    ).ToList();
                }
                return JsonHelper.ToJson(new { Success = true, Count = modules.Count, Devices = modules });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }
    }

    [McpServerToolType]
    public static class DriveCatalogTools
    {
        [McpServerTool(Name = "search_drives_full"), Description("Search the complete SINAMICS drive and SIMOTICS motor catalog. query: 'G120', 'G120C', 'G120D', 'G120X', 'S120', 'S210', 'V90', 'CU310', 'CU320', '1FK7', '1FT7', '1LE1' (motor). Returns all drives, control units, motor modules, and motors.")]
        public static string SearchDrivesFull(string query)
        {
            try
            {
                var modules = ServiceAccessor.ModuleCatalog.GetAllModules()
                    .Where(m => m.Category == "Drive-Servo" || m.Category == "Drive-VFD" ||
                               m.Category == "Motor-Servo" || m.Category == "Motor-VFD")
                    .ToList();

                if (!string.IsNullOrEmpty(query))
                {
                    var q = query.ToUpperInvariant();
                    modules = modules.Where(m =>
                        m.Description.ToUpperInvariant().Contains(q) ||
                        (m.SubCategory ?? "").ToUpperInvariant().Contains(q) ||
                        (m.OrderNumber ?? "").ToUpperInvariant().Contains(q) ||
                        (m.Tags?.Any(t => t.ToUpperInvariant().Contains(q)) == true)
                    ).ToList();
                }

                return JsonHelper.ToJson(new
                {
                    Success = true,
                    Count = modules.Count,
                    Drives = modules.Where(m => m.Category.StartsWith("Drive")).ToList(),
                    Motors = modules.Where(m => m.Category.StartsWith("Motor")).ToList()
                });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "get_drive_family_info"), Description("Get detailed info about a SINAMICS drive family. family: 'G120C', 'G120D', 'G120X', 'G115D', 'S120', 'S210', 'V90'. Returns typical use cases, telegrams, motor compatibility, and recommended applications.")]
        public static string GetDriveFamilyInfo(string family)
        {
            object? info = (family?.ToUpperInvariant()) switch
            {
                "G120C" => (object)new
                {
                    Family = "SINAMICS G120C",
                    Type = "VFD compact all-in-one",
                    PowerRange = "0.55 - 18.5 kW",
                    Applications = new[] { "Pumps", "Fans", "Conveyors", "Standard machinery" },
                    RecommendedTelegrams = new[] { 1, 20, 352 },
                    Communication = new[] { "PROFINET", "PROFIBUS DP", "USS", "Modbus RTU" },
                    KeyFeature = "Compact design — control unit and power module integrated",
                    MotorCompatibility = "1LE1 (asynchronous)",
                    Notes = "Best for cost-sensitive standard applications"
                },
                "G120D" => new
                {
                    Family = "SINAMICS G120D",
                    Type = "Distributed VFD (IP65)",
                    PowerRange = "0.75 - 7.5 kW",
                    Applications = new[] { "Field-mounted conveyors", "Distributed automation", "Material handling" },
                    RecommendedTelegrams = new[] { 1, 20 },
                    Communication = new[] { "PROFINET", "PROFIsafe" },
                    KeyFeature = "IP65 — mounts directly on the motor or in the field",
                    MotorCompatibility = "1LE1, 1LA9 with brake",
                    Notes = "Eliminates control cabinet wiring"
                },
                "G120X" => new
                {
                    Family = "SINAMICS G120X",
                    Type = "Water/HVAC dedicated VFD",
                    PowerRange = "0.75 - 630 kW",
                    Applications = new[] { "Water/wastewater", "HVAC", "Pumps", "Centrifugal fans" },
                    RecommendedTelegrams = new[] { 1, 20, 352 },
                    Communication = new[] { "PROFINET", "Modbus TCP", "BACnet" },
                    KeyFeature = "Application-specific — Hibernate, ESP, multi-pump control",
                    Notes = "Includes water/HVAC application templates"
                },
                "G115D" => new
                {
                    Family = "SINAMICS G115D",
                    Type = "Compact distributed VFD",
                    PowerRange = "0.37 - 7.5 kW",
                    Applications = new[] { "Intralogistics", "Conveyors", "AGVs", "Material handling" },
                    Communication = new[] { "PROFINET" },
                    KeyFeature = "Wall/motor-mountable IP66"
                },
                "S120" => new
                {
                    Family = "SINAMICS S120",
                    Type = "High-performance servo (modular)",
                    PowerRange = "0.5 - 4500 kW",
                    Applications = new[] { "Multi-axis machines", "Robotics", "Printing", "Packaging", "Machine tools" },
                    RecommendedTelegrams = new[] { 5, 102, 105, 6 },
                    Communication = new[] { "PROFINET IRT", "PROFIBUS DP", "DRIVE-CLiQ" },
                    KeyFeature = "Multi-axis with up to 6 coupled axes per CU320-2",
                    MotorCompatibility = "1FK7, 1FT7, 1FW3, 1PH8",
                    Notes = "Use for synchronized motion control"
                },
                "S210" => new
                {
                    Family = "SINAMICS S210",
                    Type = "PROFINET single-axis servo",
                    PowerRange = "0.05 - 7 kW",
                    Applications = new[] { "Single-axis positioning", "Standard servo applications" },
                    RecommendedTelegrams = new[] { 105, 111 },
                    Communication = new[] { "PROFINET IRT" },
                    KeyFeature = "Compact single-cable connection (motor + encoder)",
                    MotorCompatibility = "1FK2 (low inertia, perfect match)",
                    Notes = "Easiest commissioning — TIA-integrated via Startdrive"
                },
                "V90" => new
                {
                    Family = "SINAMICS V90",
                    Type = "Basic servo drive",
                    PowerRange = "0.05 - 7 kW",
                    Applications = new[] { "Pick-and-place", "Cost-sensitive servo", "Small machinery" },
                    RecommendedTelegrams = new[] { 1, 5, 102, 105, 111 },
                    Communication = new[] { "PROFINET", "USS" },
                    KeyFeature = "Cost-optimized for basic positioning",
                    MotorCompatibility = "1FL6"
                },
                _ => null
            };

            if (info == null)
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = $"Unknown family: {family}. Try: G120C, G120D, G120X, G115D, S120, S210, V90" });

            return JsonHelper.ToJson(new { Success = true, Info = info });
        }
    }
}
