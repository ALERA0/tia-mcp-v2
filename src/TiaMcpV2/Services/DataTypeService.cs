using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TiaMcpV2.Services
{
    /// <summary>
    /// Complete data type reference for Siemens TIA Portal:
    /// - Elementary types (Bool, Byte, Word, Int, Real, Time, etc.)
    /// - Complex types (Array, Struct, String)
    /// - User-Defined Types (UDT)
    /// - System data types (IEC timers, counters, motion TOs, comm structures)
    /// - Generic types (Variant, Any)
    /// </summary>
    public class DataTypeService
    {
        private readonly ILogger<DataTypeService>? _logger;
        private static readonly List<DataTypeInfo> _dataTypes;

        static DataTypeService()
        {
            _dataTypes = BuildDataTypeDatabase();
        }

        public DataTypeService(ILogger<DataTypeService>? logger = null)
        {
            _logger = logger;
        }

        public List<DataTypeInfo> GetAllDataTypes() => _dataTypes;

        public List<DataTypeInfo> GetByCategory(string category)
        {
            return _dataTypes.Where(dt => dt.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public DataTypeInfo? GetByName(string name)
        {
            return _dataTypes.FirstOrDefault(dt =>
                dt.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                (dt.Aliases != null && dt.Aliases.Contains(name, StringComparer.OrdinalIgnoreCase)));
        }

        public List<DataTypeInfo> Search(string query)
        {
            if (string.IsNullOrEmpty(query)) return _dataTypes;
            var q = query.ToUpperInvariant();
            return _dataTypes.Where(dt =>
                dt.Name.ToUpperInvariant().Contains(q) ||
                dt.Description.ToUpperInvariant().Contains(q) ||
                dt.Category.ToUpperInvariant().Contains(q) ||
                (dt.Tags?.Any(t => t.ToUpperInvariant().Contains(q)) == true)
            ).ToList();
        }

        public Dictionary<string, object?> GetConversionInfo(string fromType, string toType)
        {
            var from = GetByName(fromType);
            var to = GetByName(toType);
            if (from == null || to == null)
                return new Dictionary<string, object?> { ["Error"] = "Type not found" };

            var fromBits = from.SizeBits;
            var toBits = to.SizeBits;
            var info = new Dictionary<string, object?>
            {
                ["FromType"] = from.Name,
                ["ToType"] = to.Name,
                ["FromBits"] = fromBits,
                ["ToBits"] = toBits
            };

            // Determine conversion
            if (from.Name == to.Name)
            {
                info["Method"] = "No conversion needed (same type)";
            }
            else if (from.Category == "Integer" && to.Category == "Integer")
            {
                info["Method"] = $"{from.Name}_TO_{to.Name}";
                info["Risk"] = fromBits > toBits ? "OVERFLOW POSSIBLE — value may be truncated" : "Safe (widening)";
            }
            else if (from.Category == "Integer" && to.Category == "Float")
            {
                info["Method"] = $"{from.Name}_TO_{to.Name}";
                info["Risk"] = "Safe — integer fits within float mantissa";
            }
            else if (from.Category == "Float" && to.Category == "Integer")
            {
                info["Method"] = $"REAL_TO_{to.Name} (truncates) or ROUND() then convert";
                info["Risk"] = "TRUNCATION/OVERFLOW — use TRUNC for truncate or ROUND for nearest";
                info["Recommended"] = $"REAL_TO_{to.Name}(ROUND(value))";
            }
            else if (from.Category == "Bit String" && to.Category == "Bit String")
            {
                info["Method"] = $"Direct cast or {from.Name}_TO_{to.Name}";
                info["Risk"] = fromBits > toBits ? "Truncates lower bits" : "Pads with zeros";
            }
            else
            {
                info["Method"] = $"{from.Name}_TO_{to.Name} (if available) or use Variant";
                info["Risk"] = "Check compatibility — may require manual conversion";
            }

            return info;
        }

        #region Database

        private static List<DataTypeInfo> BuildDataTypeDatabase()
        {
            return new List<DataTypeInfo>
            {
                // ────────── BIT/BIT STRING ──────────
                new DataTypeInfo {
                    Name = "Bool", Category = "Bit", SizeBits = 1, SizeBytes = 1,
                    Range = "FALSE / TRUE", DefaultValue = "FALSE",
                    Description = "Single bit boolean value",
                    Example = "bMotorRunning : Bool := TRUE;",
                    Tags = new[] { "bool", "boolean", "bit" }
                },
                new DataTypeInfo {
                    Name = "Byte", Category = "Bit String", SizeBits = 8, SizeBytes = 1,
                    Range = "16#00 .. 16#FF (0 .. 255)", DefaultValue = "16#00",
                    Description = "8-bit unsigned bit string",
                    Example = "byStatus : Byte := 16#7F;"
                },
                new DataTypeInfo {
                    Name = "Word", Category = "Bit String", SizeBits = 16, SizeBytes = 2,
                    Range = "16#0000 .. 16#FFFF", DefaultValue = "16#0",
                    Description = "16-bit unsigned bit string",
                    Example = "wControl : Word := 16#003F;"
                },
                new DataTypeInfo {
                    Name = "DWord", Category = "Bit String", SizeBits = 32, SizeBytes = 4,
                    Range = "16#0000_0000 .. 16#FFFF_FFFF", DefaultValue = "16#0",
                    Description = "32-bit unsigned bit string",
                    Example = "dwFlags : DWord := 16#0000_0001;"
                },
                new DataTypeInfo {
                    Name = "LWord", Category = "Bit String", SizeBits = 64, SizeBytes = 8,
                    Range = "16#0 .. 16#FFFF_FFFF_FFFF_FFFF", DefaultValue = "16#0",
                    Description = "64-bit unsigned bit string (S7-1500 only)",
                    Tags = new[] { "S7-1500" }
                },

                // ────────── INTEGERS ──────────
                new DataTypeInfo {
                    Name = "SInt", Category = "Integer", SizeBits = 8, SizeBytes = 1,
                    Range = "-128 .. 127", DefaultValue = "0",
                    Description = "Short signed integer (8-bit)",
                    Example = "siCount : SInt := -1;"
                },
                new DataTypeInfo {
                    Name = "USInt", Category = "Integer", SizeBits = 8, SizeBytes = 1,
                    Range = "0 .. 255", DefaultValue = "0",
                    Description = "Unsigned short integer (8-bit)"
                },
                new DataTypeInfo {
                    Name = "Int", Category = "Integer", SizeBits = 16, SizeBytes = 2,
                    Range = "-32_768 .. 32_767", DefaultValue = "0",
                    Description = "Signed 16-bit integer — most common",
                    Example = "iSetpoint : Int := 100;",
                    Aliases = new[] { "INT" }
                },
                new DataTypeInfo {
                    Name = "UInt", Category = "Integer", SizeBits = 16, SizeBytes = 2,
                    Range = "0 .. 65_535", DefaultValue = "0",
                    Description = "Unsigned 16-bit integer"
                },
                new DataTypeInfo {
                    Name = "DInt", Category = "Integer", SizeBits = 32, SizeBytes = 4,
                    Range = "-2_147_483_648 .. 2_147_483_647", DefaultValue = "0",
                    Description = "Signed 32-bit integer (recommended for S7-1500 counters/loops)",
                    Example = "diTotalCount : DInt := 0;"
                },
                new DataTypeInfo {
                    Name = "UDInt", Category = "Integer", SizeBits = 32, SizeBytes = 4,
                    Range = "0 .. 4_294_967_295", DefaultValue = "0",
                    Description = "Unsigned 32-bit integer"
                },
                new DataTypeInfo {
                    Name = "LInt", Category = "Integer", SizeBits = 64, SizeBytes = 8,
                    Range = "-9.2E18 .. 9.2E18", DefaultValue = "0",
                    Description = "Signed 64-bit integer (S7-1500 only)",
                    Tags = new[] { "S7-1500" }
                },
                new DataTypeInfo {
                    Name = "ULInt", Category = "Integer", SizeBits = 64, SizeBytes = 8,
                    Range = "0 .. 1.84E19", DefaultValue = "0",
                    Description = "Unsigned 64-bit integer (S7-1500 only)",
                    Tags = new[] { "S7-1500" }
                },

                // ────────── FLOATING POINT ──────────
                new DataTypeInfo {
                    Name = "Real", Category = "Float", SizeBits = 32, SizeBytes = 4,
                    Range = "±1.18E-38 .. ±3.40E+38 (~7 significant digits)", DefaultValue = "0.0",
                    Description = "32-bit IEEE 754 single precision floating point",
                    Example = "rTemperature : Real := 25.5;"
                },
                new DataTypeInfo {
                    Name = "LReal", Category = "Float", SizeBits = 64, SizeBytes = 8,
                    Range = "±2.23E-308 .. ±1.79E+308 (~15 significant digits)", DefaultValue = "0.0",
                    Description = "64-bit IEEE 754 double precision (S7-1500 only)",
                    Example = "lrPrecisionValue : LReal := 3.14159265358979;",
                    Tags = new[] { "S7-1500" }
                },

                // ────────── TIMERS ──────────
                new DataTypeInfo {
                    Name = "Time", Category = "Time", SizeBits = 32, SizeBytes = 4,
                    Range = "T#-24d_20h_31m_23s_648ms .. T#24d_20h_31m_23s_647ms", DefaultValue = "T#0ms",
                    Description = "Signed time duration in ms",
                    Example = "tDelay : Time := T#5s;"
                },
                new DataTypeInfo {
                    Name = "LTime", Category = "Time", SizeBits = 64, SizeBytes = 8,
                    Range = "Wider range than Time", DefaultValue = "LT#0ns",
                    Description = "Long time duration with nanosecond resolution (S7-1500)"
                },
                new DataTypeInfo {
                    Name = "Time_Of_Day", Category = "Time", SizeBits = 32, SizeBytes = 4,
                    Range = "TOD#0:0:0.000 .. TOD#23:59:59.999", DefaultValue = "TOD#0:0:0",
                    Description = "Time of day in milliseconds",
                    Aliases = new[] { "TOD" }
                },
                new DataTypeInfo {
                    Name = "Date", Category = "Time", SizeBits = 16, SizeBytes = 2,
                    Range = "D#1990-01-01 .. D#2168-12-31", DefaultValue = "D#1990-01-01",
                    Description = "Date in days since 1990-01-01"
                },
                new DataTypeInfo {
                    Name = "DTL", Category = "Time", SizeBits = 96, SizeBytes = 12,
                    Range = "DTL#1970-01-01-00:00:00.0 .. DTL#2554-12-31-23:59:59.999_999_999", DefaultValue = "DTL#1970-01-01-0:0:0",
                    Description = "Date and Time Long — 12 bytes structured (year, month, day, hour, min, sec, nsec)",
                    Example = "dtTimestamp : DTL := DTL#2026-01-01-08:00:00.0;",
                    Aliases = new[] { "Date_And_Time_Long" }
                },

                // ────────── CHARACTERS / STRINGS ──────────
                new DataTypeInfo {
                    Name = "Char", Category = "Char", SizeBits = 8, SizeBytes = 1,
                    Range = "ASCII 0..255", DefaultValue = "' '",
                    Description = "Single ASCII character",
                    Example = "cKey : Char := 'A';"
                },
                new DataTypeInfo {
                    Name = "WChar", Category = "Char", SizeBits = 16, SizeBytes = 2,
                    Range = "Unicode UTF-16", DefaultValue = "WCHAR#16#0020",
                    Description = "Single Unicode character (S7-1500)"
                },
                new DataTypeInfo {
                    Name = "String", Category = "String", SizeBits = -1, SizeBytes = -1,
                    Range = "Up to 254 ASCII characters (default)", DefaultValue = "''",
                    Description = "Variable-length ASCII string. String[N] declares max length.",
                    Example = "sName : String[30] := 'Tank_1';"
                },
                new DataTypeInfo {
                    Name = "WString", Category = "String", SizeBits = -1, SizeBytes = -1,
                    Range = "Up to 16382 Unicode characters", DefaultValue = "WSTRING#''",
                    Description = "Variable-length Unicode string (S7-1500 only)",
                    Tags = new[] { "S7-1500", "Unicode" }
                },

                // ────────── COMPLEX TYPES ──────────
                new DataTypeInfo {
                    Name = "Array", Category = "Complex",
                    Description = "Multi-dimensional array of any data type. Syntax: Array [low..high] of TYPE",
                    Example = "aRecipes : Array[1..10] of Real;\naMatrix : Array[0..9, 0..9] of Int;",
                    Tags = new[] { "complex", "array" }
                },
                new DataTypeInfo {
                    Name = "Struct", Category = "Complex",
                    Description = "Composite data structure with named fields. Use UDT for reusable structs.",
                    Example = "Motor : Struct\n  Speed : Real;\n  Running : Bool;\nEND_STRUCT;",
                    Tags = new[] { "complex", "struct" }
                },

                // ────────── GENERIC ──────────
                new DataTypeInfo {
                    Name = "Variant", Category = "Generic", SizeBits = -1, SizeBytes = -1,
                    Description = "Tagged variant — type-safe pointer to any data. RECOMMENDED over Any. Use TypeOf, CountOfElements, VariantGet, VariantPut.",
                    Example = "VAR_INPUT iData : Variant; END_VAR\n  IF TypeOfElements(iData) = TYPE_OF(Real) THEN ...",
                    Tags = new[] { "generic", "S7-1500", "type-safe" }
                },
                new DataTypeInfo {
                    Name = "Any", Category = "Generic", SizeBits = 80, SizeBytes = 10,
                    Description = "Legacy generic pointer (10 bytes). Use Variant in S7-1500 instead.",
                    Tags = new[] { "generic", "legacy", "S7-300/400" }
                },
                new DataTypeInfo {
                    Name = "Pointer", Category = "Generic", SizeBits = 48, SizeBytes = 6,
                    Description = "Cross-area pointer (legacy). Use Variant in S7-1500.",
                    Tags = new[] { "generic", "legacy" }
                },

                // ────────── SYSTEM DATA TYPES — Timers/Counters ──────────
                new DataTypeInfo {
                    Name = "IEC_TIMER", Category = "System",
                    Description = "IEC 61131-3 timer base type. Used by TON, TOF, TP, TONR.",
                    Example = "_timer1 : IEC_TIMER;\n_timer1.PT := T#5s;"
                },
                new DataTypeInfo {
                    Name = "TON_TIME", Category = "System",
                    Description = "On-delay timer instance (microsecond resolution)",
                    Example = "_TON1 : TON_TIME;"
                },
                new DataTypeInfo {
                    Name = "TOF_TIME", Category = "System",
                    Description = "Off-delay timer instance"
                },
                new DataTypeInfo {
                    Name = "TP_TIME", Category = "System",
                    Description = "Pulse timer instance"
                },
                new DataTypeInfo {
                    Name = "IEC_COUNTER", Category = "System",
                    Description = "IEC counter base type"
                },
                new DataTypeInfo {
                    Name = "CTU", Category = "System",
                    Description = "Up counter instance"
                },
                new DataTypeInfo {
                    Name = "CTD", Category = "System",
                    Description = "Down counter instance"
                },
                new DataTypeInfo {
                    Name = "CTUD", Category = "System",
                    Description = "Up/Down counter instance"
                },

                // ────────── SYSTEM DATA TYPES — Motion ──────────
                new DataTypeInfo {
                    Name = "TO_PositioningAxis", Category = "System-Motion",
                    Description = "Position-controlled axis Technology Object",
                    Tags = new[] { "motion", "axis" }
                },
                new DataTypeInfo {
                    Name = "TO_SpeedAxis", Category = "System-Motion",
                    Description = "Speed-controlled axis (no positioning)",
                    Tags = new[] { "motion", "axis" }
                },
                new DataTypeInfo {
                    Name = "TO_SynchronousAxis", Category = "System-Motion",
                    Description = "Synchronous axis (electronic gear/cam)",
                    Tags = new[] { "motion", "axis", "synchronization" }
                },
                new DataTypeInfo {
                    Name = "TO_ExternalEncoder", Category = "System-Motion",
                    Description = "External encoder for position feedback"
                },
                new DataTypeInfo {
                    Name = "TO_Cam", Category = "System-Motion",
                    Description = "Electronic cam profile"
                },
                new DataTypeInfo {
                    Name = "TO_CamTrack", Category = "System-Motion",
                    Description = "Cam track with multiple output cams"
                },
                new DataTypeInfo {
                    Name = "TO_OutputCam", Category = "System-Motion",
                    Description = "Position-dependent digital output cam"
                },
                new DataTypeInfo {
                    Name = "TO_MeasuringInput", Category = "System-Motion",
                    Description = "High-speed position latch on hardware event"
                },

                // ────────── SYSTEM DATA TYPES — Communication ──────────
                new DataTypeInfo {
                    Name = "TCON_Param", Category = "System-Comm",
                    Description = "Connection parameters for TCP/IP / ISO-on-TCP / UDP",
                    Tags = new[] { "TCP", "communication" }
                },
                new DataTypeInfo {
                    Name = "TADDR_Param", Category = "System-Comm",
                    Description = "UDP address parameters"
                },
                new DataTypeInfo {
                    Name = "TCON_IP_v4", Category = "System-Comm",
                    Description = "IPv4 connection parameters for TCP/IP communication",
                    Example = "VAR _conn : TCON_IP_v4;\n  _conn.RemoteAddress.ADDR[1] := 192;\n  _conn.RemotePort := 2000;"
                },
                new DataTypeInfo {
                    Name = "TCON_PARAM", Category = "System-Comm",
                    Description = "Generic connection parameters block"
                },
                new DataTypeInfo {
                    Name = "MB_CLIENT", Category = "System-Comm",
                    Description = "Modbus TCP client instance",
                    Tags = new[] { "Modbus", "client" }
                },
                new DataTypeInfo {
                    Name = "MB_SERVER", Category = "System-Comm",
                    Description = "Modbus TCP server instance"
                },

                // ────────── SYSTEM DATA TYPES — PID ──────────
                new DataTypeInfo {
                    Name = "PID_Compact", Category = "System-PID",
                    Description = "PID controller with auto-tuning (continuous output 0-100%)",
                    Example = "_PID1 : PID_Compact;\n_PID1(Setpoint:=50.0, Input:=actualValue, Output=>controlValue);",
                    Tags = new[] { "PID", "control" }
                },
                new DataTypeInfo {
                    Name = "PID_3Step", Category = "System-PID",
                    Description = "PID 3-step for motorized valves (open/close pulses)"
                },
                new DataTypeInfo {
                    Name = "PID_Temp", Category = "System-PID",
                    Description = "PID temperature controller (heating/cooling)"
                },
            };
        }

        #endregion

        public class DataTypeInfo
        {
            public string Name { get; set; } = "";
            public string Category { get; set; } = "";
            public int SizeBits { get; set; }
            public int SizeBytes { get; set; }
            public string? Range { get; set; }
            public string? DefaultValue { get; set; }
            public string Description { get; set; } = "";
            public string? Example { get; set; }
            public string[]? Tags { get; set; }
            public string[]? Aliases { get; set; }
        }
    }
}
