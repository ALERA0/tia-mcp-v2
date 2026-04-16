using ModelContextProtocol.Server;
using System;
using TiaMcpV2.Core;
using TiaMcpV2.Helpers;
using TiaMcpV2.Models;

namespace TiaMcpV2.Tools
{
using System.ComponentModel;
    [McpServerToolType]
    public static class DownloadTools
    {
        [McpServerTool(Name = "download_to_plc"), Description("Download hardware and software configuration to a PLC. WARNING: This will stop the PLC and transfer the program.")]
        public static string DownloadToPlc(string deviceItemPath)
        {
            try
            {
                ServiceAccessor.Download.DownloadToPlc(deviceItemPath);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = "Download completed" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "go_online"), Description("Go online with a PLC to establish a live connection for monitoring.")]
        public static string GoOnline(string deviceItemPath)
        {
            try
            {
                ServiceAccessor.Download.GoOnline(deviceItemPath);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = "Online connection established" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "go_offline"), Description("Go offline from a PLC device.")]
        public static string GoOffline(string deviceItemPath)
        {
            try
            {
                ServiceAccessor.Download.GoOffline(deviceItemPath);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = "Offline" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }
    }
}
