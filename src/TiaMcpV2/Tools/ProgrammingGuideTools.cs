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
    public static class ProgrammingGuideTools
    {
        [McpServerTool(Name = "get_language_reference"), Description("Get reference for all PLC programming languages: LAD, FBD, STL, SCL, GRAPH, CFC. Returns when to use each, what to avoid, supported CPUs, and tips. Use this to choose the right language for a task.")]
        public static string GetLanguageReference()
        {
            try
            {
                var refs = ServiceAccessor.ProgrammingGuide.GetLanguageReference();
                return JsonHelper.ToJson(new { Success = true, Languages = refs });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "recommend_language"), Description("Recommend the best PLC programming language for an application. Examples: 'interlocks', 'motor control', 'pid loops', 'batch sequence', 'recipe management', 'continuous process', 's7-300 migration'. Returns recommended language with reason.")]
        public static string RecommendLanguage(string applicationType)
        {
            try
            {
                var rec = ServiceAccessor.ProgrammingGuide.GetLanguageRecommendation(applicationType);
                return JsonHelper.ToJson(new { Success = true, Application = applicationType, Recommendation = rec });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "get_ob_catalog"), Description("Get catalog of all Organization Block (OB) types: OB1 (main), OB100 (startup), OB10-17 (time-of-day), OB30-38 (cyclic interrupt), OB40-47 (hardware interrupt), OB80 (time error), OB82 (diagnostic), OB86 (rack failure), OB121/122 (errors). Returns triggers, priorities, and use cases for each.")]
        public static string GetObCatalog()
        {
            try
            {
                var obs = ServiceAccessor.ProgrammingGuide.GetObCatalog();
                return JsonHelper.ToJson(new { Success = true, OrganizationBlocks = obs });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "recommend_block_type"), Description("Recommend the right block type (FB, FC, GlobalDB, ARRAY DB, UDT) for a programming task. Provide a description of what the block should do. Examples: 'motor control with state', 'analog scaling', 'recipe storage', 'alarm buffer'.")]
        public static string RecommendBlockType(string description)
        {
            try
            {
                var rec = ServiceAccessor.ProgrammingGuide.RecommendBlockType(description);
                return JsonHelper.ToJson(new { Success = true, Recommendation = rec });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "validate_scl_code"), Description("Validate SCL code against world-class best practices: optimized access, naming conventions (FB_ prefix), REGION usage, comments, magic numbers, GOTO avoidance, IF nesting depth, TITLE/AUTHOR/VERSION metadata. Returns score 0-100 with detailed issues, warnings, and good points.")]
        public static string ValidateSclCode(string sclCode)
        {
            try
            {
                var result = ServiceAccessor.ProgrammingGuide.ValidateSclCode(sclCode);
                return JsonHelper.ToJson(new { Success = true, Validation = result });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "generate_graph_sequence"), Description(@"Generate a GRAPH-style sequential FB (step-transition state machine in SCL). Pass JSON describing steps:
{
  ""BlockName"": ""FB_FillProcess"",
  ""Title"": ""Fill Tank Sequence"",
  ""Steps"": [
    {""StepNumber"":1,""Name"":""OpenInletValve"",""Outputs"":[""oInletValve""],""TransitionCondition"":""iLevelOK"",""NextStep"":2,""TimeoutMs"":30000},
    {""StepNumber"":2,""Name"":""StartPump"",""Outputs"":[""oPumpRun""],""TransitionCondition"":""iFlowReached AND iLevelHigh"",""NextStep"":3,""TimeoutMs"":60000},
    {""StepNumber"":3,""Name"":""StopAndDrain"",""Outputs"":[""oDrainValve""],""TransitionCondition"":""iLevelLow"",""NextStep"":999}
  ]
}
Returns SCL FB with full state machine, hold/abort/reset, step supervision, fault handling. Ready for write_block.")]
        public static string GenerateGraphSequence(string requestJson)
        {
            try
            {
                var req = JsonSerializer.Deserialize<ProgrammingGuideService.GraphSequenceRequest>(requestJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (req == null)
                    return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = "Invalid JSON" });

                var code = ServiceAccessor.ProgrammingGuide.GenerateGraphSequence(req);
                return JsonHelper.ToJson(new { Success = true, BlockName = req.BlockName, SclCode = code });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "get_naming_conventions"), Description("Get the world-standard Siemens naming conventions for blocks, variables, tags, UDTs. Returns full prefix table (FB_, FC_, DB_, UDT_) and Hungarian-style variable prefixes (b, by, w, dw, i, di, r, lr, s, t).")]
        public static string GetNamingConventions()
        {
            return JsonHelper.ToJson(new
            {
                Success = true,
                BlockPrefixes = new[]
                {
                    new { Prefix = "FB_", Description = "Function Block (with state)", Example = "FB_MotorControl, FB_TempController" },
                    new { Prefix = "FC_", Description = "Function (stateless)", Example = "FC_ScaleAnalog, FC_ConvertCelsius" },
                    new { Prefix = "DB_", Description = "Global Data Block", Example = "DB_RecipeData, DB_SystemConfig" },
                    new { Prefix = "UDT_ or T_", Description = "User Defined Type", Example = "UDT_MotorData, T_AnalogSignal" },
                    new { Prefix = "OB_ or just OBxx", Description = "Organization Block", Example = "OB_CyclicInt_100ms, OB30" }
                },
                VariablePrefixes = new[]
                {
                    new { Prefix = "b", Type = "BOOL", Example = "bMotorRunning, bFault" },
                    new { Prefix = "by", Type = "BYTE", Example = "byStatusWord" },
                    new { Prefix = "w", Type = "WORD", Example = "wDiagnostic" },
                    new { Prefix = "dw", Type = "DWORD", Example = "dwCounter" },
                    new { Prefix = "i", Type = "INT", Example = "iSetpoint, iMode" },
                    new { Prefix = "di", Type = "DINT", Example = "diTotalCount" },
                    new { Prefix = "r", Type = "REAL", Example = "rTemperature, rFlow" },
                    new { Prefix = "lr", Type = "LREAL", Example = "lrPrecisionValue" },
                    new { Prefix = "s", Type = "STRING", Example = "sAlarmText" },
                    new { Prefix = "t", Type = "TIME", Example = "tDelay, tCycleTime" },
                    new { Prefix = "dt", Type = "DATE_AND_TIME", Example = "dtTimestamp" },
                    new { Prefix = "a", Type = "ARRAY", Example = "aRecipes" },
                    new { Prefix = "udt", Type = "UDT", Example = "udtMotor1" }
                },
                ParameterPrefixes = new[]
                {
                    new { Prefix = "i", Direction = "VAR_INPUT", Example = "iCmdStart, iSetpoint" },
                    new { Prefix = "o", Direction = "VAR_OUTPUT", Example = "oRunning, oFault" },
                    new { Prefix = "io", Direction = "VAR_IN_OUT", Example = "ioConfigData" },
                    new { Prefix = "_", Direction = "VAR (private/internal)", Example = "_state, _timer" },
                    new { Prefix = "_t_", Direction = "VAR_TEMP", Example = "_t_calc, _t_index" },
                    new { Prefix = "Glb_", Direction = "Global", Example = "Glb_SystemStatus" },
                    new { Prefix = "Cfg_", Direction = "Configuration", Example = "iCfg_MaxSpeed, _Cfg_Timeout" }
                },
                BooleanPrefixes = new[]
                {
                    new { Prefix = "Is", Use = "Indicate state", Example = "IsRunning, IsReady" },
                    new { Prefix = "Has", Use = "Existence/relationship", Example = "HasError, HasFault" },
                    new { Prefix = "Should", Use = "Expected behavior", Example = "ShouldStart" },
                    new { Prefix = "Can", Use = "Ability/permission", Example = "CanProceed, CanReset" }
                },
                StateMachine = new[]
                {
                    new { Pattern = "STATE_xxx", Use = "State machine constants", Example = "STATE_IDLE, STATE_RUNNING, STATE_FAULT" }
                }
            });
        }
    }
}
