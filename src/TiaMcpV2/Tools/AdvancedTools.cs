using System.ComponentModel;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.Text.Json;
using TiaMcpV2.Core;
using TiaMcpV2.Helpers;
using TiaMcpV2.Models;
using TiaMcpV2.Services;

namespace TiaMcpV2.Tools
{
    [McpServerToolType]
    public static class TagManagementTools
    {
        [McpServerTool(Name = "setup_standard_tag_tables"), Description("Set up the complete standard tag table organization on a PLC: Inputs_DI, Inputs_AI, Outputs_DQ, Outputs_AQ, Memory_Flags, HMI_Interface, Constants, System_Tags, Communication, Safety. Pass includeOptional=true for full set.")]
        public static string SetupStandardTagTables(string softwarePath, bool includeOptional)
        {
            try
            {
                var result = ServiceAccessor.TagManagement.SetupStandardTagTables(softwarePath, includeOptional);
                return JsonHelper.ToJson(new { Success = true, Result = result });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "bulk_create_tags"), Description(@"Bulk-create tags from a JSON array. Auto-routes tags to correct tables based on address. JSON example:
[
  {""Name"":""bMotor1_Run"",""DataType"":""Bool"",""LogicalAddress"":""%I0.0""},
  {""Name"":""iAnalogIn1"",""DataType"":""Int"",""LogicalAddress"":""%IW100""},
  {""Name"":""bMotor1_Cmd"",""DataType"":""Bool"",""LogicalAddress"":""%Q0.0""},
  {""Name"":""rTemperature"",""DataType"":""Real"",""LogicalAddress"":""%MD200"",""TableName"":""Memory_Flags""}
]")]
        public static string BulkCreateTags(string softwarePath, string tagsJson)
        {
            try
            {
                var tags = JsonSerializer.Deserialize<List<TagManagementService.TagDefinition>>(tagsJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (tags == null)
                    return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = "Invalid JSON" });

                var result = ServiceAccessor.TagManagement.BulkCreateTags(softwarePath, tags);
                return JsonHelper.ToJson(new { Success = true, Result = result });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "get_tag_tables_info"), Description("Get info about all tag tables in a PLC: which are standard, custom, tag count per table, purpose.")]
        public static string GetTagTablesInfo(string softwarePath)
        {
            try
            {
                var info = ServiceAccessor.TagManagement.GetTagTablesInfo(softwarePath);
                return JsonHelper.ToJson(new { Success = true, Tables = info });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }
    }

    [McpServerToolType]
    public static class ObManagerTools
    {
        [McpServerTool(Name = "create_organization_block"), Description(@"Create an Organization Block (OB) with proper SCL template. obType: 'Main', 'Startup', 'CyclicInterrupt', 'HardwareInterrupt', 'TimeError', 'Diagnostic', 'PullPlug', 'RackFailure', 'ProgrammingError', 'IoAccessError', 'TimeOfDay', 'TimeDelay'. Includes proper input parameters, error handling, and best-practice structure. cycleTimeMs only for CyclicInterrupt.")]
        public static string CreateOrganizationBlock(
            string softwarePath,
            string groupPath,
            string obType,
            string blockName,
            int obNumber,
            int cycleTimeMs)
        {
            try
            {
                ServiceAccessor.ObManager.GenerateAndImport(softwarePath, groupPath, obType, blockName, obNumber, cycleTimeMs);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Created {obType} OB: {blockName}" });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "generate_ob_template"), Description("Generate an OB SCL template (without importing) for review. Same obType options as create_organization_block. Returns SCL code ready for write_block.")]
        public static string GenerateObTemplate(string obType, string blockName, int obNumber, int cycleTimeMs)
        {
            try
            {
                var code = ServiceAccessor.ObManager.GenerateObTemplate(obType, blockName, obNumber, cycleTimeMs);
                return JsonHelper.ToJson(new { Success = true, ObType = obType, BlockName = blockName, SclCode = code });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "get_recommended_ob_set"), Description("Get the recommended set of OBs for a production-quality PLC project. Returns Main, Startup, CyclicInterrupt, TimeError, Diagnostic, RackFailure, ProgrammingError, IoAccessError with names and importance levels.")]
        public static string GetRecommendedObSet()
        {
            try
            {
                var set = ServiceAccessor.ObManager.GetRecommendedObSet();
                return JsonHelper.ToJson(new { Success = true, RecommendedObs = set });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "setup_production_ob_structure"), Description("Create the COMPLETE production-grade OB structure on a PLC in one call: Main (OB1), Startup (OB100), Cyclic_100ms (OB30), TimeError (OB80), Diagnostic (OB82), RackFailure (OB86), ProgrammingError (OB121), IoAccessError (OB122). All with proper templates and error handling.")]
        public static string SetupProductionObStructure(string softwarePath, string groupPath)
        {
            var created = new List<string>();
            var failed = new List<string>();
            var obs = new[]
            {
                ("Main", "Main", 1),
                ("Startup", "Startup", 100),
                ("CyclicInterrupt", "Cyclic_100ms", 30),
                ("TimeError", "TimeError", 80),
                ("Diagnostic", "DiagnosticInterrupt", 82),
                ("RackFailure", "RackFailure", 86),
                ("ProgrammingError", "ProgrammingError", 121),
                ("IoAccessError", "IoAccessError", 122)
            };

            foreach (var (type, name, num) in obs)
            {
                try
                {
                    ServiceAccessor.ObManager.GenerateAndImport(softwarePath, groupPath, type, name, num, 100);
                    created.Add($"✓ {name} ({type})");
                }
                catch (Exception ex) { failed.Add($"✗ {name}: {ex.Message}"); }
            }

            return JsonHelper.ToJson(new
            {
                Success = true,
                CreatedCount = created.Count,
                FailedCount = failed.Count,
                Created = created,
                Failed = failed
            });
        }
    }

    [McpServerToolType]
    public static class MotionControlTools
    {
        [McpServerTool(Name = "create_motion_axis_fb"), Description(@"Create a complete motion control FB for a single positioning axis. Wraps MC_Power, MC_Reset, MC_Home, MC_MoveAbsolute, MC_MoveRelative, MC_MoveJog, MC_Halt, MC_Stop. JSON: {
  ""BlockName"": ""FB_AxisX_Control"",
  ""AxisName"": ""Axis_X"",
  ""AxisType"": ""TO_PositioningAxis"",
  ""IncludeJog"": true,
  ""IncludeHoming"": true,
  ""IncludeRelativeMove"": true
}")]
        public static string CreateMotionAxisFb(
            string softwarePath,
            string groupPath,
            string requestJson)
        {
            try
            {
                var req = JsonSerializer.Deserialize<MotionControlService.MotionAxisFbRequest>(requestJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (req == null)
                    return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = "Invalid JSON" });

                var code = ServiceAccessor.MotionControl.GenerateAxisControlFB(req);
                ServiceAccessor.BlockAutonomy.WriteBlockFromScl(softwarePath, groupPath, code, req.BlockName ?? "FB_AxisControl");
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Created motion FB: {req.BlockName}" });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "generate_motion_axis_fb_xml"), Description("Generate motion axis FB SCL code (without importing). Returns SCL code ready for review or import via write_block.")]
        public static string GenerateMotionAxisFbCode(string requestJson)
        {
            try
            {
                var req = JsonSerializer.Deserialize<MotionControlService.MotionAxisFbRequest>(requestJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (req == null)
                    return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = "Invalid JSON" });

                var code = ServiceAccessor.MotionControl.GenerateAxisControlFB(req);
                return JsonHelper.ToJson(new { Success = true, BlockName = req.BlockName, SclCode = code });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "create_sync_axis_fb"), Description("Create a synchronous axis FB for electronic gearing/camming. Wraps MC_GearIn, MC_GearInPos, MC_CamIn, MC_Phasing. Pass blockName, syncAxisType (e.g. 'TO_SynchronousAxis'), masterAxisType (e.g. 'TO_PositioningAxis').")]
        public static string CreateSyncAxisFb(
            string softwarePath,
            string groupPath,
            string blockName,
            string syncAxisType,
            string masterAxisType)
        {
            try
            {
                var code = ServiceAccessor.MotionControl.GenerateSyncAxisFB(blockName, syncAxisType, masterAxisType);
                ServiceAccessor.BlockAutonomy.WriteBlockFromScl(softwarePath, groupPath, code, blockName);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Created sync axis FB: {blockName}" });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "get_motion_instructions"), Description("Get reference of all MC_ motion instructions: MC_Power, MC_Reset, MC_Home, MC_MoveAbsolute, MC_MoveRelative, MC_MoveVelocity, MC_MoveJog, MC_Halt, MC_Stop, MC_GearIn, MC_CamIn, MC_Phasing, MC_TorqueLimiting, MC_ChangeDynamic, MC_MoveSuperImposed. Returns purpose, parameters, and tips.")]
        public static string GetMotionInstructions()
        {
            try
            {
                var refs = ServiceAccessor.MotionControl.GetMotionInstructionReference();
                return JsonHelper.ToJson(new { Success = true, Instructions = refs });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "get_motion_telegrams"), Description("Get PROFIdrive telegram reference for motion control. Returns telegram numbers (1, 3, 5, 7, 9, 105, 111, 352, 353) with descriptions, recommended use cases, and data lengths.")]
        public static string GetMotionTelegrams()
        {
            try
            {
                var telegrams = ServiceAccessor.MotionControl.GetTelegramReference();
                return JsonHelper.ToJson(new { Success = true, Telegrams = telegrams });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }
    }
}
