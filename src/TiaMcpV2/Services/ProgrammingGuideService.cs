using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TiaMcpV2.Services
{
    /// <summary>
    /// Programming guidance service — provides world-class best-practice reference for:
    /// - PLC programming languages (LAD, FBD, STL, SCL, GRAPH, CFC)
    /// - Organization Block (OB) catalog with priorities and use cases
    /// - Block type selection guidance (FB vs FC vs DB vs UDT)
    /// - Naming conventions, code style validation
    /// - PLCopen / Siemens engineering guideline checks
    /// </summary>
    public class ProgrammingGuideService
    {
        private readonly ILogger<ProgrammingGuideService>? _logger;

        public ProgrammingGuideService(ILogger<ProgrammingGuideService>? logger = null)
        {
            _logger = logger;
        }

        #region Programming Language Reference

        public List<Dictionary<string, object?>> GetLanguageReference()
        {
            return new List<Dictionary<string, object?>>
            {
                new Dictionary<string, object?>
                {
                    ["Language"] = "LAD (Ladder Diagram)",
                    ["IecName"] = "LD",
                    ["Description"] = "Electrical relay-logic style — contacts, coils, timers, counters",
                    ["BestFor"] = new[] { "Boolean logic", "Interlocks", "Maintainability by electricians", "Simple I/O sequences", "Operator-readable code" },
                    ["AvoidFor"] = new[] { "Complex math", "String operations", "Loops over arrays", "Recursive algorithms" },
                    ["Cpu"] = "All S7 CPUs (1200/1500/300/400/ET200SP)",
                    ["Tip"] = "Use for hard-wired safety logic and I/O mapping. Avoid >7 contacts in series."
                },
                new Dictionary<string, object?>
                {
                    ["Language"] = "FBD (Function Block Diagram)",
                    ["IecName"] = "FBD",
                    ["Description"] = "Logic gate based — AND/OR/XOR/NOT boxes connected with wires",
                    ["BestFor"] = new[] { "Combinatorial Boolean logic", "Process control with function blocks", "Mathematical chains" },
                    ["AvoidFor"] = new[] { "Sequential logic", "State machines" },
                    ["Cpu"] = "All S7 CPUs",
                    ["Tip"] = "FBD is more compact than LAD for AND/OR-heavy logic. Use F-FBD for safety."
                },
                new Dictionary<string, object?>
                {
                    ["Language"] = "STL (Statement List)",
                    ["IecName"] = "IL (deprecated in IEC 61131-3 v3)",
                    ["Description"] = "Accumulator-based assembly-like instruction list",
                    ["BestFor"] = new[] { "Legacy S7-300/400 migration", "Time-critical bit operations", "Memory-constrained code" },
                    ["AvoidFor"] = new[] { "New S7-1500 development", "Maintainable code", "Complex algorithms" },
                    ["Cpu"] = "S7-300/400 (full support); S7-1500 (limited support, no new development recommended)",
                    ["Tip"] = "Don't write new code in STL. Migrate STL → SCL for S7-1500 projects."
                },
                new Dictionary<string, object?>
                {
                    ["Language"] = "SCL (Structured Control Language)",
                    ["IecName"] = "ST (Structured Text)",
                    ["Description"] = "Pascal-like high-level language with IF/CASE/FOR/WHILE",
                    ["BestFor"] = new[] { "Complex algorithms", "Math/floating-point", "String manipulation", "Loops/arrays/structs", "State machines", "Modular code" },
                    ["AvoidFor"] = new[] { "Pure Boolean interlocks (use LAD)", "Hardware-near time-critical code (use STL on S7-300)" },
                    ["Cpu"] = "All S7 CPUs (1200/1500/300/400)",
                    ["Tip"] = "DEFAULT choice for new development. Use REGION blocks for organization, follow naming conventions."
                },
                new Dictionary<string, object?>
                {
                    ["Language"] = "GRAPH (Sequential Function Chart — SFC)",
                    ["IecName"] = "SFC",
                    ["Description"] = "Step-transition based sequential control with parallel/alternative branches",
                    ["BestFor"] = new[] { "Batch processes (ISA-88)", "Recipe-based control", "Complex sequences", "Step-by-step machine cycles", "Discrete operations" },
                    ["AvoidFor"] = new[] { "Continuous control (use SCL)", "Pure interlock logic" },
                    ["Cpu"] = "S7-1500 (recommended), S7-300/400 (legacy)",
                    ["Tip"] = "Use for any process with discrete steps and well-defined transitions. Supervision and Interlock conditions per step."
                },
                new Dictionary<string, object?>
                {
                    ["Language"] = "CFC (Continuous Function Chart)",
                    ["IecName"] = "CFC",
                    ["Description"] = "Free-form graphical interconnection of function blocks for continuous processes",
                    ["BestFor"] = new[] { "Process automation", "Power generation", "Water/wastewater", "Multi-loop PID control" },
                    ["AvoidFor"] = new[] { "Discrete/sequential machinery", "Time-critical motion" },
                    ["Cpu"] = "S7-1500 (CFC requires PCS 7 / process automation license)",
                    ["Tip"] = "Use only for true continuous-process applications with PCS 7 framework."
                }
            };
        }

        public Dictionary<string, object?> GetLanguageRecommendation(string applicationType)
        {
            var lower = applicationType?.ToLowerInvariant() ?? "";

            if (lower.Contains("interlock") || lower.Contains("safety logic") || lower.Contains("hardwired"))
                return new Dictionary<string, object?>
                {
                    ["Recommended"] = "LAD",
                    ["Alternative"] = "FBD",
                    ["Reason"] = "Boolean interlocks read like wiring diagrams in LAD — easier for electricians to maintain"
                };
            if (lower.Contains("motor") || lower.Contains("valve") || lower.Contains("standard library") || lower.Contains("reusable"))
                return new Dictionary<string, object?>
                {
                    ["Recommended"] = "SCL",
                    ["Alternative"] = "LAD",
                    ["Reason"] = "Reusable FB libraries are cleanest in SCL. Use template generator for motor/valve blocks."
                };
            if (lower.Contains("pid") || lower.Contains("analog") || lower.Contains("scaling") || lower.Contains("math"))
                return new Dictionary<string, object?>
                {
                    ["Recommended"] = "SCL",
                    ["Alternative"] = "FBD",
                    ["Reason"] = "Math-heavy code is most compact and readable in SCL"
                };
            if (lower.Contains("sequence") || lower.Contains("batch") || lower.Contains("step") || lower.Contains("recipe"))
                return new Dictionary<string, object?>
                {
                    ["Recommended"] = "GRAPH",
                    ["Alternative"] = "SCL state machine",
                    ["Reason"] = "Step-based processes are visualized perfectly in GRAPH with step/transition diagrams"
                };
            if (lower.Contains("legacy") || lower.Contains("migration") || lower.Contains("s7-300") || lower.Contains("s7-400"))
                return new Dictionary<string, object?>
                {
                    ["Recommended"] = "STL",
                    ["Alternative"] = "SCL",
                    ["Reason"] = "Legacy STL preserved for S7-300/400. Migrate to SCL when moving to S7-1500."
                };
            if (lower.Contains("process") || lower.Contains("continuous") || lower.Contains("pcs") || lower.Contains("power plant"))
                return new Dictionary<string, object?>
                {
                    ["Recommended"] = "CFC",
                    ["Alternative"] = "SCL with PID",
                    ["Reason"] = "Continuous processes with PCS 7 framework benefit from CFC's free-form connection"
                };

            return new Dictionary<string, object?>
            {
                ["Recommended"] = "SCL",
                ["Alternative"] = "LAD",
                ["Reason"] = "SCL is the modern default for new S7-1500 development"
            };
        }

        #endregion

        #region Organization Block (OB) Catalog

        public List<Dictionary<string, object?>> GetObCatalog()
        {
            return new List<Dictionary<string, object?>>
            {
                new Dictionary<string, object?>
                {
                    ["Name"] = "OB1 — Main",
                    ["Type"] = "Cyclic main",
                    ["Number"] = 1,
                    ["DefaultPriority"] = 1,
                    ["Trigger"] = "Cyclic — runs continuously after startup",
                    ["UseFor"] = "Main program flow, calling FBs/FCs",
                    ["Tip"] = "Keep OB1 lean — it should mainly call other blocks, not contain logic"
                },
                new Dictionary<string, object?>
                {
                    ["Name"] = "OB100 — Startup (Complete Restart)",
                    ["Type"] = "Startup",
                    ["Number"] = 100,
                    ["Trigger"] = "On warm/cold restart — runs once before OB1",
                    ["UseFor"] = "Initialization, retentive variable setup, default values, comm. init",
                    ["Tip"] = "Clear non-retentive arrays, set default config, close communication channels"
                },
                new Dictionary<string, object?>
                {
                    ["Name"] = "OB10-17 — Time-of-Day Interrupt",
                    ["Type"] = "Time-of-Day Interrupt",
                    ["Number"] = "10-17",
                    ["DefaultPriority"] = 2,
                    ["Trigger"] = "At specific date/time, daily/weekly/monthly",
                    ["UseFor"] = "Daily reports, scheduled archiving, periodic backups",
                    ["Tip"] = "Configure trigger time in CPU properties"
                },
                new Dictionary<string, object?>
                {
                    ["Name"] = "OB20-23 — Time-Delay Interrupt",
                    ["Type"] = "Delay Interrupt",
                    ["Number"] = "20-23",
                    ["DefaultPriority"] = 3,
                    ["Trigger"] = "Triggered by SRT_DINT — fires once after delay",
                    ["UseFor"] = "Delayed actions without blocking main cycle"
                },
                new Dictionary<string, object?>
                {
                    ["Name"] = "OB30-38 — Cyclic Interrupt",
                    ["Type"] = "Cyclic Interrupt",
                    ["Number"] = "30-38",
                    ["DefaultPriority"] = "8 (OB30) up to 17 (OB38)",
                    ["Trigger"] = "Periodic — configurable cycle (default OB30=100ms, OB35=100ms in S7-300)",
                    ["UseFor"] = "PID controllers, motion control, fast monitoring, deterministic timing",
                    ["Tip"] = "Use OB30 for 10-100ms PID loops. Don't overload — must finish before next call!"
                },
                new Dictionary<string, object?>
                {
                    ["Name"] = "OB40-47 — Hardware Interrupt",
                    ["Type"] = "Hardware Interrupt",
                    ["Number"] = "40-47",
                    ["DefaultPriority"] = 16,
                    ["Trigger"] = "Triggered by hardware event (DI rising/falling, counter, position)",
                    ["UseFor"] = "Fast signal capture, edge detection, high-speed events",
                    ["Tip"] = "Configure trigger source on the I/O module. Keep OB code very short."
                },
                new Dictionary<string, object?>
                {
                    ["Name"] = "OB55 — DPV1 Interrupt (Status)",
                    ["Type"] = "PROFIBUS Diagnostic",
                    ["Number"] = 55,
                    ["Trigger"] = "PROFIBUS DPV1 status alarm",
                    ["UseFor"] = "Process PROFIBUS device alarms"
                },
                new Dictionary<string, object?>
                {
                    ["Name"] = "OB80 — Time Error",
                    ["Type"] = "Async Error",
                    ["Number"] = 80,
                    ["Trigger"] = "Cycle time exceeded, time-of-day error",
                    ["UseFor"] = "Handle scan time overruns, log warnings",
                    ["Tip"] = "If absent and time error occurs, CPU goes STOP. Add at minimum a logging OB80."
                },
                new Dictionary<string, object?>
                {
                    ["Name"] = "OB82 — Diagnostic Interrupt",
                    ["Type"] = "Async Diagnostic",
                    ["Number"] = 82,
                    ["Trigger"] = "Module reports diagnostic event (wire break, short circuit, channel error)",
                    ["UseFor"] = "React to module hardware faults"
                },
                new Dictionary<string, object?>
                {
                    ["Name"] = "OB83 — Pull/Plug Module",
                    ["Type"] = "Async",
                    ["Number"] = 83,
                    ["Trigger"] = "Module pulled or plugged at runtime",
                    ["UseFor"] = "Hot-swap handling"
                },
                new Dictionary<string, object?>
                {
                    ["Name"] = "OB86 — Rack/Station Failure",
                    ["Type"] = "Async",
                    ["Number"] = 86,
                    ["Trigger"] = "DP slave or PROFINET IO device fails or returns",
                    ["UseFor"] = "Handle distributed station offline/online events",
                    ["Tip"] = "Critical for ET 200 stations — handle station failure gracefully"
                },
                new Dictionary<string, object?>
                {
                    ["Name"] = "OB121 — Programming Error",
                    ["Type"] = "Sync Error",
                    ["Number"] = 121,
                    ["Trigger"] = "Division by zero, array out of bounds, conversion error",
                    ["UseFor"] = "Handle runtime program errors without CPU stop"
                },
                new Dictionary<string, object?>
                {
                    ["Name"] = "OB122 — I/O Access Error",
                    ["Type"] = "Sync Error",
                    ["Number"] = 122,
                    ["Trigger"] = "Tried to read/write inaccessible I/O",
                    ["UseFor"] = "Handle missing I/O modules gracefully",
                    ["Tip"] = "Without OB122, I/O errors stop the CPU. Always include in production code!"
                }
            };
        }

        #endregion

        #region Block Type Decision Guide

        public Dictionary<string, object?> RecommendBlockType(string description)
        {
            var lower = description?.ToLowerInvariant() ?? "";

            // FB indicators: state, instance data, multiple instances
            if (lower.Contains("motor") || lower.Contains("valve") || lower.Contains("axis") ||
                lower.Contains("state") || lower.Contains("memory") || lower.Contains("instance") ||
                lower.Contains("retain") || lower.Contains("timer needed") || lower.Contains("counter needed"))
            {
                return new Dictionary<string, object?>
                {
                    ["BlockType"] = "FB",
                    ["NamePrefix"] = "FB_",
                    ["Reason"] = "FB has its own InstanceDB and can hold state between cycles. Required for objects that need memory (motors, valves, timers).",
                    ["Example"] = "FB_MotorControl, FB_ValveControl",
                    ["AccessMode"] = "Optimized (S7-1500)",
                    ["Tip"] = "Use Multi-Instance to nest FBs efficiently — saves DB count"
                };
            }

            // FC indicators: stateless, conversion, calculation
            if (lower.Contains("convert") || lower.Contains("scale") || lower.Contains("calculate") ||
                lower.Contains("formula") || lower.Contains("transform") || lower.Contains("stateless") ||
                lower.Contains("utility") || lower.Contains("helper"))
            {
                return new Dictionary<string, object?>
                {
                    ["BlockType"] = "FC",
                    ["NamePrefix"] = "FC_ or PascalCase verb",
                    ["Reason"] = "FC is stateless — perfect for pure calculations, conversions, and transformations.",
                    ["Example"] = "ScaleAnalog, CalculateRPM, ConvertCelsiusToFahrenheit",
                    ["Tip"] = "Return value via RET_VAL or use VAR_OUTPUT. No instance DB needed."
                };
            }

            // GlobalDB indicators: shared data, configuration, recipes
            if (lower.Contains("recipe") || lower.Contains("parameter") || lower.Contains("config") ||
                lower.Contains("shared data") || lower.Contains("global") || lower.Contains("setpoint storage"))
            {
                return new Dictionary<string, object?>
                {
                    ["BlockType"] = "GlobalDB",
                    ["NamePrefix"] = "DB_",
                    ["Reason"] = "Global DB stores data accessible from anywhere in the program — recipes, configs, shared variables.",
                    ["Example"] = "DB_RecipeData, DB_SystemConfig, DB_AlarmHistory",
                    ["AccessMode"] = "Optimized — symbolic access, performance and flexibility",
                    ["Tip"] = "Use NON_RETAIN for transient data, RETAIN only for variables that must survive power loss"
                };
            }

            // ARRAY DB
            if (lower.Contains("buffer") || lower.Contains("array") || lower.Contains("queue") ||
                lower.Contains("log") || lower.Contains("history") || lower.Contains("trend"))
            {
                return new Dictionary<string, object?>
                {
                    ["BlockType"] = "ARRAY DB",
                    ["NamePrefix"] = "DB_",
                    ["Reason"] = "ARRAY DB stores a single-dimension array of UDT — efficient for buffers, logs, queues",
                    ["Example"] = "DB_AlarmBuffer (Array of UDT_Alarm), DB_TrendBuffer",
                    ["Tip"] = "S7-1500 only. Faster index access than nested arrays in normal DB."
                };
            }

            // UDT
            if (lower.Contains("data structure") || lower.Contains("record") || lower.Contains("type") ||
                lower.Contains("composite") || lower.Contains("repeating structure"))
            {
                return new Dictionary<string, object?>
                {
                    ["BlockType"] = "UDT (User Defined Type)",
                    ["NamePrefix"] = "UDT_ or T_",
                    ["Reason"] = "UDT defines reusable data structures — when many places use the same data layout (motor data, alarm record, etc.)",
                    ["Example"] = "UDT_MotorData, T_AnalogSignal, UDT_AlarmEntry",
                    ["Tip"] = "Define once, use many times. Update the UDT and all uses follow."
                };
            }

            // Default: SCL FB
            return new Dictionary<string, object?>
            {
                ["BlockType"] = "FB",
                ["NamePrefix"] = "FB_",
                ["Reason"] = "Default recommendation: FB with InstanceDB for encapsulated, reusable functionality",
                ["Example"] = "FB_MyFunction"
            };
        }

        #endregion

        #region Best-Practice Validator (SCL)

        public Dictionary<string, object?> ValidateSclCode(string sclCode)
        {
            var issues = new List<Dictionary<string, object?>>();
            var warnings = new List<string>();
            var goodPoints = new List<string>();
            int score = 100;

            if (string.IsNullOrWhiteSpace(sclCode))
                return new Dictionary<string, object?> { ["Score"] = 0, ["Message"] = "Empty code" };

            var lines = sclCode.Split('\n');

            // Check #1: Optimized access
            if (sclCode.Contains("S7_Optimized_Access := 'TRUE'"))
                goodPoints.Add("✓ Uses Optimized Access (recommended for S7-1500)");
            else if (sclCode.Contains("FUNCTION_BLOCK") || sclCode.Contains("DATA_BLOCK"))
            {
                issues.Add(new Dictionary<string, object?>
                {
                    ["Severity"] = "Warning",
                    ["Issue"] = "Missing { S7_Optimized_Access := 'TRUE' }",
                    ["Recommendation"] = "Add this attribute for S7-1500 — gives 2-5x faster access and symbolic naming"
                });
                score -= 5;
            }

            // Check #2: Naming conventions for FB
            var fbMatch = System.Text.RegularExpressions.Regex.Match(sclCode, @"FUNCTION_BLOCK\s+""([^""]+)""");
            if (fbMatch.Success)
            {
                var name = fbMatch.Groups[1].Value;
                if (!name.StartsWith("FB_"))
                {
                    issues.Add(new Dictionary<string, object?>
                    {
                        ["Severity"] = "Warning",
                        ["Issue"] = $"FB name '{name}' should have 'FB_' prefix",
                        ["Recommendation"] = $"Rename to 'FB_{name}' per Siemens style guide"
                    });
                    score -= 3;
                }
                else
                {
                    goodPoints.Add($"✓ FB naming follows convention: {name}");
                }
            }

            // Check #3: Function naming
            var fcMatch = System.Text.RegularExpressions.Regex.Match(sclCode, @"FUNCTION\s+""([^""]+)""\s*:");
            if (fcMatch.Success)
            {
                var name = fcMatch.Groups[1].Value;
                if (name.Contains(" "))
                {
                    issues.Add(new Dictionary<string, object?>
                    {
                        ["Severity"] = "Error",
                        ["Issue"] = $"FC name '{name}' contains spaces",
                        ["Recommendation"] = "Use PascalCase without spaces"
                    });
                    score -= 5;
                }
            }

            // Check #4: REGION usage
            int regionCount = System.Text.RegularExpressions.Regex.Matches(sclCode, @"\bREGION\b").Count;
            if (regionCount >= 2)
                goodPoints.Add($"✓ Uses {regionCount} REGION blocks for organization");
            else if (lines.Length > 50 && regionCount == 0)
            {
                warnings.Add("Long block without REGION sections — add REGION/END_REGION for readability");
                score -= 2;
            }

            // Check #5: TITLE / AUTHOR / VERSION
            if (!sclCode.Contains("TITLE"))
            {
                warnings.Add("Missing TITLE — add a brief description");
                score -= 2;
            }
            if (!sclCode.Contains("VERSION"))
            {
                warnings.Add("Missing VERSION — track changes with version numbers");
                score -= 2;
            }

            // Check #6: Magic numbers
            int magicNumbers = System.Text.RegularExpressions.Regex.Matches(sclCode, @"(?<![\w_])\d{3,}(?![\w_.])").Count;
            if (magicNumbers > 5)
            {
                warnings.Add($"Found {magicNumbers} large numeric literals — consider using VAR CONSTANT for magic numbers");
                score -= 3;
            }

            // Check #7: Comments
            int commentLines = lines.Count(l => l.TrimStart().StartsWith("//") || l.Contains("(*"));
            int codeLines = lines.Count(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith("//"));
            double commentRatio = codeLines > 0 ? (double)commentLines / codeLines : 0;
            if (commentRatio < 0.05 && codeLines > 30)
            {
                warnings.Add($"Low comment density ({commentRatio:P0}) — add comments explaining WHY, not WHAT");
                score -= 3;
            }
            else if (commentRatio > 0.15)
            {
                goodPoints.Add($"✓ Good comment density ({commentRatio:P0})");
            }

            // Check #8: GOTO
            if (sclCode.Contains("GOTO "))
            {
                issues.Add(new Dictionary<string, object?>
                {
                    ["Severity"] = "Warning",
                    ["Issue"] = "Uses GOTO statement",
                    ["Recommendation"] = "Avoid GOTO — restructure with IF/CASE/EXIT/CONTINUE for clarity"
                });
                score -= 5;
            }

            // Check #9: Deep IF nesting
            int maxNesting = 0;
            int currentNesting = 0;
            foreach (var line in lines)
            {
                if (line.Contains("IF ")) currentNesting++;
                if (line.Contains("END_IF")) currentNesting = Math.Max(0, currentNesting - 1);
                maxNesting = Math.Max(maxNesting, currentNesting);
            }
            if (maxNesting > 4)
            {
                warnings.Add($"Deep IF nesting (depth {maxNesting}) — consider early-return / guard clauses or CASE statements");
                score -= 5;
            }

            // Check #10: VAR_TEMP usage for temporaries
            if (sclCode.Contains("VAR_TEMP"))
                goodPoints.Add("✓ Uses VAR_TEMP for temporary variables (memory-efficient)");

            return new Dictionary<string, object?>
            {
                ["Score"] = Math.Max(0, score),
                ["MaxScore"] = 100,
                ["Verdict"] = score >= 90 ? "Excellent" : score >= 75 ? "Good" : score >= 60 ? "Acceptable" : "Needs Improvement",
                ["IssueCount"] = issues.Count,
                ["WarningCount"] = warnings.Count,
                ["GoodPoints"] = goodPoints,
                ["Issues"] = issues,
                ["Warnings"] = warnings,
                ["LineCount"] = lines.Length
            };
        }

        #endregion

        #region GRAPH (SFC) Generator

        /// <summary>
        /// Generate a GRAPH-style FB in SCL state machine form (since true GRAPH XML is complex).
        /// This generates a deterministic step-transition state machine that mimics GRAPH behavior.
        /// </summary>
        public string GenerateGraphSequence(GraphSequenceRequest req)
        {
            var sb = new System.Text.StringBuilder();
            var name = req.BlockName ?? "FB_Sequence";

            sb.AppendLine($"FUNCTION_BLOCK \"{name}\"");
            sb.AppendLine($"TITLE = '{req.Title ?? "Sequential Process Control (GRAPH-style)"}'");
            sb.AppendLine("{ S7_Optimized_Access := 'TRUE' }");
            sb.AppendLine("AUTHOR : 'TiaMcpV2'");
            sb.AppendLine("FAMILY : 'Sequence'");
            sb.AppendLine("VERSION : 1.0");
            sb.AppendLine();
            sb.AppendLine("//Step-transition sequential control following ISA-88 / GRAPH conventions");
            sb.AppendLine("//Each step has actions (Set/Reset on entry, continuous N) and transition condition");
            sb.AppendLine();
            sb.AppendLine("   VAR_INPUT");
            sb.AppendLine("      iEnable : Bool;            // Master enable");
            sb.AppendLine("      iStart : Bool;             // Start sequence (rising edge)");
            sb.AppendLine("      iHold : Bool;              // Hold current step");
            sb.AppendLine("      iAbort : Bool;             // Abort sequence");
            sb.AppendLine("      iReset : Bool;             // Reset to step 0");
            sb.AppendLine("      iAck : Bool;               // Acknowledge fault");
            sb.AppendLine("   END_VAR");
            sb.AppendLine();
            sb.AppendLine("   VAR_OUTPUT");
            sb.AppendLine("      oCurrentStep : Int;        // Active step number");
            sb.AppendLine("      oStepName : String[30];    // Active step name (for HMI)");
            sb.AppendLine("      oStepActive : Array[0..32] of Bool;");
            sb.AppendLine("      oIsRunning : Bool;");
            sb.AppendLine("      oIsHeld : Bool;");
            sb.AppendLine("      oIsComplete : Bool;");
            sb.AppendLine("      oIsAborted : Bool;");
            sb.AppendLine("      oIsFault : Bool;");
            sb.AppendLine("      oStepTime : Time;          // Time in current step");
            sb.AppendLine("   END_VAR");
            sb.AppendLine();

            // Outputs for each step's actions
            if (req.Steps != null)
            {
                sb.AppendLine("   VAR_OUTPUT");
                foreach (var step in req.Steps)
                {
                    if (step.Outputs != null)
                    {
                        foreach (var o in step.Outputs)
                            sb.AppendLine($"      {o} : Bool;     // Step {step.StepNumber} action output");
                    }
                }
                sb.AppendLine("   END_VAR");
                sb.AppendLine();
            }

            sb.AppendLine("   VAR");
            sb.AppendLine("      _state : Int;             // Internal state");
            sb.AppendLine("      _stepStart : Time;        // Step entry time");
            sb.AppendLine("      _stepTimer : DInt;");
            sb.AppendLine("      _StartCmdPrev : Bool;");
            sb.AppendLine("      _ResetCmdPrev : Bool;");
            sb.AppendLine("      _AckCmdPrev : Bool;");
            sb.AppendLine("   END_VAR");
            sb.AppendLine();

            sb.AppendLine("   VAR CONSTANT");
            sb.AppendLine("      _STATE_INIT : Int := 0;");
            sb.AppendLine("      _STATE_FAULT : Int := -1;");
            sb.AppendLine("      _STATE_DONE : Int := 999;");
            if (req.Steps != null)
            {
                foreach (var s in req.Steps)
                    sb.AppendLine($"      _STEP_{s.StepNumber}_{(s.Name ?? "STEP").ToUpperInvariant().Replace(" ", "_")} : Int := {s.StepNumber};");
            }
            sb.AppendLine("   END_VAR");
            sb.AppendLine();

            sb.AppendLine("BEGIN");
            sb.AppendLine();
            sb.AppendLine("REGION Edge detection & global control");
            sb.AppendLine("    IF iAbort OR NOT iEnable THEN");
            sb.AppendLine("        _state := _STATE_INIT;");
            sb.AppendLine("        oIsAborted := TRUE;");
            sb.AppendLine("    ELSIF iReset AND NOT _ResetCmdPrev THEN");
            sb.AppendLine("        _state := _STATE_INIT;");
            sb.AppendLine("        oIsAborted := FALSE;");
            sb.AppendLine("        oIsFault := FALSE;");
            sb.AppendLine("    END_IF;");
            sb.AppendLine("    _ResetCmdPrev := iReset;");
            sb.AppendLine("    _AckCmdPrev := iAck;");
            sb.AppendLine("END_REGION");
            sb.AppendLine();

            sb.AppendLine("REGION Step machine");
            sb.AppendLine("    IF NOT iHold THEN  // Step transitions paused while iHold");
            sb.AppendLine("        _stepTimer := _stepTimer + 1;");
            sb.AppendLine("        CASE _state OF");
            sb.AppendLine("            _STATE_INIT:");
            sb.AppendLine("                oStepName := 'INIT';");
            sb.AppendLine("                IF iStart AND NOT _StartCmdPrev AND iEnable THEN");
            sb.AppendLine($"                    _state := 1;");
            sb.AppendLine("                    _stepTimer := 0;");
            sb.AppendLine("                END_IF;");
            sb.AppendLine();

            // Generate each step
            if (req.Steps != null)
            {
                foreach (var step in req.Steps)
                {
                    sb.AppendLine($"            {step.StepNumber}:  // Step {step.StepNumber}: {step.Name ?? ""}");
                    sb.AppendLine($"                oStepName := '{step.Name ?? $"Step{step.StepNumber}"}';");
                    sb.AppendLine($"                oCurrentStep := {step.StepNumber};");

                    // Step actions
                    if (step.Outputs != null)
                    {
                        foreach (var o in step.Outputs)
                            sb.AppendLine($"                {o} := TRUE;  // N-action (continuous while in step)");
                    }

                    // Reset all OTHER steps' outputs
                    if (req.Steps != null)
                    {
                        foreach (var otherStep in req.Steps.Where(s => s.StepNumber != step.StepNumber))
                        {
                            if (otherStep.Outputs != null)
                                foreach (var o in otherStep.Outputs)
                                    sb.AppendLine($"                {o} := FALSE;");
                        }
                    }

                    // Transition
                    var transition = step.TransitionCondition ?? "/* TODO: transition condition */";
                    var nextStep = step.NextStep > 0 ? step.NextStep : (step.StepNumber + 1);
                    sb.AppendLine($"                IF {transition} THEN");
                    sb.AppendLine($"                    _state := {nextStep};");
                    sb.AppendLine($"                    _stepTimer := 0;");
                    sb.AppendLine($"                END_IF;");

                    // Supervision (timeout)
                    if (step.TimeoutMs > 0)
                    {
                        sb.AppendLine($"                // Step supervision (timeout)");
                        sb.AppendLine($"                IF _stepTimer * 10 > {step.TimeoutMs} THEN");
                        sb.AppendLine($"                    _state := _STATE_FAULT;");
                        sb.AppendLine($"                    oIsFault := TRUE;");
                        sb.AppendLine($"                END_IF;");
                    }
                    sb.AppendLine();
                }
            }

            sb.AppendLine("            _STATE_DONE:");
            sb.AppendLine("                oStepName := 'DONE';");
            sb.AppendLine("                oIsComplete := TRUE;");
            sb.AppendLine();
            sb.AppendLine("            _STATE_FAULT:");
            sb.AppendLine("                oStepName := 'FAULT';");
            sb.AppendLine("                oIsFault := TRUE;");
            sb.AppendLine("                IF iAck AND NOT _AckCmdPrev THEN");
            sb.AppendLine("                    _state := _STATE_INIT;");
            sb.AppendLine("                    oIsFault := FALSE;");
            sb.AppendLine("                END_IF;");
            sb.AppendLine("        END_CASE;");
            sb.AppendLine("    END_IF;");
            sb.AppendLine("END_REGION");
            sb.AppendLine();

            sb.AppendLine("REGION Output flags");
            sb.AppendLine("    _StartCmdPrev := iStart;");
            sb.AppendLine("    oIsRunning := (_state > 0) AND (_state < _STATE_DONE);");
            sb.AppendLine("    oIsHeld := iHold AND oIsRunning;");
            sb.AppendLine("    oStepTime := DINT_TO_TIME(_stepTimer * 10); // 10ms cycle assumed");
            sb.AppendLine("END_REGION");
            sb.AppendLine();

            sb.AppendLine("END_FUNCTION_BLOCK");
            return sb.ToString();
        }

        public class GraphSequenceRequest
        {
            public string? BlockName { get; set; }
            public string? Title { get; set; }
            public List<GraphStep>? Steps { get; set; }
        }

        public class GraphStep
        {
            public int StepNumber { get; set; }
            public string? Name { get; set; }
            public string? Description { get; set; }
            public List<string>? Outputs { get; set; }              // Outputs to set in this step
            public string? TransitionCondition { get; set; }         // SCL expression — TRUE when transition
            public int NextStep { get; set; }                        // 0 = next sequential
            public int TimeoutMs { get; set; }                       // Step supervision timeout
        }

        #endregion
    }
}
