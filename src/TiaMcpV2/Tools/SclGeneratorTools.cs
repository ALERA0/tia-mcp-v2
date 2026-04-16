using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.Text.Json;
using TiaMcpV2.Core;
using TiaMcpV2.Helpers;
using TiaMcpV2.Models;

namespace TiaMcpV2.Tools
{
using System.ComponentModel;
    [McpServerToolType]
    public static class SclGeneratorTools
    {
        [McpServerTool(Name = "get_scl_templates"), Description("List all available SCL code generation templates (motor control, valve control, analog processing, PID, state machine, UDTs, alarm handler).")]
        public static string GetSclTemplates()
        {
            try
            {
                var templates = ServiceAccessor.SclGenerator.GetAvailableTemplates();
                return JsonHelper.ToJson(new ResponseSclTemplates { Success = true, Templates = templates, Message = $"{templates.Count} templates available" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "generate_scl"), Description("Generate SCL code from a template. Returns the SCL source code without importing. Templates: MotorDOL, MotorStarDelta, MotorVFD, ValveOnOff, ValveAnalog, AnalogInput, AnalogOutput, PIDControl, StateMachine, AlarmHandler, UDT_MotorData, UDT_ValveData, UDT_AnalogData, Sequence.")]
        public static string GenerateScl(
            string templateName,
            string blockName,
            [Description("Optional JSON parameters: {\"TransitionTime\":\"T#6S\",\"StateCount\":\"5\"}")] string parametersJson)
        {
            try
            {
                Dictionary<string, string>? parameters = null;
                if (!string.IsNullOrEmpty(parametersJson) && parametersJson != "{}")
                    parameters = JsonSerializer.Deserialize<Dictionary<string, string>>(parametersJson);

                var code = ServiceAccessor.SclGenerator.GenerateScl(templateName, blockName, parameters);
                return JsonHelper.ToJson(new ResponseSclGenerate { Success = true, SclCode = code, BlockName = blockName, TemplateName = templateName });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "generate_and_import_scl"), Description("Generate SCL code from a template AND import it directly into TIA Portal. Combines generate_scl + import_scl_source in one step.")]
        public static string GenerateAndImportScl(
            string softwarePath,
            string groupPath,
            string templateName,
            string blockName,
            string parametersJson)
        {
            try
            {
                Dictionary<string, string>? parameters = null;
                if (!string.IsNullOrEmpty(parametersJson) && parametersJson != "{}")
                    parameters = JsonSerializer.Deserialize<Dictionary<string, string>>(parametersJson);

                ServiceAccessor.SclGenerator.GenerateAndImport(softwarePath, groupPath, templateName, blockName, parameters);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Generated and imported {templateName} as '{blockName}'" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }
    }
}
