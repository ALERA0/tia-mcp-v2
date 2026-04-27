using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using TiaMcpV2.Core;

namespace TiaMcpV2.Services
{
    /// <summary>
    /// Organization Block manager — generates SCL templates for ALL OB types
    /// with proper error handling, S7-1500 best practices, and recommended structure.
    /// Supports: OB1 (main), OB100 (startup), OB30-38 (cyclic), OB40-47 (hardware),
    /// OB80 (time error), OB82 (diagnostic), OB83 (pull/plug), OB86 (rack failure),
    /// OB121 (programming error), OB122 (I/O access error).
    /// </summary>
    public class ObManagerService
    {
        private readonly BlockAutonomyService _blockAutonomy;
        private readonly ILogger<ObManagerService>? _logger;

        public ObManagerService(BlockAutonomyService blockAutonomy, ILogger<ObManagerService>? logger = null)
        {
            _blockAutonomy = blockAutonomy;
            _logger = logger;
        }

        /// <summary>
        /// Generate an OB SCL template based on type. obType: 'Main', 'Startup', 'CyclicInterrupt',
        /// 'HardwareInterrupt', 'TimeError', 'Diagnostic', 'PullPlug', 'RackFailure',
        /// 'ProgrammingError', 'IoAccessError', 'TimeOfDay', 'TimeDelay'.
        /// </summary>
        public string GenerateObTemplate(string obType, string blockName, int obNumber = 0,
            int cycleTimeMs = 100, Dictionary<string, string>? options = null)
        {
            return obType?.ToUpperInvariant() switch
            {
                "MAIN" or "OB1" => GenerateMainOb(blockName),
                "STARTUP" or "OB100" => GenerateStartupOb(blockName),
                "CYCLICINTERRUPT" or "OB30" or "OB31" or "OB32" or "OB33" or "OB34"
                    or "OB35" or "OB36" or "OB37" or "OB38" => GenerateCyclicInterruptOb(blockName, cycleTimeMs),
                "HARDWAREINTERRUPT" or "OB40" or "OB41" or "OB42" or "OB43" or "OB44"
                    or "OB45" or "OB46" or "OB47" => GenerateHardwareInterruptOb(blockName),
                "TIMEERROR" or "OB80" => GenerateTimeErrorOb(blockName),
                "DIAGNOSTIC" or "OB82" => GenerateDiagnosticOb(blockName),
                "PULLPLUG" or "OB83" => GeneratePullPlugOb(blockName),
                "RACKFAILURE" or "OB86" => GenerateRackFailureOb(blockName),
                "PROGRAMMINGERROR" or "OB121" => GenerateProgrammingErrorOb(blockName),
                "IOACCESSERROR" or "OB122" => GenerateIoAccessErrorOb(blockName),
                "TIMEOFDAY" or "OB10" => GenerateTimeOfDayOb(blockName),
                "TIMEDELAY" or "OB20" => GenerateTimeDelayOb(blockName),
                _ => throw new ArgumentException($"Unknown OB type: {obType}")
            };
        }

        public void GenerateAndImport(string softwarePath, string groupPath, string obType,
            string blockName, int obNumber = 0, int cycleTimeMs = 100)
        {
            var code = GenerateObTemplate(obType, blockName, obNumber, cycleTimeMs);
            _blockAutonomy.WriteBlockFromScl(softwarePath, groupPath, code, blockName);
        }

        public List<Dictionary<string, string>> GetRecommendedObSet()
        {
            return new()
            {
                new() { ["Type"] = "Main",            ["Name"] = "Main",                  ["Number"] = "OB1",   ["Importance"] = "Required" },
                new() { ["Type"] = "Startup",         ["Name"] = "Startup",               ["Number"] = "OB100", ["Importance"] = "Recommended" },
                new() { ["Type"] = "CyclicInterrupt", ["Name"] = "Cyclic_100ms",          ["Number"] = "OB30",  ["Importance"] = "If using PID/motion" },
                new() { ["Type"] = "TimeError",       ["Name"] = "TimeError",             ["Number"] = "OB80",  ["Importance"] = "Recommended for production" },
                new() { ["Type"] = "Diagnostic",      ["Name"] = "DiagnosticInterrupt",   ["Number"] = "OB82",  ["Importance"] = "Recommended for production" },
                new() { ["Type"] = "RackFailure",     ["Name"] = "RackFailure",           ["Number"] = "OB86",  ["Importance"] = "Critical for distributed I/O" },
                new() { ["Type"] = "ProgrammingError",["Name"] = "ProgrammingError",      ["Number"] = "OB121", ["Importance"] = "Recommended" },
                new() { ["Type"] = "IoAccessError",   ["Name"] = "IoAccessError",         ["Number"] = "OB122", ["Importance"] = "REQUIRED to prevent CPU stop on I/O fault" }
            };
        }

        #region OB Templates

        private string GenerateMainOb(string name) => $@"ORGANIZATION_BLOCK ""{name}""
TITLE = ""Main Program""
{{ S7_Optimized_Access := 'TRUE' }}
AUTHOR : 'TiaMcpV2'
FAMILY : 'System'
VERSION : 1.0
//Main cyclic OB — runs continuously after startup
//Best practice: Keep OB1 lean; mainly call FBs/FCs

   VAR_TEMP
      _t_cycle : Real;
   END_VAR

BEGIN

REGION System tasks
    // System initialization, watchdog refresh, HMI sync
END_REGION

REGION Process logic
    // Call your standard library FBs here
    // Example:
    //   ""DB_Motor1""(...);
    //   ""DB_Valve1""(...);
END_REGION

REGION Communication
    // External communication tasks
END_REGION

END_ORGANIZATION_BLOCK";

        private string GenerateStartupOb(string name) => $@"ORGANIZATION_BLOCK ""{name}""
TITLE = ""Startup OB (Complete Restart)""
{{ S7_Optimized_Access := 'TRUE' }}
AUTHOR : 'TiaMcpV2'
FAMILY : 'System'
VERSION : 1.0
//Runs ONCE at warm/cold restart before OB1 begins cycling
//Use for: variable initialization, retentive checks, default values, comm reset

   VAR_INPUT
      LostRetentive : Bool;       // TRUE if retentive memory was lost
      LostRtcClock : Bool;         // TRUE if RTC clock was reset
   END_VAR

   VAR_TEMP
      _t_status : Word;
   END_VAR

BEGIN

REGION Retentive memory check
    IF LostRetentive THEN
        // Apply default values, set safe state, log event
    END_IF;
END_REGION

REGION System defaults
    // Initialize global counters, flags, mode selectors
    // Reset all alarms to safe state
END_REGION

REGION Communication initialization
    // Reset comm buffers, clear connection statuses
END_REGION

REGION HMI defaults
    // Set initial HMI state
END_REGION

END_ORGANIZATION_BLOCK";

        private string GenerateCyclicInterruptOb(string name, int cycleTimeMs) => $@"ORGANIZATION_BLOCK ""{name}""
TITLE = ""Cyclic Interrupt OB ({cycleTimeMs}ms)""
{{ S7_Optimized_Access := 'TRUE' }}
AUTHOR : 'TiaMcpV2'
FAMILY : 'System'
VERSION : 1.0
//Periodic OB called every {cycleTimeMs}ms — use for PID, motion, fast monitoring
//IMPORTANT: Code must finish BEFORE next call (else OB80 triggered)
//Best for: PID controllers (cycle = Ti/10), high-speed signal monitoring, motion

   VAR_INPUT
      InitialCall : Bool;          // TRUE on first call after startup
      EventCount : Int;            // Number of triggers since startup
   END_VAR

   VAR_TEMP
      _t_cycle_overrun : Bool;
   END_VAR

BEGIN

REGION First-call initialization
    IF InitialCall THEN
        // Initialize PID/filter states
    END_IF;
END_REGION

REGION PID controllers
    // Example:
    //   ""DB_PID_TempControl""(Setpoint:=#sp, Input:=#pv, Output=>#cv);
END_REGION

REGION Fast filters & monitoring
    // First-order filters, edge detection, alarms with low hysteresis
END_REGION

REGION Motion calls (if axis cycle time matches)
    // MC_ instructions that need deterministic timing
END_REGION

END_ORGANIZATION_BLOCK";

        private string GenerateHardwareInterruptOb(string name) => $@"ORGANIZATION_BLOCK ""{name}""
TITLE = ""Hardware Interrupt OB""
{{ S7_Optimized_Access := 'TRUE' }}
AUTHOR : 'TiaMcpV2'
FAMILY : 'System'
VERSION : 1.0
//Triggered by hardware event (DI rising/falling edge, counter, position)
//IMPORTANT: Keep code SHORT — high priority blocks main cycle
//Best for: emergency stop signals, fast event capture, critical interlocks

   VAR_INPUT
      LADDR : HW_IO;               // Hardware address that triggered
      USI : Word;                  // User structure identifier
      IChannel : USInt;            // Channel that triggered
      EventType : Byte;            // Event type
   END_VAR

   VAR_TEMP
      _t_capture_time : Time;
   END_VAR

BEGIN

REGION Critical event handling
    // KEEP SHORT — set flag for OB1/cyclic OB to process
    // Example:
    //   ""DB_Globals"".bEdgeDetected := TRUE;
    //   ""DB_Globals"".tEventTime := READ_LOC_T();
END_REGION

END_ORGANIZATION_BLOCK";

        private string GenerateTimeErrorOb(string name) => $@"ORGANIZATION_BLOCK ""{name}""
TITLE = ""Time Error OB (OB80)""
{{ S7_Optimized_Access := 'TRUE' }}
AUTHOR : 'TiaMcpV2'
FAMILY : 'System'
VERSION : 1.0
//Triggered when scan cycle time exceeded or time-of-day error
//Without this OB, time error → CPU goes STOP

   VAR_INPUT
      Fault_ID : Byte;             // Type of time fault
      Csg_OBnr : OB_ANY;           // OB that caused the fault
      Csg_Prio : UInt;             // Priority of faulting OB
   END_VAR

   VAR_TEMP
      _t_log_entry : Word;
   END_VAR

BEGIN

REGION Log time error
    // Log Fault_ID, Csg_OBnr, time of occurrence
    // Increment system error counter
END_REGION

REGION Continue execution (do not stop CPU)
    // Optionally signal HMI: ""DB_System"".bTimeError := TRUE;
END_REGION

END_ORGANIZATION_BLOCK";

        private string GenerateDiagnosticOb(string name) => $@"ORGANIZATION_BLOCK ""{name}""
TITLE = ""Diagnostic Interrupt OB (OB82)""
{{ S7_Optimized_Access := 'TRUE' }}
AUTHOR : 'TiaMcpV2'
FAMILY : 'System'
VERSION : 1.0
//Triggered when an I/O module reports a diagnostic event
//(wire break, short circuit, channel error, module loss, etc.)

   VAR_INPUT
      IO_State : Word;             // I/O state of affected module
      LADDR : HW_DEVICE;           // Logical address of module
      Channel : UInt;              // Channel that triggered (or 16#FFFF for module-level)
      MultiError : Bool;           // Multiple events pending
   END_VAR

BEGIN

REGION Diagnose error
    // Read module diagnostic data using GET_DIAG / GET_NAME
    // Determine error type: wire break, short circuit, supply voltage missing
END_REGION

REGION React to fault
    // Set safety flag for affected channel
    // Notify HMI: ""DB_Diagnostics"".aChannelFault[Channel] := TRUE;
    // Log to alarm history
END_REGION

END_ORGANIZATION_BLOCK";

        private string GeneratePullPlugOb(string name) => $@"ORGANIZATION_BLOCK ""{name}""
TITLE = ""Pull/Plug Module OB (OB83)""
{{ S7_Optimized_Access := 'TRUE' }}
AUTHOR : 'TiaMcpV2'
FAMILY : 'System'
VERSION : 1.0
//Triggered when a module is removed/inserted at runtime (hot-swap)

   VAR_INPUT
      LADDR : HW_DEVICE;           // Address of pulled/plugged module
      Event_Class : Byte;          // 16#38 = Pull, 16#39 = Plug
      Fault_ID : Byte;
   END_VAR

BEGIN

REGION Handle hot-swap
    IF Event_Class = 16#38 THEN
        // Module pulled — set fault flag, transition affected I/O to safe state
    ELSIF Event_Class = 16#39 THEN
        // Module plugged — re-init parameters, clear fault when configuration matches
    END_IF;
END_REGION

END_ORGANIZATION_BLOCK";

        private string GenerateRackFailureOb(string name) => $@"ORGANIZATION_BLOCK ""{name}""
TITLE = ""Rack/Station Failure OB (OB86)""
{{ S7_Optimized_Access := 'TRUE' }}
AUTHOR : 'TiaMcpV2'
FAMILY : 'System'
VERSION : 1.0
//Triggered when a DP slave or PROFINET IO device fails or returns
//CRITICAL for distributed I/O — handle station offline gracefully

   VAR_INPUT
      LADDR : HW_DEVICE;           // Logical address of failing station
      Event_Class : Byte;          // 16#38 = Failure, 16#39 = Return
      Fault_ID : Byte;
   END_VAR

BEGIN

REGION Station event handling
    IF Event_Class = 16#38 THEN
        // Station FAILED — transition affected outputs to substitute values
        // Set ""DB_Stations"".aStationOnline[N] := FALSE;
        // Notify HMI/upper level
    ELSIF Event_Class = 16#39 THEN
        // Station RETURNED — clear faults, restore normal operation
        // Set ""DB_Stations"".aStationOnline[N] := TRUE;
    END_IF;
END_REGION

END_ORGANIZATION_BLOCK";

        private string GenerateProgrammingErrorOb(string name) => $@"ORGANIZATION_BLOCK ""{name}""
TITLE = ""Programming Error OB (OB121)""
{{ S7_Optimized_Access := 'TRUE' }}
AUTHOR : 'TiaMcpV2'
FAMILY : 'System'
VERSION : 1.0
//Triggered by programming errors at runtime
//(division by zero, array out of bounds, conversion error)
//Without this OB, programming error → CPU STOP

   VAR_INPUT
      Fault_ID : Byte;             // Error type
      OB_Nr : OB_ANY;
      BlockType : Byte;
      ErrorBlock : Block_FB;       // FB/FC/OB that caused error
      ErrorBlock_Number : UInt;
      ErrorPriority : Byte;
   END_VAR

BEGIN

REGION Log error
    // Record Fault_ID, ErrorBlock for diagnosis
    // Set ""DB_Diagnostics"".bProgrammingError := TRUE;
END_REGION

// IMPORTANT: For S7-1500, prefer LOCAL error handling with GetError/GetErrorID
// inside individual blocks — this OB121 acts as a fallback safety net.

END_ORGANIZATION_BLOCK";

        private string GenerateIoAccessErrorOb(string name) => $@"ORGANIZATION_BLOCK ""{name}""
TITLE = ""I/O Access Error OB (OB122)""
{{ S7_Optimized_Access := 'TRUE' }}
AUTHOR : 'TiaMcpV2'
FAMILY : 'System'
VERSION : 1.0
//Triggered when accessing an inaccessible I/O point
//WITHOUT THIS OB, I/O access errors → CPU STOP. Always include in production!

   VAR_INPUT
      Fault_ID : Byte;
      MemoryArea : Byte;
      LADDR : HW_DEVICE;
      Channel : UInt;
      AccessMode : Byte;            // Read/Write
      ErrorBlock : Block_FB;
      ErrorBlock_Number : UInt;
   END_VAR

BEGIN

REGION I/O fault recovery
    // Module/channel inaccessible — likely station fault or pulled module
    // Set substitute value for that I/O channel
    // Log incident and notify HMI
    // Example:
    //   ""DB_Diagnostics"".aIoFault[Channel] := TRUE;
END_REGION

END_ORGANIZATION_BLOCK";

        private string GenerateTimeOfDayOb(string name) => $@"ORGANIZATION_BLOCK ""{name}""
TITLE = ""Time-of-Day Interrupt OB (OB10-17)""
{{ S7_Optimized_Access := 'TRUE' }}
AUTHOR : 'TiaMcpV2'
FAMILY : 'System'
VERSION : 1.0
//Triggered at specific date/time, configurable in CPU properties
//Use for: daily reports, scheduled maintenance, archiving

   VAR_INPUT
      ExecutionDateTime : DTL;     // When this OB was executed
   END_VAR

BEGIN
    // Daily/weekly/monthly scheduled task
END_ORGANIZATION_BLOCK";

        private string GenerateTimeDelayOb(string name) => $@"ORGANIZATION_BLOCK ""{name}""
TITLE = ""Time-Delay Interrupt OB (OB20-23)""
{{ S7_Optimized_Access := 'TRUE' }}
AUTHOR : 'TiaMcpV2'
FAMILY : 'System'
VERSION : 1.0
//Triggered once after delay set by SRT_DINT instruction

   VAR_INPUT
      Sign : Word;                 // User signature passed via SRT_DINT
   END_VAR

BEGIN
    // Delayed action without blocking main cycle
END_ORGANIZATION_BLOCK";

        #endregion
    }
}
