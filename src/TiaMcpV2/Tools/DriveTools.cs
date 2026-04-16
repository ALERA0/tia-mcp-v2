using ModelContextProtocol.Server;
using System;
using TiaMcpV2.Core;
using TiaMcpV2.Helpers;
using TiaMcpV2.Models;

namespace TiaMcpV2.Tools
{
using System.ComponentModel;
    [McpServerToolType]
    public static class DriveTools
    {
        [McpServerTool(Name = "get_drive_objects"), Description("Get drive objects (SINAMICS, MICROMASTER, G120, S120, S210) on a device.")]
        public static string GetDriveObjects(string deviceItemPath)
        {
            try { return JsonHelper.ToJson(new ResponseDriveObjects { Success = true, DriveObjects = ServiceAccessor.Drives.GetDriveObjects(deviceItemPath) }); }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "configure_telegram"), Description("Configure a standard telegram on a drive (e.g. 1, 20, 111, 352, 390).")]
        public static string ConfigureTelegram(
            string deviceItemPath,
            int telegramNumber)
        {
            try
            {
                ServiceAccessor.Drives.ConfigureTelegram(deviceItemPath, telegramNumber);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Configured telegram {telegramNumber}" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "get_technological_objects"), Description("Get all technological objects (motion control TO_, PID controllers PID_) in PLC software.")]
        public static string GetTechnologicalObjects(string softwarePath)
        {
            try { return JsonHelper.ToJson(new ResponseTechObjects { Success = true, TechnologicalObjects = ServiceAccessor.Drives.GetTechnologicalObjects(softwarePath) }); }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }
    }
}
