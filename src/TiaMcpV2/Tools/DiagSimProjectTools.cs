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
    public static class DiagnosticsCommissioningTools
    {
        [McpServerTool(Name = "create_watch_table_db"), Description(@"Create a Watch Table-style DB containing variables to monitor during commissioning. Pass JSON array: [
  {""Name"":""rTemperature"",""DataType"":""Real"",""Comment"":""Tank 1 temperature""},
  {""Name"":""bMotor1_Run"",""DataType"":""Bool"",""Comment"":""Pump status""}
]")]
        public static string CreateWatchTableDb(string softwarePath, string groupPath, string dbName, string entriesJson)
        {
            try
            {
                var entries = JsonSerializer.Deserialize<List<DiagnosticsCommissioningService.WatchEntry>>(
                    entriesJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (entries == null || entries.Count == 0)
                    return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = "Empty or invalid entries" });

                var code = ServiceAccessor.DiagnosticsCommissioning.GenerateWatchTableDb(dbName, entries);
                ServiceAccessor.BlockAutonomy.WriteBlockFromScl(softwarePath, groupPath, code, dbName);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Created watch table DB: {dbName} ({entries.Count} entries)" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "create_trace_signal_db"), Description("Create a high-speed Trace signal buffer DB. Pass signals JSON array (e.g. [\"rPosition\",\"rVelocity\",\"rTorque\"]) and sample count. Use TIA Portal Trace tool to capture data into this DB at OB30 cycle.")]
        public static string CreateTraceSignalDb(string softwarePath, string groupPath, string dbName, string signalsJson, int sampleCount)
        {
            try
            {
                var signals = JsonSerializer.Deserialize<List<string>>(signalsJson) ?? new List<string>();
                var code = ServiceAccessor.DiagnosticsCommissioning.GenerateTraceSignalDb(
                    dbName, signals, sampleCount > 0 ? sampleCount : 1000);
                ServiceAccessor.BlockAutonomy.WriteBlockFromScl(softwarePath, groupPath, code, dbName);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Created trace DB: {dbName} ({signals.Count} signals × {sampleCount} samples)" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "generate_cross_reference_report"), Description("Generate a cross-reference report — list all blocks with their type, language, consistency status, last modified date. Useful for project audit and inconsistency detection.")]
        public static string GenerateCrossReferenceReport(string softwarePath)
        {
            try
            {
                var report = ServiceAccessor.DiagnosticsCommissioning.GenerateCrossReferenceReport(softwarePath);
                return JsonHelper.ToJson(new { Success = true, Report = report });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "read_diagnostic_buffer"), Description("Read CPU diagnostic buffer (online required). Returns operating mode, online state, recent events. Use go_online first.")]
        public static string ReadDiagnosticBuffer(string deviceItemPath)
        {
            try
            {
                var info = ServiceAccessor.DiagnosticsCommissioning.ReadDiagnosticBuffer(deviceItemPath);
                return JsonHelper.ToJson(new { Success = true, DiagnosticBuffer = info });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "create_commissioning_checklist"), Description("Create a comprehensive commissioning checklist DB with all SAT phases: Hardware, Network, I/O Test, Functional Test, Integration Test, Safety Test, Performance, Final Acceptance. Includes signature fields and completion tracking.")]
        public static string CreateCommissioningChecklist(string softwarePath, string groupPath, string dbName, string projectName)
        {
            try
            {
                var code = ServiceAccessor.DiagnosticsCommissioning.GenerateCommissioningChecklistDb(dbName, projectName);
                ServiceAccessor.BlockAutonomy.WriteBlockFromScl(softwarePath, groupPath, code, dbName);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Created commissioning checklist: {dbName}" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }
    }

    [McpServerToolType]
    public static class SimulationTools
    {
        [McpServerTool(Name = "get_simulation_tools"), Description("Get reference of all Siemens simulation tools: PLCSIM (logic only), PLCSIM Advanced (virtual CPU on PROFINET), SIMIT (process+mechanical), TIA Test Suite (unit testing). Returns features, setup, and use cases.")]
        public static string GetSimulationTools()
        {
            try
            {
                var tools = ServiceAccessor.Simulation.GetSimulationToolReference();
                return JsonHelper.ToJson(new { Success = true, Tools = tools });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "create_mock_data_db"), Description(@"Create a Mock Data DB for simulation — overrides real I/O during testing. Pass entries JSON array: [
  {""Name"":""rTempSim"",""DataType"":""Real"",""InitialValue"":""25.0""},
  {""Name"":""bSensorSim"",""DataType"":""Bool"",""InitialValue"":""TRUE""}
]")]
        public static string CreateMockDataDb(string softwarePath, string groupPath, string dbName, string entriesJson)
        {
            try
            {
                var entries = JsonSerializer.Deserialize<List<SimulationService.MockDataEntry>>(entriesJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (entries == null)
                    return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = "Invalid entries" });

                var code = ServiceAccessor.Simulation.GenerateMockDataDb(dbName, entries);
                ServiceAccessor.BlockAutonomy.WriteBlockFromScl(softwarePath, groupPath, code, dbName);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Created mock data DB: {dbName}" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "create_unit_test_fb"), Description(@"Create a unit test FB for a target block. Pass test cases JSON: [
  {""Name"":""Start motor at 50% speed"",""InputDescription"":""iCmdStart=TRUE, iSpeed=50"",""ExpectedOutputs"":""oRunning=TRUE""},
  {""Name"":""Stop motor"",""InputDescription"":""iCmdStop=TRUE"",""ExpectedOutputs"":""oRunning=FALSE""}
]")]
        public static string CreateUnitTestFb(string softwarePath, string groupPath, string testName, string targetBlockName, string testCasesJson)
        {
            try
            {
                var cases = JsonSerializer.Deserialize<List<SimulationService.TestCase>>(testCasesJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (cases == null)
                    return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = "Invalid test cases" });

                var code = ServiceAccessor.Simulation.GenerateUnitTestFb(testName, targetBlockName, cases);
                ServiceAccessor.BlockAutonomy.WriteBlockFromScl(softwarePath, groupPath, code, testName);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Created unit test FB: {testName} ({cases.Count} cases)" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }
    }

    [McpServerToolType]
    public static class ProjectManagementTools
    {
        [McpServerTool(Name = "get_library_concepts"), Description("Get reference for TIA Portal library concepts: Master Copy (snapshot), Library Type (versioned, v1.0.0/v1.0.1), Global Library (firm-wide), Project Library (per-project). Returns pros/cons and usage guidance.")]
        public static string GetLibraryConcepts()
        {
            try
            {
                var concepts = ServiceAccessor.ProjectManagement.GetLibraryConcepts();
                return JsonHelper.ToJson(new { Success = true, Concepts = concepts });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "promote_block_to_library_type"), Description("Promote a block to a Library Type with semantic versioning. Pass blockPath, version (e.g. '1.0.0'), and author. Library types lock instances and provide change tracking.")]
        public static string PromoteBlockToLibraryType(string softwarePath, string blockPath, string version, string author)
        {
            try
            {
                var result = ServiceAccessor.ProjectManagement.PromoteToLibraryType(softwarePath, blockPath, version, author);
                return JsonHelper.ToJson(new { Success = true, Result = result });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "create_project_version_db"), Description("Create a Project Version Information DB with project name, version, author, release date, and a 5-entry change log. HMI can read this for an 'About' screen.")]
        public static string CreateProjectVersionDb(string softwarePath, string groupPath, string dbName,
            string projectName, string version, string author, string description)
        {
            try
            {
                var code = ServiceAccessor.ProjectManagement.GenerateProjectVersionDb(dbName, projectName, version, author, description);
                ServiceAccessor.BlockAutonomy.WriteBlockFromScl(softwarePath, groupPath, code, dbName);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Created version info DB: {dbName} v{version}" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "get_multiuser_guide"), Description("Get TIA Portal Multiuser Engineering setup guide: server installation, project upload, local session check-out (.als), best practices for concurrent editing, conflict resolution.")]
        public static string GetMultiuserGuide()
        {
            try
            {
                var guide = ServiceAccessor.ProjectManagement.GetMultiuserGuide();
                return JsonHelper.ToJson(new { Success = true, Guide = guide });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "get_version_control_guide"), Description("Get Version Control Interface (VCI) / Git integration guide: workflow, branching strategy (main/develop/feature/hotfix), .gitignore template, recommended Git clients.")]
        public static string GetVersionControlGuide()
        {
            try
            {
                var guide = ServiceAccessor.ProjectManagement.GetVersionControlGuide();
                return JsonHelper.ToJson(new { Success = true, Guide = guide });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "get_openness_guide"), Description("Get TIA Portal Openness API guide: use cases (bulk tag, hardware automation, batch import/export, CI/CD), key classes, limitations.")]
        public static string GetOpennessGuide()
        {
            try
            {
                var guide = ServiceAccessor.ProjectManagement.GetOpennessGuide();
                return JsonHelper.ToJson(new { Success = true, Guide = guide });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "get_sivarc_guide"), Description("Get SiVArc (Siemens Visualization Architect) guide: configuration-driven auto-generation of HMI/PLC code from rules and plant model.")]
        public static string GetSiVArcGuide()
        {
            try
            {
                var guide = ServiceAccessor.ProjectManagement.GetSiVArcGuide();
                return JsonHelper.ToJson(new { Success = true, Guide = guide });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }
    }
}
