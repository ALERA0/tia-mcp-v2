using ModelContextProtocol.Server;
using System;
using TiaMcpV2.Core;
using TiaMcpV2.Helpers;
using TiaMcpV2.Models;

namespace TiaMcpV2.Tools
{
using System.ComponentModel;
    [McpServerToolType]
    public static class DeviceTools
    {
        [McpServerTool(Name = "get_project_tree"), Description("Get the full project tree showing all devices and their hardware structure.")]
        public static string GetProjectTree()
        {
            try
            {
                var tree = ServiceAccessor.Hardware.GetProjectTree();
                return JsonHelper.ToJson(new ResponseProjectTree { Success = true, Tree = tree });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "get_devices"), Description("List all devices in the project (PLCs, HMIs, drives, ET200).")]
        public static string GetDevices()
        {
            try
            {
                ServiceAccessor.Portal.EnsureProjectOpen();
                var devices = new System.Collections.Generic.List<DeviceInfo>();
                foreach (var d in ServiceAccessor.Portal.GetDevices())
                {
                    devices.Add(new DeviceInfo { Name = d.Name, TypeIdentifier = d.TypeIdentifier });
                }
                return JsonHelper.ToJson(new ResponseDevices { Success = true, Devices = devices });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "get_device_info"), Description("Get detailed information about a device including all attributes.")]
        public static string GetDeviceInfo(string deviceName)
        {
            try
            {
                var device = ServiceAccessor.Portal.FindDevice(deviceName);
                if (device == null) return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = $"Device not found: {deviceName}" });
                return JsonHelper.ToJson(new ResponseDeviceInfo
                {
                    Success = true,
                    Name = device.Name,
                    TypeName = device.TypeIdentifier,
                    Attributes = AttributeHelper.GetAttributes(device)
                });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "get_device_item_info"), Description("Get detailed information about a device item (module/submodule). Path format: 'DeviceName/ItemName/SubItemName'.")]
        public static string GetDeviceItemInfo(string deviceItemPath)
        {
            try
            {
                var item = ServiceAccessor.Portal.FindDeviceItem(deviceItemPath);
                if (item == null) return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = $"Device item not found: {deviceItemPath}" });
                return JsonHelper.ToJson(new ResponseDeviceInfo
                {
                    Success = true,
                    Name = item.Name,
                    TypeName = item.TypeIdentifier,
                    Attributes = AttributeHelper.GetAttributes(item)
                });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }
    }
}
