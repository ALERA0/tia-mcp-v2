using ModelContextProtocol.Server;
using System;
using TiaMcpV2.Core;
using TiaMcpV2.Helpers;
using TiaMcpV2.Models;

namespace TiaMcpV2.Tools
{
using System.ComponentModel;
    [McpServerToolType]
    public static class CodeAnalysisTools
    {
        [McpServerTool(Name = "analyze_project"), Description("Analyze PLC project for code quality: naming conventions, consistency, tag organization. Returns a score out of 100 with detailed issues and suggestions.")]
        public static string AnalyzeProject(string devicePath)
        {
            try
            {
                var result = ServiceAccessor.CodeAnalysis.AnalyzeProject(devicePath);
                return JsonHelper.ToJson(new { Success = true, Analysis = result });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }
    }
}
