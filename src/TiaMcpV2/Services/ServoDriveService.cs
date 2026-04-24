using Microsoft.Extensions.Logging;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.TechnologicalObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using TiaMcpV2.Core;

namespace TiaMcpV2.Services
{
    /// <summary>
    /// Complete servo drive workflow:
    /// 1. Add SINAMICS drive to project (S210, V90, S120, G120)
    /// 2. Configure motor with specifications (power, speed, torque, encoder)
    /// 3. Configure PROFIdrive telegram
    /// 4. Connect drive to PROFINET / PLC IO system
    /// 5. Create TO (TO_PositioningAxis / TO_SpeedAxis / TO_SynchronousAxis)
    /// 6. Link TO to drive via ActorInterface
    /// 7. Link TO encoder via SensorInterface
    /// 8. Configure all axis parameters (limits, dynamics, homing, etc.)
    /// </summary>
    public class ServoDriveService
    {
        private readonly PortalEngine _portal;
        private readonly HardwareService _hardwareService;
        private readonly NetworkService _networkService;
        private readonly DriveService _driveService;
        private readonly TechnologyObjectService _toService;
        private readonly BlockService _blockService;
        private readonly ILogger<ServoDriveService>? _logger;

        public ServoDriveService(
            PortalEngine portal,
            HardwareService hardwareService,
            NetworkService networkService,
            DriveService driveService,
            TechnologyObjectService toService,
            BlockService blockService,
            ILogger<ServoDriveService>? logger = null)
        {
            _portal = portal;
            _hardwareService = hardwareService;
            _networkService = networkService;
            _driveService = driveService;
            _toService = toService;
            _blockService = blockService;
            _logger = logger;
        }

        #region Complete Drive + Axis Setup (One-Shot)

        /// <summary>
        /// Complete one-shot setup: Add drive → Configure motor → Create TO → Link hardware → Set parameters.
        /// This is the RECOMMENDED method for setting up a complete servo axis in one call.
        /// </summary>
        public Dictionary<string, object?> SetupCompleteServoAxis(ServoSetupRequest req)
        {
            var steps = new List<string>();
            var warnings = new List<string>();

            // ───────── Step 1: Add drive device ─────────
            Device? driveDevice = null;
            try
            {
                driveDevice = _hardwareService.CreateDevice(req.DriveTypeIdentifier, req.DriveName, req.DriveName);
                steps.Add($"✓ Added drive: {req.DriveName}");
            }
            catch (Exception ex)
            {
                // Drive may already exist
                driveDevice = _portal.FindDevice(req.DriveName);
                if (driveDevice == null)
                    throw new PortalException(PortalErrorCode.OperationFailed, $"Could not add drive: {ex.Message}", ex);
                steps.Add($"• Drive already exists: {req.DriveName}");
            }

            // Get network interface path once
            var networkInterfacePath = GetNetworkInterfacePath(driveDevice);

            // ───────── Step 2: Connect drive to subnet ─────────
            if (!string.IsNullOrEmpty(req.SubnetName))
            {
                try
                {
                    if (networkInterfacePath != null)
                    {
                        _networkService.ConnectToSubnet(networkInterfacePath, req.SubnetName);
                        steps.Add($"✓ Connected drive to subnet: {req.SubnetName}");
                    }
                    else
                    {
                        warnings.Add("Could not find drive network interface — connect to subnet manually");
                    }
                }
                catch (Exception ex)
                {
                    warnings.Add($"Subnet connection warning: {ex.Message}");
                }
            }

            // ───────── Step 3: Set IP address ─────────
            if (!string.IsNullOrEmpty(req.DriveIpAddress))
            {
                try
                {
                    if (networkInterfacePath != null)
                    {
                        _networkService.SetIpAddress(networkInterfacePath, req.DriveIpAddress,
                            req.DriveSubnetMask ?? "255.255.255.0",
                            req.DriveRouterAddress ?? "");
                        steps.Add($"✓ Set drive IP: {req.DriveIpAddress}");
                    }
                }
                catch (Exception ex)
                {
                    warnings.Add($"IP set warning: {ex.Message}");
                }
            }

            // ───────── Step 4: Set PROFINET device name ─────────
            if (!string.IsNullOrEmpty(req.ProfinetDeviceName))
            {
                try
                {
                    if (networkInterfacePath != null)
                    {
                        _networkService.SetProfinetDeviceName(networkInterfacePath, req.ProfinetDeviceName);
                        steps.Add($"✓ Set PROFINET name: {req.ProfinetDeviceName}");
                    }
                }
                catch (Exception ex)
                {
                    warnings.Add($"PROFINET name warning: {ex.Message}");
                }
            }

            // ───────── Step 5: Connect drive to IO system ─────────
            if (!string.IsNullOrEmpty(req.IoSystemName))
            {
                try
                {
                    if (networkInterfacePath != null)
                    {
                        _networkService.ConnectToIoSystem(networkInterfacePath, req.IoSystemName);
                        steps.Add($"✓ Connected drive to IO system: {req.IoSystemName}");
                    }
                }
                catch (Exception ex)
                {
                    warnings.Add($"IO system warning: {ex.Message}");
                }
            }

            // ───────── Step 6: Configure telegram ─────────
            if (req.TelegramNumber > 0)
            {
                try
                {
                    var driveItem = FindDriveObjectInDevice(driveDevice);
                    if (driveItem != null)
                    {
                        _driveService.ConfigureTelegram(BuildItemPath(driveItem), req.TelegramNumber);
                        steps.Add($"✓ Configured telegram {req.TelegramNumber}");
                    }
                    else
                    {
                        warnings.Add("Could not find drive object for telegram configuration");
                    }
                }
                catch (Exception ex)
                {
                    warnings.Add($"Telegram warning: {ex.Message}");
                }
            }

            // ───────── Step 7: Configure motor parameters ─────────
            if (req.MotorConfig != null)
            {
                try
                {
                    ApplyMotorConfig(driveDevice, req.MotorConfig);
                    steps.Add($"✓ Applied motor config: {req.MotorConfig.MotorOrderNumber ?? "custom"}");
                }
                catch (Exception ex)
                {
                    warnings.Add($"Motor config warning: {ex.Message}");
                }
            }

            // ───────── Step 8: Create TO on PLC ─────────
            TechnologicalInstanceDB? to = null;
            if (!string.IsNullOrEmpty(req.TargetPlcPath) && !string.IsNullOrEmpty(req.TOName))
            {
                try
                {
                    var toType = req.TOType ?? "TO_PositioningAxis";
                    var toVersion = req.TOVersion ?? DefaultTOVersion(toType);
                    to = _toService.CreateTechnologicalObject(req.TargetPlcPath, req.TOName, toType, toVersion);
                    steps.Add($"✓ Created TO: {req.TOName} ({toType} v{toVersion})");
                }
                catch (Exception ex)
                {
                    throw new PortalException(PortalErrorCode.OperationFailed,
                        $"TO creation failed: {ex.Message}", ex);
                }

                // ───────── Step 9: Link TO actor to drive ─────────
                try
                {
                    var driveItem = FindDriveObjectInDevice(driveDevice);
                    if (driveItem != null)
                    {
                        _toService.ConnectToHardware(req.TargetPlcPath, req.TOName,
                            BuildItemPath(driveItem), "actor");
                        steps.Add($"✓ Linked TO actor → drive");
                    }
                    else
                    {
                        warnings.Add("Could not find drive object — connect TO actor manually");
                    }
                }
                catch (Exception ex)
                {
                    warnings.Add($"TO actor link warning: {ex.Message}");
                }

                // ───────── Step 10: Link TO sensor (encoder from drive) ─────────
                try
                {
                    var driveItem = FindDriveObjectInDevice(driveDevice);
                    if (driveItem != null)
                    {
                        _toService.ConnectToHardware(req.TargetPlcPath, req.TOName,
                            BuildItemPath(driveItem), "sensor");
                        steps.Add($"✓ Linked TO sensor → drive encoder");
                    }
                }
                catch (Exception ex)
                {
                    warnings.Add($"TO sensor link warning: {ex.Message}");
                }

                // ───────── Step 11: Apply axis parameters ─────────
                if (req.AxisParameters != null && req.AxisParameters.Count > 0)
                {
                    int applied = 0;
                    foreach (var kvp in req.AxisParameters)
                    {
                        try
                        {
                            _toService.SetToParameter(req.TargetPlcPath, req.TOName, kvp.Key, kvp.Value);
                            applied++;
                        }
                        catch (Exception ex)
                        {
                            warnings.Add($"Parameter '{kvp.Key}': {ex.Message}");
                        }
                    }
                    steps.Add($"✓ Applied {applied}/{req.AxisParameters.Count} axis parameters");
                }
            }

            return new Dictionary<string, object?>
            {
                ["Success"] = true,
                ["DriveName"] = req.DriveName,
                ["TOName"] = req.TOName,
                ["StepsExecuted"] = steps,
                ["Warnings"] = warnings,
                ["CompletionStatus"] = warnings.Count == 0 ? "Complete" : "Complete with warnings"
            };
        }

        #endregion

        #region Motor Configuration

        public void ApplyMotorConfig(Device driveDevice, MotorConfig config)
        {
            // Find the motor sub-item or drive object
            var driveObj = FindDriveObjectInDevice(driveDevice);
            if (driveObj == null)
                throw new PortalException(PortalErrorCode.NotFound, "Drive object not found inside device");

            // Try to set motor attributes on the drive object
            var attributes = new Dictionary<string, object?>
            {
                ["RatedPower"] = config.RatedPowerKW,
                ["RatedVoltage"] = config.RatedVoltageV,
                ["RatedCurrent"] = config.RatedCurrentA,
                ["RatedSpeed"] = config.RatedSpeedRpm,
                ["MaxSpeed"] = config.MaxSpeedRpm,
                ["RatedTorque"] = config.RatedTorqueNm,
                ["MaxTorque"] = config.MaxTorqueNm,
                ["RatedFrequency"] = config.RatedFrequencyHz,
                ["MotorCosPhi"] = config.CosPhi,
                ["MotorMoment"] = config.InertiaKgCm2,
                ["PolePairs"] = config.PolePairs,
                ["MotorOrderNumber"] = config.MotorOrderNumber,
                ["MotorType"] = config.MotorType, // "Synchronous", "Asynchronous"
                ["EncoderResolution"] = config.EncoderResolution,
                ["EncoderType"] = config.EncoderType // "Incremental", "Absolute", "Resolver"
            };

            int applied = 0;
            foreach (var kvp in attributes)
            {
                if (kvp.Value == null) continue;
                try
                {
                    driveObj.SetAttribute(kvp.Key, kvp.Value);
                    applied++;
                }
                catch { /* attribute may not exist for this drive type */ }
            }

            _logger?.LogInformation("Applied {Count} motor attributes to {Drive}", applied, driveDevice.Name);
        }

        #endregion

        #region Helpers

        private string BuildItemPath(DeviceItem item)
        {
            // Walk up parent chain to build the full path
            var names = new List<string>();
            var current = item as IEngineeringObject;
            while (current != null)
            {
                if (current is DeviceItem di)
                    names.Insert(0, di.Name);
                else if (current is Device dev)
                {
                    names.Insert(0, dev.Name);
                    break;
                }
                current = current.Parent;
            }
            return string.Join("/", names);
        }

        private DeviceItem? FindNetworkInterface(Device device)
        {
            foreach (var item in device.DeviceItems)
            {
                if (item.GetService<NetworkInterface>() != null)
                    return item;

                // Check sub-items
                foreach (var sub in item.DeviceItems)
                {
                    if (sub.GetService<NetworkInterface>() != null)
                        return sub;
                }
            }
            return null;
        }

        private string? GetNetworkInterfacePath(Device device)
        {
            var ni = FindNetworkInterface(device);
            return ni != null ? BuildItemPath(ni) : null;
        }

        private DeviceItem? FindDriveObjectInDevice(Device device)
        {
            // SINAMICS drives have a "Drive" or "Servo control unit" sub-item
            foreach (var item in device.DeviceItems)
            {
                var typeId = item.TypeIdentifier?.ToUpperInvariant() ?? "";
                if (typeId.Contains("SINAMICS") || typeId.Contains("SERVO") ||
                    typeId.Contains("DRIVE") || typeId.Contains("MOTOR") ||
                    item.Name.ToUpperInvariant().Contains("DRIVE"))
                    return item;

                // Recursively search sub-items
                foreach (var sub in item.DeviceItems)
                {
                    var subType = sub.TypeIdentifier?.ToUpperInvariant() ?? "";
                    if (subType.Contains("SINAMICS") || subType.Contains("DRIVE") ||
                        subType.Contains("SERVO") || subType.Contains("CU "))
                        return sub;
                }
            }

            // Fallback: return the first device item
            return device.DeviceItems.FirstOrDefault();
        }

        private string DefaultTOVersion(string toType)
        {
            switch (toType.ToUpperInvariant())
            {
                case "TO_POSITIONINGAXIS":
                case "TO_SPEEDAXIS":
                case "TO_SYNCHRONOUSAXIS":
                case "TO_EXTERNALENCODER":
                    return "8.0"; // V20
                case "PID_COMPACT":
                case "PID_3STEP":
                case "PID_TEMP":
                    return "2.5";
                default:
                    return "1.0";
            }
        }

        #endregion

        #region Recommended Axis Parameter Presets

        /// <summary>
        /// Get recommended axis parameter presets based on application type.
        /// Returns a dictionary of parameter paths and values.
        /// </summary>
        public Dictionary<string, string> GetAxisParameterPreset(string applicationType, MotorConfig? motor = null)
        {
            var preset = new Dictionary<string, string>();
            var ratedSpeed = motor?.RatedSpeedRpm ?? 3000.0;
            var maxSpeed = motor?.MaxSpeedRpm ?? (ratedSpeed * 1.2);

            switch (applicationType?.ToLowerInvariant())
            {
                case "highdynamic":
                case "high_dynamic":
                case "servo_fast":
                    preset["DynamicDefaults.Velocity"] = (ratedSpeed * 0.8).ToString();
                    preset["DynamicDefaults.Acceleration"] = "5000";
                    preset["DynamicDefaults.Deceleration"] = "5000";
                    preset["DynamicDefaults.Jerk"] = "50000";
                    preset["DynamicLimits.MaxVelocity"] = maxSpeed.ToString();
                    preset["DynamicLimits.MaxAcceleration"] = "10000";
                    preset["DynamicLimits.MaxDeceleration"] = "10000";
                    preset["DynamicLimits.MaxJerk"] = "100000";
                    break;

                case "conveyor":
                case "standard":
                    preset["DynamicDefaults.Velocity"] = (ratedSpeed * 0.5).ToString();
                    preset["DynamicDefaults.Acceleration"] = "500";
                    preset["DynamicDefaults.Deceleration"] = "500";
                    preset["DynamicDefaults.Jerk"] = "5000";
                    preset["DynamicLimits.MaxVelocity"] = (ratedSpeed * 0.8).ToString();
                    preset["DynamicLimits.MaxAcceleration"] = "1000";
                    preset["DynamicLimits.MaxDeceleration"] = "1000";
                    preset["DynamicLimits.MaxJerk"] = "10000";
                    break;

                case "heavyload":
                case "heavy_load":
                case "crane":
                    preset["DynamicDefaults.Velocity"] = (ratedSpeed * 0.3).ToString();
                    preset["DynamicDefaults.Acceleration"] = "200";
                    preset["DynamicDefaults.Deceleration"] = "200";
                    preset["DynamicDefaults.Jerk"] = "2000";
                    preset["DynamicLimits.MaxVelocity"] = (ratedSpeed * 0.5).ToString();
                    preset["DynamicLimits.MaxAcceleration"] = "500";
                    preset["DynamicLimits.MaxDeceleration"] = "500";
                    preset["DynamicLimits.MaxJerk"] = "5000";
                    break;

                case "precision":
                case "cnc":
                case "positioning":
                    preset["DynamicDefaults.Velocity"] = (ratedSpeed * 0.6).ToString();
                    preset["DynamicDefaults.Acceleration"] = "2000";
                    preset["DynamicDefaults.Deceleration"] = "2000";
                    preset["DynamicDefaults.Jerk"] = "20000";
                    preset["DynamicLimits.MaxVelocity"] = (ratedSpeed * 0.9).ToString();
                    preset["DynamicLimits.MaxAcceleration"] = "5000";
                    preset["DynamicLimits.MaxDeceleration"] = "5000";
                    preset["DynamicLimits.MaxJerk"] = "50000";
                    preset["PositionLimits.SwLimit.MinPosition"] = "0";
                    preset["PositionLimits.SwLimit.MaxPosition"] = "1000";
                    break;

                default:
                    preset["DynamicDefaults.Velocity"] = (ratedSpeed * 0.5).ToString();
                    preset["DynamicDefaults.Acceleration"] = "1000";
                    preset["DynamicDefaults.Deceleration"] = "1000";
                    preset["DynamicDefaults.Jerk"] = "10000";
                    break;
            }

            return preset;
        }

        #endregion

        #region Data Models

        public class ServoSetupRequest
        {
            // Drive hardware
            public string DriveTypeIdentifier { get; set; } = "";     // e.g. "OrderNumber:6SL3 210-5HB10-4UF0"
            public string DriveName { get; set; } = "";                // e.g. "Drive_Servo1"
            public string? SubnetName { get; set; }                    // PROFINET subnet name
            public string? DriveIpAddress { get; set; }                // e.g. "192.168.0.5"
            public string? DriveSubnetMask { get; set; }
            public string? DriveRouterAddress { get; set; }
            public string? ProfinetDeviceName { get; set; }
            public string? IoSystemName { get; set; }
            public int TelegramNumber { get; set; } = 111;             // 1, 3, 5, 105, 111, 352, etc.

            // Motor
            public MotorConfig? MotorConfig { get; set; }

            // Technology Object
            public string? TargetPlcPath { get; set; }                 // e.g. "PLC_1"
            public string? TOName { get; set; }                        // e.g. "Axis_X"
            public string? TOType { get; set; } = "TO_PositioningAxis"; // TO_SpeedAxis, TO_SynchronousAxis, etc.
            public string? TOVersion { get; set; }                     // "8.0" for V20

            // Axis parameters (as dotted paths, e.g. "DynamicLimits.MaxVelocity")
            public Dictionary<string, string>? AxisParameters { get; set; }
        }

        public class MotorConfig
        {
            public string? MotorOrderNumber { get; set; }       // e.g. "1FK2104-5AG00-0MA0"
            public string? MotorType { get; set; }              // "Synchronous" / "Asynchronous" / "Permanent Magnet"
            public double? RatedPowerKW { get; set; }
            public double? RatedVoltageV { get; set; }
            public double? RatedCurrentA { get; set; }
            public double? RatedSpeedRpm { get; set; }
            public double? MaxSpeedRpm { get; set; }
            public double? RatedTorqueNm { get; set; }
            public double? MaxTorqueNm { get; set; }
            public double? RatedFrequencyHz { get; set; }
            public double? CosPhi { get; set; }
            public double? InertiaKgCm2 { get; set; }
            public int? PolePairs { get; set; }
            public int? EncoderResolution { get; set; }
            public string? EncoderType { get; set; }            // "Incremental" / "Absolute" / "Resolver"
        }

        #endregion
    }
}
