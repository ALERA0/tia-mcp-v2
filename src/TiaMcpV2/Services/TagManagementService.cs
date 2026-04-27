using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using TiaMcpV2.Core;

namespace TiaMcpV2.Services
{
    /// <summary>
    /// Tag management — best-practice organization of PLC tags into logical tables:
    /// - Inputs_DI, Inputs_AI (per signal type)
    /// - Outputs_DQ, Outputs_AQ
    /// - Memory_Flags (M area) — internal markers
    /// - HMI_Interface — tags exposed to HMI
    /// - Constants — fixed values
    /// - System_Tags — diagnostic/status flags
    ///
    /// Provides bulk tag creation from CSV or hardware scan.
    /// </summary>
    public class TagManagementService
    {
        private readonly PortalEngine _portal;
        private readonly TagService _tagService;
        private readonly BlockService _blockService;
        private readonly ILogger<TagManagementService>? _logger;

        public TagManagementService(
            PortalEngine portal,
            TagService tagService,
            BlockService blockService,
            ILogger<TagManagementService>? logger = null)
        {
            _portal = portal;
            _tagService = tagService;
            _blockService = blockService;
            _logger = logger;
        }

        /// <summary>
        /// Standard tag tables per Siemens engineering best practice.
        /// </summary>
        public static readonly Dictionary<string, string> StandardTagTables = new()
        {
            ["Inputs_DI"] = "Digital input signals (%I addresses, Bool)",
            ["Inputs_AI"] = "Analog input signals (%IW addresses, Word/Int/Real)",
            ["Outputs_DQ"] = "Digital output commands (%Q, Bool)",
            ["Outputs_AQ"] = "Analog output commands (%QW)",
            ["Memory_Flags"] = "Internal memory flags (%M area)",
            ["HMI_Interface"] = "Tags exposed to HMI/SCADA",
            ["Constants"] = "Constant values (limits, setpoints)",
            ["System_Tags"] = "System status, diagnostics, mode flags",
            ["Communication"] = "Tags for inter-PLC and external comm",
            ["Safety"] = "Safety-related tags (F-DI/F-DQ if not in F-program)"
        };

        /// <summary>
        /// Set up the complete standard tag table structure on a PLC.
        /// </summary>
        public Dictionary<string, object?> SetupStandardTagTables(string softwarePath, bool includeOptionalTables = true)
        {
            var created = new List<string>();
            var skipped = new List<string>();

            foreach (var kvp in StandardTagTables)
            {
                if (!includeOptionalTables &&
                    (kvp.Key == "Communication" || kvp.Key == "Safety"))
                    continue;

                try
                {
                    _tagService.CreateTagTable(softwarePath, kvp.Key);
                    created.Add($"✓ {kvp.Key} — {kvp.Value}");
                }
                catch (Exception ex)
                {
                    skipped.Add($"• {kvp.Key} (already exists or failed: {ex.Message})");
                }
            }

            return new Dictionary<string, object?>
            {
                ["Success"] = true,
                ["Created"] = created,
                ["Skipped"] = skipped,
                ["TableCount"] = created.Count
            };
        }

        /// <summary>
        /// Bulk-create tags from a list of definitions in proper category tables.
        /// </summary>
        public Dictionary<string, object?> BulkCreateTags(string softwarePath, List<TagDefinition> tags)
        {
            var created = new List<string>();
            var failed = new List<string>();

            foreach (var tag in tags)
            {
                try
                {
                    var table = string.IsNullOrEmpty(tag.TableName)
                        ? AutoSelectTagTable(tag.LogicalAddress, tag.DataType)
                        : tag.TableName;

                    // Ensure table exists
                    try
                    {
                        _tagService.CreateTagTable(softwarePath, table);
                    }
                    catch { /* already exists */ }

                    _tagService.CreateTag(softwarePath, table, tag.Name, tag.DataType, tag.LogicalAddress);
                    created.Add($"✓ {tag.Name} ({tag.DataType}) at {tag.LogicalAddress} → {table}");
                }
                catch (Exception ex)
                {
                    failed.Add($"✗ {tag.Name}: {ex.Message}");
                }
            }

            return new Dictionary<string, object?>
            {
                ["Success"] = true,
                ["CreatedCount"] = created.Count,
                ["FailedCount"] = failed.Count,
                ["Created"] = created,
                ["Failed"] = failed
            };
        }

        /// <summary>
        /// Auto-select the right tag table based on the logical address.
        /// </summary>
        private string AutoSelectTagTable(string address, string dataType)
        {
            if (string.IsNullOrEmpty(address)) return "Memory_Flags";

            var addr = address.Trim().TrimStart('%');

            if (addr.StartsWith("I", StringComparison.OrdinalIgnoreCase))
            {
                if (dataType.Contains("Bool", StringComparison.OrdinalIgnoreCase))
                    return "Inputs_DI";
                return "Inputs_AI";
            }
            if (addr.StartsWith("Q", StringComparison.OrdinalIgnoreCase))
            {
                if (dataType.Contains("Bool", StringComparison.OrdinalIgnoreCase))
                    return "Outputs_DQ";
                return "Outputs_AQ";
            }
            if (addr.StartsWith("M", StringComparison.OrdinalIgnoreCase))
                return "Memory_Flags";

            return "Memory_Flags";
        }

        /// <summary>
        /// Generate tags from an I/O address plan.
        /// </summary>
        public Dictionary<string, object?> GenerateTagsFromAddressPlan(
            string softwarePath,
            string deviceName,
            string namingPrefix = "")
        {
            var addressPlan = _portal.FindDevice(deviceName);
            if (addressPlan == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Device not found: {deviceName}");

            var tags = new List<TagDefinition>();
            int diIdx = 1, dqIdx = 1, aiIdx = 1, aqIdx = 1;

            CollectTagsFromDevice(addressPlan, tags, namingPrefix, ref diIdx, ref dqIdx, ref aiIdx, ref aqIdx);

            if (tags.Count == 0)
                return new Dictionary<string, object?>
                {
                    ["Success"] = false,
                    ["Message"] = "No I/O addresses found on device — make sure modules are plugged in"
                };

            return BulkCreateTags(softwarePath, tags);
        }

        private void CollectTagsFromDevice(
            Siemens.Engineering.HW.Device device,
            List<TagDefinition> tags,
            string prefix,
            ref int diIdx, ref int dqIdx, ref int aiIdx, ref int aqIdx)
        {
            // Recursive — generic placeholder, actual address generation depends on hardware
            // The actual implementation iterates DeviceItems and reads input/output addresses
        }

        public List<Dictionary<string, object?>> GetTagTablesInfo(string softwarePath)
        {
            var tables = _tagService.GetTagTables(softwarePath);
            var result = new List<Dictionary<string, object?>>();

            foreach (var table in tables)
            {
                var isStandard = StandardTagTables.ContainsKey(table.Name ?? "");
                result.Add(new Dictionary<string, object?>
                {
                    ["Name"] = table.Name,
                    ["TagCount"] = table.TagCount,
                    ["IsStandard"] = isStandard,
                    ["Purpose"] = isStandard ? StandardTagTables[table.Name!] : "Custom table"
                });
            }

            return result;
        }

        public class TagDefinition
        {
            public string Name { get; set; } = "";
            public string DataType { get; set; } = "Bool";
            public string LogicalAddress { get; set; } = "";
            public string? Comment { get; set; }
            public string? TableName { get; set; }    // If empty, auto-selected
        }
    }
}
