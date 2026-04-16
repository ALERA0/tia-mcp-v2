using ModelContextProtocol.Server;
using System;
using TiaMcpV2.Core;
using TiaMcpV2.Helpers;
using TiaMcpV2.Models;

namespace TiaMcpV2.Tools
{
using System.ComponentModel;
    [McpServerToolType]
    public static class DiagnosticsTools
    {
        [McpServerTool(Name = "get_memory_usage"), Description("Get memory usage analysis: block counts by type, tag counts, UDT counts.")]
        public static string GetMemoryUsage(string devicePath)
        {
            try
            {
                var usage = ServiceAccessor.Diagnostics.GetMemoryUsage(devicePath);
                return JsonHelper.ToJson(new { Success = true, MemoryUsage = usage });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "get_block_consistency_report"), Description("Get a report of all inconsistent blocks that need recompilation.")]
        public static string GetBlockConsistencyReport(string devicePath)
        {
            try
            {
                var report = ServiceAccessor.Diagnostics.GetBlockConsistencyReport(devicePath);
                return JsonHelper.ToJson(new { Success = true, InconsistentBlocks = report, Count = report.Count, Message = report.Count == 0 ? "All blocks are consistent" : $"{report.Count} inconsistent blocks found" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "get_device_diagnostics"), Description("Get hardware diagnostics for a device: modules, network interfaces, configuration status.")]
        public static string GetDeviceDiagnostics(string deviceName)
        {
            try
            {
                var diag = ServiceAccessor.Diagnostics.GetDeviceDiagnostics(deviceName);
                return JsonHelper.ToJson(new { Success = true, Diagnostics = diag });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }
    }
}
