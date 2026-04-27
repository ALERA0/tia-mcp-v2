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
    /// Distributed I/O setup service for ET 200 family:
    /// - ET 200SP (bit-modular)
    /// - ET 200MP (S7-1500 module compatible)
    /// - ET 200AL (IP65 field)
    /// - ET 200pro (heavy industry)
    /// - ET 200eco PN (compact field box)
    ///
    /// Handles: head module placement, PROFINET connection, MRP/MRPD redundancy,
    /// Shared Device, BaseUnit configuration, ServerModule placement.
    /// </summary>
    public class DistributedIoService
    {
        private readonly PortalEngine _portal;
        private readonly HardwareService _hardwareService;
        private readonly NetworkService _networkService;
        private readonly ModuleConfigService _moduleConfigService;
        private readonly ILogger<DistributedIoService>? _logger;

        public DistributedIoService(
            PortalEngine portal,
            HardwareService hardwareService,
            NetworkService networkService,
            ModuleConfigService moduleConfigService,
            ILogger<DistributedIoService>? logger = null)
        {
            _portal = portal;
            _hardwareService = hardwareService;
            _networkService = networkService;
            _moduleConfigService = moduleConfigService;
            _logger = logger;
        }

        /// <summary>
        /// One-shot ET 200 station setup:
        /// 1. Add head module (IM 155-x)
        /// 2. Connect to PROFINET subnet
        /// 3. Set IP address & PROFINET device name
        /// 4. Connect to PLC's IO system
        /// 5. Configure MRP role (if specified)
        /// 6. Plug all I/O modules in sequence
        /// 7. Add ServerModule at the end (for ET 200SP)
        /// 8. Configure module parameters
        /// </summary>
        public Dictionary<string, object?> SetupDistributedIoStation(DistributedIoSetupRequest req)
        {
            var steps = new List<string>();
            var warnings = new List<string>();

            // ─── Step 1: Add head module ───
            Device? station;
            try
            {
                station = _hardwareService.CreateDevice(req.HeadModuleTypeIdentifier, req.StationName, req.StationName);
                steps.Add($"✓ Created station: {req.StationName} ({req.HeadModuleTypeIdentifier})");
            }
            catch (Exception ex)
            {
                station = _portal.FindDevice(req.StationName);
                if (station == null)
                    throw new PortalException(PortalErrorCode.OperationFailed, $"Could not create station: {ex.Message}", ex);
                steps.Add($"• Station already exists: {req.StationName}");
            }

            var headModule = station.DeviceItems.FirstOrDefault();
            if (headModule == null)
                throw new PortalException(PortalErrorCode.NotFound, "Head module not found in station");

            var headPath = $"{station.Name}/{headModule.Name}";

            // ─── Step 2: Connect to subnet ───
            if (!string.IsNullOrEmpty(req.SubnetName))
            {
                try
                {
                    _networkService.ConnectToSubnet(headPath, req.SubnetName);
                    steps.Add($"✓ Connected to subnet: {req.SubnetName}");
                }
                catch (Exception ex)
                {
                    warnings.Add($"Subnet connect: {ex.Message}");
                }
            }

            // ─── Step 3: Set IP & PROFINET name ───
            if (!string.IsNullOrEmpty(req.IpAddress))
            {
                try
                {
                    _networkService.SetIpAddress(headPath, req.IpAddress,
                        req.SubnetMask ?? "255.255.255.0", req.RouterAddress ?? "");
                    steps.Add($"✓ Set IP: {req.IpAddress}");
                }
                catch (Exception ex) { warnings.Add($"IP set: {ex.Message}"); }
            }

            if (!string.IsNullOrEmpty(req.ProfinetDeviceName))
            {
                try
                {
                    _networkService.SetProfinetDeviceName(headPath, req.ProfinetDeviceName);
                    steps.Add($"✓ Set PROFINET name: {req.ProfinetDeviceName}");
                }
                catch (Exception ex) { warnings.Add($"PROFINET name: {ex.Message}"); }
            }

            // ─── Step 4: Connect to IO system ───
            if (!string.IsNullOrEmpty(req.IoSystemName))
            {
                try
                {
                    _networkService.ConnectToIoSystem(headPath, req.IoSystemName);
                    steps.Add($"✓ Connected to IO system: {req.IoSystemName}");
                }
                catch (Exception ex) { warnings.Add($"IO system connect: {ex.Message}"); }
            }

            // ─── Step 5: Configure MRP role ───
            if (!string.IsNullOrEmpty(req.MrpRole))
            {
                try
                {
                    _networkService.ConfigureMrp(headPath, req.MrpRole);
                    steps.Add($"✓ Configured MRP role: {req.MrpRole}");
                }
                catch (Exception ex) { warnings.Add($"MRP: {ex.Message}"); }
            }

            // ─── Step 6: Plug all I/O modules ───
            int slotIdx = 1; // ET 200SP modules start at slot 1
            if (req.Modules != null)
            {
                foreach (var moduleReq in req.Modules)
                {
                    try
                    {
                        var slot = moduleReq.Slot > 0 ? moduleReq.Slot : slotIdx;
                        _hardwareService.PlugModule(headPath, moduleReq.TypeIdentifier,
                            moduleReq.Name ?? $"Module_{slot}", slot);
                        steps.Add($"✓ Plugged {moduleReq.Name ?? "module"} at slot {slot}");
                        slotIdx = slot + 1;

                        // Apply parameters if provided
                        if (moduleReq.Parameters != null && moduleReq.Parameters.Count > 0)
                        {
                            try
                            {
                                var modulePath = $"{headPath}/{moduleReq.Name}";
                                var converted = moduleReq.Parameters.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
                                _moduleConfigService.ConfigureModule(modulePath, converted);
                                steps.Add($"  ✓ Applied {moduleReq.Parameters.Count} params");
                            }
                            catch (Exception exP) { warnings.Add($"Module {moduleReq.Name} params: {exP.Message}"); }
                        }
                    }
                    catch (Exception ex)
                    {
                        warnings.Add($"Module slot {slotIdx}: {ex.Message}");
                    }
                }
            }

            // ─── Step 7: Add ServerModule (ET 200SP) ───
            if (req.AddServerModule)
            {
                try
                {
                    _hardwareService.PlugModule(headPath,
                        "OrderNumber:6ES7 193-6PA00-0AA0", "Server module_1", slotIdx);
                    steps.Add($"✓ Added ServerModule at slot {slotIdx}");
                }
                catch (Exception ex)
                {
                    warnings.Add($"ServerModule: {ex.Message}");
                }
            }

            return new Dictionary<string, object?>
            {
                ["Success"] = true,
                ["StationName"] = req.StationName,
                ["StationPath"] = station.Name,
                ["HeadModulePath"] = headPath,
                ["StepsExecuted"] = steps,
                ["Warnings"] = warnings
            };
        }

        /// <summary>
        /// Configure Shared Device — multiple controllers can access the same ET 200 station.
        /// </summary>
        public void ConfigureSharedDevice(string stationPath, List<string> controllerPaths)
        {
            var station = _portal.FindDevice(stationPath);
            if (station == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Station not found: {stationPath}");

            var headModule = station.DeviceItems.FirstOrDefault();
            if (headModule == null)
                throw new PortalException(PortalErrorCode.NotFound, "Head module not found");

            try
            {
                headModule.SetAttribute("SharedDevice", true);
                _logger?.LogInformation("Enabled SharedDevice on {Path}", stationPath);
            }
            catch (Exception ex)
            {
                throw new PortalException(PortalErrorCode.OperationFailed,
                    $"Could not enable SharedDevice: {ex.Message}", ex);
            }

            // Note: Full Shared Device configuration (which controller owns which module)
            // requires per-module attribute setup that is hardware-dependent
        }

        /// <summary>
        /// Get all distributed I/O stations in the project with their connectivity status.
        /// </summary>
        public List<Dictionary<string, object?>> GetDistributedStations()
        {
            _portal.EnsureProjectOpen();
            var result = new List<Dictionary<string, object?>>();

            foreach (var device in _portal.GetDevices())
            {
                var typeId = device.TypeIdentifier ?? "";
                if (typeId.Contains("ET 200") || typeId.Contains("IM 155") || typeId.Contains("IM 157") ||
                    typeId.Contains("IM 154"))
                {
                    var info = new Dictionary<string, object?>
                    {
                        ["Name"] = device.Name,
                        ["TypeIdentifier"] = typeId,
                        ["ModuleCount"] = device.DeviceItems.FirstOrDefault()?.DeviceItems.Count ?? 0
                    };

                    // Try to read network info
                    try
                    {
                        var headModule = device.DeviceItems.FirstOrDefault();
                        if (headModule != null)
                        {
                            var ni = headModule.GetService<NetworkInterface>();
                            if (ni != null)
                            {
                                var node = ni.Nodes.FirstOrDefault();
                                if (node != null)
                                {
                                    try { info["IpAddress"] = node.GetAttribute("Address"); } catch { }
                                }
                                try { info["DeviceName"] = ni.GetAttribute("ProfinetDeviceName"); } catch { }
                                try { info["MrpRole"] = ni.GetAttribute("MrpRole"); } catch { }
                            }
                        }
                    }
                    catch { }

                    result.Add(info);
                }
            }

            return result;
        }

        #region Data Models

        public class DistributedIoSetupRequest
        {
            public string HeadModuleTypeIdentifier { get; set; } = ""; // e.g. "OrderNumber:6ES7 155-6AU01-0BN0"
            public string StationName { get; set; } = "";              // e.g. "ET200SP_Station1"
            public string? SubnetName { get; set; }
            public string? IpAddress { get; set; }
            public string? SubnetMask { get; set; }
            public string? RouterAddress { get; set; }
            public string? ProfinetDeviceName { get; set; }
            public string? IoSystemName { get; set; }
            public string? MrpRole { get; set; }                       // "Manager", "Client", "NotParticipant"
            public List<DistributedIoModule>? Modules { get; set; }
            public bool AddServerModule { get; set; } = true;
        }

        public class DistributedIoModule
        {
            public string TypeIdentifier { get; set; } = "";
            public string? Name { get; set; }
            public int Slot { get; set; } = 0;                         // 0 = auto-increment
            public Dictionary<string, string>? Parameters { get; set; }
        }

        #endregion
    }
}
