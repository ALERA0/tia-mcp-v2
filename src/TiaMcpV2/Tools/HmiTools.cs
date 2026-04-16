using ModelContextProtocol.Server;
using System;
using TiaMcpV2.Core;
using TiaMcpV2.Helpers;
using TiaMcpV2.Models;

namespace TiaMcpV2.Tools
{
using System.ComponentModel;
    [McpServerToolType]
    public static class HmiTools
    {
        [McpServerTool(Name = "get_hmi_targets"), Description("Get all HMI targets (panels, WinCC RT) in the project.")]
        public static string GetHmiTargets()
        {
            try { return JsonHelper.ToJson(new ResponseHmiTargets { Success = true, HmiTargets = ServiceAccessor.Hmi.GetHmiTargets() }); }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "get_hmi_screens"), Description("Get all screens in an HMI target.")]
        public static string GetHmiScreens(string hmiPath)
        {
            try { return JsonHelper.ToJson(new ResponseHmiScreens { Success = true, Screens = ServiceAccessor.Hmi.GetHmiScreens(hmiPath) }); }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "create_hmi_connection"), Description("Create an HMI connection to a PLC (limited Openness support).")]
        public static string CreateHmiConnection(
            string hmiPath,
            string plcPath,
            string connectionName)
        {
            try
            {
                ServiceAccessor.Hmi.CreateHmiConnection(hmiPath, plcPath, connectionName);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Created HMI connection: {connectionName}" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }
    }
}
