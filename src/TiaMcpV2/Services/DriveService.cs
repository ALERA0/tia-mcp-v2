using Microsoft.Extensions.Logging;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;
using System;
using System.Collections.Generic;
using System.Linq;
using TiaMcpV2.Core;

namespace TiaMcpV2.Services
{
    public class DriveService
    {
        private readonly PortalEngine _portal;
        private readonly BlockService _blockService;
        private readonly ILogger<DriveService>? _logger;

        public DriveService(PortalEngine portal, BlockService blockService, ILogger<DriveService>? logger = null)
        {
            _portal = portal;
            _blockService = blockService;
            _logger = logger;
        }

        public List<Dictionary<string, object?>> GetDriveObjects(string deviceItemPath)
        {
            var item = _portal.FindDeviceItem(deviceItemPath);
            if (item == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Device item not found: {deviceItemPath}");

            var result = new List<Dictionary<string, object?>>();
            CollectDriveObjects(item, result);
            return result;
        }

        private void CollectDriveObjects(DeviceItem item, List<Dictionary<string, object?>> result)
        {
            try
            {
                var typeId = item.TypeIdentifier;
                if (!string.IsNullOrEmpty(typeId) && (
                    typeId.Contains("SINAMICS") ||
                    typeId.Contains("MICROMASTER") ||
                    typeId.Contains("G120") ||
                    typeId.Contains("S120") ||
                    typeId.Contains("S210") ||
                    typeId.Contains("S110")))
                {
                    result.Add(new Dictionary<string, object?>
                    {
                        ["Name"] = item.Name,
                        ["TypeIdentifier"] = typeId,
                        ["PositionNumber"] = item.PositionNumber,
                        ["Classification"] = item.Classification.ToString()
                    });
                }
            }
            catch { }

            foreach (var sub in item.DeviceItems)
            {
                CollectDriveObjects(sub, result);
            }
        }

        public void ConfigureTelegram(string deviceItemPath, int telegramNumber)
        {
            var item = _portal.FindDeviceItem(deviceItemPath);
            if (item == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Device item not found: {deviceItemPath}");

            try
            {
                item.SetAttribute("Telegram", telegramNumber);
                _logger?.LogInformation("Configured telegram {Telegram} on {Path}", telegramNumber, deviceItemPath);
            }
            catch (Exception ex)
            {
                throw new PortalException(PortalErrorCode.OperationFailed,
                    $"Failed to configure telegram on {deviceItemPath}: {ex.Message}", ex);
            }
        }

        public List<Dictionary<string, object?>> GetTechnologicalObjects(string softwarePath)
        {
            var sw = _blockService.GetPlcSoftware(softwarePath);
            var result = new List<Dictionary<string, object?>>();

            foreach (var block in sw.BlockGroup.Blocks)
            {
                if (block is InstanceDB instanceDb)
                {
                    try
                    {
                        var instanceOfName = instanceDb.InstanceOfName;
                        if (!string.IsNullOrEmpty(instanceOfName) && (
                            instanceOfName.Contains("TO_") ||
                            instanceOfName.Contains("MC_") ||
                            instanceOfName.Contains("PID_")))
                        {
                            result.Add(new Dictionary<string, object?>
                            {
                                ["Name"] = block.Name,
                                ["Number"] = block.Number,
                                ["InstanceOf"] = instanceOfName,
                                ["Type"] = "TechnologicalObject"
                            });
                        }
                    }
                    catch { }
                }
            }

            return result;
        }
    }
}
