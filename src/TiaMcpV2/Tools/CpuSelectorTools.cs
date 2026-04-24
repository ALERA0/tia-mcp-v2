using System.ComponentModel;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using TiaMcpV2.Core;
using TiaMcpV2.Helpers;
using TiaMcpV2.Models;
using TiaMcpV2.Services;

namespace TiaMcpV2.Tools
{
    [McpServerToolType]
    public static class CpuSelectorTools
    {
        [McpServerTool(Name = "list_cpus"), Description("List all Siemens CPUs in the catalog with full specs. family filter: 'S7-1200', 'S7-1500', 'S7-1500 F', 'S7-1500 T', 'ET 200SP', 'Compact' (or empty for all). Returns work/load/retentive memory, instruction speeds, port counts, max connections, TOs, integrated I/O.")]
        public static string ListCpus(string family)
        {
            try
            {
                var cpus = ServiceAccessor.CpuSelector.GetByFamily(family ?? "");
                return JsonHelper.ToJson(new { Success = true, Count = cpus.Count, Family = family, Cpus = cpus });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "get_cpu_specs"), Description("Get detailed specifications of a specific CPU. Pass CPU name (e.g. 'CPU 1516-3 PN/DP' or '1515-2 PN') or order number.")]
        public static string GetCpuSpecs(string cpuName)
        {
            try
            {
                var cpu = ServiceAccessor.CpuSelector.GetByName(cpuName);
                if (cpu == null)
                    return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = $"CPU not found: {cpuName}" });
                return JsonHelper.ToJson(new { Success = true, Cpu = cpu });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "compare_cpus"), Description("Side-by-side comparison of two CPUs. Returns memory ratios, speed ratios, connection differences, TO capacity, price ratio. Use this when choosing between two candidates.")]
        public static string CompareCpus(string cpu1Name, string cpu2Name)
        {
            try
            {
                var result = ServiceAccessor.CpuSelector.Compare(cpu1Name, cpu2Name);
                return JsonHelper.ToJson(new { Success = true, Result = result });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "recommend_cpu"), Description(@"Recommend the best CPU(s) for a project based on requirements. Returns ranked list with scores, suitability levels, and estimated resource usage. Input is JSON with fields: DigitalInputs, DigitalOutputs, AnalogInputs, AnalogOutputs, FunctionBlockCount, FunctionCount, DataBlockCount, TagCount, TechnologyObjectCount, CommunicationConnections, RequiredCycleTimeMs, MinPROFINETPorts, MinPROFIBUSPorts, RequireSafety, RequireMotion, PreferredFamily. Example: {
  ""DigitalInputs"": 64,
  ""DigitalOutputs"": 32,
  ""AnalogInputs"": 8,
  ""FunctionBlockCount"": 20,
  ""TechnologyObjectCount"": 4,
  ""RequiredCycleTimeMs"": 10,
  ""CommunicationConnections"": 5,
  ""RequireMotion"": true,
  ""PreferredFamily"": ""S7-1500""
}")]
        public static string RecommendCpu(string requirementsJson)
        {
            try
            {
                var req = JsonSerializer.Deserialize<CpuSelectorService.CpuRequirements>(requirementsJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (req == null)
                    return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = "Invalid requirements JSON" });

                var recommendations = ServiceAccessor.CpuSelector.Recommend(req);

                // Return top 5
                var top = recommendations.Take(5).Select(r => new
                {
                    r.Suitability,
                    r.Score,
                    CpuName = r.Cpu?.Name,
                    CpuFamily = r.Cpu?.Family,
                    CpuOrderNumber = r.Cpu?.OrderNumber,
                    CpuTypeIdentifier = r.Cpu?.TypeIdentifier,
                    WorkMemoryKB = r.Cpu?.WorkMemoryBytes / 1024,
                    EstimatedUsagePercent = r.EstimatedUsage?.EstimatedWorkMemoryPercent,
                    EstimatedCycleTimeMs = r.EstimatedUsage?.EstimatedCycleTimeMs,
                    MaxConnections = r.Cpu?.MaxConnections,
                    MaxTechnologyObjects = r.Cpu?.MaxTechnologyObjects,
                    IsFailsafe = r.Cpu?.IsFailsafe,
                    SupportsMotion = r.Cpu?.SupportsMotion,
                    UseCase = r.Cpu?.UseCase,
                    Notes = r.Notes
                }).ToList();

                return JsonHelper.ToJson(new
                {
                    Success = true,
                    Count = recommendations.Count,
                    TopRecommendations = top,
                    BestChoice = top.FirstOrDefault()
                });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "estimate_cpu_load"), Description("Estimate memory usage and cycle time for a specific CPU given requirements. Use this to check if a pre-selected CPU will meet the project needs. Inputs: cpuName + requirementsJson (same format as recommend_cpu).")]
        public static string EstimateCpuLoad(string cpuName, string requirementsJson)
        {
            try
            {
                var cpu = ServiceAccessor.CpuSelector.GetByName(cpuName);
                if (cpu == null)
                    return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = $"CPU not found: {cpuName}" });

                var req = JsonSerializer.Deserialize<CpuSelectorService.CpuRequirements>(requirementsJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (req == null)
                    return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = "Invalid requirements JSON" });

                var estimate = ServiceAccessor.CpuSelector.EstimateUsage(cpu, req);

                var verdict = "✓ CPU is suitable";
                var warnings = new List<string>();

                if (estimate.EstimatedWorkMemoryPercent > 100)
                {
                    verdict = "⚠ INSUFFICIENT: Work memory overflow";
                    warnings.Add($"Work memory usage {estimate.EstimatedWorkMemoryPercent:F1}% exceeds 100%");
                }
                else if (estimate.EstimatedWorkMemoryPercent > 85)
                {
                    verdict = "⚠ TIGHT: Work memory near limit";
                    warnings.Add($"Work memory usage {estimate.EstimatedWorkMemoryPercent:F1}% is high — consider larger CPU");
                }

                if (req.RequiredCycleTimeMs > 0 && estimate.EstimatedCycleTimeMs > req.RequiredCycleTimeMs)
                {
                    verdict = "⚠ SLOW: Cycle time target not met";
                    warnings.Add($"Estimated cycle time {estimate.EstimatedCycleTimeMs:F2}ms > target {req.RequiredCycleTimeMs}ms");
                }

                if (req.TechnologyObjectCount > cpu.MaxTechnologyObjects)
                {
                    verdict = "⚠ INSUFFICIENT: TO capacity exceeded";
                    warnings.Add($"TO count {req.TechnologyObjectCount} exceeds max {cpu.MaxTechnologyObjects}");
                }

                return JsonHelper.ToJson(new
                {
                    Success = true,
                    Cpu = cpu.Name,
                    Family = cpu.Family,
                    Verdict = verdict,
                    Warnings = warnings,
                    Estimate = new
                    {
                        estimate.EstimatedWorkMemoryBytes,
                        WorkMemoryKB = estimate.EstimatedWorkMemoryBytes / 1024,
                        WorkMemoryAvailableKB = cpu.WorkMemoryBytes / 1024,
                        estimate.EstimatedWorkMemoryPercent,
                        estimate.EstimatedCycleTimeMs,
                        estimate.BitOperations,
                        estimate.WordOperations,
                        estimate.FloatOperations
                    }
                });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "analyze_current_cpu"), Description("Analyze the CURRENTLY OPEN project's CPU and compare to project requirements. Reads actual project state (blocks, tags, TOs, connections) and reports whether the CPU is appropriately sized. Pass the softwarePath of the PLC device to analyze.")]
        public static string AnalyzeCurrentCpu(string softwarePath)
        {
            try
            {
                var sw = ServiceAccessor.Blocks.GetPlcSoftware(softwarePath);
                var device = ServiceAccessor.Portal.FindDevice(softwarePath);
                if (device == null)
                    return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = $"Device not found: {softwarePath}" });

                // Try to identify CPU from device type
                var memory = ServiceAccessor.Diagnostics.GetMemoryUsage(softwarePath);
                var cpuTypeId = device.TypeIdentifier;

                // Match against database
                var matchedCpu = ServiceAccessor.CpuSelector.GetAllCpus()
                    .FirstOrDefault(c => c.TypeIdentifier.Equals(cpuTypeId, StringComparison.OrdinalIgnoreCase) ||
                                         cpuTypeId?.Contains(c.OrderNumber) == true);

                // Build actual requirements from project
                var totalBlocks = memory.ContainsKey("TotalBlocks") ? Convert.ToInt32(memory["TotalBlocks"]) : 0;
                var fbCount = memory.ContainsKey("FunctionBlocks") ? Convert.ToInt32(memory["FunctionBlocks"]) : 0;
                var fcCount = memory.ContainsKey("Functions") ? Convert.ToInt32(memory["Functions"]) : 0;
                var obCount = memory.ContainsKey("OrganizationBlocks") ? Convert.ToInt32(memory["OrganizationBlocks"]) : 0;
                var dbCount = memory.ContainsKey("DataBlocks") ? Convert.ToInt32(memory["DataBlocks"]) : 0;
                var tagCount = memory.ContainsKey("TotalTags") ? Convert.ToInt32(memory["TotalTags"]) : 0;

                int toCount = 0;
                try
                {
                    var tos = ServiceAccessor.TechObjects.GetTechnologicalObjects(softwarePath);
                    toCount = tos.Count;
                }
                catch { }

                var req = new CpuSelectorService.CpuRequirements
                {
                    FunctionBlockCount = fbCount,
                    FunctionCount = fcCount,
                    OrganizationBlockCount = obCount,
                    DataBlockCount = dbCount,
                    TagCount = tagCount,
                    TechnologyObjectCount = toCount
                };

                var result = new Dictionary<string, object?>
                {
                    ["SoftwarePath"] = softwarePath,
                    ["DeviceName"] = device.Name,
                    ["CpuTypeIdentifier"] = cpuTypeId,
                    ["IdentifiedCpu"] = matchedCpu?.Name ?? "Not identified from database",
                    ["ActualProject"] = new
                    {
                        TotalBlocks = totalBlocks,
                        fbCount,
                        fcCount,
                        obCount,
                        dbCount,
                        tagCount,
                        toCount
                    }
                };

                if (matchedCpu != null)
                {
                    var estimate = ServiceAccessor.CpuSelector.EstimateUsage(matchedCpu, req);
                    result["CurrentCpuSpec"] = matchedCpu;
                    result["Estimate"] = estimate;

                    // Also recommend better options
                    var recommendations = ServiceAccessor.CpuSelector.Recommend(req);
                    var better = recommendations.Where(r => r.Cpu?.Name != matchedCpu.Name).Take(3).ToList();
                    result["AlternativeRecommendations"] = better.Select(r => new
                    {
                        r.Suitability,
                        r.Score,
                        CpuName = r.Cpu?.Name,
                        CpuOrderNumber = r.Cpu?.OrderNumber,
                        r.Notes
                    });
                }

                return JsonHelper.ToJson(new { Success = true, Analysis = result });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }
    }
}
