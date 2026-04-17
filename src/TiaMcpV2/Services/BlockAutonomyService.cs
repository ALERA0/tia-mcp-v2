using Microsoft.Extensions.Logging;
using Siemens.Engineering;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;
using Siemens.Engineering.SW.ExternalSources;
using Siemens.Engineering.SW.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TiaMcpV2.Core;

namespace TiaMcpV2.Services
{
    /// <summary>
    /// Autonomous block management — Claude can create, read, update, delete ANY block
    /// (FB, FC, OB, DB, UDT) in ANY language without external intervention.
    /// All file operations happen in temp directory and are cleaned up automatically.
    /// </summary>
    public class BlockAutonomyService
    {
        private readonly PortalEngine _portal;
        private readonly BlockService _blockService;
        private readonly ILogger<BlockAutonomyService>? _logger;
        private readonly string _tempDir;

        public BlockAutonomyService(PortalEngine portal, BlockService blockService, ILogger<BlockAutonomyService>? logger = null)
        {
            _portal = portal;
            _blockService = blockService;
            _logger = logger;
            _tempDir = Path.Combine(Path.GetTempPath(), "TiaMcpV2", "blocks");
            Directory.CreateDirectory(_tempDir);
        }

        #region Write Block (SCL Source → TIA Portal)

        /// <summary>
        /// Write a block from SCL source code. This is the PRIMARY method for creating blocks.
        /// Supports: FUNCTION_BLOCK, FUNCTION, ORGANIZATION_BLOCK, DATA_BLOCK, TYPE (UDT).
        /// If block exists, it will be overwritten.
        /// </summary>
        public string WriteBlockFromScl(string softwarePath, string groupPath, string sclCode, string blockName)
        {
            var sw = _blockService.GetPlcSoftware(softwarePath);

            // Check if it's a UDT/TYPE declaration
            var trimmed = sclCode.TrimStart();
            if (trimmed.StartsWith("TYPE ", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("TYPE\"", StringComparison.OrdinalIgnoreCase))
            {
                return WriteUdtFromScl(sw, groupPath, sclCode, blockName);
            }

            // For blocks (FB, FC, OB, DB) use external source import
            var sourceName = SanitizeFileName(blockName);
            var tempFile = Path.Combine(_tempDir, $"{sourceName}_{Guid.NewGuid():N}.scl");

            try
            {
                File.WriteAllText(tempFile, sclCode, Encoding.UTF8);

                // Delete existing external source with same name if any
                var existingSource = sw.ExternalSourceGroup.ExternalSources
                    .FirstOrDefault(s => s.Name == sourceName);
                if (existingSource != null)
                {
                    try { existingSource.Delete(); } catch { }
                }

                var source = sw.ExternalSourceGroup.ExternalSources.CreateFromFile(sourceName, tempFile);
                source.GenerateBlocksFromSource();

                // Clean up the external source
                try { source.Delete(); } catch { }

                _logger?.LogInformation("Wrote block from SCL: {Name}", blockName);
                return $"Block '{blockName}' created/updated from SCL source";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to write block {Name} from SCL", blockName);
                throw new PortalException(PortalErrorCode.ImportFailed,
                    $"Failed to import SCL for '{blockName}': {ex.Message}", ex);
            }
            finally
            {
                CleanupFile(tempFile);
            }
        }

        private string WriteUdtFromScl(PlcSoftware sw, string groupPath, string sclCode, string blockName)
        {
            var sourceName = SanitizeFileName(blockName);
            var tempFile = Path.Combine(_tempDir, $"{sourceName}_{Guid.NewGuid():N}.udt");

            try
            {
                File.WriteAllText(tempFile, sclCode, Encoding.UTF8);

                var existingSource = sw.ExternalSourceGroup.ExternalSources
                    .FirstOrDefault(s => s.Name == sourceName);
                if (existingSource != null)
                {
                    try { existingSource.Delete(); } catch { }
                }

                var source = sw.ExternalSourceGroup.ExternalSources.CreateFromFile(sourceName, tempFile);
                source.GenerateBlocksFromSource();
                try { source.Delete(); } catch { }

                _logger?.LogInformation("Wrote UDT from SCL: {Name}", blockName);
                return $"UDT '{blockName}' created/updated from SCL source";
            }
            catch (Exception ex)
            {
                throw new PortalException(PortalErrorCode.ImportFailed,
                    $"Failed to import UDT '{blockName}': {ex.Message}", ex);
            }
            finally
            {
                CleanupFile(tempFile);
            }
        }

        #endregion

        #region Write Block (XML → TIA Portal)

        /// <summary>
        /// Write a block from SimaticML XML content. Supports all block types.
        /// Used for LAD/FBD blocks or when XML format is preferred.
        /// </summary>
        public string WriteBlockFromXml(string softwarePath, string groupPath, string xmlContent, string blockName)
        {
            var sw = _blockService.GetPlcSoftware(softwarePath);
            var tempFile = Path.Combine(_tempDir, $"{SanitizeFileName(blockName)}_{Guid.NewGuid():N}.xml");

            try
            {
                File.WriteAllText(tempFile, xmlContent, Encoding.UTF8);
                var fileInfo = new FileInfo(tempFile);

                // Determine if it's a type or block from the XML content
                if (xmlContent.Contains("SW.Types.PlcStruct") || xmlContent.Contains("PlcStruct"))
                {
                    // Import as UDT
                    if (string.IsNullOrEmpty(groupPath))
                        sw.TypeGroup.Types.Import(fileInfo, ImportOptions.Override);
                    else
                    {
                        var group = FindTypeGroup(sw, groupPath);
                        if (group != null)
                            group.Types.Import(fileInfo, ImportOptions.Override);
                        else
                            sw.TypeGroup.Types.Import(fileInfo, ImportOptions.Override);
                    }
                }
                else
                {
                    // Import as block (FB, FC, OB, DB)
                    var targetGroup = _blockService.FindBlockGroup(sw, groupPath);
                    targetGroup.Blocks.Import(fileInfo, ImportOptions.Override);
                }

                _logger?.LogInformation("Wrote block from XML: {Name}", blockName);
                return $"Block '{blockName}' imported from XML";
            }
            catch (Exception ex)
            {
                throw new PortalException(PortalErrorCode.ImportFailed,
                    $"Failed to import XML for '{blockName}': {ex.Message}", ex);
            }
            finally
            {
                CleanupFile(tempFile);
            }
        }

        #endregion

        #region Read Block (TIA Portal → XML/Content)

        /// <summary>
        /// Read a block's full XML content. Returns the SimaticML XML string.
        /// Works for all block types: FB, FC, OB, DB (any programming language).
        /// </summary>
        public string ReadBlockAsXml(string softwarePath, string blockPath)
        {
            var sw = _blockService.GetPlcSoftware(softwarePath);
            var block = _blockService.FindBlock(sw, blockPath);
            if (block == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Block not found: {blockPath}");

            var tempFile = Path.Combine(_tempDir, $"export_{SanitizeFileName(block.Name)}_{Guid.NewGuid():N}.xml");

            try
            {
                var fileInfo = new FileInfo(tempFile);
                block.Export(fileInfo, ExportOptions.WithDefaults);

                var content = File.ReadAllText(tempFile, Encoding.UTF8);
                _logger?.LogInformation("Read block as XML: {Name}", blockPath);
                return content;
            }
            finally
            {
                CleanupFile(tempFile);
            }
        }

        /// <summary>
        /// Read a UDT/Type's full XML content.
        /// </summary>
        public string ReadTypeAsXml(string softwarePath, string typeName)
        {
            var sw = _blockService.GetPlcSoftware(softwarePath);
            var type = FindType(sw, typeName);
            if (type == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Type not found: {typeName}");

            var tempFile = Path.Combine(_tempDir, $"export_{SanitizeFileName(typeName)}_{Guid.NewGuid():N}.xml");

            try
            {
                var fileInfo = new FileInfo(tempFile);
                type.Export(fileInfo, ExportOptions.WithDefaults);

                var content = File.ReadAllText(tempFile, Encoding.UTF8);
                _logger?.LogInformation("Read type as XML: {Name}", typeName);
                return content;
            }
            finally
            {
                CleanupFile(tempFile);
            }
        }

        #endregion

        #region Update Block

        /// <summary>
        /// Update an existing block with new SCL code.
        /// Deletes the old block and imports the new one. Handles the full cycle.
        /// </summary>
        public string UpdateBlockFromScl(string softwarePath, string blockPath, string sclCode)
        {
            var sw = _blockService.GetPlcSoftware(softwarePath);
            var block = _blockService.FindBlock(sw, blockPath);

            // Extract block name from path
            var parts = blockPath.Replace('\\', '/').Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var blockName = parts.Last();
            var groupPath = parts.Length > 1
                ? string.Join("/", parts.Take(parts.Length - 1))
                : "";

            // Delete existing block if found
            if (block != null)
            {
                try
                {
                    block.Delete();
                    _logger?.LogInformation("Deleted existing block for update: {Name}", blockName);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Could not delete existing block {Name}, trying override import", blockName);
                }
            }

            // Import new version
            return WriteBlockFromScl(softwarePath, groupPath, sclCode, blockName);
        }

        /// <summary>
        /// Update an existing block with new XML content.
        /// Uses ImportOptions.Override so the existing block is replaced.
        /// </summary>
        public string UpdateBlockFromXml(string softwarePath, string blockPath, string xmlContent)
        {
            var parts = blockPath.Replace('\\', '/').Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var blockName = parts.Last();
            var groupPath = parts.Length > 1
                ? string.Join("/", parts.Take(parts.Length - 1))
                : "";

            return WriteBlockFromXml(softwarePath, groupPath, xmlContent, blockName);
        }

        #endregion

        #region Clone / Copy Block

        /// <summary>
        /// Clone a block: export from source, import to target location.
        /// </summary>
        public string CloneBlock(string softwarePath, string sourceBlockPath, string targetGroupPath, string newName)
        {
            // Export original
            var xml = ReadBlockAsXml(softwarePath, sourceBlockPath);

            // Replace block name in XML if new name given
            if (!string.IsNullOrEmpty(newName))
            {
                var sw = _blockService.GetPlcSoftware(softwarePath);
                var originalBlock = _blockService.FindBlock(sw, sourceBlockPath);
                if (originalBlock != null)
                {
                    xml = xml.Replace($"<Name>{originalBlock.Name}</Name>", $"<Name>{newName}</Name>");
                }
            }

            // Import at target
            return WriteBlockFromXml(softwarePath, targetGroupPath, xml, newName ?? "cloned_block");
        }

        /// <summary>
        /// Clone a type/UDT.
        /// </summary>
        public string CloneType(string softwarePath, string sourceTypeName, string targetGroupPath, string newName)
        {
            var xml = ReadTypeAsXml(softwarePath, sourceTypeName);

            if (!string.IsNullOrEmpty(newName))
            {
                xml = xml.Replace($"<Name>{sourceTypeName}</Name>", $"<Name>{newName}</Name>");
            }

            return WriteBlockFromXml(softwarePath, targetGroupPath, xml, newName ?? sourceTypeName);
        }

        #endregion

        #region Batch Operations

        /// <summary>
        /// Write multiple blocks from a combined SCL source containing several block declarations.
        /// Each FUNCTION_BLOCK, FUNCTION, DATA_BLOCK, TYPE declaration is treated as a separate block.
        /// </summary>
        public List<string> WriteMultipleBlocksFromScl(string softwarePath, string groupPath, string sclCode)
        {
            var sw = _blockService.GetPlcSoftware(softwarePath);
            var results = new List<string>();

            // The SCL source may contain multiple blocks - import it as a single source file
            // TIA Portal will create all blocks defined in the source
            var sourceName = $"MultiBlock_{Guid.NewGuid():N}";
            var tempFile = Path.Combine(_tempDir, $"{sourceName}.scl");

            try
            {
                File.WriteAllText(tempFile, sclCode, Encoding.UTF8);

                var existingSource = sw.ExternalSourceGroup.ExternalSources
                    .FirstOrDefault(s => s.Name == sourceName);
                if (existingSource != null)
                    try { existingSource.Delete(); } catch { }

                var source = sw.ExternalSourceGroup.ExternalSources.CreateFromFile(sourceName, tempFile);
                source.GenerateBlocksFromSource();
                try { source.Delete(); } catch { }

                results.Add("All blocks from SCL source imported successfully");
                _logger?.LogInformation("Imported multiple blocks from combined SCL source");
            }
            catch (Exception ex)
            {
                results.Add($"Error: {ex.Message}");
            }
            finally
            {
                CleanupFile(tempFile);
            }

            return results;
        }

        /// <summary>
        /// Export all blocks in a group and return a dictionary of name → XML content.
        /// </summary>
        public Dictionary<string, string> ExportGroupAsXml(string softwarePath, string groupPath)
        {
            var sw = _blockService.GetPlcSoftware(softwarePath);
            var group = _blockService.FindBlockGroup(sw, groupPath);
            var result = new Dictionary<string, string>();

            foreach (var block in group.Blocks)
            {
                try
                {
                    var tempFile = Path.Combine(_tempDir, $"exp_{SanitizeFileName(block.Name)}_{Guid.NewGuid():N}.xml");
                    try
                    {
                        block.Export(new FileInfo(tempFile), ExportOptions.WithDefaults);
                        result[block.Name] = File.ReadAllText(tempFile, Encoding.UTF8);
                    }
                    finally
                    {
                        CleanupFile(tempFile);
                    }
                }
                catch (Exception ex)
                {
                    result[block.Name] = $"<error>{ex.Message}</error>";
                }
            }

            return result;
        }

        #endregion

        #region Helpers

        private PlcType? FindType(PlcSoftware sw, string name)
        {
            var type = sw.TypeGroup.Types.FirstOrDefault(t =>
                t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (type != null) return type;

            return FindTypeInGroups(sw.TypeGroup.Groups, name);
        }

        private PlcType? FindTypeInGroups(PlcTypeUserGroupComposition groups, string name)
        {
            foreach (var group in groups)
            {
                var t = group.Types.FirstOrDefault(x =>
                    x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (t != null) return t;

                var found = FindTypeInGroups(group.Groups, name);
                if (found != null) return found;
            }
            return null;
        }

        private PlcTypeUserGroup? FindTypeGroup(PlcSoftware sw, string groupPath)
        {
            if (string.IsNullOrEmpty(groupPath)) return null;

            var parts = groupPath.Replace('\\', '/').Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            PlcTypeUserGroupComposition groups = sw.TypeGroup.Groups;
            PlcTypeUserGroup? current = null;

            foreach (var part in parts)
            {
                current = groups.FirstOrDefault(g =>
                    g.Name.Equals(part, StringComparison.OrdinalIgnoreCase));
                if (current == null) return null;
                groups = current.Groups;
            }
            return current;
        }

        private string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(name.Length);
            foreach (var c in name)
            {
                sb.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
            }
            return sb.ToString();
        }

        private void CleanupFile(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }

        #endregion
    }
}
