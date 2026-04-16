using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using TiaMcpV2.Core;
using TiaMcpV2.Helpers;
using TiaMcpV2.Models;

namespace TiaMcpV2.Tools
{
using System.ComponentModel;
    [McpServerToolType]
    public static class SoftwareTools
    {
        [McpServerTool(Name = "get_software_info"), Description("Get PLC software information for a device (programming language, version, attributes).")]
        public static string GetSoftwareInfo(string softwarePath)
        {
            try
            {
                var info = ServiceAccessor.Diagnostics.GetSoftwareInfo(softwarePath);
                return JsonHelper.ToJson(new { Success = true, Info = info });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "compile_software"), Description("Compile PLC software. Returns error count, warning count, and compilation messages.")]
        public static string CompileSoftware(string softwarePath)
        {
            try
            {
                var result = ServiceAccessor.Portal.CompileSoftware(softwarePath);
                var messages = new List<object>();
                int errors = 0, warnings = 0;

                foreach (var msg in result.Messages)
                {
                    var msgDict = new Dictionary<string, object>
                    {
                        ["Description"] = msg.Description,
                        ["DateTime"] = msg.DateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        ["Path"] = msg.Path
                    };

                    var msgState = msg.State.ToString();
                    if (msgState.Contains("Warning")) { warnings++; msgDict["Type"] = "Warning"; }
                    else if (msgState.Contains("Error")) { errors++; msgDict["Type"] = "Error"; }
                    else { msgDict["Type"] = "Info"; }

                    messages.Add(msgDict);
                }

                return JsonHelper.ToJson(new ResponseCompile
                {
                    Success = errors == 0,
                    ErrorCount = errors,
                    WarningCount = warnings,
                    Messages = messages,
                    Message = errors == 0 ? $"Compilation successful ({warnings} warnings)" : $"Compilation failed ({errors} errors, {warnings} warnings)"
                });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "get_software_tree"), Description("Get the full software tree (blocks, types, tag tables) for a PLC.")]
        public static string GetSoftwareTree(string softwarePath)
        {
            try
            {
                var blocks = ServiceAccessor.Blocks.GetBlocksWithHierarchy(softwarePath);
                var types = ServiceAccessor.Types.GetTypes(softwarePath);
                var tags = ServiceAccessor.Tags.GetTagTables(softwarePath);

                return JsonHelper.ToJson(new ResponseSoftwareTree
                {
                    Success = true,
                    Tree = new { Blocks = blocks, Types = types, TagTables = tags }
                });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }
    }
}
