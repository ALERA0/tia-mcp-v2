using Microsoft.Extensions.Logging;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using TiaMcpV2.Core;

namespace TiaMcpV2.Services
{
    /// <summary>
    /// Configures parameters on I/O modules and channels:
    /// - DI: Input delay, hardware/diagnostic interrupt
    /// - DQ: Reaction to CPU stop, substitute value, short-circuit detection
    /// - AI: Measurement type (Voltage/Current/RTD/TC), range, filter, smoothing, alarms
    /// - AQ: Output type, range, smoothing
    /// - Counter: Operating mode, signal type, evaluation, hysteresis
    /// - IO-Link: Port mode, baud rate, cycle time
    /// - F-DI/F-DQ: Discrepancy time, channel evaluation, short-circuit test
    /// </summary>
    public class ModuleConfigService
    {
        private readonly PortalEngine _portal;
        private readonly ILogger<ModuleConfigService>? _logger;

        public ModuleConfigService(PortalEngine portal, ILogger<ModuleConfigService>? logger = null)
        {
            _portal = portal;
            _logger = logger;
        }

        #region Generic Module Parameter Configuration

        /// <summary>
        /// Configure multiple parameters on a module at once.
        /// Each parameter can be a module-level attribute or a channel-level attribute (e.g. "Channel[0].MeasurementType").
        /// </summary>
        public Dictionary<string, object?> ConfigureModule(string deviceItemPath, Dictionary<string, object> parameters)
        {
            var item = _portal.FindDeviceItem(deviceItemPath);
            if (item == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Module not found: {deviceItemPath}");

            var applied = new List<string>();
            var failed = new Dictionary<string, string>();

            foreach (var kvp in parameters)
            {
                try
                {
                    if (kvp.Key.StartsWith("Channel[") || kvp.Key.Contains("/"))
                    {
                        // Channel-specific parameter — find the channel sub-item
                        ApplyChannelParameter(item, kvp.Key, kvp.Value);
                    }
                    else
                    {
                        // Module-level attribute
                        item.SetAttribute(kvp.Key, ConvertValue(kvp.Value));
                    }
                    applied.Add(kvp.Key);
                }
                catch (Exception ex)
                {
                    failed[kvp.Key] = ex.Message;
                }
            }

            _logger?.LogInformation("Configured module {Path}: {Applied} applied, {Failed} failed",
                deviceItemPath, applied.Count, failed.Count);

            return new Dictionary<string, object?>
            {
                ["Module"] = deviceItemPath,
                ["AppliedCount"] = applied.Count,
                ["FailedCount"] = failed.Count,
                ["Applied"] = applied,
                ["Failed"] = failed
            };
        }

        private void ApplyChannelParameter(DeviceItem module, string parameterPath, object value)
        {
            // Parse "Channel[0].MeasurementType" or "Ch0/MeasurementType"
            int channelIdx = -1;
            string attributeName = parameterPath;

            if (parameterPath.StartsWith("Channel["))
            {
                var endBracket = parameterPath.IndexOf(']');
                if (endBracket > 0)
                {
                    var idxStr = parameterPath.Substring(8, endBracket - 8);
                    int.TryParse(idxStr, out channelIdx);
                    attributeName = parameterPath.Substring(endBracket + 2); // Skip "].
                }
            }
            else if (parameterPath.Contains("/"))
            {
                var parts = parameterPath.Split('/');
                if (parts[0].StartsWith("Ch") && int.TryParse(parts[0].Substring(2), out var chIdx))
                {
                    channelIdx = chIdx;
                    attributeName = parts[1];
                }
            }

            if (channelIdx >= 0 && channelIdx < module.DeviceItems.Count)
            {
                module.DeviceItems.ElementAt(channelIdx).SetAttribute(attributeName, ConvertValue(value));
            }
            else
            {
                module.SetAttribute(attributeName, ConvertValue(value));
            }
        }

        private object ConvertValue(object value)
        {
            if (value == null) return null!;
            // JSON numbers come as JsonElement — convert
            if (value is System.Text.Json.JsonElement je)
            {
                switch (je.ValueKind)
                {
                    case System.Text.Json.JsonValueKind.Number:
                        if (je.TryGetInt32(out int i)) return i;
                        if (je.TryGetDouble(out double d)) return d;
                        return je.GetRawText();
                    case System.Text.Json.JsonValueKind.True: return true;
                    case System.Text.Json.JsonValueKind.False: return false;
                    case System.Text.Json.JsonValueKind.String: return je.GetString() ?? "";
                    default: return je.GetRawText();
                }
            }
            return value;
        }

        #endregion

        #region Specific Module Type Configurators

        /// <summary>
        /// Configure an Analog Input module: measurement type, range, filter, alarm limits.
        /// </summary>
        public void ConfigureAnalogInput(string moduleItemPath, AnalogInputConfig config)
        {
            var module = _portal.FindDeviceItem(moduleItemPath);
            if (module == null)
                throw new PortalException(PortalErrorCode.NotFound, $"AI module not found: {moduleItemPath}");

            // Apply per-channel
            int channelToConfig = config.ChannelIndex >= 0 ? config.ChannelIndex : 0;
            var channels = config.ChannelIndex >= 0
                ? new[] { module.DeviceItems.ElementAt(channelToConfig) }
                : module.DeviceItems.ToArray();

            foreach (var channel in channels)
            {
                TrySetAttribute(channel, "MeasurementType", config.MeasurementType);
                TrySetAttribute(channel, "MeasurementRange", config.MeasurementRange);
                TrySetAttribute(channel, "Filter", config.FilterLevel);
                TrySetAttribute(channel, "Smoothing", config.Smoothing);
                TrySetAttribute(channel, "WireBreakDetection", config.WireBreakDetection);
                TrySetAttribute(channel, "DiagnosticInterrupt", config.DiagnosticInterruptEnable);
                TrySetAttribute(channel, "HardwareInterrupt", config.HardwareInterruptEnable);
                TrySetAttribute(channel, "UpperLimit", config.UpperAlarmLimit);
                TrySetAttribute(channel, "LowerLimit", config.LowerAlarmLimit);
                TrySetAttribute(channel, "RTDType", config.RtdType);
                TrySetAttribute(channel, "TCType", config.ThermocoupleType);
                TrySetAttribute(channel, "ConnectionType", config.ConnectionType);
                TrySetAttribute(channel, "TemperatureCoefficient", config.TemperatureCoefficient);
            }

            _logger?.LogInformation("Configured AI module: {Path}", moduleItemPath);
        }

        /// <summary>
        /// Configure a Digital Input module: input delay, hardware/diagnostic interrupt.
        /// </summary>
        public void ConfigureDigitalInput(string moduleItemPath, DigitalInputConfig config)
        {
            var module = _portal.FindDeviceItem(moduleItemPath);
            if (module == null)
                throw new PortalException(PortalErrorCode.NotFound, $"DI module not found: {moduleItemPath}");

            TrySetAttribute(module, "InputDelay", config.InputDelay);
            TrySetAttribute(module, "DiagnosticInterrupt", config.DiagnosticInterruptEnable);
            TrySetAttribute(module, "HardwareInterrupt", config.HardwareInterruptEnable);
            TrySetAttribute(module, "NoSensorSupplyVoltage", config.NoSensorSupplyDiagnostic);
            TrySetAttribute(module, "WireBreak", config.WireBreakDiagnostic);

            _logger?.LogInformation("Configured DI module: {Path}", moduleItemPath);
        }

        /// <summary>
        /// Configure a Digital Output module: substitute value, reaction to CPU stop, diagnostics.
        /// </summary>
        public void ConfigureDigitalOutput(string moduleItemPath, DigitalOutputConfig config)
        {
            var module = _portal.FindDeviceItem(moduleItemPath);
            if (module == null)
                throw new PortalException(PortalErrorCode.NotFound, $"DQ module not found: {moduleItemPath}");

            TrySetAttribute(module, "DiagnosticInterrupt", config.DiagnosticInterruptEnable);
            TrySetAttribute(module, "ShortCircuit", config.ShortCircuitDiagnostic);
            TrySetAttribute(module, "WireBreak", config.WireBreakDiagnostic);
            TrySetAttribute(module, "ReactionToCpuStop", config.ReactionToCpuStop);
            TrySetAttribute(module, "SubstituteValue", config.SubstituteValue);

            _logger?.LogInformation("Configured DQ module: {Path}", moduleItemPath);
        }

        /// <summary>
        /// Configure a Counter (TM Count) module: operating mode, signal type, evaluation.
        /// </summary>
        public void ConfigureCounter(string moduleItemPath, CounterConfig config)
        {
            var module = _portal.FindDeviceItem(moduleItemPath);
            if (module == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Counter module not found: {moduleItemPath}");

            TrySetAttribute(module, "OperatingMode", config.OperatingMode);
            TrySetAttribute(module, "CountingDirection", config.CountingDirection);
            TrySetAttribute(module, "SignalType", config.SignalType);
            TrySetAttribute(module, "SignalEvaluation", config.SignalEvaluation);
            TrySetAttribute(module, "FilterFrequency", config.FilterFrequencyHz);
            TrySetAttribute(module, "HysteresisCount", config.Hysteresis);
            TrySetAttribute(module, "HardwareInterrupt", config.HardwareInterruptEnable);
            TrySetAttribute(module, "ReferencePoint", config.ReferencePoint);
            TrySetAttribute(module, "EncoderType", config.EncoderType);
            TrySetAttribute(module, "Resolution", config.EncoderResolution);

            _logger?.LogInformation("Configured Counter module: {Path}", moduleItemPath);
        }

        /// <summary>
        /// Configure an IO-Link Master port.
        /// </summary>
        public void ConfigureIoLinkPort(string moduleItemPath, int portIndex, IoLinkPortConfig config)
        {
            var module = _portal.FindDeviceItem(moduleItemPath);
            if (module == null)
                throw new PortalException(PortalErrorCode.NotFound, $"IO-Link module not found: {moduleItemPath}");

            DeviceItem port;
            try
            {
                port = module.DeviceItems.ElementAt(portIndex);
            }
            catch
            {
                throw new PortalException(PortalErrorCode.NotFound, $"IO-Link port {portIndex} not found on {moduleItemPath}");
            }

            TrySetAttribute(port, "PortMode", config.PortMode);
            TrySetAttribute(port, "BaudRate", config.BaudRate);
            TrySetAttribute(port, "CycleTime", config.CycleTimeUs);
            TrySetAttribute(port, "ProcessDataInputLength", config.InputLengthBytes);
            TrySetAttribute(port, "ProcessDataOutputLength", config.OutputLengthBytes);
            TrySetAttribute(port, "VendorID", config.VendorId);
            TrySetAttribute(port, "DeviceID", config.DeviceId);

            _logger?.LogInformation("Configured IO-Link port {Idx} on {Path}", portIndex, moduleItemPath);
        }

        /// <summary>
        /// Configure an F-DI/F-DQ safety module.
        /// </summary>
        public void ConfigureSafetyModule(string moduleItemPath, SafetyModuleConfig config)
        {
            var module = _portal.FindDeviceItem(moduleItemPath);
            if (module == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Safety module not found: {moduleItemPath}");

            TrySetAttribute(module, "FShortCircuitTest", config.ShortCircuitTestEnable);
            TrySetAttribute(module, "DiscrepancyTime", config.DiscrepancyTimeMs);
            TrySetAttribute(module, "ChannelEvaluation", config.ChannelEvaluation);
            TrySetAttribute(module, "FParameterSignature", config.ParameterSignature);
            TrySetAttribute(module, "ReactionTime", config.ReactionTimeMs);

            _logger?.LogInformation("Configured Safety module: {Path}", moduleItemPath);
        }

        #endregion

        #region Helper

        private void TrySetAttribute(IEngineeringObject obj, string name, object? value)
        {
            if (value == null) return;
            try
            {
                obj.SetAttribute(name, value);
            }
            catch (Exception ex)
            {
                _logger?.LogDebug("Skip attribute {Name}: {Err}", name, ex.Message);
            }
        }

        #endregion

        #region Configuration DTOs

        public class AnalogInputConfig
        {
            public int ChannelIndex { get; set; } = -1;          // -1 = all channels
            public string? MeasurementType { get; set; }          // "Voltage", "Current", "RTD", "Thermocouple", "Resistance"
            public string? MeasurementRange { get; set; }         // "+/-10V", "0..10V", "4..20mA", "0..20mA", "PT100", etc.
            public string? FilterLevel { get; set; }              // "None", "Weak", "Medium", "Strong"
            public string? Smoothing { get; set; }                // Same as filter
            public bool? WireBreakDetection { get; set; }
            public bool? DiagnosticInterruptEnable { get; set; }
            public bool? HardwareInterruptEnable { get; set; }
            public double? UpperAlarmLimit { get; set; }
            public double? LowerAlarmLimit { get; set; }
            public string? RtdType { get; set; }                  // "Pt100", "Pt1000", "Ni100", "Ni1000"
            public string? ThermocoupleType { get; set; }         // "K", "J", "T", "E", "N", "R", "S", "B"
            public string? ConnectionType { get; set; }           // "2-wire", "3-wire", "4-wire"
            public string? TemperatureCoefficient { get; set; }   // for RTD
        }

        public class DigitalInputConfig
        {
            public string? InputDelay { get; set; }              // "0.05ms", "0.1ms", "0.5ms", "3ms", "20ms"
            public bool? DiagnosticInterruptEnable { get; set; }
            public bool? HardwareInterruptEnable { get; set; }
            public bool? NoSensorSupplyDiagnostic { get; set; }
            public bool? WireBreakDiagnostic { get; set; }
        }

        public class DigitalOutputConfig
        {
            public bool? DiagnosticInterruptEnable { get; set; }
            public bool? ShortCircuitDiagnostic { get; set; }
            public bool? WireBreakDiagnostic { get; set; }
            public string? ReactionToCpuStop { get; set; }       // "TurnOff", "ApplySubstitute", "KeepLastValue"
            public bool? SubstituteValue { get; set; }
        }

        public class CounterConfig
        {
            public string? OperatingMode { get; set; }          // "Counting", "Measurement", "Position"
            public string? CountingDirection { get; set; }      // "Up", "Down", "Bidirectional"
            public string? SignalType { get; set; }             // "Pulse_24V", "Encoder_24V", "RS422", "SSI"
            public string? SignalEvaluation { get; set; }       // "Single", "Double", "Quadruple"
            public int? FilterFrequencyHz { get; set; }
            public int? Hysteresis { get; set; }
            public bool? HardwareInterruptEnable { get; set; }
            public string? ReferencePoint { get; set; }
            public string? EncoderType { get; set; }            // "Incremental", "Absolute", "SSI"
            public int? EncoderResolution { get; set; }
        }

        public class IoLinkPortConfig
        {
            public string? PortMode { get; set; }              // "Disabled", "DI", "DQ", "IO-Link Auto", "IO-Link Manual"
            public string? BaudRate { get; set; }              // "COM1", "COM2", "COM3"
            public int? CycleTimeUs { get; set; }
            public int? InputLengthBytes { get; set; }
            public int? OutputLengthBytes { get; set; }
            public string? VendorId { get; set; }
            public string? DeviceId { get; set; }
        }

        public class SafetyModuleConfig
        {
            public bool? ShortCircuitTestEnable { get; set; }
            public int? DiscrepancyTimeMs { get; set; }
            public string? ChannelEvaluation { get; set; }    // "1oo1", "1oo2"
            public string? ParameterSignature { get; set; }
            public int? ReactionTimeMs { get; set; }
        }

        #endregion
    }
}
