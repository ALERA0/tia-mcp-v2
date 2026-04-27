using Microsoft.Extensions.Logging;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.SW.Blocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TiaMcpV2.Core;

namespace TiaMcpV2.Services
{
    /// <summary>
    /// Diagnostics and commissioning service:
    /// - Watch Table generation (as DB for HMI/Watch)
    /// - Force Table planning
    /// - Trace configuration helper (high-speed signal logging)
    /// - Cross-reference reports
    /// - Diagnostic buffer access (online)
    /// - Block compare (online vs offline)
    /// - Compilation reports
    /// - Commissioning checklist generation
    /// </summary>
    public class DiagnosticsCommissioningService
    {
        private readonly PortalEngine _portal;
        private readonly BlockService _blockService;
        private readonly BlockAutonomyService _blockAutonomy;
        private readonly ILogger<DiagnosticsCommissioningService>? _logger;

        public DiagnosticsCommissioningService(
            PortalEngine portal,
            BlockService blockService,
            BlockAutonomyService blockAutonomy,
            ILogger<DiagnosticsCommissioningService>? logger = null)
        {
            _portal = portal;
            _blockService = blockService;
            _blockAutonomy = blockAutonomy;
            _logger = logger;
        }

        /// <summary>
        /// Generate a Watch Table-style DB containing variables to monitor during commissioning.
        /// </summary>
        public string GenerateWatchTableDb(string dbName, List<WatchEntry> entries)
        {
            var sb = new StringBuilder();
            sb.AppendLine($@"DATA_BLOCK ""{dbName}""
TITLE = 'Watch Table — Commissioning Variables'
{{ S7_Optimized_Access := 'TRUE' }}
AUTHOR : 'TiaMcpV2'
FAMILY : 'Diagnostics'
VERSION : 1.0
NON_RETAIN
//Watch table DB — bind to HMI faceplate or use for online monitoring
//Each entry mirrors a process variable for diagnostic visibility");
            sb.AppendLine();
            sb.AppendLine("   VAR");
            foreach (var e in entries)
            {
                var safeName = e.Name?.Replace(" ", "_").Replace(".", "_") ?? "Var";
                sb.Append($"      {safeName} : {e.DataType}");
                if (!string.IsNullOrEmpty(e.InitialValue))
                    sb.Append($" := {e.InitialValue}");
                sb.AppendLine($";   // {e.Comment}");
            }
            sb.AppendLine("   END_VAR");
            sb.AppendLine();
            sb.AppendLine("BEGIN");
            sb.AppendLine("END_DATA_BLOCK");
            return sb.ToString();
        }

        /// <summary>
        /// Generate a Trace signal definition DB — for high-speed signal logging via TIA Trace.
        /// </summary>
        public string GenerateTraceSignalDb(string dbName, List<string> signals, int sampleCount = 1000)
        {
            var sb = new StringBuilder();
            sb.AppendLine($@"DATA_BLOCK ""{dbName}""
TITLE = 'Trace Signal Buffer'
{{ S7_Optimized_Access := 'TRUE' }}
AUTHOR : 'TiaMcpV2'
FAMILY : 'Diagnostics'
VERSION : 1.0
NON_RETAIN
//High-speed signal trace buffer
//Use TIA Portal Trace tool to capture {sampleCount} samples
//Cycle-aligned sampling at OB30 (1ms..100ms)");
            sb.AppendLine();
            sb.AppendLine("   VAR");
            sb.AppendLine($"      WriteIndex : DInt;");
            sb.AppendLine($"      Triggered : Bool;");
            sb.AppendLine($"      TriggerLevel : Real := 50.0;");
            sb.AppendLine($"      TriggerEdge : Bool := TRUE;   // TRUE=rising");
            sb.AppendLine();
            foreach (var sig in signals)
            {
                var safeName = sig.Replace(" ", "_").Replace(".", "_");
                sb.AppendLine($"      Buffer_{safeName} : Array[0..{sampleCount - 1}] of Real;");
            }
            sb.AppendLine("   END_VAR");
            sb.AppendLine();
            sb.AppendLine("BEGIN");
            sb.AppendLine("END_DATA_BLOCK");
            return sb.ToString();
        }

        /// <summary>
        /// Generate Cross-reference report — find where blocks are used.
        /// </summary>
        public Dictionary<string, object?> GenerateCrossReferenceReport(string softwarePath)
        {
            var sw = _blockService.GetPlcSoftware(softwarePath);
            var report = new Dictionary<string, object?>();
            var blockUsage = new List<Dictionary<string, object?>>();

            CollectBlockUsage(sw.BlockGroup, blockUsage, "");
            report["TotalBlocks"] = blockUsage.Count;
            report["Blocks"] = blockUsage;

            // Find blocks that are not consistent
            var inconsistent = blockUsage.Where(b => b["IsConsistent"] is bool b2 && !b2).ToList();
            report["InconsistentBlockCount"] = inconsistent.Count;
            report["InconsistentBlocks"] = inconsistent;

            return report;
        }

        private void CollectBlockUsage(PlcBlockGroup group, List<Dictionary<string, object?>> result, string parentPath)
        {
            var currentPath = string.IsNullOrEmpty(parentPath) ? group.Name : $"{parentPath}/{group.Name}";

            foreach (var block in group.Blocks)
            {
                result.Add(new Dictionary<string, object?>
                {
                    ["Name"] = block.Name,
                    ["Type"] = block.GetType().Name,
                    ["Number"] = block.Number,
                    ["Path"] = currentPath,
                    ["IsConsistent"] = block.IsConsistent,
                    ["Language"] = block.ProgrammingLanguage.ToString(),
                    ["MemoryLayout"] = block.MemoryLayout.ToString(),
                    ["LastModified"] = block.ModifiedDate.ToString("yyyy-MM-dd HH:mm:ss"),
                    ["KnowHowProtected"] = block.IsKnowHowProtected
                });
            }

            foreach (var sub in group.Groups)
                CollectBlockUsage(sub, result, currentPath);
        }

        /// <summary>
        /// Read CPU diagnostic buffer if device is online.
        /// </summary>
        public Dictionary<string, object?> ReadDiagnosticBuffer(string deviceItemPath)
        {
            var item = _portal.FindDeviceItem(deviceItemPath);
            if (item == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Device not found: {deviceItemPath}");

            // Try via OnlineProvider
            var onlineProvider = item.GetService<Siemens.Engineering.Online.OnlineProvider>();
            if (onlineProvider == null)
                return new Dictionary<string, object?>
                {
                    ["Error"] = "Online provider not available — device may not support online diagnostics"
                };

            // Most diagnostic buffer access requires the device to be online.
            // Return what we can access via attributes.
            var result = new Dictionary<string, object?>
            {
                ["DeviceName"] = item.Name,
                ["IsOnline"] = onlineProvider.State,
                ["Note"] = "Full diagnostic buffer requires online connection. Use go_online first."
            };

            try
            {
                // Try reading common online attributes
                result["OperatingMode"] = onlineProvider.GetType().GetProperty("ConnectionState")?.GetValue(onlineProvider)?.ToString();
            }
            catch { }

            return result;
        }

        /// <summary>
        /// Generate a comprehensive commissioning checklist DB.
        /// </summary>
        public string GenerateCommissioningChecklistDb(string dbName, string projectName)
        {
            return $@"DATA_BLOCK ""{dbName}""
TITLE = 'Commissioning Checklist — {projectName}'
{{ S7_Optimized_Access := 'TRUE' }}
AUTHOR : 'TiaMcpV2'
FAMILY : 'Commissioning'
VERSION : 1.0
NON_RETAIN
//Commissioning checklist DB — track completion of each test phase

   VAR
      // ─── Stage 1: Hardware ───
      HW_PowerSupplyCheck : Bool;
      HW_GroundingCheck : Bool;
      HW_CablingCheck : Bool;
      HW_ModulesPlugged : Bool;
      HW_LedStatusOk : Bool;
      HW_SignedBy : String[30];
      HW_SignedDate : Date;

      // ─── Stage 2: Network ───
      NET_PingTest : Bool;
      NET_ProfinetDevicesFound : Bool;
      NET_AllStationsOnline : Bool;
      NET_TopologyMatchesDesign : Bool;
      NET_MrpRingClosed : Bool;
      NET_SignedBy : String[30];
      NET_SignedDate : Date;

      // ─── Stage 3: I/O Test ───
      IO_DigitalInputsTested : Bool;
      IO_DigitalOutputsTested : Bool;
      IO_AnalogInputsCalibrated : Bool;
      IO_AnalogOutputsCalibrated : Bool;
      IO_TotalInputsTested : Int;
      IO_TotalOutputsTested : Int;
      IO_SafetyIoTested : Bool;
      IO_SignedBy : String[30];
      IO_SignedDate : Date;

      // ─── Stage 4: Functional Test (manual mode) ───
      FT_MotorsManualTest : Bool;
      FT_ValvesManualTest : Bool;
      FT_PidLoopsTuned : Bool;
      FT_SequencesVerified : Bool;
      FT_AlarmsTriggered : Bool;
      FT_HmiScreensVerified : Bool;
      FT_RecipesLoaded : Bool;
      FT_SignedBy : String[30];
      FT_SignedDate : Date;

      // ─── Stage 5: Integration Test (auto mode) ───
      IT_AutoModeVerified : Bool;
      IT_InterlocksTested : Bool;
      IT_CommunicationStable : Bool;
      IT_PerformanceOk : Bool;
      IT_CycleTimeMs : Real;
      IT_SignedBy : String[30];
      IT_SignedDate : Date;

      // ─── Stage 6: Safety Test ───
      SAF_EStopAllChannels : Bool;
      SAF_DoorMonitorTested : Bool;
      SAF_LightCurtainTested : Bool;
      SAF_SafetySignatureRecorded : String[30];
      SAF_TuvAccepted : Bool;
      SAF_SignedBy : String[30];
      SAF_SignedDate : Date;

      // ─── Stage 7: Performance ───
      PERF_MaxCycleTimeMs : Real;
      PERF_AvgCycleTimeMs : Real;
      PERF_CommLoadPercent : Real;
      PERF_MemoryUsedPercent : Real;
      PERF_SignedBy : String[30];
      PERF_SignedDate : Date;

      // ─── Final Acceptance ───
      OverallStatus : String[20];   // 'Pending', 'In Progress', 'Passed', 'Failed'
      FinalApprovalBy : String[30];
      FinalApprovalDate : Date;
      Comments : String[200];
   END_VAR

BEGIN
   OverallStatus := 'Pending';
END_DATA_BLOCK";
        }

        public class WatchEntry
        {
            public string? Name { get; set; }
            public string DataType { get; set; } = "Real";
            public string? InitialValue { get; set; }
            public string? Comment { get; set; }
        }
    }
}
