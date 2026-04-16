using Microsoft.Extensions.Logging;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.Online;
using Siemens.Engineering.SW;
using System;
using System.Collections.Generic;
using System.Linq;
using TiaMcpV2.Core;
using TiaMcpV2.Models;

namespace TiaMcpV2.Services
{
    /// <summary>
    /// V2 NEW SERVICE: Diagnostics, online monitoring, and system analysis.
    /// </summary>
    public class DiagnosticsService
    {
        private readonly PortalEngine _portal;
        private readonly BlockService _blockService;
        private readonly ILogger<DiagnosticsService>? _logger;

        public DiagnosticsService(PortalEngine portal, BlockService blockService, ILogger<DiagnosticsService>? logger = null)
        {
            _portal = portal;
            _blockService = blockService;
            _logger = logger;
        }

        public Dictionary<string, object?> GetSoftwareInfo(string devicePath)
        {
            var sw = _blockService.GetPlcSoftware(devicePath);
            var info = new Dictionary<string, object?>
            {
                ["Name"] = sw.Name,
                ["TypeName"] = sw.GetType().Name
            };

            // Collect attribute info
            try
            {
                foreach (var attr in sw.GetAttributeInfos())
                {
                    try
                    {
                        info[attr.Name] = sw.GetAttribute(attr.Name);
                    }
                    catch { }
                }
            }
            catch { }

            return info;
        }

        public Dictionary<string, object?> GetMemoryUsage(string devicePath)
        {
            var sw = _blockService.GetPlcSoftware(devicePath);
            var result = new Dictionary<string, object?>();

            // Count blocks by type
            int fbCount = 0, fcCount = 0, obCount = 0, dbCount = 0, totalBlocks = 0;
            CountBlocks(sw.BlockGroup, ref fbCount, ref fcCount, ref obCount, ref dbCount, ref totalBlocks);

            result["TotalBlocks"] = totalBlocks;
            result["FunctionBlocks"] = fbCount;
            result["Functions"] = fcCount;
            result["OrganizationBlocks"] = obCount;
            result["DataBlocks"] = dbCount;

            // Count types
            int typeCount = 0;
            CountTypes(sw.TypeGroup, ref typeCount);
            result["UserDefinedTypes"] = typeCount;

            // Count tags
            int tagCount = 0;
            foreach (var table in sw.TagTableGroup.TagTables)
                tagCount += table.Tags.Count;
            result["TotalTags"] = tagCount;
            result["TagTables"] = sw.TagTableGroup.TagTables.Count;

            return result;
        }

        private void CountBlocks(Siemens.Engineering.SW.Blocks.PlcBlockGroup group, ref int fb, ref int fc, ref int ob, ref int db, ref int total)
        {
            foreach (var block in group.Blocks)
            {
                total++;
                var typeName = block.GetType().Name;
                if (typeName.Contains("FB") || typeName.Contains("FunctionBlock")) fb++;
                else if (typeName.Contains("FC") || typeName.Contains("Function")) fc++;
                else if (typeName.Contains("OB") || typeName.Contains("Organization")) ob++;
                else if (typeName.Contains("DB") || typeName.Contains("Data")) db++;
            }

            foreach (var sub in group.Groups)
                CountBlocks(sub, ref fb, ref fc, ref ob, ref db, ref total);
        }

        private void CountTypes(Siemens.Engineering.SW.Types.PlcTypeGroup group, ref int count)
        {
            count += group.Types.Count;
            foreach (var sub in group.Groups)
                CountTypes(sub, ref count);
        }

        public List<Dictionary<string, object?>> GetBlockConsistencyReport(string devicePath)
        {
            var sw = _blockService.GetPlcSoftware(devicePath);
            var report = new List<Dictionary<string, object?>>();
            CollectInconsistentBlocks(sw.BlockGroup, report, "");
            return report;
        }

        private void CollectInconsistentBlocks(Siemens.Engineering.SW.Blocks.PlcBlockGroup group, List<Dictionary<string, object?>> report, string path)
        {
            var currentPath = string.IsNullOrEmpty(path) ? group.Name : $"{path}/{group.Name}";

            foreach (var block in group.Blocks)
            {
                if (!block.IsConsistent)
                {
                    report.Add(new Dictionary<string, object?>
                    {
                        ["Name"] = block.Name,
                        ["Path"] = currentPath,
                        ["Type"] = block.GetType().Name,
                        ["Number"] = block.Number,
                        ["IsConsistent"] = false,
                        ["ModifiedDate"] = block.ModifiedDate.ToString("yyyy-MM-dd HH:mm:ss")
                    });
                }
            }

            foreach (var sub in group.Groups)
                CollectInconsistentBlocks(sub, report, currentPath);
        }

        public Dictionary<string, object?> GetDeviceDiagnostics(string deviceName)
        {
            var device = _portal.FindDevice(deviceName);
            if (device == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Device not found: {deviceName}");

            var info = new Dictionary<string, object?>
            {
                ["DeviceName"] = device.Name,
                ["TypeIdentifier"] = device.TypeIdentifier
            };

            // Collect module info
            var modules = new List<Dictionary<string, object?>>();
            foreach (var item in device.DeviceItems)
            {
                CollectDeviceItemDiagnostics(item, modules);
            }
            info["Modules"] = modules;
            info["ModuleCount"] = modules.Count;

            return info;
        }

        private void CollectDeviceItemDiagnostics(DeviceItem item, List<Dictionary<string, object?>> modules)
        {
            if (!string.IsNullOrEmpty(item.TypeIdentifier))
            {
                var moduleInfo = new Dictionary<string, object?>
                {
                    ["Name"] = item.Name,
                    ["TypeIdentifier"] = item.TypeIdentifier,
                    ["PositionNumber"] = item.PositionNumber,
                    ["Classification"] = item.Classification.ToString()
                };

                // Check for network interface
                var ni = item.GetService<NetworkInterface>();
                if (ni != null)
                {
                    moduleInfo["HasNetworkInterface"] = true;
                    moduleInfo["InterfaceType"] = ni.InterfaceType.ToString();
                }

                modules.Add(moduleInfo);
            }

            foreach (var sub in item.DeviceItems)
                CollectDeviceItemDiagnostics(sub, modules);
        }
    }
}
