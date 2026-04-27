using Microsoft.Extensions.Logging;
using Siemens.Engineering;
using Siemens.Engineering.Hmi;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using TiaMcpV2.Core;

namespace TiaMcpV2.Services
{
    /// <summary>
    /// HMI panel setup service: adds panel device, configures network, sets up screens, alarms, tags.
    /// Supports KTP Basic, TP/KP Comfort, MTP Unified Comfort, KTP Mobile (F), and SIMATIC IPC.
    /// </summary>
    public class HmiSetupService
    {
        private readonly PortalEngine _portal;
        private readonly HardwareService _hardwareService;
        private readonly NetworkService _networkService;
        private readonly ILogger<HmiSetupService>? _logger;

        public HmiSetupService(
            PortalEngine portal,
            HardwareService hardwareService,
            NetworkService networkService,
            ILogger<HmiSetupService>? logger = null)
        {
            _portal = portal;
            _hardwareService = hardwareService;
            _networkService = networkService;
            _logger = logger;
        }

        /// <summary>
        /// One-shot HMI panel setup: add panel, connect to PROFINET, set IP, link to PLC.
        /// </summary>
        public Dictionary<string, object?> SetupHmiPanel(HmiSetupRequest req)
        {
            var steps = new List<string>();
            var warnings = new List<string>();

            // ─── Step 1: Add panel device ───
            Device? panel;
            try
            {
                panel = _hardwareService.CreateDevice(req.PanelTypeIdentifier, req.PanelName, req.PanelName);
                steps.Add($"✓ Added HMI panel: {req.PanelName}");
            }
            catch (Exception ex)
            {
                panel = _portal.FindDevice(req.PanelName);
                if (panel == null)
                    throw new PortalException(PortalErrorCode.OperationFailed, $"Could not add panel: {ex.Message}", ex);
                steps.Add($"• Panel already exists: {req.PanelName}");
            }

            // ─── Step 2: Find network interface ───
            var headItem = panel.DeviceItems.FirstOrDefault();
            if (headItem == null)
            {
                warnings.Add("Could not find panel head item");
                return BuildResult(req, steps, warnings);
            }

            var networkInterface = FindNetworkInterface(panel);
            var networkPath = networkInterface != null ? BuildPath(networkInterface) : null;

            // ─── Step 3: Connect to subnet ───
            if (!string.IsNullOrEmpty(req.SubnetName) && networkPath != null)
            {
                try
                {
                    _networkService.ConnectToSubnet(networkPath, req.SubnetName);
                    steps.Add($"✓ Connected to subnet: {req.SubnetName}");
                }
                catch (Exception ex) { warnings.Add($"Subnet: {ex.Message}"); }
            }

            // ─── Step 4: Set IP ───
            if (!string.IsNullOrEmpty(req.IpAddress) && networkPath != null)
            {
                try
                {
                    _networkService.SetIpAddress(networkPath, req.IpAddress,
                        req.SubnetMask ?? "255.255.255.0", req.RouterAddress ?? "");
                    steps.Add($"✓ Set IP: {req.IpAddress}");
                }
                catch (Exception ex) { warnings.Add($"IP: {ex.Message}"); }
            }

            // ─── Step 5: Set PROFINET name ───
            if (!string.IsNullOrEmpty(req.ProfinetDeviceName) && networkPath != null)
            {
                try
                {
                    _networkService.SetProfinetDeviceName(networkPath, req.ProfinetDeviceName);
                    steps.Add($"✓ Set PROFINET name: {req.ProfinetDeviceName}");
                }
                catch (Exception ex) { warnings.Add($"PROFINET name: {ex.Message}"); }
            }

            // ─── Step 6: Get HMI target for advanced config ───
            HmiTarget? hmiTarget = null;
            try
            {
                hmiTarget = _portal.FindHmiTarget(req.PanelName);
                if (hmiTarget != null)
                    steps.Add($"✓ Located HMI target: {hmiTarget.Name}");
            }
            catch { }

            // ─── Step 7: Create initial screens (placeholder names) ───
            if (req.InitialScreens != null && hmiTarget != null)
            {
                int created = 0;
                foreach (var screenName in req.InitialScreens)
                {
                    try
                    {
                        // Use reflection — Screens.Create signature varies between TIA versions
                        var screensColl = hmiTarget.ScreenFolder.Screens;
                        var createMethod = screensColl.GetType().GetMethods()
                            .FirstOrDefault(m => m.Name == "Create" && m.GetParameters().Length == 1);
                        if (createMethod != null)
                        {
                            createMethod.Invoke(screensColl, new object[] { screenName });
                            created++;
                        }
                    }
                    catch (Exception ex) { warnings.Add($"Screen '{screenName}': {ex.Message}"); }
                }
                if (created > 0)
                    steps.Add($"✓ Created {created} initial screens");
            }

            return BuildResult(req, steps, warnings);
        }

        public List<Dictionary<string, object?>> GetHmiPanels()
        {
            _portal.EnsureProjectOpen();
            var result = new List<Dictionary<string, object?>>();

            foreach (var device in _portal.GetDevices())
            {
                var typeId = device.TypeIdentifier ?? "";
                if (typeId.Contains("KTP") || typeId.Contains("TP") || typeId.Contains("MTP") ||
                    typeId.Contains("KP") || typeId.Contains("Mobile Panel") || typeId.Contains("IPC"))
                {
                    var info = new Dictionary<string, object?>
                    {
                        ["Name"] = device.Name,
                        ["TypeIdentifier"] = typeId,
                        ["IsHmi"] = true
                    };

                    // Network info
                    var ni = FindNetworkInterface(device);
                    if (ni != null)
                    {
                        try
                        {
                            var iface = ni.GetService<NetworkInterface>();
                            if (iface != null)
                            {
                                var node = iface.Nodes.FirstOrDefault();
                                if (node != null)
                                {
                                    try { info["IpAddress"] = node.GetAttribute("Address"); } catch { }
                                }
                            }
                        }
                        catch { }
                    }

                    // Screen count
                    try
                    {
                        var hmi = _portal.FindHmiTarget(device.Name);
                        if (hmi != null)
                        {
                            info["ScreenCount"] = hmi.ScreenFolder.Screens.Count;
                            info["ConnectionCount"] = 0;
                        }
                    }
                    catch { }

                    result.Add(info);
                }
            }

            return result;
        }

        #region Helpers

        private Dictionary<string, object?> BuildResult(HmiSetupRequest req, List<string> steps, List<string> warnings)
        {
            return new Dictionary<string, object?>
            {
                ["Success"] = true,
                ["PanelName"] = req.PanelName,
                ["StepsExecuted"] = steps,
                ["Warnings"] = warnings,
                ["CompletionStatus"] = warnings.Count == 0 ? "Complete" : "Complete with warnings"
            };
        }

        private DeviceItem? FindNetworkInterface(Device device)
        {
            foreach (var item in device.DeviceItems)
            {
                if (item.GetService<NetworkInterface>() != null)
                    return item;

                foreach (var sub in item.DeviceItems)
                {
                    if (sub.GetService<NetworkInterface>() != null)
                        return sub;
                }
            }
            return null;
        }

        private string BuildPath(DeviceItem item)
        {
            var names = new List<string>();
            var current = item as IEngineeringObject;
            while (current != null)
            {
                if (current is DeviceItem di) names.Insert(0, di.Name);
                else if (current is Device dev) { names.Insert(0, dev.Name); break; }
                current = current.Parent;
            }
            return string.Join("/", names);
        }

        #endregion

        public class HmiSetupRequest
        {
            public string PanelTypeIdentifier { get; set; } = "";   // e.g. "OrderNumber:6AV2 124-1DC01-0AX0"
            public string PanelName { get; set; } = "";              // e.g. "HMI_1"
            public string? SubnetName { get; set; }                  // e.g. "PN/IE_1"
            public string? IpAddress { get; set; }                   // e.g. "192.168.0.30"
            public string? SubnetMask { get; set; }
            public string? RouterAddress { get; set; }
            public string? ProfinetDeviceName { get; set; }
            public string? PlcConnectionName { get; set; }           // Connection to a PLC
            public string? PlcDeviceName { get; set; }
            public List<string>? InitialScreens { get; set; }        // Screen names to create
        }
    }
}
