using Microsoft.Extensions.Logging;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using TiaMcpV2.Core;
using TiaMcpV2.Helpers;
using TiaMcpV2.Models;

namespace TiaMcpV2.Services
{
    public class HardwareService
    {
        private readonly PortalEngine _portal;
        private readonly ILogger<HardwareService>? _logger;

        public HardwareService(PortalEngine portal, ILogger<HardwareService>? logger = null)
        {
            _portal = portal;
            _logger = logger;
        }

        public Device CreateDevice(string typeIdentifier, string deviceName, string deviceItemName)
        {
            _portal.EnsureProjectOpen();
            var device = _portal.Project!.Devices.CreateWithItem(typeIdentifier, deviceName, deviceItemName);
            _logger?.LogInformation("Created device: {Name} ({Type})", deviceName, typeIdentifier);
            return device;
        }

        public void DeleteDevice(string deviceName)
        {
            var device = _portal.FindDevice(deviceName);
            if (device == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Device not found: {deviceName}");

            device.Delete();
            _logger?.LogInformation("Deleted device: {Name}", deviceName);
        }

        public DeviceItem PlugModule(string deviceItemPath, string typeIdentifier, string name, int slot)
        {
            var parent = _portal.FindDeviceItem(deviceItemPath);
            if (parent == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Device item not found: {deviceItemPath}");

            var module = parent.DeviceItems.FirstOrDefault()?.GetService<GsdDeviceItem>();

            // Try plugging via HW utility
            var plugged = parent.DeviceItems.FirstOrDefault(di => di.PositionNumber == slot);
            if (plugged != null && plugged.Name != name)
                throw new PortalException(PortalErrorCode.InvalidState, $"Slot {slot} is already occupied by {plugged.Name}");

            var result = parent.PlugNew(typeIdentifier, name, slot);
            _logger?.LogInformation("Plugged module: {Name} at slot {Slot}", name, slot);
            return result;
        }

        public void UnplugModule(string deviceItemPath)
        {
            var item = _portal.FindDeviceItem(deviceItemPath);
            if (item == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Device item not found: {deviceItemPath}");

            item.Delete();
            _logger?.LogInformation("Unplugged module: {Path}", deviceItemPath);
        }

        public List<RackSlotInfo> GetRackSlots(string deviceItemPath)
        {
            var parent = _portal.FindDeviceItem(deviceItemPath);
            if (parent == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Device item not found: {deviceItemPath}");

            var slots = new List<RackSlotInfo>();
            foreach (var item in parent.DeviceItems)
            {
                slots.Add(new RackSlotInfo
                {
                    SlotNumber = item.PositionNumber,
                    ModuleName = item.Name,
                    TypeIdentifier = item.TypeIdentifier,
                    IsOccupied = !string.IsNullOrEmpty(item.TypeIdentifier)
                });
            }
            return slots;
        }

        public void SetDeviceAttribute(string deviceItemPath, string attributeName, object value)
        {
            var item = _portal.FindDeviceItem(deviceItemPath);
            if (item == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Device item not found: {deviceItemPath}");

            item.SetAttribute(attributeName, value);
            _logger?.LogInformation("Set attribute {Attr}={Val} on {Path}", attributeName, value, deviceItemPath);
        }

        public object GetDeviceAttribute(string deviceItemPath, string attributeName)
        {
            var item = _portal.FindDeviceItem(deviceItemPath);
            if (item == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Device item not found: {deviceItemPath}");

            return item.GetAttribute(attributeName);
        }

        public List<Dictionary<string, object?>> GetProjectTree()
        {
            _portal.EnsureProjectOpen();
            var tree = new List<Dictionary<string, object?>>();

            foreach (var device in _portal.GetDevices())
            {
                var deviceNode = new Dictionary<string, object?>
                {
                    ["Name"] = device.Name,
                    ["TypeIdentifier"] = device.TypeIdentifier,
                    ["Items"] = BuildDeviceItemTree(device.DeviceItems)
                };
                tree.Add(deviceNode);
            }

            return tree;
        }

        private List<Dictionary<string, object?>> BuildDeviceItemTree(DeviceItemComposition items)
        {
            var result = new List<Dictionary<string, object?>>();
            foreach (var item in items)
            {
                var node = new Dictionary<string, object?>
                {
                    ["Name"] = item.Name,
                    ["PositionNumber"] = item.PositionNumber,
                    ["Classification"] = item.Classification.ToString(),
                    ["TypeIdentifier"] = item.TypeIdentifier,
                    ["Items"] = BuildDeviceItemTree(item.DeviceItems)
                };
                result.Add(node);
            }
            return result;
        }
    }
}
