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

        [McpServerTool(Name = "get_devices"), Description("List all devices in the project (PLCs, HMIs, drives, ET200). Returns device names, types, and their DeviceItem names for use as softwarePath parameters. IMPORTANT: Use the device Name directly as softwarePath (e.g. 'PLC_1' or the full device name even if it contains '/' characters).")]
        public static string GetDevices()
        {
            try
            {
                ServiceAccessor.Portal.EnsureProjectOpen();
                var devices = new System.Collections.Generic.List<object>();
                foreach (var d in ServiceAccessor.Portal.GetDevices())
                {
                    var deviceItems = new System.Collections.Generic.List<string>();
                    foreach (var item in d.DeviceItems)
                    {
                        deviceItems.Add(item.Name);
                        foreach (var sub in item.DeviceItems)
                            deviceItems.Add($"  {sub.Name}");
                    }

                    // Check if this device has PLC software
                    string? plcSoftwarePath = null;
                    foreach (var item in d.DeviceItems)
                    {
                        var sw = item.GetService<Siemens.Engineering.HW.Features.SoftwareContainer>();
                        if (sw?.Software is Siemens.Engineering.SW.PlcSoftware)
                        {
                            plcSoftwarePath = item.Name;
                            break;
                        }
                        foreach (var sub in item.DeviceItems)
                        {
                            sw = sub.GetService<Siemens.Engineering.HW.Features.SoftwareContainer>();
                            if (sw?.Software is Siemens.Engineering.SW.PlcSoftware)
                            {
                                plcSoftwarePath = sub.Name;
                                break;
                            }
                        }
                    }

                    devices.Add(new
                    {
                        Name = d.Name,
                        TypeIdentifier = d.TypeIdentifier,
                        DeviceItems = deviceItems,
                        SoftwarePath = plcSoftwarePath,
                        Hint = plcSoftwarePath != null
                            ? $"Use '{plcSoftwarePath}' as softwarePath for block/tag/type operations"
                            : "No PLC software found on this device"
                    });
                }
                return JsonHelper.ToJson(new { Success = true, Devices = devices, Count = devices.Count });
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
