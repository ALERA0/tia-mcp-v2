using ModelContextProtocol.Server;
using System;
using TiaMcpV2.Core;
using TiaMcpV2.Helpers;
using TiaMcpV2.Models;

namespace TiaMcpV2.Tools
{
using System.ComponentModel;
    [McpServerToolType]
    public static class IoAddressTools
    {
        [McpServerTool(Name = "get_io_address_plan"), Description("Get the I/O address plan for a device showing all module addresses, byte ranges, and input/output mapping.")]
        public static string GetIoAddressPlan(string deviceName)
        {
            try
            {
                var plan = ServiceAccessor.IoAddress.GetIoAddressPlan(deviceName);
                return JsonHelper.ToJson(new ResponseIoAddressPlan { Success = true, AddressPlan = plan, Message = $"{plan.Count} I/O address entries" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "get_io_summary"), Description("Get I/O summary for a device: total input/output bytes, module counts by type (DI/DQ/AI/AQ).")]
        public static string GetIoSummary(string deviceName)
        {
            try
            {
                var summary = ServiceAccessor.IoAddress.GetIoSummary(deviceName);
                return JsonHelper.ToJson(new { Success = true, IoSummary = summary });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }
    }
}
