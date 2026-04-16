using Microsoft.Extensions.Logging;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering;
using System;
using System.Collections.Generic;
using System.Linq;
using TiaMcpV2.Core;
using TiaMcpV2.Models;

namespace TiaMcpV2.Services
{
    /// <summary>
    /// V2 NEW SERVICE: I/O address planning, analysis, and tag table generation.
    /// </summary>
    public class IoAddressService
    {
        private readonly PortalEngine _portal;
        private readonly ILogger<IoAddressService>? _logger;

        public IoAddressService(PortalEngine portal, ILogger<IoAddressService>? logger = null)
        {
            _portal = portal;
            _logger = logger;
        }

        public List<IoAddressEntry> GetIoAddressPlan(string deviceName)
        {
            var device = _portal.FindDevice(deviceName);
            if (device == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Device not found: {deviceName}");

            var plan = new List<IoAddressEntry>();
            foreach (var item in device.DeviceItems)
            {
                CollectIoAddresses(item, plan);
            }

            return plan.OrderBy(e => e.StartByte).ToList();
        }

        private void CollectIoAddresses(DeviceItem item, List<IoAddressEntry> plan)
        {
            try
            {
                // Read I/O addresses via attributes
                if (!string.IsNullOrEmpty(item.TypeIdentifier))
                {
                    try
                    {
                        var inputAddr = item.GetAttribute("Input").ToString();
                        var outputAddr = item.GetAttribute("Output").ToString();

                        if (!string.IsNullOrEmpty(inputAddr) || !string.IsNullOrEmpty(outputAddr))
                        {
                            plan.Add(new IoAddressEntry
                            {
                                ModuleName = item.Name,
                                ModuleType = item.TypeIdentifier,
                                Slot = item.PositionNumber,
                                AddressRange = !string.IsNullOrEmpty(inputAddr) ? $"I: {inputAddr}" : $"Q: {outputAddr}",
                                Description = $"{item.Name} ({item.Classification})"
                            });
                        }
                    }
                    catch
                    {
                        // Module may not have I/O addresses
                        plan.Add(new IoAddressEntry
                        {
                            ModuleName = item.Name,
                            ModuleType = item.TypeIdentifier,
                            Slot = item.PositionNumber,
                            Description = $"{item.Name} ({item.Classification})"
                        });
                    }
                }
            }
            catch { }

            foreach (var sub in item.DeviceItems)
            {
                CollectIoAddresses(sub, plan);
            }
        }

        public Dictionary<string, object?> GetIoSummary(string deviceName)
        {
            var plan = GetIoAddressPlan(deviceName);

            int totalInputBytes = 0, totalOutputBytes = 0;
            int diModules = 0, doModules = 0, aiModules = 0, aoModules = 0;

            foreach (var entry in plan)
            {
                var type = entry.ModuleType?.ToUpperInvariant() ?? "";
                if (entry.AddressRange?.StartsWith("I") == true)
                {
                    totalInputBytes += entry.Length;
                    if (type.Contains("DI")) diModules++;
                    else if (type.Contains("AI")) aiModules++;
                }
                else
                {
                    totalOutputBytes += entry.Length;
                    if (type.Contains("DQ") || type.Contains("DO")) doModules++;
                    else if (type.Contains("AQ") || type.Contains("AO")) aoModules++;
                }
            }

            return new Dictionary<string, object?>
            {
                ["DeviceName"] = deviceName,
                ["TotalInputBytes"] = totalInputBytes,
                ["TotalOutputBytes"] = totalOutputBytes,
                ["DigitalInputModules"] = diModules,
                ["DigitalOutputModules"] = doModules,
                ["AnalogInputModules"] = aiModules,
                ["AnalogOutputModules"] = aoModules,
                ["TotalModules"] = plan.Count,
                ["AddressPlan"] = plan
            };
        }
    }
}
