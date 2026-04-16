using ModelContextProtocol.Server;
using System;
using TiaMcpV2.Core;
using TiaMcpV2.Helpers;
using TiaMcpV2.Models;

namespace TiaMcpV2.Tools
{
using System.ComponentModel;
    [McpServerToolType]
    public static class SafetyTools
    {
        [McpServerTool(Name = "safety_login"), Description("Login to F-CPU safety administration. Required before modifying safety (F-) blocks.")]
        public static string SafetyLogin(
            string softwarePath,
            string password)
        {
            try
            {
                ServiceAccessor.Safety.SafetyLogin(softwarePath, password);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = "Safety login successful" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "safety_logout"), Description("Logout from F-CPU safety administration.")]
        public static string SafetyLogout(string softwarePath)
        {
            try
            {
                ServiceAccessor.Safety.SafetyLogout(softwarePath);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = "Safety logout successful" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "get_safety_info"), Description("Get safety administration information (login status, safety signature).")]
        public static string GetSafetyInfo(string softwarePath)
        {
            try
            {
                var info = ServiceAccessor.Safety.GetSafetyInfo(softwarePath);
                return JsonHelper.ToJson(new ResponseSafetyInfo { Success = true, SafetyData = info });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }
    }
}
