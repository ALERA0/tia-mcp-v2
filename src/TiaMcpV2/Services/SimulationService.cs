using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace TiaMcpV2.Services
{
    /// <summary>
    /// PLCSIM / PLCSIM Advanced / SIMIT simulation guidance and helpers.
    /// MCP can:
    /// - Provide setup guidance for each simulation tool
    /// - Generate mock data DBs for testing
    /// - Generate test stubs for FBs
    /// (Direct connection to PLCSIM Advanced requires its own API, separate from Openness)
    /// </summary>
    public class SimulationService
    {
        private readonly BlockAutonomyService _blockAutonomy;
        private readonly ILogger<SimulationService>? _logger;

        public SimulationService(BlockAutonomyService blockAutonomy, ILogger<SimulationService>? logger = null)
        {
            _blockAutonomy = blockAutonomy;
            _logger = logger;
        }

        public List<Dictionary<string, object?>> GetSimulationToolReference()
        {
            return new()
            {
                new() {
                    ["Tool"] = "PLCSIM (S7-PLCSIM V18+)",
                    ["Type"] = "Logic simulation (no hardware)",
                    ["UseFor"] = "Test program logic without HW. CPU 1500 supported.",
                    ["Limitations"] = new[] { "Limited motion simulation", "No PROFINET I/O", "No real-time" },
                    ["Setup"] = "TIA Portal → Online → Start simulation"
                },
                new() {
                    ["Tool"] = "PLCSIM Advanced",
                    ["Type"] = "Virtual CPU on PROFINET",
                    ["UseFor"] = "Real-time simulation; HMI/SCADA/3rd-party can connect via PROFINET; multi-instance support; OPC UA available",
                    ["Features"] = new[]
                    {
                        "Multiple virtual CPUs simultaneously",
                        "Synchronizable with SIMIT for full process simulation",
                        "Time scaling (slow down / speed up)",
                        "Snapshot save/restore",
                        "API for automated test"
                    },
                    ["Setup"] = "Install PLCSIM Advanced license; create instance in PLCSIM Adv UI; download PLC project to it"
                },
                new() {
                    ["Tool"] = "SIMIT",
                    ["Type"] = "Comprehensive process and mechanical simulation",
                    ["UseFor"] = "Full plant simulation, virtual commissioning, training",
                    ["Features"] = new[]
                    {
                        "Process simulation (motors, valves, tanks, conveyors)",
                        "Mechanical models",
                        "Connect to real PLC or PLCSIM Advanced via PROFINET",
                        "VR / 3D visualization (with NX MCD)",
                        "Operator training scenarios"
                    },
                    ["Setup"] = "SIMIT separate installation, integrate with TIA via PROFINET sim network"
                },
                new() {
                    ["Tool"] = "TIA Test Suite",
                    ["Type"] = "Unit and integration test framework",
                    ["UseFor"] = "Automated testing of PLC blocks (industrial CI/CD)",
                    ["Features"] = new[]
                    {
                        "Unit tests for individual FBs/FCs",
                        "Test cases via JSON/CSV",
                        "Coverage analysis",
                        "Integration with PLCSIM Advanced"
                    }
                }
            };
        }

        /// <summary>
        /// Generate a Mock Data DB — pre-defined test stimulation values for simulation.
        /// </summary>
        public string GenerateMockDataDb(string dbName, List<MockDataEntry> entries)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($@"DATA_BLOCK ""{dbName}""
TITLE = 'Mock Data — Simulation Stimulus'
{{ S7_Optimized_Access := 'TRUE' }}
AUTHOR : 'TiaMcpV2'
FAMILY : 'Simulation'
VERSION : 1.0
NON_RETAIN
//Mock data DB — overrides real I/O during simulation
//Use SimEnable flag in your FBs to switch between real and mock data");

            sb.AppendLine();
            sb.AppendLine("   VAR");
            sb.AppendLine("      EnableSimulation : Bool;");
            sb.AppendLine("      Scenario : Int := 1;     // Switch between predefined scenarios");
            sb.AppendLine();
            foreach (var e in entries)
            {
                var safeName = e.Name?.Replace(" ", "_") ?? "Var";
                sb.Append($"      {safeName} : {e.DataType}");
                if (!string.IsNullOrEmpty(e.InitialValue))
                    sb.Append($" := {e.InitialValue}");
                sb.AppendLine($";   // {e.Comment ?? ""}");
            }
            sb.AppendLine("   END_VAR");
            sb.AppendLine();
            sb.AppendLine("BEGIN");
            sb.AppendLine("END_DATA_BLOCK");
            return sb.ToString();
        }

        /// <summary>
        /// Generate a unit test stub FB for a given target block.
        /// </summary>
        public string GenerateUnitTestFb(string testName, string targetBlockName, List<TestCase> testCases)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($@"FUNCTION_BLOCK ""{testName}""
TITLE = 'Unit Test for {targetBlockName}'
{{ S7_Optimized_Access := 'TRUE' }}
AUTHOR : 'TiaMcpV2'
FAMILY : 'Test'
VERSION : 1.0
//Unit test driver — apply each test case sequentially and verify outputs");

            sb.AppendLine();
            sb.AppendLine("   VAR_INPUT");
            sb.AppendLine("      iRunTests : Bool;");
            sb.AppendLine("      iTestCaseSelect : Int := 0;  // 0 = all, 1..N = single");
            sb.AppendLine("   END_VAR");
            sb.AppendLine();
            sb.AppendLine("   VAR_OUTPUT");
            sb.AppendLine("      oTestsRunning : Bool;");
            sb.AppendLine("      oCurrentTest : Int;");
            sb.AppendLine("      oTestsPassed : Int;");
            sb.AppendLine("      oTestsFailed : Int;");
            sb.AppendLine("      oTestsTotal : Int;");
            sb.AppendLine("      oLastFailedTest : String[60];");
            sb.AppendLine("      oAllPassed : Bool;");
            sb.AppendLine("   END_VAR");
            sb.AppendLine();
            sb.AppendLine("   VAR");
            sb.AppendLine($"      _target : \"{targetBlockName}\";   // Instance of unit under test");
            sb.AppendLine("      _state : Int;");
            sb.AppendLine("      _stepTimer : DInt;");
            sb.AppendLine("      _runReqPrev : Bool;");
            sb.AppendLine("   END_VAR");
            sb.AppendLine();
            sb.AppendLine("   VAR CONSTANT");
            sb.AppendLine($"      _TEST_COUNT : Int := {testCases.Count};");
            sb.AppendLine("   END_VAR");
            sb.AppendLine();
            sb.AppendLine("BEGIN");
            sb.AppendLine();
            sb.AppendLine("REGION Trigger detection");
            sb.AppendLine("    IF iRunTests AND NOT _runReqPrev THEN");
            sb.AppendLine("        _state := 1;");
            sb.AppendLine("        oTestsPassed := 0;");
            sb.AppendLine("        oTestsFailed := 0;");
            sb.AppendLine("        oTestsTotal := _TEST_COUNT;");
            sb.AppendLine("    END_IF;");
            sb.AppendLine("    _runReqPrev := iRunTests;");
            sb.AppendLine("END_REGION");
            sb.AppendLine();
            sb.AppendLine("REGION Test execution");
            sb.AppendLine("    CASE _state OF");
            for (int i = 0; i < testCases.Count; i++)
            {
                var tc = testCases[i];
                sb.AppendLine($"        {i + 1}:  // Test {i + 1}: {tc.Name}");
                sb.AppendLine($"            oCurrentTest := {i + 1};");
                sb.AppendLine($"            // TODO: Apply inputs: {tc.InputDescription ?? ""}");
                sb.AppendLine($"            // _target.iX := {tc.Inputs ?? "<inputs>"};");
                sb.AppendLine($"            _target();");
                sb.AppendLine($"            // TODO: Verify outputs: {tc.ExpectedOutputs ?? ""}");
                sb.AppendLine($"            // IF _target.oResult = expected THEN oTestsPassed := oTestsPassed + 1;");
                sb.AppendLine($"            // ELSE oTestsFailed := oTestsFailed + 1; oLastFailedTest := '{tc.Name}'; END_IF;");
                sb.AppendLine($"            _state := {i + 2};");
                sb.AppendLine();
            }
            sb.AppendLine($"        {testCases.Count + 1}:  // Done");
            sb.AppendLine($"            oAllPassed := oTestsFailed = 0;");
            sb.AppendLine($"            _state := 0;");
            sb.AppendLine($"    END_CASE;");
            sb.AppendLine($"    oTestsRunning := _state > 0;");
            sb.AppendLine("END_REGION");
            sb.AppendLine();
            sb.AppendLine("END_FUNCTION_BLOCK");
            return sb.ToString();
        }

        public class MockDataEntry
        {
            public string? Name { get; set; }
            public string DataType { get; set; } = "Real";
            public string? InitialValue { get; set; }
            public string? Comment { get; set; }
        }

        public class TestCase
        {
            public string? Name { get; set; }
            public string? InputDescription { get; set; }
            public string? Inputs { get; set; }
            public string? ExpectedOutputs { get; set; }
        }
    }
}
