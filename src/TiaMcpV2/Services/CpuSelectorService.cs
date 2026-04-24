using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TiaMcpV2.Services
{
    /// <summary>
    /// Siemens CPU selection, sizing and recommendation service.
    /// Maintains an up-to-date database of S7-1200, S7-1500, ET 200SP CPUs
    /// with full technical specifications for sizing calculations.
    /// </summary>
    public class CpuSelectorService
    {
        private readonly ILogger<CpuSelectorService>? _logger;
        private static readonly List<CpuSpec> _cpuDatabase;

        static CpuSelectorService()
        {
            _cpuDatabase = BuildCpuDatabase();
        }

        public CpuSelectorService(ILogger<CpuSelectorService>? logger = null)
        {
            _logger = logger;
        }

        #region Public API

        public List<CpuSpec> GetAllCpus() => _cpuDatabase;

        public List<CpuSpec> GetByFamily(string family)
        {
            if (string.IsNullOrEmpty(family)) return _cpuDatabase;
            return _cpuDatabase.Where(c =>
                c.Family.IndexOf(family, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }

        public CpuSpec? GetByName(string cpuName)
        {
            if (string.IsNullOrEmpty(cpuName)) return null;
            return _cpuDatabase.FirstOrDefault(c =>
                c.Name.Equals(cpuName, StringComparison.OrdinalIgnoreCase) ||
                c.OrderNumber.Equals(cpuName, StringComparison.OrdinalIgnoreCase) ||
                c.Name.Replace(" ", "").Equals(cpuName.Replace(" ", ""), StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Recommend the most suitable CPU(s) for given requirements.
        /// Uses scoring based on memory margin, I/O capacity, TO count, comm load.
        /// </summary>
        public List<CpuRecommendation> Recommend(CpuRequirements req)
        {
            var recommendations = new List<CpuRecommendation>();

            foreach (var cpu in _cpuDatabase)
            {
                // Filter out CPUs that don't meet hard requirements
                if (req.RequireSafety && !cpu.IsFailsafe) continue;
                if (req.RequireMotion && !cpu.SupportsMotion) continue;
                if (req.MinPROFINETPorts > cpu.ProfinetPorts) continue;
                if (req.MinPROFIBUSPorts > cpu.ProfibusPorts) continue;

                // Estimate resource usage
                var estimate = EstimateUsage(cpu, req);

                // Score: how well does this CPU fit?
                int score = 100;
                var notes = new List<string>();

                // Memory score
                var workMemUsage = estimate.EstimatedWorkMemoryBytes / (double)cpu.WorkMemoryBytes;
                if (workMemUsage > 1.0)
                {
                    score = 0;  // Insufficient
                    notes.Add($"⚠ Work memory INSUFFICIENT ({workMemUsage * 100:F0}%)");
                }
                else if (workMemUsage > 0.85)
                {
                    score -= 30;
                    notes.Add($"⚠ Work memory tight ({workMemUsage * 100:F0}%) - consider larger CPU");
                }
                else if (workMemUsage > 0.60)
                {
                    score -= 10;
                    notes.Add($"• Work memory moderate ({workMemUsage * 100:F0}%)");
                }
                else if (workMemUsage < 0.15)
                {
                    score -= 15;
                    notes.Add($"• Work memory over-sized ({workMemUsage * 100:F0}%) - smaller CPU may suffice");
                }

                // TO capacity
                if (req.TechnologyObjectCount > cpu.MaxTechnologyObjects)
                {
                    score = 0;
                    notes.Add($"⚠ TO count ({req.TechnologyObjectCount}) exceeds max ({cpu.MaxTechnologyObjects})");
                }
                else if (cpu.MaxTechnologyObjects > 0 && req.TechnologyObjectCount > cpu.MaxTechnologyObjects * 0.8)
                {
                    score -= 15;
                    notes.Add($"⚠ TO count near limit ({req.TechnologyObjectCount}/{cpu.MaxTechnologyObjects})");
                }

                // Communication resources
                if (req.CommunicationConnections > cpu.MaxConnections)
                {
                    score -= 40;
                    notes.Add($"⚠ Connections ({req.CommunicationConnections}) exceed max ({cpu.MaxConnections})");
                }

                // I/O capacity
                var totalIo = req.DigitalInputs + req.DigitalOutputs;
                if (totalIo > cpu.MaxProcessImageInputs + cpu.MaxProcessImageOutputs)
                {
                    score -= 20;
                    notes.Add($"• Need distributed I/O for {totalIo} I/O points");
                }

                // Cycle time check
                if (req.RequiredCycleTimeMs > 0)
                {
                    var estimatedCycleTime = estimate.EstimatedCycleTimeMs;
                    if (estimatedCycleTime > req.RequiredCycleTimeMs)
                    {
                        score -= 50;
                        notes.Add($"⚠ Est. cycle time {estimatedCycleTime:F1}ms exceeds target {req.RequiredCycleTimeMs}ms");
                    }
                }

                // Family preference
                if (!string.IsNullOrEmpty(req.PreferredFamily) &&
                    cpu.Family.IndexOf(req.PreferredFamily, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    score -= 5;
                }

                // Cost weighting (prefer cheaper if fits)
                score -= (int)(cpu.RelativePrice * 2);

                if (score < 0) continue;

                recommendations.Add(new CpuRecommendation
                {
                    Cpu = cpu,
                    Score = score,
                    EstimatedUsage = estimate,
                    Notes = notes,
                    Suitability = score >= 80 ? "Excellent"
                                : score >= 60 ? "Good"
                                : score >= 40 ? "Acceptable"
                                : "Marginal"
                });
            }

            return recommendations.OrderByDescending(r => r.Score).ToList();
        }

        /// <summary>
        /// Estimate resource usage (memory, cycle time) for a CPU given requirements.
        /// </summary>
        public UsageEstimate EstimateUsage(CpuSpec cpu, CpuRequirements req)
        {
            // Memory estimates (bytes)
            int fbMemory = req.FunctionBlockCount * 8192;      // ~8KB per FB (typical)
            int fcMemory = req.FunctionCount * 2048;            // ~2KB per FC
            int obMemory = req.OrganizationBlockCount * 4096;   // ~4KB per OB
            int dbMemory = req.DataBlockCount * 4096;           // ~4KB per DB (average)
            int toMemory = req.TechnologyObjectCount * 16384;   // ~16KB per TO (axis/PID)
            int tagMemory = req.TagCount * 32;                  // ~32 bytes per tag

            int totalWork = fbMemory + fcMemory + obMemory + dbMemory + toMemory + tagMemory;
            totalWork += req.EstimatedCodeBytes; // User-provided code estimate

            // Cycle time estimate (ms)
            // Rough: (bit_count × bit_time_ns + word_count × word_time_ns) / 1_000_000
            var bitOps = req.EstimatedBitOperationsPerCycle;
            var wordOps = req.EstimatedWordOperationsPerCycle;
            var fpOps = req.EstimatedFloatOperationsPerCycle;

            // If not provided, estimate from I/O and blocks
            if (bitOps == 0)
            {
                bitOps = (req.DigitalInputs + req.DigitalOutputs) * 10
                       + req.FunctionBlockCount * 500;
            }
            if (wordOps == 0)
            {
                wordOps = req.AnalogInputs * 20 + req.AnalogOutputs * 20
                        + req.FunctionCount * 200;
            }
            if (fpOps == 0 && req.TechnologyObjectCount > 0)
            {
                fpOps = req.TechnologyObjectCount * 300;
            }

            double cycleTimeUs = (bitOps * cpu.BitInstructionNs
                                  + wordOps * cpu.WordInstructionNs
                                  + fpOps * cpu.FloatInstructionNs) / 1000.0;

            // Add overhead (OB scheduling, system tasks) ~15%
            cycleTimeUs *= 1.15;

            // Communication overhead
            cycleTimeUs += req.CommunicationConnections * 50; // ~50 µs per active connection

            return new UsageEstimate
            {
                EstimatedWorkMemoryBytes = totalWork,
                EstimatedLoadMemoryBytes = totalWork * 2,
                EstimatedCycleTimeMs = cycleTimeUs / 1000.0,
                EstimatedWorkMemoryPercent = totalWork * 100.0 / cpu.WorkMemoryBytes,
                BitOperations = bitOps,
                WordOperations = wordOps,
                FloatOperations = fpOps
            };
        }

        /// <summary>
        /// Compare two CPUs side-by-side.
        /// </summary>
        public Dictionary<string, object?> Compare(string cpu1Name, string cpu2Name)
        {
            var c1 = GetByName(cpu1Name) ?? throw new ArgumentException($"CPU not found: {cpu1Name}");
            var c2 = GetByName(cpu2Name) ?? throw new ArgumentException($"CPU not found: {cpu2Name}");

            return new Dictionary<string, object?>
            {
                ["CPU1"] = c1,
                ["CPU2"] = c2,
                ["Comparison"] = new Dictionary<string, object?>
                {
                    ["WorkMemoryRatio"] = (double)c2.WorkMemoryBytes / c1.WorkMemoryBytes,
                    ["LoadMemoryRatio"] = (double)c2.LoadMemoryBytes / c1.LoadMemoryBytes,
                    ["BitSpeedRatio"] = c1.BitInstructionNs > 0 ? c1.BitInstructionNs / c2.BitInstructionNs : 1,
                    ["ConnectionsDiff"] = c2.MaxConnections - c1.MaxConnections,
                    ["MaxTOsDiff"] = c2.MaxTechnologyObjects - c1.MaxTechnologyObjects,
                    ["PriceRatio"] = c1.RelativePrice > 0 ? c2.RelativePrice / c1.RelativePrice : 1
                }
            };
        }

        #endregion

        #region CPU Database

        private static List<CpuSpec> BuildCpuDatabase()
        {
            return new List<CpuSpec>
            {
                // ─────────── S7-1200 Family ───────────
                new CpuSpec
                {
                    Name = "CPU 1211C DC/DC/DC", Family = "S7-1200",
                    OrderNumber = "6ES7 211-1AE40-0XB0", TypeIdentifier = "OrderNumber:6ES7 211-1AE40-0XB0/V4.5",
                    WorkMemoryBytes = 50 * 1024, LoadMemoryBytes = 1 * 1024 * 1024, RetentiveMemoryBytes = 10 * 1024,
                    BitInstructionNs = 85, WordInstructionNs = 1700, FloatInstructionNs = 2300,
                    ProfinetPorts = 1, ProfibusPorts = 0, MaxConnections = 8,
                    MaxProcessImageInputs = 1024, MaxProcessImageOutputs = 1024, MaxProcessImageAi = 0, MaxProcessImageAq = 0,
                    MaxTechnologyObjects = 4, SupportsMotion = true, IsFailsafe = false,
                    IntegratedDI = 6, IntegratedDQ = 4, IntegratedAI = 2, IntegratedAQ = 0,
                    RelativePrice = 10, UseCase = "Small standalone machines, basic control"
                },
                new CpuSpec
                {
                    Name = "CPU 1212C DC/DC/DC", Family = "S7-1200",
                    OrderNumber = "6ES7 212-1AE40-0XB0", TypeIdentifier = "OrderNumber:6ES7 212-1AE40-0XB0/V4.5",
                    WorkMemoryBytes = 75 * 1024, LoadMemoryBytes = 1 * 1024 * 1024, RetentiveMemoryBytes = 10 * 1024,
                    BitInstructionNs = 85, WordInstructionNs = 1700, FloatInstructionNs = 2300,
                    ProfinetPorts = 1, ProfibusPorts = 0, MaxConnections = 8,
                    MaxProcessImageInputs = 1024, MaxProcessImageOutputs = 1024,
                    MaxTechnologyObjects = 4, SupportsMotion = true, IsFailsafe = false,
                    IntegratedDI = 8, IntegratedDQ = 6, IntegratedAI = 2, IntegratedAQ = 0,
                    RelativePrice = 12, UseCase = "Small machines with slightly more I/O"
                },
                new CpuSpec
                {
                    Name = "CPU 1214C DC/DC/DC", Family = "S7-1200",
                    OrderNumber = "6ES7 214-1AG40-0XB0", TypeIdentifier = "OrderNumber:6ES7 214-1AG40-0XB0/V4.5",
                    WorkMemoryBytes = 100 * 1024, LoadMemoryBytes = 4 * 1024 * 1024, RetentiveMemoryBytes = 10 * 1024,
                    BitInstructionNs = 85, WordInstructionNs = 1700, FloatInstructionNs = 2300,
                    ProfinetPorts = 1, ProfibusPorts = 0, MaxConnections = 8,
                    MaxProcessImageInputs = 1024, MaxProcessImageOutputs = 1024,
                    MaxTechnologyObjects = 8, SupportsMotion = true, IsFailsafe = false,
                    IntegratedDI = 14, IntegratedDQ = 10, IntegratedAI = 2, IntegratedAQ = 0,
                    RelativePrice = 15, UseCase = "Mid-size machines, most common S7-1200"
                },
                new CpuSpec
                {
                    Name = "CPU 1215C DC/DC/DC", Family = "S7-1200",
                    OrderNumber = "6ES7 215-1AG40-0XB0", TypeIdentifier = "OrderNumber:6ES7 215-1AG40-0XB0/V4.5",
                    WorkMemoryBytes = 125 * 1024, LoadMemoryBytes = 4 * 1024 * 1024, RetentiveMemoryBytes = 10 * 1024,
                    BitInstructionNs = 85, WordInstructionNs = 1700, FloatInstructionNs = 2300,
                    ProfinetPorts = 2, ProfibusPorts = 0, MaxConnections = 8,
                    MaxProcessImageInputs = 1024, MaxProcessImageOutputs = 1024,
                    MaxTechnologyObjects = 8, SupportsMotion = true, IsFailsafe = false,
                    IntegratedDI = 14, IntegratedDQ = 10, IntegratedAI = 2, IntegratedAQ = 2,
                    RelativePrice = 18, UseCase = "Mid-size machines with 2 PROFINET ports"
                },
                new CpuSpec
                {
                    Name = "CPU 1217C DC/DC/DC", Family = "S7-1200",
                    OrderNumber = "6ES7 217-1AG40-0XB0", TypeIdentifier = "OrderNumber:6ES7 217-1AG40-0XB0/V4.5",
                    WorkMemoryBytes = 150 * 1024, LoadMemoryBytes = 4 * 1024 * 1024, RetentiveMemoryBytes = 10 * 1024,
                    BitInstructionNs = 85, WordInstructionNs = 1700, FloatInstructionNs = 2300,
                    ProfinetPorts = 2, ProfibusPorts = 0, MaxConnections = 8,
                    MaxProcessImageInputs = 1024, MaxProcessImageOutputs = 1024,
                    MaxTechnologyObjects = 8, SupportsMotion = true, IsFailsafe = false,
                    IntegratedDI = 14, IntegratedDQ = 10, IntegratedAI = 2, IntegratedAQ = 2,
                    RelativePrice = 22, UseCase = "Top S7-1200 with differential inputs (100kHz PTO)"
                },

                // ─────────── S7-1500 Family ───────────
                new CpuSpec
                {
                    Name = "CPU 1511-1 PN", Family = "S7-1500",
                    OrderNumber = "6ES7 511-1AK02-0AB0", TypeIdentifier = "OrderNumber:6ES7 511-1AK02-0AB0/V2.9",
                    WorkMemoryBytes = 150 * 1024, LoadMemoryBytes = 1 * 1024 * 1024, RetentiveMemoryBytes = 88 * 1024,
                    BitInstructionNs = 60, WordInstructionNs = 72, FloatInstructionNs = 96,
                    ProfinetPorts = 1, ProfibusPorts = 0, MaxConnections = 128,
                    MaxProcessImageInputs = 32768, MaxProcessImageOutputs = 32768, MaxProcessImageAi = 0, MaxProcessImageAq = 0,
                    MaxTechnologyObjects = 800, SupportsMotion = true, IsFailsafe = false,
                    RelativePrice = 30, UseCase = "Entry-level S7-1500 for medium machines"
                },
                new CpuSpec
                {
                    Name = "CPU 1511F-1 PN", Family = "S7-1500 F",
                    OrderNumber = "6ES7 511-1FK02-0AB0", TypeIdentifier = "OrderNumber:6ES7 511-1FK02-0AB0/V2.9",
                    WorkMemoryBytes = 225 * 1024, LoadMemoryBytes = 1 * 1024 * 1024, RetentiveMemoryBytes = 128 * 1024,
                    BitInstructionNs = 60, WordInstructionNs = 72, FloatInstructionNs = 96,
                    ProfinetPorts = 1, ProfibusPorts = 0, MaxConnections = 128,
                    MaxTechnologyObjects = 800, SupportsMotion = true, IsFailsafe = true,
                    RelativePrice = 45, UseCase = "Entry-level F-CPU for safety applications (SIL 3)"
                },
                new CpuSpec
                {
                    Name = "CPU 1512C-1 PN", Family = "S7-1500 Compact",
                    OrderNumber = "6ES7 512-1CK01-0AB0", TypeIdentifier = "OrderNumber:6ES7 512-1CK01-0AB0/V2.9",
                    WorkMemoryBytes = 250 * 1024, LoadMemoryBytes = 4 * 1024 * 1024, RetentiveMemoryBytes = 88 * 1024,
                    BitInstructionNs = 48, WordInstructionNs = 58, FloatInstructionNs = 77,
                    ProfinetPorts = 1, ProfibusPorts = 0, MaxConnections = 128,
                    MaxTechnologyObjects = 800, SupportsMotion = true, IsFailsafe = false,
                    IntegratedDI = 32, IntegratedDQ = 32, IntegratedAI = 5, IntegratedAQ = 2,
                    RelativePrice = 40, UseCase = "Compact S7-1500 with integrated I/O"
                },
                new CpuSpec
                {
                    Name = "CPU 1513-1 PN", Family = "S7-1500",
                    OrderNumber = "6ES7 513-1AL02-0AB0", TypeIdentifier = "OrderNumber:6ES7 513-1AL02-0AB0/V2.9",
                    WorkMemoryBytes = 300 * 1024, LoadMemoryBytes = 1 * 1024 * 1024, RetentiveMemoryBytes = 88 * 1024,
                    BitInstructionNs = 40, WordInstructionNs = 48, FloatInstructionNs = 64,
                    ProfinetPorts = 1, ProfibusPorts = 0, MaxConnections = 128,
                    MaxTechnologyObjects = 800, SupportsMotion = true, IsFailsafe = false,
                    RelativePrice = 40, UseCase = "S7-1500 for complex single-machine applications"
                },
                new CpuSpec
                {
                    Name = "CPU 1513F-1 PN", Family = "S7-1500 F",
                    OrderNumber = "6ES7 513-1FL02-0AB0", TypeIdentifier = "OrderNumber:6ES7 513-1FL02-0AB0/V2.9",
                    WorkMemoryBytes = 450 * 1024, LoadMemoryBytes = 1 * 1024 * 1024, RetentiveMemoryBytes = 128 * 1024,
                    BitInstructionNs = 40, WordInstructionNs = 48, FloatInstructionNs = 64,
                    ProfinetPorts = 1, ProfibusPorts = 0, MaxConnections = 128,
                    MaxTechnologyObjects = 800, SupportsMotion = true, IsFailsafe = true,
                    RelativePrice = 55, UseCase = "Mid-range F-CPU with more memory"
                },
                new CpuSpec
                {
                    Name = "CPU 1515-2 PN", Family = "S7-1500",
                    OrderNumber = "6ES7 515-2AM02-0AB0", TypeIdentifier = "OrderNumber:6ES7 515-2AM02-0AB0/V2.9",
                    WorkMemoryBytes = 500 * 1024, LoadMemoryBytes = 5 * 1024 * 1024, RetentiveMemoryBytes = 153 * 1024,
                    BitInstructionNs = 30, WordInstructionNs = 36, FloatInstructionNs = 48,
                    ProfinetPorts = 2, ProfibusPorts = 0, MaxConnections = 256,
                    MaxTechnologyObjects = 1600, SupportsMotion = true, IsFailsafe = false,
                    RelativePrice = 60, UseCase = "Standard workhorse for multi-machine plants"
                },
                new CpuSpec
                {
                    Name = "CPU 1516-3 PN/DP", Family = "S7-1500",
                    OrderNumber = "6ES7 516-3AN02-0AB0", TypeIdentifier = "OrderNumber:6ES7 516-3AN02-0AB0/V2.9",
                    WorkMemoryBytes = 1500 * 1024, LoadMemoryBytes = 5 * 1024 * 1024, RetentiveMemoryBytes = 153 * 1024,
                    BitInstructionNs = 15, WordInstructionNs = 18, FloatInstructionNs = 24,
                    ProfinetPorts = 2, ProfibusPorts = 1, MaxConnections = 256,
                    MaxTechnologyObjects = 2400, SupportsMotion = true, IsFailsafe = false,
                    RelativePrice = 85, UseCase = "Large applications with PROFINET + PROFIBUS"
                },
                new CpuSpec
                {
                    Name = "CPU 1516F-3 PN/DP", Family = "S7-1500 F",
                    OrderNumber = "6ES7 516-3FN02-0AB0", TypeIdentifier = "OrderNumber:6ES7 516-3FN02-0AB0/V2.9",
                    WorkMemoryBytes = 2250 * 1024, LoadMemoryBytes = 5 * 1024 * 1024, RetentiveMemoryBytes = 306 * 1024,
                    BitInstructionNs = 15, WordInstructionNs = 18, FloatInstructionNs = 24,
                    ProfinetPorts = 2, ProfibusPorts = 1, MaxConnections = 256,
                    MaxTechnologyObjects = 2400, SupportsMotion = true, IsFailsafe = true,
                    RelativePrice = 115, UseCase = "High-performance F-CPU for large safety applications"
                },
                new CpuSpec
                {
                    Name = "CPU 1517-3 PN/DP", Family = "S7-1500",
                    OrderNumber = "6ES7 517-3AP00-0AB0", TypeIdentifier = "OrderNumber:6ES7 517-3AP00-0AB0/V2.9",
                    WorkMemoryBytes = 2 * 1024 * 1024, LoadMemoryBytes = 8 * 1024 * 1024, RetentiveMemoryBytes = 153 * 1024,
                    BitInstructionNs = 10, WordInstructionNs = 12, FloatInstructionNs = 16,
                    ProfinetPorts = 2, ProfibusPorts = 1, MaxConnections = 384,
                    MaxTechnologyObjects = 3200, SupportsMotion = true, IsFailsafe = false,
                    RelativePrice = 130, UseCase = "High-end CPU for process control / large plants"
                },
                new CpuSpec
                {
                    Name = "CPU 1518-4 PN/DP", Family = "S7-1500",
                    OrderNumber = "6ES7 518-4AP00-0AB0", TypeIdentifier = "OrderNumber:6ES7 518-4AP00-0AB0/V2.9",
                    WorkMemoryBytes = 4 * 1024 * 1024, LoadMemoryBytes = 20 * 1024 * 1024, RetentiveMemoryBytes = 153 * 1024,
                    BitInstructionNs = 1, WordInstructionNs = 2, FloatInstructionNs = 6,
                    ProfinetPorts = 3, ProfibusPorts = 1, MaxConnections = 576,
                    MaxTechnologyObjects = 4800, SupportsMotion = true, IsFailsafe = false,
                    RelativePrice = 200, UseCase = "Top-tier CPU for very large applications"
                },

                // ─────────── S7-1500 Technology (T/TF) for motion ───────────
                new CpuSpec
                {
                    Name = "CPU 1515T-2 PN", Family = "S7-1500 T",
                    OrderNumber = "6ES7 515-2TM01-0AB0", TypeIdentifier = "OrderNumber:6ES7 515-2TM01-0AB0/V2.9",
                    WorkMemoryBytes = 750 * 1024, LoadMemoryBytes = 5 * 1024 * 1024, RetentiveMemoryBytes = 153 * 1024,
                    BitInstructionNs = 30, WordInstructionNs = 36, FloatInstructionNs = 48,
                    ProfinetPorts = 2, ProfibusPorts = 0, MaxConnections = 256,
                    MaxTechnologyObjects = 1600, SupportsMotion = true, IsFailsafe = false,
                    RelativePrice = 80,
                    UseCase = "Motion-optimized CPU for synchronous axes, cams, kinematics"
                },
                new CpuSpec
                {
                    Name = "CPU 1517T-3 PN/DP", Family = "S7-1500 T",
                    OrderNumber = "6ES7 517-3TP00-0AB0", TypeIdentifier = "OrderNumber:6ES7 517-3TP00-0AB0/V2.9",
                    WorkMemoryBytes = 3 * 1024 * 1024, LoadMemoryBytes = 8 * 1024 * 1024, RetentiveMemoryBytes = 153 * 1024,
                    BitInstructionNs = 10, WordInstructionNs = 12, FloatInstructionNs = 16,
                    ProfinetPorts = 2, ProfibusPorts = 1, MaxConnections = 384,
                    MaxTechnologyObjects = 3200, SupportsMotion = true, IsFailsafe = false,
                    RelativePrice = 160, UseCase = "High-performance motion CPU"
                },

                // ─────────── ET 200SP CPU ───────────
                new CpuSpec
                {
                    Name = "CPU 1510SP-1 PN", Family = "ET 200SP",
                    OrderNumber = "6ES7 510-1DJ01-0AB0", TypeIdentifier = "OrderNumber:6ES7 510-1DJ01-0AB0/V2.9",
                    WorkMemoryBytes = 100 * 1024, LoadMemoryBytes = 20 * 1024 * 1024, RetentiveMemoryBytes = 90 * 1024,
                    BitInstructionNs = 72, WordInstructionNs = 86, FloatInstructionNs = 115,
                    ProfinetPorts = 1, ProfibusPorts = 0, MaxConnections = 88,
                    MaxTechnologyObjects = 64, SupportsMotion = true, IsFailsafe = false,
                    RelativePrice = 35, UseCase = "Distributed CPU inside ET 200SP station"
                },
                new CpuSpec
                {
                    Name = "CPU 1512SP-1 PN", Family = "ET 200SP",
                    OrderNumber = "6ES7 512-1DK01-0AB0", TypeIdentifier = "OrderNumber:6ES7 512-1DK01-0AB0/V2.9",
                    WorkMemoryBytes = 250 * 1024, LoadMemoryBytes = 50 * 1024 * 1024, RetentiveMemoryBytes = 170 * 1024,
                    BitInstructionNs = 48, WordInstructionNs = 58, FloatInstructionNs = 77,
                    ProfinetPorts = 1, ProfibusPorts = 0, MaxConnections = 88,
                    MaxTechnologyObjects = 128, SupportsMotion = true, IsFailsafe = false,
                    RelativePrice = 50, UseCase = "Larger distributed CPU for ET 200SP"
                },
                new CpuSpec
                {
                    Name = "CPU 1515SP PC", Family = "ET 200SP PC",
                    OrderNumber = "6ES7 677-2AA41-0FA0", TypeIdentifier = "OrderNumber:6ES7 677-2AA41-0FA0/V2.9",
                    WorkMemoryBytes = 1500 * 1024, LoadMemoryBytes = 50 * 1024 * 1024, RetentiveMemoryBytes = 512 * 1024,
                    BitInstructionNs = 30, WordInstructionNs = 36, FloatInstructionNs = 48,
                    ProfinetPorts = 3, ProfibusPorts = 0, MaxConnections = 256,
                    MaxTechnologyObjects = 800, SupportsMotion = true, IsFailsafe = false,
                    RelativePrice = 180, UseCase = "Open controller — PLC + Windows IPC combined"
                }
            };
        }

        #endregion

        #region Data Models

        public class CpuSpec
        {
            public string Name { get; set; } = "";
            public string Family { get; set; } = "";
            public string OrderNumber { get; set; } = "";
            public string TypeIdentifier { get; set; } = "";
            public int WorkMemoryBytes { get; set; }
            public int LoadMemoryBytes { get; set; }
            public int RetentiveMemoryBytes { get; set; }
            public double BitInstructionNs { get; set; }
            public double WordInstructionNs { get; set; }
            public double FloatInstructionNs { get; set; }
            public int ProfinetPorts { get; set; }
            public int ProfibusPorts { get; set; }
            public int MaxConnections { get; set; }
            public int MaxProcessImageInputs { get; set; }
            public int MaxProcessImageOutputs { get; set; }
            public int MaxProcessImageAi { get; set; }
            public int MaxProcessImageAq { get; set; }
            public int MaxTechnologyObjects { get; set; }
            public bool SupportsMotion { get; set; }
            public bool IsFailsafe { get; set; }
            public int IntegratedDI { get; set; }
            public int IntegratedDQ { get; set; }
            public int IntegratedAI { get; set; }
            public int IntegratedAQ { get; set; }
            public double RelativePrice { get; set; }  // Relative cost (arbitrary units, 10=cheapest)
            public string UseCase { get; set; } = "";
        }

        public class CpuRequirements
        {
            // I/O requirements
            public int DigitalInputs { get; set; }
            public int DigitalOutputs { get; set; }
            public int AnalogInputs { get; set; }
            public int AnalogOutputs { get; set; }

            // Program complexity
            public int FunctionBlockCount { get; set; }
            public int FunctionCount { get; set; }
            public int OrganizationBlockCount { get; set; } = 1;
            public int DataBlockCount { get; set; }
            public int TagCount { get; set; }
            public int TechnologyObjectCount { get; set; }

            // User-provided estimates (0 = auto-estimate)
            public int EstimatedCodeBytes { get; set; }
            public int EstimatedBitOperationsPerCycle { get; set; }
            public int EstimatedWordOperationsPerCycle { get; set; }
            public int EstimatedFloatOperationsPerCycle { get; set; }

            // Performance targets
            public double RequiredCycleTimeMs { get; set; }

            // Communication
            public int CommunicationConnections { get; set; }
            public int MinPROFINETPorts { get; set; }
            public int MinPROFIBUSPorts { get; set; }

            // Constraints
            public bool RequireSafety { get; set; }
            public bool RequireMotion { get; set; }
            public string? PreferredFamily { get; set; }
            public double MaxBudget { get; set; }  // Relative price cap
        }

        public class UsageEstimate
        {
            public int EstimatedWorkMemoryBytes { get; set; }
            public int EstimatedLoadMemoryBytes { get; set; }
            public double EstimatedCycleTimeMs { get; set; }
            public double EstimatedWorkMemoryPercent { get; set; }
            public int BitOperations { get; set; }
            public int WordOperations { get; set; }
            public int FloatOperations { get; set; }
        }

        public class CpuRecommendation
        {
            public CpuSpec? Cpu { get; set; }
            public int Score { get; set; }
            public UsageEstimate? EstimatedUsage { get; set; }
            public List<string>? Notes { get; set; }
            public string? Suitability { get; set; }
        }

        #endregion
    }
}
