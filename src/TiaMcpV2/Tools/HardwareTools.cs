using ModelContextProtocol.Server;
using System;
using TiaMcpV2.Core;
using TiaMcpV2.Helpers;
using TiaMcpV2.Models;

namespace TiaMcpV2.Tools
{
using System.ComponentModel;
    [McpServerToolType]
    public static class HardwareTools
    {
        [McpServerTool(Name = "create_device"), Description("Create a new hardware device (PLC, HMI, drive, ET200SP). Use search_catalog to find the typeIdentifier. Format: 'OrderNumber:6ES7 XXX-XXXXX-XXXX/VX.X'.")]
        public static string CreateDevice(
            string typeIdentifier,
            string deviceName,
            string deviceItemName)
        {
            try
            {
                ServiceAccessor.Hardware.CreateDevice(typeIdentifier, deviceName, deviceItemName);
                return JsonHelper.ToJson(new ResponseCreateDevice { Success = true, DeviceName = deviceName, TypeIdentifier = typeIdentifier });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "delete_device"), Description("Delete a device from the project.")]
        public static string DeleteDevice(string deviceName)
        {
            try
            {
                ServiceAccessor.Hardware.DeleteDevice(deviceName);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Deleted device: {deviceName}" });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "plug_module"), Description("Insert a module into a rack slot. Use search_catalog to find the typeIdentifier.")]
        public static string PlugModule(
            string deviceItemPath,
            string typeIdentifier,
            string name,
            int slot)
        {
            try
            {
                ServiceAccessor.Hardware.PlugModule(deviceItemPath, typeIdentifier, name, slot);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Plugged {name} at slot {slot}" });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "unplug_module"), Description("Remove a module from a rack slot.")]
        public static string UnplugModule(string deviceItemPath)
        {
            try
            {
                ServiceAccessor.Hardware.UnplugModule(deviceItemPath);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Unplugged module: {deviceItemPath}" });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "get_rack_slots"), Description("List all slots on a rack with their status (occupied/empty).")]
        public static string GetRackSlots(string deviceItemPath)
        {
            try
            {
                var slots = ServiceAccessor.Hardware.GetRackSlots(deviceItemPath);
                return JsonHelper.ToJson(new ResponseRackSlots { Success = true, Slots = slots });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "set_device_attribute"), Description("Set an attribute on a device item.")]
        public static string SetDeviceAttribute(
            string deviceItemPath,
            string attributeName,
            string value)
        {
            try
            {
                ServiceAccessor.Hardware.SetDeviceAttribute(deviceItemPath, attributeName, value);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Set {attributeName} = {value}" });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "get_device_attribute"), Description("Get a device item attribute value.")]
        public static string GetDeviceAttribute(
            string deviceItemPath,
            string attributeName)
        {
            try
            {
                var value = ServiceAccessor.Hardware.GetDeviceAttribute(deviceItemPath, attributeName);
                return JsonHelper.ToJson(new ResponseGetAttribute { Success = true, AttributeName = attributeName, Value = value });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "search_catalog"), Description("Search hardware catalog for devices/modules by keyword. Returns typeIdentifiers for use with create_device and plug_module. Search by: CPU, DI, DQ, AI, AQ, HMI, F-CPU, ET200SP, CP, PS, or order number.")]
        public static string SearchCatalog(string query)
        {
            try
            {
                var results = ServiceAccessor.Catalog.Search(query);
                return JsonHelper.ToJson(new ResponseSearchCatalog { Success = true, Results = results, Message = $"Found {results.Count} results" });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }
    }
}
