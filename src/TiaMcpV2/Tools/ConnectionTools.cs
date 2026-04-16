using ModelContextProtocol.Server;
using System;
using TiaMcpV2.Core;
using TiaMcpV2.Helpers;
using TiaMcpV2.Models;

namespace TiaMcpV2.Tools
{
using System.ComponentModel;
    [McpServerToolType]
    public static class ConnectionTools
    {
        [McpServerTool(Name = "connect"), Description("Connect to a running TIA Portal instance (V20/V21). Must be called before any other operation. TIA Portal must be running.")]
        public static string Connect()
        {
            try
            {
                ServiceAccessor.Portal.Connect();
                var state = ServiceAccessor.Portal.State;
                return JsonHelper.ToJson(new ResponseConnect
                {
                    Success = true,
                    Message = $"Connected to TIA Portal V{state.TiaVersion}",
                    SessionId = state.SessionId,
                    ProjectName = state.ProjectName,
                    TiaVersion = state.TiaVersion
                });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "disconnect"), Description("Disconnect from the TIA Portal instance.")]
        public static string Disconnect()
        {
            try
            {
                ServiceAccessor.Portal.Disconnect();
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = "Disconnected from TIA Portal" });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "get_state"), Description("Get the current connection state: connected status, open project name, TIA version.")]
        public static string GetState()
        {
            try
            {
                var state = ServiceAccessor.Portal.State;
                return JsonHelper.ToJson(new ResponseState
                {
                    Success = true,
                    IsConnected = state.IsConnected,
                    ProjectName = state.ProjectName,
                    SessionId = state.SessionId,
                    TiaVersion = state.TiaVersion
                });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }
    }
}
