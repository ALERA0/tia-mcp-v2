using Microsoft.Extensions.Logging;
using Siemens.Engineering;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;
using Siemens.Engineering.SW.ExternalSources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TiaMcpV2.Core;
using TiaMcpV2.Helpers;
using TiaMcpV2.Models;

namespace TiaMcpV2.Services
{
    public class BlockService
    {
        private readonly PortalEngine _portal;
        private readonly ILogger<BlockService>? _logger;

        public BlockService(PortalEngine portal, ILogger<BlockService>? logger = null)
        {
            _portal = portal;
            _logger = logger;
        }

        public PlcSoftware GetPlcSoftware(string devicePath)
        {
            var sw = _portal.FindPlcSoftware(devicePath);
            if (sw == null)
                throw new PortalException(PortalErrorCode.NotFound, $"PLC software not found for: {devicePath}");
            return sw;
        }

        public List<BlockInfo> GetBlocks(string softwarePath)
        {
            var sw = GetPlcSoftware(softwarePath);
            var blocks = new List<BlockInfo>();
            CollectBlocks(sw.BlockGroup, blocks, "");
            return blocks;
        }

        private void CollectBlocks(PlcBlockGroup group, List<BlockInfo> blocks, string parentPath)
        {
            var currentPath = string.IsNullOrEmpty(parentPath) ? group.Name : $"{parentPath}/{group.Name}";

            foreach (var block in group.Blocks)
            {
                blocks.Add(new BlockInfo
                {
                    Name = block.Name,
                    TypeName = block.GetType().Name,
                    Number = block.Number,
                    ProgrammingLanguage = Enum.GetName(typeof(ProgrammingLanguage), block.ProgrammingLanguage),
                    MemoryLayout = Enum.GetName(typeof(MemoryLayout), block.MemoryLayout),
                    IsConsistent = block.IsConsistent,
                    ModifiedDate = block.ModifiedDate,
                    IsKnowHowProtected = block.IsKnowHowProtected,
                    GroupPath = currentPath
                });
            }

            foreach (var sub in group.Groups)
            {
                CollectBlocks(sub, blocks, currentPath);
            }
        }

        public BlockGroupInfo GetBlocksWithHierarchy(string softwarePath)
        {
            var sw = GetPlcSoftware(softwarePath);
            return AttributeHelper.BuildBlockHierarchy(sw.BlockGroup);
        }

        public PlcBlock? FindBlock(PlcSoftware software, string blockPath)
        {
            var parts = blockPath.Replace('\\', '/').Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            PlcBlockComposition blocks = software.BlockGroup.Blocks;
            PlcBlockUserGroupComposition groups = software.BlockGroup.Groups;

            // Navigate to group
            for (int i = 0; i < parts.Length - 1; i++)
            {
                var subGroup = groups.FirstOrDefault(g =>
                    g.Name.Equals(parts[i], StringComparison.OrdinalIgnoreCase));
                if (subGroup == null)
                    return null;
                blocks = subGroup.Blocks;
                groups = subGroup.Groups;
            }

            // Find block
            var blockName = parts.Last();
            return blocks.FirstOrDefault(b =>
                b.Name.Equals(blockName, StringComparison.OrdinalIgnoreCase));
        }

        public PlcBlockUserGroup? FindBlockGroupAsUser(PlcSoftware software, string groupPath)
        {
            if (string.IsNullOrEmpty(groupPath))
                return null; // Indicates root group

            var parts = groupPath.Replace('\\', '/').Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            PlcBlockUserGroupComposition groups = software.BlockGroup.Groups;
            PlcBlockUserGroup? current = null;

            foreach (var part in parts)
            {
                current = groups.FirstOrDefault(g =>
                    g.Name.Equals(part, StringComparison.OrdinalIgnoreCase));
                if (current == null)
                    throw new PortalException(PortalErrorCode.NotFound, $"Block group not found: {groupPath}");
                groups = current.Groups;
            }

            return current;
        }

        public PlcBlockGroup FindBlockGroup(PlcSoftware software, string groupPath)
        {
            if (string.IsNullOrEmpty(groupPath))
                return software.BlockGroup;

            var userGroup = FindBlockGroupAsUser(software, groupPath);
            if (userGroup == null)
                return software.BlockGroup;
            return userGroup;
        }

        public PlcBlockGroup CreateBlockGroup(string softwarePath, string parentGroupPath, string groupName)
        {
            var sw = GetPlcSoftware(softwarePath);
            var parentGroup = FindBlockGroup(sw, parentGroupPath);
            var newGroup = parentGroup.Groups.Create(groupName);
            _logger?.LogInformation("Created block group: {Name}", groupName);
            return newGroup;
        }

        public void DeleteBlock(string softwarePath, string blockPath)
        {
            var sw = GetPlcSoftware(softwarePath);
            var block = FindBlock(sw, blockPath);
            if (block == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Block not found: {blockPath}");

            block.Delete();
            _logger?.LogInformation("Deleted block: {Path}", blockPath);
        }

        public void ImportSclSource(string softwarePath, string groupPath, string sclSourceCode, string sourceName)
        {
            var sw = GetPlcSoftware(softwarePath);
            var group = FindBlockGroup(sw, groupPath);

            var tempDir = Path.Combine(Path.GetTempPath(), "TiaMcpV2");
            Directory.CreateDirectory(tempDir);

            var tempFile = Path.Combine(tempDir, $"{sourceName}.scl");
            try
            {
                File.WriteAllText(tempFile, sclSourceCode);

                var externalSourceGroup = sw.ExternalSourceGroup;
                var source = externalSourceGroup.ExternalSources.CreateFromFile(sourceName, tempFile);
                source.GenerateBlocksFromSource();

                // Clean up source after generation
                try { source.Delete(); } catch { }

                _logger?.LogInformation("Imported SCL source: {Name}", sourceName);
            }
            finally
            {
                try { File.Delete(tempFile); } catch { }
            }
        }

        public void ExportBlock(string softwarePath, string blockPath, string exportPath)
        {
            var sw = GetPlcSoftware(softwarePath);
            var block = FindBlock(sw, blockPath);
            if (block == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Block not found: {blockPath}");

            Directory.CreateDirectory(exportPath);
            var filePath = Path.Combine(exportPath, $"{block.Name}.xml");
            var fileInfo = new FileInfo(filePath);

            if (fileInfo.Exists)
                fileInfo.Delete();

            block.Export(fileInfo, ExportOptions.WithDefaults);
            _logger?.LogInformation("Exported block: {Name} to {Path}", block.Name, filePath);
        }

        public int ExportAllBlocks(string softwarePath, string exportPath)
        {
            var sw = GetPlcSoftware(softwarePath);
            Directory.CreateDirectory(exportPath);

            int count = 0;
            ExportBlocksFromGroup(sw.BlockGroup, exportPath, ref count);
            return count;
        }

        private void ExportBlocksFromGroup(PlcBlockGroup group, string exportPath, ref int count)
        {
            foreach (var block in group.Blocks)
            {
                try
                {
                    var filePath = Path.Combine(exportPath, $"{block.Name}.xml");
                    var fileInfo = new FileInfo(filePath);
                    if (fileInfo.Exists) fileInfo.Delete();
                    block.Export(fileInfo, ExportOptions.WithDefaults);
                    count++;
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to export block: {Name}", block.Name);
                }
            }

            foreach (var sub in group.Groups)
            {
                var subDir = Path.Combine(exportPath, sub.Name);
                Directory.CreateDirectory(subDir);
                ExportBlocksFromGroup(sub, subDir, ref count);
            }
        }

        public void ImportBlock(string softwarePath, string importFilePath, string groupPath)
        {
            var sw = GetPlcSoftware(softwarePath);
            var group = FindBlockGroup(sw, groupPath);

            if (!File.Exists(importFilePath))
                throw new PortalException(PortalErrorCode.NotFound, $"Import file not found: {importFilePath}");

            group.Blocks.Import(new FileInfo(importFilePath), ImportOptions.Override);
            _logger?.LogInformation("Imported block from: {Path}", importFilePath);
        }

        public void ExportAsDocument(string softwarePath, string blockPath, string exportPath)
        {
            var sw = GetPlcSoftware(softwarePath);
            var block = FindBlock(sw, blockPath);
            if (block == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Block not found: {blockPath}");

            Directory.CreateDirectory(exportPath);
            var filePath = Path.Combine(exportPath, $"{block.Name}.xml");
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Exists) fileInfo.Delete();

            block.Export(fileInfo, ExportOptions.WithDefaults | ExportOptions.WithReadOnly);
            _logger?.LogInformation("Exported block as document: {Name}", block.Name);
        }

        public void ImportFromDocument(string softwarePath, string importFilePath, string groupPath, string importOption)
        {
            var sw = GetPlcSoftware(softwarePath);
            var group = FindBlockGroup(sw, groupPath);

            if (!File.Exists(importFilePath))
                throw new PortalException(PortalErrorCode.NotFound, $"Import file not found: {importFilePath}");

            var option = ParseImportOption(importOption);
            group.Blocks.Import(new FileInfo(importFilePath), option);
            _logger?.LogInformation("Imported from document: {Path}", importFilePath);
        }

        private ImportOptions ParseImportOption(string option)
        {
            switch (option?.ToLower())
            {
                case "createnew": return ImportOptions.None;
                case "skip": return ImportOptions.None;
                case "override":
                default: return ImportOptions.Override;
            }
        }
    }
}
