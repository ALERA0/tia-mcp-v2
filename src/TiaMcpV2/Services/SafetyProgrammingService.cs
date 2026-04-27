using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using TiaMcpV2.Core;

namespace TiaMcpV2.Services
{
    /// <summary>
    /// F-CPU Safety programming service:
    /// - F-block (F-OB, F-FB, F-FC, F-DB) SCL templates
    /// - F-instruction reference (ESTOP1, EDM, SFDOOR, FDBACK, MUTING, TWO_H_EN, ACK_GL, ACK_REI)
    /// - PROFIsafe configuration guidance
    /// - Safety signature & validation workflow
    /// - F-runtime group setup
    /// </summary>
    public class SafetyProgrammingService
    {
        private readonly BlockAutonomyService _blockAutonomy;
        private readonly ILogger<SafetyProgrammingService>? _logger;

        public SafetyProgrammingService(BlockAutonomyService blockAutonomy, ILogger<SafetyProgrammingService>? logger = null)
        {
            _blockAutonomy = blockAutonomy;
            _logger = logger;
        }

        public List<Dictionary<string, object?>> GetFInstructionReference()
        {
            return new()
            {
                new() {
                    ["Name"] = "ESTOP1",
                    ["Description"] = "Emergency stop with monitoring (Cat 0 / Cat 1)",
                    ["Inputs"] = "E_STOP, ACK_NEC, ACK",
                    ["Outputs"] = "Q",
                    ["UseFor"] = "All emergency stop circuits — never write own E-Stop logic",
                    ["Standard"] = "ISO 13850, EN 60204"
                },
                new() {
                    ["Name"] = "EDM",
                    ["Description"] = "External Device Monitoring — verifies external contactor responds correctly",
                    ["Inputs"] = "ON, EDM (feedback), MON_T (monitoring time)",
                    ["Outputs"] = "Q, ERROR",
                    ["UseFor"] = "Monitoring contactor feedback — Cat 4 redundancy"
                },
                new() {
                    ["Name"] = "SFDOOR",
                    ["Description"] = "Safety door monitoring — dual-channel position switch",
                    ["Inputs"] = "POSITION_1, POSITION_2, ACK_NEC, ACK",
                    ["Outputs"] = "Q",
                    ["UseFor"] = "Guard door, safety gate monitoring"
                },
                new() {
                    ["Name"] = "FDBACK",
                    ["Description"] = "Feedback monitoring (general purpose)",
                    ["Inputs"] = "ON, FEEDBACK, FB_TIME",
                    ["Outputs"] = "Q, ERROR"
                },
                new() {
                    ["Name"] = "MUTING",
                    ["Description"] = "Light curtain muting (palette transport bypass)",
                    ["Inputs"] = "MUT_EN, MS_11, MS_12, MS_21, MS_22, FREE",
                    ["Outputs"] = "Q, MUTING_ACT"
                },
                new() {
                    ["Name"] = "TWO_H_EN",
                    ["Description"] = "Two-hand control (synchronous press)",
                    ["Inputs"] = "IN1, IN2, ENABLE",
                    ["Outputs"] = "Q",
                    ["Standard"] = "EN 574 Type IIIA/IIIC"
                },
                new() {
                    ["Name"] = "ACK_GL",
                    ["Description"] = "Global F-acknowledgment for passivated F-I/O",
                    ["Inputs"] = "ACK_GLOB",
                    ["Outputs"] = "OUT_FOR_LIB",
                    ["UseFor"] = "Reintegrate F-I/O after passivation"
                },
                new() {
                    ["Name"] = "ACK_REI",
                    ["Description"] = "Single-channel reintegration acknowledge",
                    ["Inputs"] = "ACK_ID",
                    ["UseFor"] = "Per-channel F-I/O acknowledge"
                },
                new() {
                    ["Name"] = "ENABLE_SWITCH",
                    ["Description"] = "Enabling switch (3-position)",
                    ["Inputs"] = "IN, FB_AT_MIDDLE",
                    ["Outputs"] = "Q",
                    ["UseFor"] = "Jog/teach mode permission with hold-to-run"
                }
            };
        }

        public Dictionary<string, object?> GetSafetyWorkflow()
        {
            return new Dictionary<string, object?>
            {
                ["Steps"] = new[]
                {
                    "1. Plan: SRS (Safety Requirement Specification) per ISO 13849 / IEC 61508",
                    "2. Hardware: Configure F-CPU + F-I/O modules with PROFIsafe addresses",
                    "3. Safety Administration: Set F-runtime group, F-cycle time, password",
                    "4. F-Programming: Use ONLY ESTOP1, SFDOOR, MUTING, TWO_H_EN — never custom logic",
                    "5. Compile: Generate F-collective signature for validation",
                    "6. Document: Print safety summary, attach to project file",
                    "7. Acceptance test: Verify each safety function with formal procedure",
                    "8. TÜV/Validation: Independent verification per IEC 61508 Part 3",
                    "9. Sign-off: Customer + safety officer + TÜV signatures",
                    "10. Production: F-program signature locked — no further changes without re-validation"
                },
                ["StandardsReferences"] = new[]
                {
                    "ISO 13849-1 (PL determination)",
                    "ISO 13849-2 (Validation)",
                    "IEC 61508-3 (SIL software)",
                    "IEC 62061 (Machinery functional safety)",
                    "ISO 13850 (E-Stop)",
                    "EN 60204-1 (Machine electrical equipment)",
                    "ISO 14119 (Interlocking devices for guards)"
                },
                ["BestPractices"] = new[]
                {
                    "✓ Always use built-in F-blocks (ESTOP1, SFDOOR, etc.) — they are TÜV-certified",
                    "✓ Use 1oo2 (dual-channel) evaluation for SIL 3 / PLe",
                    "✓ Set discrepancy time = 100..500ms typical",
                    "✓ Test inputs on F-DI modules to detect short-circuits",
                    "✓ Document every F-function with required SIL/PL and standard reference",
                    "✓ Restrict F-program access with password",
                    "✓ Never bypass safety logic, even temporarily",
                    "✗ Never write your own E-Stop algorithm",
                    "✗ Never mix safety and standard logic in the same block",
                    "✗ Never use timer/counter outputs as safety functions"
                }
            };
        }

        public string GenerateFobTemplate(string blockName)
        {
            return @"FUNCTION_BLOCK """ + blockName + @"""
TITLE = 'F-OB Safety Runtime Group'
{ S7_Optimized_Access := 'TRUE' }
{ S7_Fail_Safe := 'True' }
AUTHOR : 'TiaMcpV2'
FAMILY : 'Safety'
VERSION : 1.0
//F-OB called by F-runtime group at safety cycle (typically 100ms)
//ALL safety logic MUST be in F-blocks — standard blocks NOT allowed in F-runtime

   VAR
      _ESTOP1 : ESTOP1;
      _DOOR_FRONT : SFDOOR;
      _LIGHT_CURTAIN : MUTING;
      _ACK_GL : ACK_GL;
   END_VAR

BEGIN

REGION Global F-acknowledgment (passivated I/O reintegration)
    _ACK_GL(ACK_GLOB := ""DB_FSafety"".bAckGlobal);
END_REGION

REGION Emergency stop chain
    _ESTOP1(E_STOP := ""DB_FSafety"".bEStopChain,
            ACK_NEC := TRUE,
            ACK := ""DB_FSafety"".bAckOperator,
            Q => ""DB_FSafety"".bSafetyOk);
END_REGION

REGION Safety door
    _DOOR_FRONT(POSITION_1 := ""DB_FSafety"".bDoor1_NC1,
                POSITION_2 := ""DB_FSafety"".bDoor1_NC2,
                ACK_NEC := TRUE,
                ACK := ""DB_FSafety"".bAckOperator,
                Q => ""DB_FSafety"".bDoorOk);
END_REGION

REGION Light curtain with muting
    _LIGHT_CURTAIN(MUT_EN := ""DB_FSafety"".bMutingEnable,
                   MS_11 := ""DB_FSafety"".bMutingS1,
                   MS_12 := ""DB_FSafety"".bMutingS2,
                   FREE := ""DB_FSafety"".bLightCurtainClear,
                   Q => ""DB_FSafety"".bCurtainOk);
END_REGION

REGION Output combination
    // Combine all safety conditions for final output
    ""DB_FSafety"".bGlobalSafety := ""DB_FSafety"".bSafetyOk
                                  AND ""DB_FSafety"".bDoorOk
                                  AND ""DB_FSafety"".bCurtainOk;
END_REGION

END_FUNCTION_BLOCK";
        }

        public string GenerateFDbTemplate(string blockName)
        {
            return @"DATA_BLOCK """ + blockName + @"""
TITLE = 'F-DB Safety Data'
{ S7_Optimized_Access := 'TRUE' }
{ S7_Fail_Safe := 'True' }
AUTHOR : 'TiaMcpV2'
FAMILY : 'Safety'
VERSION : 1.0
NON_RETAIN
//F-DB — only F-blocks can write here. Standard blocks can READ via specific safe-read pattern.

   VAR
      // E-Stop chain
      bEStopChain : Bool;
      bAckOperator : Bool;
      bAckGlobal : Bool;
      bSafetyOk : Bool;

      // Doors
      bDoor1_NC1 : Bool;
      bDoor1_NC2 : Bool;
      bDoorOk : Bool;

      // Light curtain
      bMutingEnable : Bool;
      bMutingS1 : Bool;
      bMutingS2 : Bool;
      bLightCurtainClear : Bool;
      bCurtainOk : Bool;

      // Global
      bGlobalSafety : Bool;
   END_VAR

BEGIN
   bAckGlobal := FALSE;
END_DATA_BLOCK";
        }
    }
}
