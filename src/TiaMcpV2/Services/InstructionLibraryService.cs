using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TiaMcpV2.Services
{
    /// <summary>
    /// System instruction library reference for SCL programming:
    /// - Timers (TON, TOF, TP, TONR)
    /// - Counters (CTU, CTD, CTUD)
    /// - Math (ABS, SQR, SQRT, LN, EXP, SIN, COS, TAN, ASIN, ACOS, ATAN, ROUND, TRUNC)
    /// - String (FIND, CONCAT, LEFT, MID, RIGHT, LEN, INSERT, DELETE, REPLACE)
    /// - Conversion (NORM_X, SCALE_X, INT_TO_REAL, etc.)
    /// - Comparison (EQ, NE, GT, LT, GE, LE)
    /// - Bit logic (AND, OR, XOR, NOT, SHL, SHR, ROL, ROR, SET_BF, RESET_BF)
    /// - PID (PID_Compact, PID_3Step, PID_Temp)
    /// - Filters (LP_FILTER, HP_FILTER) and Ramps (RAMP_FUNCTION)
    /// - Motion (MC_Power, MC_Home, MC_MoveAbsolute, MC_MoveRelative, MC_MoveVelocity, MC_Halt, MC_Stop, MC_Reset, MC_GearIn, MC_CamIn)
    /// - Modbus (MB_CLIENT, MB_SERVER)
    /// - Open communication (TSEND_C, TRCV_C, TCON, TDISCON, TUSEND, TURCV)
    /// - S7 communication (GET, PUT, BSEND, BRCV, USEND, URCV)
    /// </summary>
    public class InstructionLibraryService
    {
        private readonly ILogger<InstructionLibraryService>? _logger;
        private static readonly List<InstructionInfo> _instructions;

        static InstructionLibraryService()
        {
            _instructions = BuildInstructionLibrary();
        }

        public InstructionLibraryService(ILogger<InstructionLibraryService>? logger = null)
        {
            _logger = logger;
        }

        public List<InstructionInfo> GetAll() => _instructions;

        public List<InstructionInfo> GetByCategory(string category)
        {
            return _instructions.Where(i => i.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public InstructionInfo? GetByName(string name)
        {
            return _instructions.FirstOrDefault(i =>
                i.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public List<InstructionInfo> Search(string query)
        {
            if (string.IsNullOrEmpty(query)) return _instructions;
            var q = query.ToUpperInvariant();
            return _instructions.Where(i =>
                i.Name.ToUpperInvariant().Contains(q) ||
                i.Description.ToUpperInvariant().Contains(q) ||
                i.Category.ToUpperInvariant().Contains(q) ||
                (i.Tags?.Any(t => t.ToUpperInvariant().Contains(q)) == true)
            ).ToList();
        }

        #region Database

        private static List<InstructionInfo> BuildInstructionLibrary()
        {
            return new List<InstructionInfo>
            {
                // ────────── TIMERS ──────────
                new InstructionInfo {
                    Name = "TON", Category = "Timer", Family = "IEC Timer",
                    Description = "On-delay timer — Q goes TRUE after IN has been TRUE for time PT",
                    Inputs = "IN: Bool, PT: Time",
                    Outputs = "Q: Bool, ET: Time (elapsed)",
                    SclExample = "_timer : TON;\n_timer(IN := iStart, PT := T#5s);\nIF _timer.Q THEN ... END_IF;",
                    Tags = new[] { "timer", "on-delay", "delay" }
                },
                new InstructionInfo {
                    Name = "TOF", Category = "Timer", Family = "IEC Timer",
                    Description = "Off-delay timer — Q stays TRUE for PT after IN goes FALSE",
                    Inputs = "IN: Bool, PT: Time",
                    Outputs = "Q: Bool, ET: Time",
                    SclExample = "_offDelay : TOF;\n_offDelay(IN := iSignal, PT := T#3s);\noPulse := _offDelay.Q;"
                },
                new InstructionInfo {
                    Name = "TP", Category = "Timer", Family = "IEC Timer",
                    Description = "Pulse timer — Q gives a fixed-width TRUE pulse on rising edge of IN",
                    Inputs = "IN: Bool, PT: Time",
                    Outputs = "Q: Bool, ET: Time",
                    SclExample = "_pulse : TP;\n_pulse(IN := iTrigger, PT := T#100ms);"
                },
                new InstructionInfo {
                    Name = "TONR", Category = "Timer", Family = "IEC Timer",
                    Description = "Retentive on-delay — accumulates time across IN cycles, reset by R",
                    Inputs = "IN: Bool, R: Bool (reset), PT: Time",
                    Outputs = "Q: Bool, ET: Time",
                    SclExample = "_runtime : TONR;\n_runtime(IN := iRun, R := iReset, PT := T#1h);"
                },

                // ────────── COUNTERS ──────────
                new InstructionInfo {
                    Name = "CTU", Category = "Counter", Family = "IEC Counter",
                    Description = "Up counter — increments CV on rising edge of CU, Q TRUE when CV >= PV",
                    Inputs = "CU: Bool (count up), R: Bool (reset), PV: Int",
                    Outputs = "Q: Bool, CV: Int (current value)",
                    SclExample = "_counter : CTU;\n_counter(CU := iPulse, R := iReset, PV := 100);\noLimit := _counter.Q;"
                },
                new InstructionInfo {
                    Name = "CTD", Category = "Counter", Family = "IEC Counter",
                    Description = "Down counter — decrements CV on rising edge of CD, Q TRUE when CV <= 0",
                    Inputs = "CD: Bool, LD: Bool (load PV), PV: Int",
                    Outputs = "Q: Bool, CV: Int"
                },
                new InstructionInfo {
                    Name = "CTUD", Category = "Counter", Family = "IEC Counter",
                    Description = "Bidirectional counter — counts up on CU, down on CD",
                    Inputs = "CU, CD: Bool, R: Bool, LD: Bool, PV: Int",
                    Outputs = "QU: Bool (>=PV), QD: Bool (<=0), CV: Int"
                },

                // ────────── MATH ──────────
                new InstructionInfo {
                    Name = "ABS", Category = "Math",
                    Description = "Absolute value — works on Int, DInt, Real, LReal",
                    SclExample = "rPositive := ABS(rValue);"
                },
                new InstructionInfo {
                    Name = "SQR", Category = "Math",
                    Description = "Square (x²)",
                    SclExample = "rArea := SQR(rRadius);"
                },
                new InstructionInfo {
                    Name = "SQRT", Category = "Math",
                    Description = "Square root √x",
                    SclExample = "rResult := SQRT(rValue);"
                },
                new InstructionInfo {
                    Name = "LN", Category = "Math", Description = "Natural logarithm ln(x)" },
                new InstructionInfo {
                    Name = "LOG", Category = "Math", Description = "Common logarithm log10(x)" },
                new InstructionInfo {
                    Name = "EXP", Category = "Math", Description = "Exponential e^x" },
                new InstructionInfo {
                    Name = "SIN", Category = "Math", Description = "Sine (radians)",
                    SclExample = "rSin := SIN(rAngleRad);" },
                new InstructionInfo {
                    Name = "COS", Category = "Math", Description = "Cosine (radians)" },
                new InstructionInfo {
                    Name = "TAN", Category = "Math", Description = "Tangent (radians)" },
                new InstructionInfo {
                    Name = "ASIN", Category = "Math", Description = "Arcsine (returns radians)" },
                new InstructionInfo {
                    Name = "ACOS", Category = "Math", Description = "Arccosine" },
                new InstructionInfo {
                    Name = "ATAN", Category = "Math", Description = "Arctangent" },
                new InstructionInfo {
                    Name = "ROUND", Category = "Math",
                    Description = "Round to nearest integer (half-up)",
                    SclExample = "diRounded := ROUND(rValue);" },
                new InstructionInfo {
                    Name = "TRUNC", Category = "Math",
                    Description = "Truncate decimals (toward zero)",
                    SclExample = "diTruncated := TRUNC(rValue);" },
                new InstructionInfo {
                    Name = "CEIL", Category = "Math", Description = "Round up to next integer" },
                new InstructionInfo {
                    Name = "FLOOR", Category = "Math", Description = "Round down to next integer" },
                new InstructionInfo {
                    Name = "MIN", Category = "Math", Description = "Minimum of 2-32 inputs",
                    SclExample = "rLowest := MIN(IN1 := r1, IN2 := r2, IN3 := r3);" },
                new InstructionInfo {
                    Name = "MAX", Category = "Math", Description = "Maximum of 2-32 inputs" },
                new InstructionInfo {
                    Name = "LIMIT", Category = "Math",
                    Description = "Clamp value between MN and MX",
                    SclExample = "rLimited := LIMIT(MN := 0.0, IN := rValue, MX := 100.0);" },
                new InstructionInfo {
                    Name = "MOD", Category = "Math",
                    Description = "Modulo (remainder)",
                    SclExample = "iRemainder := iValue MOD 10;" },

                // ────────── CONVERSION ──────────
                new InstructionInfo {
                    Name = "NORM_X", Category = "Conversion",
                    Description = "Normalize value to 0.0..1.0 range. 0.0 = MIN, 1.0 = MAX",
                    Inputs = "MIN, VALUE, MAX (typically Int)",
                    Outputs = "OUT: Real (0.0..1.0)",
                    SclExample = "rNormalized := NORM_X(MIN := 0, VALUE := iRaw, MAX := 27648);",
                    Tags = new[] { "scaling", "analog" }
                },
                new InstructionInfo {
                    Name = "SCALE_X", Category = "Conversion",
                    Description = "Scale 0.0..1.0 to engineering units. Inverse of NORM_X.",
                    Inputs = "MIN, VALUE: Real (0..1), MAX",
                    Outputs = "OUT (engineering value)",
                    SclExample = "rEng := SCALE_X(MIN := 0.0, VALUE := rNormalized, MAX := 100.0);",
                    Tags = new[] { "scaling", "analog" }
                },
                new InstructionInfo {
                    Name = "INT_TO_REAL", Category = "Conversion",
                    Description = "Convert Int to Real",
                    SclExample = "rResult := INT_TO_REAL(iValue);"
                },
                new InstructionInfo {
                    Name = "REAL_TO_INT", Category = "Conversion",
                    Description = "Convert Real to Int (rounds to nearest)" },
                new InstructionInfo {
                    Name = "DINT_TO_REAL", Category = "Conversion",
                    Description = "Convert DInt to Real" },
                new InstructionInfo {
                    Name = "REAL_TO_DINT", Category = "Conversion",
                    Description = "Convert Real to DInt" },
                new InstructionInfo {
                    Name = "STRG_VAL", Category = "Conversion",
                    Description = "Convert string to numeric value" },
                new InstructionInfo {
                    Name = "VAL_STRG", Category = "Conversion",
                    Description = "Convert numeric to string" },

                // ────────── STRING ──────────
                new InstructionInfo {
                    Name = "LEN", Category = "String",
                    Description = "Length of string",
                    SclExample = "iLen := LEN(IN := sName);"
                },
                new InstructionInfo {
                    Name = "CONCAT", Category = "String",
                    Description = "Concatenate two strings",
                    SclExample = "sFull := CONCAT(IN1 := sFirst, IN2 := sLast);"
                },
                new InstructionInfo {
                    Name = "LEFT", Category = "String",
                    Description = "Left N characters",
                    SclExample = "sPrefix := LEFT(IN := sFull, L := 3);"
                },
                new InstructionInfo {
                    Name = "RIGHT", Category = "String", Description = "Right N characters" },
                new InstructionInfo {
                    Name = "MID", Category = "String", Description = "Middle substring",
                    SclExample = "sMiddle := MID(IN := s, L := 5, P := 3);" },
                new InstructionInfo {
                    Name = "FIND", Category = "String", Description = "Find substring position (returns 0 if not found)" },
                new InstructionInfo {
                    Name = "INSERT", Category = "String", Description = "Insert string at position" },
                new InstructionInfo {
                    Name = "DELETE", Category = "String", Description = "Delete N chars from position" },
                new InstructionInfo {
                    Name = "REPLACE", Category = "String", Description = "Replace substring" },

                // ────────── COMPARISON (operators in SCL) ──────────
                new InstructionInfo {
                    Name = "EQ", Category = "Comparison",
                    Description = "Equal — SCL: =",
                    SclExample = "IF iValue = 100 THEN ..." },
                new InstructionInfo {
                    Name = "NE", Category = "Comparison",
                    Description = "Not equal — SCL: <>" },
                new InstructionInfo {
                    Name = "GT", Category = "Comparison",
                    Description = "Greater than — SCL: >" },
                new InstructionInfo {
                    Name = "LT", Category = "Comparison",
                    Description = "Less than — SCL: <" },
                new InstructionInfo {
                    Name = "GE", Category = "Comparison",
                    Description = "Greater or equal — SCL: >=" },
                new InstructionInfo {
                    Name = "LE", Category = "Comparison",
                    Description = "Less or equal — SCL: <=" },

                // ────────── BIT/WORD LOGIC ──────────
                new InstructionInfo {
                    Name = "AND", Category = "Logic",
                    Description = "Bitwise AND (Word/DWord) or boolean AND",
                    SclExample = "wResult := wMask AND wValue;\nbBoth := bA AND bB;" },
                new InstructionInfo {
                    Name = "OR", Category = "Logic", Description = "Bitwise OR / boolean OR" },
                new InstructionInfo {
                    Name = "XOR", Category = "Logic", Description = "Bitwise XOR / boolean XOR" },
                new InstructionInfo {
                    Name = "NOT", Category = "Logic", Description = "Bitwise/boolean negation" },
                new InstructionInfo {
                    Name = "SHL", Category = "Logic",
                    Description = "Shift left",
                    SclExample = "wShifted := SHL(IN := wValue, N := 3);" },
                new InstructionInfo {
                    Name = "SHR", Category = "Logic", Description = "Shift right" },
                new InstructionInfo {
                    Name = "ROL", Category = "Logic", Description = "Rotate left" },
                new InstructionInfo {
                    Name = "ROR", Category = "Logic", Description = "Rotate right" },
                new InstructionInfo {
                    Name = "SET_BF", Category = "Logic",
                    Description = "Set bit field (range of bits to TRUE)" },
                new InstructionInfo {
                    Name = "RESET_BF", Category = "Logic",
                    Description = "Reset bit field to FALSE" },

                // ────────── PID ──────────
                new InstructionInfo {
                    Name = "PID_Compact", Category = "PID",
                    Description = "Universal PID controller with integrated auto-tuning",
                    SclExample = "_PID : PID_Compact;\n_PID(Setpoint := rSp, Input := rPv, Output => rOut);",
                    Tags = new[] { "PID", "control" }
                },
                new InstructionInfo {
                    Name = "PID_3Step", Category = "PID",
                    Description = "PID 3-step for motorized valves (open/close/stop pulses)" },
                new InstructionInfo {
                    Name = "PID_Temp", Category = "PID",
                    Description = "Temperature PID with heating/cooling outputs" },

                // ────────── FILTERS / RAMPS ──────────
                new InstructionInfo {
                    Name = "LP_FILTER", Category = "Filter",
                    Description = "Low-pass first-order filter",
                    SclExample = "_filter : LP_FILTER;\n_filter(IN := rRaw, T1 := 0.5);" },
                new InstructionInfo {
                    Name = "HP_FILTER", Category = "Filter",
                    Description = "High-pass first-order filter" },
                new InstructionInfo {
                    Name = "RAMP_FUNCTION", Category = "Filter",
                    Description = "Ramp function generator with up/down rates and limits" },

                // ────────── MOTION CONTROL (MC_) ──────────
                new InstructionInfo {
                    Name = "MC_Power", Category = "Motion",
                    Description = "Enable/disable axis. Required before any motion command.",
                    Inputs = "Axis: TO_Axis, Enable: Bool, StartMode: Int, StopMode: Int",
                    Outputs = "Status, Busy, Error, ErrorID",
                    SclExample = "MC_Power(Axis := \"AxisX\", Enable := iEnable, Status => oReady);"
                },
                new InstructionInfo {
                    Name = "MC_Home", Category = "Motion",
                    Description = "Reference axis (home/zero search)",
                    SclExample = "MC_Home(Axis := \"AxisX\", Execute := iHome, Mode := 0, Done => oHomed);"
                },
                new InstructionInfo {
                    Name = "MC_MoveAbsolute", Category = "Motion",
                    Description = "Move to absolute position",
                    Inputs = "Axis, Execute, Position, Velocity, Acceleration, Deceleration, Jerk",
                    Outputs = "Done, Busy, Error",
                    SclExample = "MC_MoveAbsolute(Axis := \"AxisX\", Execute := iMove, Position := 100.0, Velocity := 50.0, Done => oDone);"
                },
                new InstructionInfo {
                    Name = "MC_MoveRelative", Category = "Motion",
                    Description = "Move by relative distance" },
                new InstructionInfo {
                    Name = "MC_MoveVelocity", Category = "Motion",
                    Description = "Move at constant velocity (jog/speed control)" },
                new InstructionInfo {
                    Name = "MC_MoveJog", Category = "Motion",
                    Description = "Manual jog forward/backward" },
                new InstructionInfo {
                    Name = "MC_Halt", Category = "Motion",
                    Description = "Decelerate axis to standstill (controlled stop)" },
                new InstructionInfo {
                    Name = "MC_Stop", Category = "Motion",
                    Description = "Emergency stop (high-priority deceleration, blocks new commands)" },
                new InstructionInfo {
                    Name = "MC_Reset", Category = "Motion",
                    Description = "Reset axis errors" },
                new InstructionInfo {
                    Name = "MC_GearIn", Category = "Motion",
                    Description = "Engage electronic gear (master-slave ratio)" },
                new InstructionInfo {
                    Name = "MC_GearInPos", Category = "Motion",
                    Description = "Engage electronic gear at specific position" },
                new InstructionInfo {
                    Name = "MC_CamIn", Category = "Motion",
                    Description = "Engage electronic cam profile" },
                new InstructionInfo {
                    Name = "MC_MoveSuperImposed", Category = "Motion",
                    Description = "Add a superimposed motion to an active movement" },

                // ────────── MODBUS ──────────
                new InstructionInfo {
                    Name = "MB_CLIENT", Category = "Modbus",
                    Description = "Modbus TCP client — read/write Modbus server",
                    Inputs = "REQ, DISCONNECT, CONNECT_ID, IPV4_SERVER, MB_MODE, MB_DATA_ADDR, MB_DATA_LEN, MB_DATA_PTR",
                    SclExample = "_mbClient : MB_CLIENT;\n_mbClient(REQ := iRead, MB_MODE := 0, MB_DATA_ADDR := 40001, ...);"
                },
                new InstructionInfo {
                    Name = "MB_SERVER", Category = "Modbus",
                    Description = "Modbus TCP server — accept Modbus client requests" },
                new InstructionInfo {
                    Name = "Modbus_Master", Category = "Modbus",
                    Description = "Modbus RTU master (serial CM PtP)" },
                new InstructionInfo {
                    Name = "Modbus_Slave", Category = "Modbus",
                    Description = "Modbus RTU slave (serial CM PtP)" },

                // ────────── OPEN COMMUNICATION ──────────
                new InstructionInfo {
                    Name = "TSEND_C", Category = "OpenComm",
                    Description = "TCP/UDP send with automatic connect (TCON+TSEND)",
                    Inputs = "REQ, CONT (continuous connection), CONNECT (connection params), DATA, LEN",
                    Tags = new[] { "TCP", "UDP", "send" }
                },
                new InstructionInfo {
                    Name = "TRCV_C", Category = "OpenComm",
                    Description = "TCP/UDP receive with auto-connect" },
                new InstructionInfo {
                    Name = "TCON", Category = "OpenComm",
                    Description = "Establish TCP/UDP connection" },
                new InstructionInfo {
                    Name = "TDISCON", Category = "OpenComm",
                    Description = "Close connection" },
                new InstructionInfo {
                    Name = "TSEND", Category = "OpenComm",
                    Description = "Send data on established TCP connection" },
                new InstructionInfo {
                    Name = "TRCV", Category = "OpenComm",
                    Description = "Receive data on established TCP connection" },
                new InstructionInfo {
                    Name = "TUSEND", Category = "OpenComm",
                    Description = "UDP send" },
                new InstructionInfo {
                    Name = "TURCV", Category = "OpenComm",
                    Description = "UDP receive" },

                // ────────── S7 COMMUNICATION ──────────
                new InstructionInfo {
                    Name = "GET", Category = "S7Comm",
                    Description = "Read data from a remote S7 PLC (acyclic)",
                    SclExample = "_get : GET;\n_get(REQ := iRead, ID := 16#1, ADDR_1 := P#DB1.DBX0.0 BYTE 100, ...);"
                },
                new InstructionInfo {
                    Name = "PUT", Category = "S7Comm",
                    Description = "Write data to a remote S7 PLC (acyclic)" },
                new InstructionInfo {
                    Name = "BSEND", Category = "S7Comm",
                    Description = "Block-oriented send (large data with handshake)" },
                new InstructionInfo {
                    Name = "BRCV", Category = "S7Comm",
                    Description = "Block-oriented receive" },
                new InstructionInfo {
                    Name = "USEND", Category = "S7Comm",
                    Description = "Unacknowledged send (S7-300/400 — fast, no handshake)" },
                new InstructionInfo {
                    Name = "URCV", Category = "S7Comm",
                    Description = "Unacknowledged receive" },

                // ────────── EDGE DETECTION ──────────
                new InstructionInfo {
                    Name = "R_TRIG", Category = "Edge",
                    Description = "Rising edge detector",
                    SclExample = "_rtrig : R_TRIG;\n_rtrig(CLK := iSignal);\nIF _rtrig.Q THEN ... END_IF;"
                },
                new InstructionInfo {
                    Name = "F_TRIG", Category = "Edge",
                    Description = "Falling edge detector" },

                // ────────── DATA BLOCK ACCESS ──────────
                new InstructionInfo {
                    Name = "READ_DBL", Category = "DBAccess",
                    Description = "Read from load memory DB into work memory" },
                new InstructionInfo {
                    Name = "WRIT_DBL", Category = "DBAccess",
                    Description = "Write from work memory DB to load memory" },

                // ────────── SYSTEM ──────────
                new InstructionInfo {
                    Name = "RD_SYS_T", Category = "System",
                    Description = "Read CPU system time (UTC)" },
                new InstructionInfo {
                    Name = "WR_SYS_T", Category = "System",
                    Description = "Write CPU system time" },
                new InstructionInfo {
                    Name = "RD_LOC_T", Category = "System",
                    Description = "Read local time" },
                new InstructionInfo {
                    Name = "T_DIFF", Category = "System",
                    Description = "Time difference between two DTL" },
                new InstructionInfo {
                    Name = "T_ADD", Category = "System",
                    Description = "Add Time to DTL" },
                new InstructionInfo {
                    Name = "T_SUB", Category = "System",
                    Description = "Subtract Time from DTL" },
            };
        }

        #endregion

        public class InstructionInfo
        {
            public string Name { get; set; } = "";
            public string Category { get; set; } = "";
            public string? Family { get; set; }
            public string Description { get; set; } = "";
            public string? Inputs { get; set; }
            public string? Outputs { get; set; }
            public string? SclExample { get; set; }
            public string[]? Tags { get; set; }
        }
    }
}
