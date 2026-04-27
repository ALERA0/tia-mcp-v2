using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using TiaMcpV2.Core;

namespace TiaMcpV2.Services
{
    /// <summary>
    /// Safety hardware design helper:
    /// - Recommends safety devices for given hazards (E-Stop, light curtain, door switch, two-hand)
    /// - Lists available SIRIUS 3SK safety relays and 3SE/3SU safety switches
    /// - Generates safety wiring plan with F-DI/F-DQ assignments
    /// - Provides PROFIsafe address allocation
    /// - Generates safety circuit SCL templates (ESTOP1, TWO_HAND, MUTING, ENABLE_SWITCH)
    /// </summary>
    public class SafetyHardwareService
    {
        private readonly ModuleCatalogService _catalog;
        private readonly ILogger<SafetyHardwareService>? _logger;

        public SafetyHardwareService(ModuleCatalogService catalog, ILogger<SafetyHardwareService>? logger = null)
        {
            _catalog = catalog;
            _logger = logger;
        }

        /// <summary>
        /// Recommend safety devices for a list of hazards.
        /// </summary>
        public List<Dictionary<string, object?>> RecommendSafetyDevices(SafetyRequirement req)
        {
            var allSafety = _catalog.GetByCategory("Safety");
            var recommendations = new List<Dictionary<string, object?>>();

            foreach (var hazard in req.Hazards ?? new List<string>())
            {
                var lower = hazard.ToLowerInvariant();
                List<ModuleCatalogService.ModuleSpec> matched = new();

                if (lower.Contains("e-stop") || lower.Contains("emergency"))
                {
                    matched = allSafety.Where(m => m.SubCategory == "E-Stop").ToList();
                    matched.AddRange(allSafety.Where(m => m.SubCategory == "Safety-Relay" &&
                        m.Description.Contains("E-Stop", StringComparison.OrdinalIgnoreCase)));
                }
                else if (lower.Contains("door") || lower.Contains("kapı"))
                {
                    matched = allSafety.Where(m =>
                        m.SubCategory == "Safety-Switch" ||
                        m.Tags?.Contains("door") == true).ToList();
                }
                else if (lower.Contains("light curtain") || lower.Contains("ışık perdesi"))
                {
                    matched = allSafety.Where(m => m.SubCategory == "Light-Curtain").ToList();
                }
                else if (lower.Contains("scanner") || lower.Contains("agv"))
                {
                    matched = allSafety.Where(m => m.SubCategory == "Laser-Scanner").ToList();
                }
                else if (lower.Contains("two-hand") || lower.Contains("iki el"))
                {
                    matched = allSafety.Where(m => m.SubCategory == "Safety-Control" &&
                        m.Tags?.Contains("two-hand") == true).ToList();
                }
                else if (lower.Contains("enabling") || lower.Contains("enable"))
                {
                    matched = allSafety.Where(m => m.Tags?.Contains("enabling-switch") == true).ToList();
                }
                else if (lower.Contains("cable") || lower.Contains("rope") || lower.Contains("halat"))
                {
                    matched = allSafety.Where(m => m.Tags?.Contains("cable-pull") == true).ToList();
                }

                recommendations.Add(new Dictionary<string, object?>
                {
                    ["Hazard"] = hazard,
                    ["RecommendedDevices"] = matched.Take(3).Select(m => new
                    {
                        m.OrderNumber, m.Description, m.Tags
                    }).ToList()
                });
            }

            return recommendations;
        }

        /// <summary>
        /// Generate a safety wiring plan with F-DI/F-DQ allocation.
        /// </summary>
        public Dictionary<string, object?> GenerateSafetyWiringPlan(List<SafetyDeviceAssignment> assignments,
            int fDiStartByte = 0, int fDqStartByte = 0)
        {
            var plan = new List<Dictionary<string, object?>>();
            int currentFDiByte = fDiStartByte;
            int currentFDqByte = fDqStartByte;

            foreach (var dev in assignments)
            {
                var entry = new Dictionary<string, object?>
                {
                    ["DeviceName"] = dev.DeviceName,
                    ["DeviceType"] = dev.DeviceType,
                    ["Hazard"] = dev.Hazard
                };

                // Assign F-DI for inputs
                if (dev.InputChannels > 0)
                {
                    var addresses = new List<string>();
                    for (int i = 0; i < dev.InputChannels; i++)
                    {
                        addresses.Add($"%I{currentFDiByte}.{i}");
                    }
                    entry["F_DI_Addresses"] = addresses;
                    if (dev.InputChannels > 1)
                        entry["F_DI_ChannelEvaluation"] = "1oo2 (dual channel)";
                    currentFDiByte++;
                }

                // Assign F-DQ for outputs
                if (dev.OutputChannels > 0)
                {
                    var addresses = new List<string>();
                    for (int i = 0; i < dev.OutputChannels; i++)
                    {
                        addresses.Add($"%Q{currentFDqByte}.{i}");
                    }
                    entry["F_DQ_Addresses"] = addresses;
                    currentFDqByte++;
                }

                // Recommended SIL/PL
                entry["TargetSIL"] = dev.RequiredSIL ?? "SIL3";
                entry["TargetPL"] = dev.RequiredPL ?? "PLe";
                entry["Standard"] = "IEC 61508 / ISO 13849";

                plan.Add(entry);
            }

            return new Dictionary<string, object?>
            {
                ["WiringPlan"] = plan,
                ["TotalDevices"] = assignments.Count,
                ["FDiBytesUsed"] = currentFDiByte - fDiStartByte,
                ["FDqBytesUsed"] = currentFDqByte - fDqStartByte
            };
        }

        /// <summary>
        /// Generate SCL safety code templates.
        /// </summary>
        public string GenerateSafetyTemplate(string templateType, string blockName, Dictionary<string, string>? parameters = null)
        {
            switch (templateType?.ToUpperInvariant())
            {
                case "ESTOP1": return GenerateEStopTemplate(blockName);
                case "TWO_HAND": return GenerateTwoHandTemplate(blockName);
                case "MUTING": return GenerateMutingTemplate(blockName);
                case "ENABLE_SWITCH": return GenerateEnableSwitchTemplate(blockName);
                case "DOOR_LOCK": return GenerateDoorLockTemplate(blockName);
                default:
                    throw new ArgumentException($"Unknown safety template: {templateType}. Use: ESTOP1, TWO_HAND, MUTING, ENABLE_SWITCH, DOOR_LOCK");
            }
        }

        private string GenerateEStopTemplate(string blockName) => $@"FUNCTION_BLOCK ""{blockName}""
TITLE = 'E-Stop Safety Function (PROFIsafe)'
{{ S7_Optimized_Access := 'TRUE' }}
AUTHOR : 'TiaMcpV2'
FAMILY : 'Safety'
VERSION : 1.0
//F-block: E-Stop circuit per ISO 13850, evaluated dual-channel via PROFIsafe (1oo2)
//Use ESTOP1 system block — never write your own E-Stop logic from scratch!

VAR_INPUT
    iE_Stop_NC1 : Bool;       // F-DI dual-channel input (NC contact 1)
    iE_Stop_NC2 : Bool;       // F-DI dual-channel input (NC contact 2)
    iAck : Bool;              // Acknowledge button (NO contact)
    iCfg_AutoStart : Bool := FALSE; // FALSE = manual ack required (recommended)
END_VAR

VAR_OUTPUT
    oSafetyOk : Bool;         // Safety output — TRUE when E-Stop released and acknowledged
    oFault : Bool;            // Discrepancy fault (channels disagree)
END_VAR

VAR
    _ESTOP1 : ESTOP1;         // Built-in F-system block (use exactly this — do not replace!)
END_VAR

BEGIN

REGION E-Stop evaluation
    _ESTOP1(E_STOP := iE_Stop_NC1 AND iE_Stop_NC2,
            ACK_NEC := NOT iCfg_AutoStart,
            ACK := iAck,
            Q := oSafetyOk);
END_REGION

REGION Discrepancy detection
    oFault := iE_Stop_NC1 <> iE_Stop_NC2;
END_REGION

END_FUNCTION_BLOCK";

        private string GenerateTwoHandTemplate(string blockName) => $@"FUNCTION_BLOCK ""{blockName}""
TITLE = 'Two-Hand Control (Type IIIA per EN 574)'
{{ S7_Optimized_Access := 'TRUE' }}
AUTHOR : 'TiaMcpV2'
FAMILY : 'Safety'
VERSION : 1.0

VAR_INPUT
    iButton1 : Bool;          // Left button F-DI input
    iButton2 : Bool;           // Right button F-DI input
END_VAR

VAR_OUTPUT
    oRelease : Bool;          // Press release output (TRUE = safe to actuate)
END_VAR

VAR
    _TWO_HAND : TWO_HAND;     // Built-in F-system block
END_VAR

BEGIN

    _TWO_HAND(IN1 := iButton1, IN2 := iButton2, Q := oRelease);
    // Synchronous press within 500ms required by both buttons
    // Buttons must be released between cycles (re-pressing required)

END_FUNCTION_BLOCK";

        private string GenerateMutingTemplate(string blockName) => $@"FUNCTION_BLOCK ""{blockName}""
TITLE = 'Light Curtain Muting (palette/material flow)'
{{ S7_Optimized_Access := 'TRUE' }}
AUTHOR : 'TiaMcpV2'
FAMILY : 'Safety'
VERSION : 1.0

VAR_INPUT
    iLightCurtain : Bool;     // Light curtain output (TRUE = clear)
    iMutingSensor1 : Bool;    // Material detection sensor 1
    iMutingSensor2 : Bool;    // Material detection sensor 2
    iMutingEnable : Bool;     // External enable for muting
END_VAR

VAR_OUTPUT
    oSafetyOk : Bool;         // TRUE = safe to operate
    oMutingActive : Bool;     // Muting indicator lamp
END_VAR

VAR
    _MUTING : MUTING;
END_VAR

BEGIN

    _MUTING(MUT_EN := iMutingEnable,
            MS_11 := iMutingSensor1,
            MS_12 := iMutingSensor2,
            FREE := iLightCurtain,
            Q := oSafetyOk);
    oMutingActive := _MUTING.MUTING_ACT;

END_FUNCTION_BLOCK";

        private string GenerateEnableSwitchTemplate(string blockName) => $@"FUNCTION_BLOCK ""{blockName}""
TITLE = 'Enabling (Hold-to-Run) Switch — 3-position'
{{ S7_Optimized_Access := 'TRUE' }}
AUTHOR : 'TiaMcpV2'
FAMILY : 'Safety'
VERSION : 1.0

VAR_INPUT
    iEnable_NO : Bool;        // Pos 2 (middle): pressed
    iEnable_NC : Bool;        // Pos 1 (released): NOT pressed
END_VAR

VAR_OUTPUT
    oRelease : Bool;          // TRUE = jog/teach mode permitted
END_VAR

VAR
    _ENABLE_SWITCH : ENABLE_SWITCH;
END_VAR

BEGIN

    _ENABLE_SWITCH(IN := iEnable_NO,
                   FB_AT_MIDDLE := iEnable_NO AND iEnable_NC,
                   Q := oRelease);
    // Pos 1 (released): no movement
    // Pos 2 (held middle): movement allowed
    // Pos 3 (panic-pressed): movement stopped

END_FUNCTION_BLOCK";

        private string GenerateDoorLockTemplate(string blockName) => $@"FUNCTION_BLOCK ""{blockName}""
TITLE = 'Safety Door Lock with Guard Locking'
{{ S7_Optimized_Access := 'TRUE' }}
AUTHOR : 'TiaMcpV2'
FAMILY : 'Safety'
VERSION : 1.0

VAR_INPUT
    iDoorClosed_NC1 : Bool;   // Door switch channel 1
    iDoorClosed_NC2 : Bool;   // Door switch channel 2
    iDoorLocked : Bool;       // Mechanical lock feedback
    iLockRequest : Bool;      // Request to lock door
    iUnlockRequest : Bool;    // Request to unlock (e.g. cycle complete)
    iAck : Bool;              // Operator acknowledge
END_VAR

VAR_OUTPUT
    oLockSolenoid : Bool;     // Lock solenoid output (F-DQ)
    oSafetyOk : Bool;         // TRUE when door closed AND locked
    oFault : Bool;
END_VAR

VAR
    _DoorMonitor : SFDOOR;
END_VAR

BEGIN

    _DoorMonitor(POSITION_1 := iDoorClosed_NC1,
                 POSITION_2 := iDoorClosed_NC2,
                 ACK_NEC := TRUE,
                 ACK := iAck,
                 Q := oSafetyOk);

    // Lock control: lock when requested AND machine is dangerous
    IF iLockRequest AND iDoorClosed_NC1 AND iDoorClosed_NC2 THEN
        oLockSolenoid := TRUE;
    ELSIF iUnlockRequest THEN
        oLockSolenoid := FALSE;
    END_IF;

    oFault := iDoorClosed_NC1 <> iDoorClosed_NC2;

END_FUNCTION_BLOCK";

        public class SafetyRequirement
        {
            public List<string>? Hazards { get; set; }
            public string? RequiredSIL { get; set; }     // SIL1, SIL2, SIL3
            public string? RequiredPL { get; set; }       // PLa, PLb, PLc, PLd, PLe
            public string? Application { get; set; }      // "robot cell", "press", "AGV", "conveyor"
        }

        public class SafetyDeviceAssignment
        {
            public string DeviceName { get; set; } = "";
            public string DeviceType { get; set; } = "";    // "E-Stop", "DoorSwitch", "LightCurtain", etc.
            public string? Hazard { get; set; }
            public int InputChannels { get; set; } = 2;     // Default 1oo2 dual-channel
            public int OutputChannels { get; set; } = 0;
            public string? RequiredSIL { get; set; }
            public string? RequiredPL { get; set; }
        }
    }
}
