using Microsoft.Extensions.Logging;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;
using System;
using System.Collections.Generic;
using System.Linq;
using TiaMcpV2.Core;

namespace TiaMcpV2.Services
{
    /// <summary>
    /// V2 NEW SERVICE: Code quality analysis for PLC programs.
    /// Checks naming conventions, consistency, and best practices.
    /// </summary>
    public class CodeAnalysisService
    {
        private readonly PortalEngine _portal;
        private readonly BlockService _blockService;
        private readonly ILogger<CodeAnalysisService>? _logger;

        public CodeAnalysisService(PortalEngine portal, BlockService blockService, ILogger<CodeAnalysisService>? logger = null)
        {
            _portal = portal;
            _blockService = blockService;
            _logger = logger;
        }

        public Dictionary<string, object?> AnalyzeProject(string devicePath)
        {
            var sw = _blockService.GetPlcSoftware(devicePath);
            var issues = new List<Dictionary<string, object?>>();
            var suggestions = new List<string>();
            int score = 100;

            // Check block naming
            CheckBlockNaming(sw.BlockGroup, issues, ref score, "");

            // Check for inconsistent blocks
            CheckConsistency(sw.BlockGroup, issues, ref score, "");

            // Check type naming
            CheckTypeNaming(sw.TypeGroup, issues, ref score);

            // Check tag table organization
            CheckTagOrganization(sw, issues, suggestions, ref score);

            // General suggestions
            if (issues.Count == 0)
                suggestions.Add("Code quality looks good! No major issues found.");

            if (score < 0) score = 0;

            return new Dictionary<string, object?>
            {
                ["Score"] = score,
                ["MaxScore"] = 100,
                ["IssueCount"] = issues.Count,
                ["Issues"] = issues,
                ["Suggestions"] = suggestions
            };
        }

        private void CheckBlockNaming(PlcBlockGroup group, List<Dictionary<string, object?>> issues, ref int score, string path)
        {
            var currentPath = string.IsNullOrEmpty(path) ? group.Name : $"{path}/{group.Name}";

            foreach (var block in group.Blocks)
            {
                var typeName = block.GetType().Name;
                var name = block.Name;

                // FB should have FB_ prefix
                if (typeName.Contains("FB") && !name.StartsWith("FB_") && name != "Main")
                {
                    issues.Add(CreateIssue("Naming", "Warning",
                        $"Function Block '{name}' should have 'FB_' prefix",
                        currentPath, name));
                    score -= 2;
                }

                // FC should have descriptive name (PascalCase)
                if (typeName.Contains("FC") && name.Contains(" "))
                {
                    issues.Add(CreateIssue("Naming", "Warning",
                        $"Function '{name}' should use PascalCase without spaces",
                        currentPath, name));
                    score -= 2;
                }

                // DB should have DB_ prefix (except instance DBs)
                if (typeName.Contains("GlobalDB") && !name.StartsWith("DB_"))
                {
                    issues.Add(CreateIssue("Naming", "Info",
                        $"Global Data Block '{name}' should have 'DB_' prefix",
                        currentPath, name));
                    score -= 1;
                }

                // Check for know-how protection
                if (block.IsKnowHowProtected)
                {
                    issues.Add(CreateIssue("Access", "Info",
                        $"Block '{name}' is know-how protected - cannot analyze internals",
                        currentPath, name));
                }
            }

            foreach (var sub in group.Groups)
                CheckBlockNaming(sub, issues, ref score, currentPath);
        }

        private void CheckConsistency(PlcBlockGroup group, List<Dictionary<string, object?>> issues, ref int score, string path)
        {
            var currentPath = string.IsNullOrEmpty(path) ? group.Name : $"{path}/{group.Name}";

            foreach (var block in group.Blocks)
            {
                if (!block.IsConsistent)
                {
                    issues.Add(CreateIssue("Consistency", "Error",
                        $"Block '{block.Name}' is inconsistent and needs recompilation",
                        currentPath, block.Name));
                    score -= 5;
                }
            }

            foreach (var sub in group.Groups)
                CheckConsistency(sub, issues, ref score, currentPath);
        }

        private void CheckTypeNaming(Siemens.Engineering.SW.Types.PlcTypeGroup group, List<Dictionary<string, object?>> issues, ref int score)
        {
            foreach (var type in group.Types)
            {
                if (!type.Name.StartsWith("T_") && !type.Name.StartsWith("UDT_"))
                {
                    issues.Add(CreateIssue("Naming", "Info",
                        $"UDT '{type.Name}' should have 'T_' or 'UDT_' prefix",
                        "PLC data types", type.Name));
                    score -= 1;
                }
            }

            foreach (var sub in group.Groups)
                CheckTypeNaming(sub, issues, ref score);
        }

        private void CheckTagOrganization(PlcSoftware sw, List<Dictionary<string, object?>> issues, List<string> suggestions, ref int score)
        {
            var tables = sw.TagTableGroup.TagTables;
            int totalTags = 0;

            foreach (var table in tables)
            {
                totalTags += table.Tags.Count;

                if (table.Tags.Count > 200)
                {
                    issues.Add(CreateIssue("Organization", "Warning",
                        $"Tag table '{table.Name}' has {table.Tags.Count} tags. Consider splitting into smaller tables.",
                        "Tag Tables", table.Name));
                    score -= 3;
                }
            }

            if (tables.Count == 1 && totalTags > 50)
            {
                suggestions.Add("Consider organizing tags into multiple tag tables by function area (e.g., Inputs, Outputs, Motors, Valves).");
            }
        }

        private Dictionary<string, object?> CreateIssue(string category, string severity, string message, string path, string blockName)
        {
            return new Dictionary<string, object?>
            {
                ["Category"] = category,
                ["Severity"] = severity,
                ["Message"] = message,
                ["Path"] = path,
                ["BlockName"] = blockName
            };
        }
    }
}
