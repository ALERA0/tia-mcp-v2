using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using TiaMcpV2.Core;

namespace TiaMcpV2.Services
{
    /// <summary>
    /// Motion control programming service:
    /// - Generates SCL templates using MC_ instructions for axis control
    /// - Provides commissioning helpers (jog, home, test moves)
    /// - PROFIdrive telegram reference and IRT configuration guidance
    /// - Single-axis and multi-axis motion patterns
    /// </summary>
    public class MotionControlService
    {
        private readonly BlockAutonomyService _blockAutonomy;
        private readonly ILogger<MotionControlService>? _logger;

        public MotionControlService(BlockAutonomyService blockAutonomy, ILogger<MotionControlService>? logger = null)
        {
            _blockAutonomy = blockAutonomy;
            _logger = logger;
        }

        /// <summary>
        /// Generate a complete motion control FB for a single positioning axis.
        /// Includes MC_Power, MC_Reset, MC_Home, MC_MoveAbsolute, MC_MoveJog, MC_Halt, MC_Stop.
        /// </summary>
        public string GenerateAxisControlFB(MotionAxisFbRequest req)
        {
            var name = req.BlockName ?? "FB_AxisControl";
            var axisRef = req.AxisName ?? "AxisX";

            var template = @"FUNCTION_BLOCK ""__NAME__""
TITLE = ""Single-Axis Motion Control""
{ S7_Optimized_Access := 'TRUE' }
AUTHOR : 'TiaMcpV2'
FAMILY : 'Motion'
VERSION : 1.0
//Wraps MC_ instructions for a TO_PositioningAxis or TO_SpeedAxis
//Provides: Power, Reset, Home, Move Absolute, Move Relative, Jog +/-, Halt, Stop
//Built-in state monitoring, fault collection, position/velocity feedback

   VAR_INPUT
      iEnable : Bool;              // Power axis on (rising edge)
      iReset : Bool;               // Acknowledge faults
      iHome : Bool;                // Home/reference axis
      iMoveAbs : Bool;             // Trigger absolute move (rising edge)
      iMoveRel : Bool;             // Trigger relative move
      iJogForward : Bool;          // Jog forward (held)
      iJogBackward : Bool;         // Jog backward (held)
      iHalt : Bool;                // Decelerate to stop
      iStop : Bool;                // Emergency stop
      iCfg_Position : LReal;       // Target position (mm/deg/etc.)
      iCfg_Velocity : LReal := 100.0; // Move velocity
      iCfg_Acceleration : LReal := 1000.0;
      iCfg_Deceleration : LReal := 1000.0;
      iCfg_Jerk : LReal := 10000.0;
      iCfg_JogVelocity : LReal := 50.0;
   END_VAR

   VAR_IN_OUT
      ioAxis : ""__AXISREF__"";       // Reference to TO axis
   END_VAR

   VAR_OUTPUT
      oReady : Bool;               // Axis enabled and homed
      oHomed : Bool;               // Reference position known
      oMoving : Bool;              // Axis is moving
      oAtTarget : Bool;            // Position reached
      oFault : Bool;               // Any fault active
      oFaultCode : DInt;           // Last error code
      oActualPosition : LReal;     // Current position
      oActualVelocity : LReal;     // Current velocity
   END_VAR

   VAR
      _MC_Power : MC_Power;
      _MC_Reset : MC_Reset;
      _MC_Home : MC_Home;
      _MC_MoveAbsolute : MC_MoveAbsolute;
      _MC_MoveRelative : MC_MoveRelative;
      _MC_MoveJog : MC_MoveJog;
      _MC_Halt : MC_Halt;
      _MC_Stop : MC_Stop;

      _ResetCmdPrev : Bool;
      _HomeCmdPrev : Bool;
      _MoveAbsCmdPrev : Bool;
      _MoveRelCmdPrev : Bool;
   END_VAR

BEGIN

REGION Power & Reset (always called)
    _MC_Power(Axis := ioAxis, Enable := iEnable, StartMode := 1, StopMode := 0);
    oReady := _MC_Power.Status;

    IF iReset AND NOT _ResetCmdPrev THEN
        _MC_Reset(Axis := ioAxis, Execute := TRUE, Restart := FALSE);
    ELSE
        _MC_Reset(Axis := ioAxis, Execute := FALSE);
    END_IF;
    _ResetCmdPrev := iReset;
END_REGION

REGION Homing
    IF iHome AND NOT _HomeCmdPrev AND oReady THEN
        _MC_Home(Axis := ioAxis, Execute := TRUE, Position := 0.0, Mode := 0);
    ELSE
        _MC_Home(Axis := ioAxis, Execute := FALSE);
    END_IF;
    _HomeCmdPrev := iHome;
    oHomed := _MC_Home.Done OR ioAxis.StatusBits.HomingDone;
END_REGION

REGION Movement commands
    IF iStop THEN
        _MC_Stop(Axis := ioAxis, Execute := TRUE);
    ELSE
        _MC_Stop(Axis := ioAxis, Execute := FALSE);
    END_IF;

    IF iHalt AND NOT iStop THEN
        _MC_Halt(Axis := ioAxis, Execute := TRUE);
    ELSE
        _MC_Halt(Axis := ioAxis, Execute := FALSE);
    END_IF;

    // Jog forward/backward
    _MC_MoveJog(Axis := ioAxis,
                JogForward := iJogForward AND oReady AND oHomed AND NOT iHalt AND NOT iStop,
                JogBackward := iJogBackward AND oReady AND oHomed AND NOT iHalt AND NOT iStop,
                Velocity := iCfg_JogVelocity,
                Acceleration := iCfg_Acceleration,
                Deceleration := iCfg_Deceleration,
                Jerk := iCfg_Jerk);

    // Absolute move (edge-triggered)
    IF iMoveAbs AND NOT _MoveAbsCmdPrev AND oReady AND oHomed THEN
        _MC_MoveAbsolute(Axis := ioAxis,
                         Execute := TRUE,
                         Position := iCfg_Position,
                         Velocity := iCfg_Velocity,
                         Acceleration := iCfg_Acceleration,
                         Deceleration := iCfg_Deceleration,
                         Jerk := iCfg_Jerk,
                         Direction := 1);
    ELSE
        _MC_MoveAbsolute(Axis := ioAxis, Execute := FALSE);
    END_IF;
    _MoveAbsCmdPrev := iMoveAbs;

    // Relative move
    IF iMoveRel AND NOT _MoveRelCmdPrev AND oReady AND oHomed THEN
        _MC_MoveRelative(Axis := ioAxis,
                         Execute := TRUE,
                         Distance := iCfg_Position,
                         Velocity := iCfg_Velocity,
                         Acceleration := iCfg_Acceleration,
                         Deceleration := iCfg_Deceleration,
                         Jerk := iCfg_Jerk);
    ELSE
        _MC_MoveRelative(Axis := ioAxis, Execute := FALSE);
    END_IF;
    _MoveRelCmdPrev := iMoveRel;
END_REGION

REGION Status & Outputs
    oMoving := ioAxis.StatusBits.Moving;
    oAtTarget := _MC_MoveAbsolute.Done OR _MC_MoveRelative.Done;
    oFault := ioAxis.StatusBits.Error OR _MC_MoveAbsolute.Error OR _MC_MoveRelative.Error;
    IF oFault THEN
        oFaultCode := _MC_MoveAbsolute.ErrorID;
    ELSE
        oFaultCode := 0;
    END_IF;
    oActualPosition := ioAxis.Position;
    oActualVelocity := ioAxis.Velocity;
END_REGION

END_FUNCTION_BLOCK";
            return template.Replace("__NAME__", name).Replace("__AXISREF__", axisRef);
        }

        /// <summary>
        /// Generate a synchronous axis FB for electronic gearing/camming.
        /// </summary>
        public string GenerateSyncAxisFB(string blockName, string syncAxisType, string masterAxisType)
        {
            return $@"FUNCTION_BLOCK ""{blockName}""
TITLE = ""Synchronous Axis Control (Electronic Gear/Cam)""
{{ S7_Optimized_Access := 'TRUE' }}
AUTHOR : 'TiaMcpV2'
FAMILY : 'Motion'
VERSION : 1.0
//Synchronous axis with master-slave gearing + cam capability
//Wraps MC_GearIn, MC_GearInPos, MC_CamIn, MC_Phasing

   VAR_INPUT
      iGearIn : Bool;              // Engage electronic gear
      iCamIn : Bool;               // Engage cam profile
      iSyncOut : Bool;             // Disengage synchronization
      iCfg_GearRatioNumerator : DInt := 1;
      iCfg_GearRatioDenominator : DInt := 1;
      iCfg_PhasingShift : LReal := 0.0;
      iCfg_CamId : DInt := 1;
   END_VAR

   VAR_IN_OUT
      ioMaster : ""{masterAxisType}"";
      ioSlave : ""{syncAxisType}"";
   END_VAR

   VAR_OUTPUT
      oSynchronized : Bool;
      oPhase : LReal;
      oFault : Bool;
      oFaultCode : DInt;
   END_VAR

   VAR
      _MC_GearIn : MC_GearIn;
      _MC_CamIn : MC_CamIn;
      _MC_Phasing : MC_Phasing;
      _MC_SyncOut : MC_SynchronizedMotionSimulation;
   END_VAR

BEGIN

REGION Gear-in (electronic gearing)
    IF iGearIn THEN
        _MC_GearIn(Master := ioMaster,
                   Slave := ioSlave,
                   Execute := TRUE,
                   RatioNumerator := iCfg_GearRatioNumerator,
                   RatioDenominator := iCfg_GearRatioDenominator,
                   Acceleration := 1000.0,
                   Deceleration := 1000.0,
                   Jerk := 10000.0);
    ELSE
        _MC_GearIn(Master := ioMaster, Slave := ioSlave, Execute := FALSE);
    END_IF;
END_REGION

REGION Cam-in (electronic cam profile)
    IF iCamIn THEN
        _MC_CamIn(Master := ioMaster,
                  Slave := ioSlave,
                  Execute := TRUE,
                  MasterOffset := 0.0,
                  SlaveOffset := 0.0,
                  MasterScaling := 1.0,
                  SlaveScaling := 1.0);
    ELSE
        _MC_CamIn(Master := ioMaster, Slave := ioSlave, Execute := FALSE);
    END_IF;
END_REGION

REGION Phasing adjustment
    IF iCfg_PhasingShift <> 0.0 THEN
        _MC_Phasing(Master := ioMaster,
                    Slave := ioSlave,
                    Execute := TRUE,
                    PhaseShift := iCfg_PhasingShift,
                    Velocity := 100.0,
                    Acceleration := 1000.0,
                    Deceleration := 1000.0);
    ELSE
        _MC_Phasing(Master := ioMaster, Slave := ioSlave, Execute := FALSE);
    END_IF;
END_REGION

REGION Outputs
    oSynchronized := _MC_GearIn.InGear OR _MC_CamIn.InSync;
    oPhase := _MC_Phasing.PhaseShift;
    oFault := _MC_GearIn.Error OR _MC_CamIn.Error;
END_REGION

END_FUNCTION_BLOCK";
        }

        public List<Dictionary<string, object?>> GetMotionInstructionReference()
        {
            return new()
            {
                new() {
                    ["Instruction"] = "MC_Power",
                    ["Purpose"] = "Enable axis (REQUIRED before any motion)",
                    ["StartMode"] = "0=Velocity-only, 1=Position-controlled (default for positioning axis)",
                    ["StopMode"] = "0=Ramp down, 1=Coast, 2=Emergency",
                    ["Tip"] = "Call this every cycle, not edge-triggered"
                },
                new() {
                    ["Instruction"] = "MC_Reset",
                    ["Purpose"] = "Acknowledge axis faults",
                    ["Tip"] = "Edge-triggered (Execute := rising edge)"
                },
                new() {
                    ["Instruction"] = "MC_Home",
                    ["Purpose"] = "Reference axis to absolute position",
                    ["Mode"] = "0=Direct (set position), 5=Active homing with sensor, 8=Absolute encoder",
                    ["Tip"] = "Required after every fault for incremental encoders"
                },
                new() {
                    ["Instruction"] = "MC_MoveAbsolute",
                    ["Purpose"] = "Move to absolute position",
                    ["Direction"] = "0=Shortest, 1=Positive, 2=Negative, 3=Current direction",
                    ["Tip"] = "Axis must be Ready AND Homed first"
                },
                new() {
                    ["Instruction"] = "MC_MoveRelative",
                    ["Purpose"] = "Move by relative distance",
                    ["Tip"] = "No homing required"
                },
                new() {
                    ["Instruction"] = "MC_MoveVelocity",
                    ["Purpose"] = "Move at constant velocity (speed mode)",
                    ["Tip"] = "Use for conveyor-like applications"
                },
                new() {
                    ["Instruction"] = "MC_MoveJog",
                    ["Purpose"] = "Manual jog forward/backward",
                    ["Tip"] = "Held command — release stops axis"
                },
                new() {
                    ["Instruction"] = "MC_Halt",
                    ["Purpose"] = "Decelerate to stop (controlled)",
                    ["Tip"] = "New commands accepted after Halt complete"
                },
                new() {
                    ["Instruction"] = "MC_Stop",
                    ["Purpose"] = "Emergency stop (high priority)",
                    ["Tip"] = "Blocks new commands until Execute=FALSE"
                },
                new() {
                    ["Instruction"] = "MC_GearIn",
                    ["Purpose"] = "Engage electronic gear (master-slave ratio)",
                    ["Tip"] = "Use RatioNumerator/Denominator for exact ratios"
                },
                new() {
                    ["Instruction"] = "MC_CamIn",
                    ["Purpose"] = "Engage electronic cam profile",
                    ["Tip"] = "Cam must be defined as TO_Cam first"
                },
                new() {
                    ["Instruction"] = "MC_Phasing",
                    ["Purpose"] = "Adjust phase shift between master and slave",
                    ["Tip"] = "Use for registration/offset corrections"
                },
                new() {
                    ["Instruction"] = "MC_TorqueLimiting",
                    ["Purpose"] = "Limit motor torque (for clamping/probing)",
                    ["Tip"] = "Drive must support torque limiting (S120/S210)"
                },
                new() {
                    ["Instruction"] = "MC_ChangeDynamic",
                    ["Purpose"] = "Change axis dynamic limits at runtime",
                    ["Tip"] = "Lower limits during sensitive moves, restore for fast moves"
                },
                new() {
                    ["Instruction"] = "MC_MoveSuperImposed",
                    ["Purpose"] = "Add superimposed motion to active move",
                    ["Tip"] = "Use for vibration compensation, on-the-fly corrections"
                },
            };
        }

        public List<Dictionary<string, object?>> GetTelegramReference()
        {
            return new()
            {
                new() { ["Number"] = 1,   ["Description"] = "Standard speed control",
                    ["UseFor"] = "Simple VFD speed control", ["DataLength"] = "PZD-2/2" },
                new() { ["Number"] = 3,   ["Description"] = "Speed with 1 position encoder",
                    ["UseFor"] = "Speed control with position feedback", ["DataLength"] = "PZD-9/5" },
                new() { ["Number"] = 5,   ["Description"] = "DSC (Dynamic Servo Control) with 1 encoder",
                    ["UseFor"] = "S120 servo axis", ["DataLength"] = "PZD-9/9" },
                new() { ["Number"] = 7,   ["Description"] = "Positioning, basic positioner",
                    ["UseFor"] = "G120 with EPos", ["DataLength"] = "PZD-2/2" },
                new() { ["Number"] = 9,   ["Description"] = "Positioning with direct setpoint input",
                    ["UseFor"] = "EPos with traversing blocks" },
                new() { ["Number"] = 105, ["Description"] = "DSC + torque reduction + 1 encoder",
                    ["UseFor"] = "RECOMMENDED for S120 PositioningAxis", ["DataLength"] = "PZD-10/10" },
                new() { ["Number"] = 111, ["Description"] = "Basic positioner telegram",
                    ["UseFor"] = "RECOMMENDED for S210 PositioningAxis", ["DataLength"] = "PZD-12/12" },
                new() { ["Number"] = 352, ["Description"] = "Standard with diagnostics",
                    ["UseFor"] = "G120 with diagnostic data", ["DataLength"] = "PZD-6/6" },
                new() { ["Number"] = 353, ["Description"] = "Standard 1 + parameter access",
                    ["UseFor"] = "Drive parameter readback", ["DataLength"] = "PZD-2/2 + PKW" }
            };
        }

        public class MotionAxisFbRequest
        {
            public string? BlockName { get; set; }
            public string? AxisName { get; set; }
            public string? AxisType { get; set; } = "TO_PositioningAxis";   // or TO_SpeedAxis, TO_SynchronousAxis
            public bool IncludeJog { get; set; } = true;
            public bool IncludeHoming { get; set; } = true;
            public bool IncludeRelativeMove { get; set; } = true;
        }
    }
}
