using System.ComponentModel;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.Text.Json;
using TiaMcpV2.Core;
using TiaMcpV2.Helpers;
using TiaMcpV2.Models;
using TiaMcpV2.Services;

namespace TiaMcpV2.Tools
{
    [McpServerToolType]
    public static class ServoDriveTools
    {
        [McpServerTool(Name = "setup_servo_axis"), Description(@"ONE-SHOT complete servo axis setup. Does ALL steps in one call: (1) Add SINAMICS drive, (2) Connect to PROFINET, (3) Set IP+device name, (4) Connect to IO system, (5) Configure telegram, (6) Apply motor config, (7) Create TO on PLC, (8) Link TO actor+sensor to drive, (9) Apply all axis parameters. Pass a JSON configuration. Example: {
  ""DriveTypeIdentifier"": ""OrderNumber:6SL3 210-5HB10-4UF0"",
  ""DriveName"": ""Drive_X"",
  ""SubnetName"": ""PN/IE_1"",
  ""DriveIpAddress"": ""192.168.0.5"",
  ""IoSystemName"": ""PROFINET IO-System"",
  ""TelegramNumber"": 111,
  ""MotorConfig"": { ""MotorOrderNumber"": ""1FK2104-5AG00-0MA0"", ""RatedPowerKW"": 0.75, ""RatedSpeedRpm"": 3000, ""RatedTorqueNm"": 2.4, ""MotorType"": ""Synchronous"", ""EncoderType"": ""Absolute"" },
  ""TargetPlcPath"": ""PLC_1"",
  ""TOName"": ""Axis_X"",
  ""TOType"": ""TO_PositioningAxis"",
  ""TOVersion"": ""8.0"",
  ""AxisParameters"": { ""DynamicLimits.MaxVelocity"": ""1000"", ""DynamicLimits.MaxAcceleration"": ""5000"" }
}")]
        public static string SetupServoAxis(string configJson)
        {
            try
            {
                var req = JsonSerializer.Deserialize<ServoDriveService.ServoSetupRequest>(configJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (req == null)
                    return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = "Invalid JSON configuration" });

                var result = ServiceAccessor.ServoDrive.SetupCompleteServoAxis(req);
                return JsonHelper.ToJson(result);
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "apply_motor_config"), Description("Apply motor configuration parameters to a drive device. Use this to set motor rated power, current, speed, torque, encoder type, pole pairs, etc. without changing other drive settings. Pass a JSON MotorConfig object.")]
        public static string ApplyMotorConfig(
            string driveName,
            string motorConfigJson)
        {
            try
            {
                var config = JsonSerializer.Deserialize<ServoDriveService.MotorConfig>(motorConfigJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (config == null)
                    return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = "Invalid motor config JSON" });

                var device = ServiceAccessor.Portal.FindDevice(driveName);
                if (device == null)
                    return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = $"Drive not found: {driveName}" });

                ServiceAccessor.ServoDrive.ApplyMotorConfig(device, config);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Applied motor config to {driveName}" });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "get_axis_parameter_preset"), Description("Get recommended axis parameter preset for common applications. applicationType: 'highDynamic' (fast positioning), 'conveyor' (standard), 'heavyLoad' (crane/lifting), 'precision' (CNC/precision positioning). Returns parameter dictionary ready to pass to set_to_parameters.")]
        public static string GetAxisParameterPreset(
            string applicationType,
            string motorConfigJson)
        {
            try
            {
                ServoDriveService.MotorConfig? motor = null;
                if (!string.IsNullOrEmpty(motorConfigJson) && motorConfigJson != "{}")
                {
                    motor = JsonSerializer.Deserialize<ServoDriveService.MotorConfig>(motorConfigJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }

                var preset = ServiceAccessor.ServoDrive.GetAxisParameterPreset(applicationType, motor);
                return JsonHelper.ToJson(new
                {
                    Success = true,
                    ApplicationType = applicationType,
                    Preset = preset,
                    Hint = "Pass this preset dictionary to set_to_parameters as the parametersJson argument"
                });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "search_servo_drives"), Description("Search the catalog for SINAMICS servo drives and motors. query examples: 'S210', 'V90', 'G120', '1FK2', '1FL6', or power specs like '0.75kW'.")]
        public static string SearchServoDrives(string query)
        {
            try
            {
                var results = ServiceAccessor.Catalog.Search(query);
                var filtered = new List<object>();
                foreach (var r in results)
                {
                    if (r.Category?.StartsWith("Drive") == true || r.Category?.StartsWith("Motor") == true)
                    {
                        filtered.Add(new
                        {
                            r.TypeIdentifier,
                            r.OrderNumber,
                            r.Description,
                            r.Category
                        });
                    }
                }
                return JsonHelper.ToJson(new { Success = true, Results = filtered, Count = filtered.Count });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "get_telegram_reference"), Description("Get reference of available PROFIdrive telegram numbers with their purposes. Use this to know which telegram to use in setup_servo_axis.")]
        public static string GetTelegramReference()
        {
            var telegrams = new List<object>
            {
                new { Number = 1, Name = "Standard Telegram 1", Description = "Speed control, 16-bit setpoint (VIK/NAMUR)" },
                new { Number = 2, Name = "Standard Telegram 2", Description = "Speed control, 32-bit setpoint" },
                new { Number = 3, Name = "Standard Telegram 3", Description = "Speed control with 1 position encoder" },
                new { Number = 4, Name = "Standard Telegram 4", Description = "Speed control with 2 position encoders" },
                new { Number = 5, Name = "Standard Telegram 5", Description = "DSC (Dynamic Servo Control) with 1 encoder" },
                new { Number = 6, Name = "Standard Telegram 6", Description = "DSC with 2 encoders" },
                new { Number = 7, Name = "Standard Telegram 7", Description = "Positioning, basic positioner" },
                new { Number = 9, Name = "Standard Telegram 9", Description = "Positioning with direct setpoint input" },
                new { Number = 20, Name = "Standard Telegram 20", Description = "Basic speed control with actual values" },
                new { Number = 81, Name = "SIEMENS Telegram 81", Description = "Synchronous operation, encoder 1" },
                new { Number = 83, Name = "SIEMENS Telegram 83", Description = "Synchronous operation, encoder 1+2" },
                new { Number = 102, Name = "SIEMENS Telegram 102", Description = "Speed control with torque reduction + 1 position encoder" },
                new { Number = 103, Name = "SIEMENS Telegram 103", Description = "Speed control with torque reduction + 2 position encoders" },
                new { Number = 105, Name = "SIEMENS Telegram 105", Description = "DSC with torque reduction + 1 encoder (RECOMMENDED for S120/S210 axes)" },
                new { Number = 106, Name = "SIEMENS Telegram 106", Description = "DSC with torque reduction + 2 encoders" },
                new { Number = 111, Name = "SIEMENS Telegram 111", Description = "Basic positioner telegram (RECOMMENDED for S210/V90 positioning)" },
                new { Number = 352, Name = "SIEMENS Telegram 352", Description = "Process data for VFDs" },
                new { Number = 353, Name = "SIEMENS Telegram 353", Description = "Standard telegram 1 + PKW (parameter access)" }
            };
            return JsonHelper.ToJson(new { Success = true, Telegrams = telegrams,
                Recommendations = new Dictionary<string, int>
                {
                    ["Positioning (S210/V90)"] = 111,
                    ["Servo axis (S120)"] = 105,
                    ["Speed/VFD (G120)"] = 352,
                    ["Simple speed control"] = 1,
                    ["Synchronous operation"] = 83
                }
            });
        }

        [McpServerTool(Name = "get_motor_config_template"), Description("Get a motor configuration template for a specific SINAMICS motor by order number. Returns preset MotorConfig JSON with typical motor parameters. Motor orders: 1FK2103, 1FK2104, 1FK2106, 1FK7032, 1FK7042, 1FL6024, 1FL6042, 1FL6044, 1FL6064.")]
        public static string GetMotorConfigTemplate(string motorOrderNumber)
        {
            var templates = new Dictionary<string, ServoDriveService.MotorConfig>(StringComparer.OrdinalIgnoreCase)
            {
                // 1FK2 series (low inertia, for S210)
                ["1FK2103"] = new ServoDriveService.MotorConfig
                {
                    MotorOrderNumber = "1FK2103-4AG00-0MA0", MotorType = "Synchronous",
                    RatedPowerKW = 0.4, RatedVoltageV = 400, RatedCurrentA = 1.05,
                    RatedSpeedRpm = 3000, MaxSpeedRpm = 6000,
                    RatedTorqueNm = 1.27, MaxTorqueNm = 4.5,
                    PolePairs = 3, InertiaKgCm2 = 0.18,
                    EncoderType = "Absolute", EncoderResolution = 4194304
                },
                ["1FK2104"] = new ServoDriveService.MotorConfig
                {
                    MotorOrderNumber = "1FK2104-5AG00-0MA0", MotorType = "Synchronous",
                    RatedPowerKW = 0.75, RatedVoltageV = 400, RatedCurrentA = 1.93,
                    RatedSpeedRpm = 3000, MaxSpeedRpm = 6000,
                    RatedTorqueNm = 2.4, MaxTorqueNm = 8.5,
                    PolePairs = 3, InertiaKgCm2 = 0.38,
                    EncoderType = "Absolute", EncoderResolution = 4194304
                },
                ["1FK2106"] = new ServoDriveService.MotorConfig
                {
                    MotorOrderNumber = "1FK2106-3AG00-0MA0", MotorType = "Synchronous",
                    RatedPowerKW = 1.5, RatedVoltageV = 400, RatedCurrentA = 3.85,
                    RatedSpeedRpm = 3000, MaxSpeedRpm = 6000,
                    RatedTorqueNm = 4.78, MaxTorqueNm = 17.0,
                    PolePairs = 3, InertiaKgCm2 = 0.99,
                    EncoderType = "Absolute", EncoderResolution = 4194304
                },
                // 1FK7 series (compact)
                ["1FK7032"] = new ServoDriveService.MotorConfig
                {
                    MotorOrderNumber = "1FK7032-5AK71-1AH0", MotorType = "Synchronous",
                    RatedPowerKW = 0.8, RatedVoltageV = 400, RatedCurrentA = 2.1,
                    RatedSpeedRpm = 3000, MaxSpeedRpm = 6000,
                    RatedTorqueNm = 2.6, MaxTorqueNm = 9.0,
                    PolePairs = 3, InertiaKgCm2 = 0.29,
                    EncoderType = "Incremental", EncoderResolution = 2048
                },
                ["1FK7042"] = new ServoDriveService.MotorConfig
                {
                    MotorOrderNumber = "1FK7042-5AK71-1AH0", MotorType = "Synchronous",
                    RatedPowerKW = 1.3, RatedVoltageV = 400, RatedCurrentA = 3.4,
                    RatedSpeedRpm = 3000, MaxSpeedRpm = 6000,
                    RatedTorqueNm = 4.2, MaxTorqueNm = 14.5,
                    PolePairs = 3, InertiaKgCm2 = 0.61,
                    EncoderType = "Incremental", EncoderResolution = 2048
                },
                // 1FL6 series (V90 compatible)
                ["1FL6022"] = new ServoDriveService.MotorConfig
                {
                    MotorOrderNumber = "1FL6022-2AF21-1AG1", MotorType = "Synchronous",
                    RatedPowerKW = 0.05, RatedVoltageV = 220, RatedCurrentA = 0.6,
                    RatedSpeedRpm = 3000, MaxSpeedRpm = 5000,
                    RatedTorqueNm = 0.16, MaxTorqueNm = 0.5,
                    PolePairs = 4, EncoderType = "Incremental"
                },
                ["1FL6024"] = new ServoDriveService.MotorConfig
                {
                    MotorOrderNumber = "1FL6024-2AF21-1AG1", MotorType = "Synchronous",
                    RatedPowerKW = 0.1, RatedVoltageV = 220, RatedCurrentA = 1.0,
                    RatedSpeedRpm = 3000, MaxSpeedRpm = 5000,
                    RatedTorqueNm = 0.32, MaxTorqueNm = 1.0,
                    PolePairs = 4, EncoderType = "Incremental"
                },
                ["1FL6042"] = new ServoDriveService.MotorConfig
                {
                    MotorOrderNumber = "1FL6042-1AF61-2AB1", MotorType = "Synchronous",
                    RatedPowerKW = 0.4, RatedVoltageV = 400, RatedCurrentA = 1.4,
                    RatedSpeedRpm = 3000, MaxSpeedRpm = 5000,
                    RatedTorqueNm = 1.27, MaxTorqueNm = 3.8,
                    PolePairs = 4, EncoderType = "Absolute"
                },
                ["1FL6044"] = new ServoDriveService.MotorConfig
                {
                    MotorOrderNumber = "1FL6044-1AF61-2AB1", MotorType = "Synchronous",
                    RatedPowerKW = 0.75, RatedVoltageV = 400, RatedCurrentA = 2.6,
                    RatedSpeedRpm = 3000, MaxSpeedRpm = 5000,
                    RatedTorqueNm = 2.39, MaxTorqueNm = 7.2,
                    PolePairs = 4, EncoderType = "Absolute"
                },
                ["1FL6064"] = new ServoDriveService.MotorConfig
                {
                    MotorOrderNumber = "1FL6064-1AC61-2AB1", MotorType = "Synchronous",
                    RatedPowerKW = 1.5, RatedVoltageV = 400, RatedCurrentA = 5.3,
                    RatedSpeedRpm = 2000, MaxSpeedRpm = 3000,
                    RatedTorqueNm = 7.16, MaxTorqueNm = 21.5,
                    PolePairs = 4, EncoderType = "Absolute"
                },
            };

            var key = motorOrderNumber?.Substring(0, Math.Min(7, motorOrderNumber?.Length ?? 0)) ?? "";
            if (templates.TryGetValue(key, out var template))
            {
                return JsonHelper.ToJson(new { Success = true, MotorOrderNumber = motorOrderNumber, Template = template });
            }

            return JsonHelper.ToJson(new
            {
                Success = false,
                Message = $"No template for '{motorOrderNumber}'. Available prefixes: {string.Join(", ", templates.Keys)}"
            });
        }
    }
}
