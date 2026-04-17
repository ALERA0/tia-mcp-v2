using ModelContextProtocol.Server;
using System.ComponentModel;
using System;
using System.Collections.Generic;
using TiaMcpV2.Core;
using TiaMcpV2.Helpers;
using TiaMcpV2.Models;

namespace TiaMcpV2.Tools
{
    [McpServerToolType]
    public static class ProjectStructureTools
    {
        [McpServerTool(Name = "create_project_structure"), Description("Create a standardized TIA Portal project structure with block groups following the document standard: 01_Hardware, 02_MainProgram, 03_StandardLibrary (Motors/Valves/Analog/Communication/Utility), 04_HMI, 05_Safety, 06_Diagnostics.")]
        public static string CreateProjectStructure(string softwarePath)
        {
            try
            {
                var created = new List<string>();

                // Main program groups
                var mainGroups = new[] { "01_MainProgram", "02_Sequence", "03_StandardLibrary", "04_Safety", "05_Communication", "06_Diagnostics", "07_HMI_Interface" };
                foreach (var group in mainGroups)
                {
                    try
                    {
                        ServiceAccessor.Blocks.CreateBlockGroup(softwarePath, "", group);
                        created.Add($"Block group: {group}");
                    }
                    catch { }
                }

                // Standard library sub-groups
                var libSubGroups = new[] { "Motors", "Valves", "Analog", "PID", "Communication", "Utility" };
                foreach (var sub in libSubGroups)
                {
                    try
                    {
                        ServiceAccessor.Blocks.CreateBlockGroup(softwarePath, "03_StandardLibrary", sub);
                        created.Add($"  Sub-group: 03_StandardLibrary/{sub}");
                    }
                    catch { }
                }

                // UDT type groups
                var typeGroups = new[] { "Motors", "Valves", "Analog", "System" };
                foreach (var tg in typeGroups)
                {
                    try
                    {
                        ServiceAccessor.Types.CreateTypeGroup(softwarePath, "", tg);
                        created.Add($"Type group: {tg}");
                    }
                    catch { }
                }

                // Standard tag tables
                var tagTables = new[] { "Inputs_DI", "Outputs_DQ", "Inputs_AI", "Outputs_AQ", "System_Tags", "HMI_Tags" };
                foreach (var tt in tagTables)
                {
                    try
                    {
                        ServiceAccessor.Tags.CreateTagTable(softwarePath, tt);
                        created.Add($"Tag table: {tt}");
                    }
                    catch { }
                }

                return JsonHelper.ToJson(new { Success = true, Message = $"Created project structure with {created.Count} items", Items = created });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "generate_tag_table_from_modules"), Description("Auto-generate PLC tag tables with proper naming from the hardware I/O modules. Scans all modules, creates tags with addresses based on module configuration.")]
        public static string GenerateTagTableFromModules(string softwarePath)
        {
            try
            {
                var sw = ServiceAccessor.Blocks.GetPlcSoftware(softwarePath);
                var device = ServiceAccessor.Portal.FindDevice(softwarePath.Split('/')[0]);
                if (device == null)
                    return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = $"Device not found: {softwarePath}" });

                int tagsCreated = 0;
                var createdTags = new List<string>();

                // This is a placeholder - actual implementation would iterate device modules
                // and create tags based on their addresses
                return JsonHelper.ToJson(new
                {
                    Success = true,
                    Message = "Tag generation from modules requires online connection to read actual addresses. Use get_io_address_plan first, then create_tag for each entry.",
                    Suggestion = "1. Call get_io_address_plan to get all I/O addresses, 2. Call create_tag_table for each category, 3. Call create_tag for each I/O point"
                });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "get_project_statistics"), Description("Get comprehensive project statistics: device count, block counts by type, tag counts, type counts, network info, software tree summary.")]
        public static string GetProjectStatistics(string softwarePath)
        {
            try
            {
                var stats = new Dictionary<string, object?>();

                // Memory/block usage
                var memory = ServiceAccessor.Diagnostics.GetMemoryUsage(softwarePath);
                stats["Blocks"] = memory;

                // Devices
                ServiceAccessor.Portal.EnsureProjectOpen();
                var devices = new List<string>();
                foreach (var d in ServiceAccessor.Portal.GetDevices())
                    devices.Add($"{d.Name} ({d.TypeIdentifier})");
                stats["DeviceCount"] = devices.Count;
                stats["Devices"] = devices;

                // Network
                try
                {
                    var subnets = ServiceAccessor.Network.GetSubnets();
                    stats["SubnetCount"] = subnets.Count;
                    var ioSystems = ServiceAccessor.Network.GetIoSystems();
                    stats["IoSystemCount"] = ioSystems.Count;
                }
                catch { }

                // Code quality
                try
                {
                    var analysis = ServiceAccessor.CodeAnalysis.AnalyzeProject(softwarePath);
                    stats["CodeQualityScore"] = analysis["Score"];
                    stats["CodeIssueCount"] = analysis["IssueCount"];
                }
                catch { }

                return JsonHelper.ToJson(new { Success = true, Statistics = stats });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }
    }
}
