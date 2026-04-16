using Microsoft.Extensions.Logging;
using Siemens.Engineering;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TiaMcpV2.Core;
using TiaMcpV2.Helpers;
using TiaMcpV2.Models;

namespace TiaMcpV2.Services
{
    public class TypeService
    {
        private readonly PortalEngine _portal;
        private readonly BlockService _blockService;
        private readonly ILogger<TypeService>? _logger;

        public TypeService(PortalEngine portal, BlockService blockService, ILogger<TypeService>? logger = null)
        {
            _portal = portal;
            _blockService = blockService;
            _logger = logger;
        }

        public List<TypeInfo> GetTypes(string softwarePath)
        {
            var sw = _blockService.GetPlcSoftware(softwarePath);
            var types = new List<TypeInfo>();
            CollectTypes(sw.TypeGroup, types, "");
            return types;
        }

        private void CollectTypes(PlcTypeGroup group, List<TypeInfo> types, string parentPath)
        {
            var currentPath = string.IsNullOrEmpty(parentPath) ? group.Name : $"{parentPath}/{group.Name}";

            foreach (var type in group.Types)
            {
                types.Add(new TypeInfo
                {
                    Name = type.Name,
                    GroupPath = currentPath,
                    Attributes = AttributeHelper.GetAttributes(type)
                });
            }

            foreach (var sub in group.Groups)
            {
                CollectTypes(sub, types, currentPath);
            }
        }

        public PlcTypeUserGroup FindTypeGroup(string softwarePath, string groupPath)
        {
            var sw = _blockService.GetPlcSoftware(softwarePath);

            if (string.IsNullOrEmpty(groupPath))
                return null; // Return null to indicate root group

            var parts = groupPath.Replace('\\', '/').Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            PlcTypeUserGroupComposition groups = sw.TypeGroup.Groups;
            PlcTypeUserGroup current = null;

            foreach (var part in parts)
            {
                current = groups.FirstOrDefault(g =>
                    g.Name.Equals(part, StringComparison.OrdinalIgnoreCase));
                if (current == null)
                    throw new PortalException(PortalErrorCode.NotFound, $"Type group not found: {groupPath}");
                groups = current.Groups;
            }

            return current;
        }

        public PlcTypeUserGroup CreateTypeGroup(string softwarePath, string parentGroupPath, string groupName)
        {
            var sw = _blockService.GetPlcSoftware(softwarePath);
            PlcTypeUserGroupComposition groups;

            if (string.IsNullOrEmpty(parentGroupPath))
            {
                groups = sw.TypeGroup.Groups;
            }
            else
            {
                var parent = FindTypeGroup(softwarePath, parentGroupPath);
                groups = parent.Groups;
            }

            var newGroup = groups.Create(groupName);
            _logger?.LogInformation("Created type group: {Name}", groupName);
            return newGroup;
        }

        public void ExportType(string softwarePath, string typeName, string exportPath)
        {
            var sw = _blockService.GetPlcSoftware(softwarePath);
            var type = FindType(sw, typeName);
            if (type == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Type not found: {typeName}");

            Directory.CreateDirectory(exportPath);
            var filePath = Path.Combine(exportPath, $"{type.Name}.xml");
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Exists) fileInfo.Delete();

            type.Export(fileInfo, ExportOptions.WithDefaults);
            _logger?.LogInformation("Exported type: {Name}", typeName);
        }

        public int ExportAllTypes(string softwarePath, string exportPath)
        {
            var sw = _blockService.GetPlcSoftware(softwarePath);
            Directory.CreateDirectory(exportPath);

            int count = 0;
            ExportTypesFromGroup(sw.TypeGroup, exportPath, ref count);
            return count;
        }

        private void ExportTypesFromGroup(PlcTypeGroup group, string exportPath, ref int count)
        {
            foreach (var type in group.Types)
            {
                try
                {
                    var filePath = Path.Combine(exportPath, $"{type.Name}.xml");
                    var fileInfo = new FileInfo(filePath);
                    if (fileInfo.Exists) fileInfo.Delete();
                    type.Export(fileInfo, ExportOptions.WithDefaults);
                    count++;
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to export type: {Name}", type.Name);
                }
            }

            foreach (var sub in group.Groups)
            {
                var subDir = Path.Combine(exportPath, sub.Name);
                Directory.CreateDirectory(subDir);
                ExportTypesFromGroup(sub, subDir, ref count);
            }
        }

        public void ImportType(string softwarePath, string importFilePath, string groupPath)
        {
            var sw = _blockService.GetPlcSoftware(softwarePath);

            if (!File.Exists(importFilePath))
                throw new PortalException(PortalErrorCode.NotFound, $"Import file not found: {importFilePath}");

            if (string.IsNullOrEmpty(groupPath))
            {
                sw.TypeGroup.Types.Import(new FileInfo(importFilePath), ImportOptions.Override);
            }
            else
            {
                var group = FindTypeGroup(softwarePath, groupPath);
                group.Types.Import(new FileInfo(importFilePath), ImportOptions.Override);
            }

            _logger?.LogInformation("Imported type from: {Path}", importFilePath);
        }

        private PlcType? FindType(PlcSoftware software, string typeName)
        {
            var type = software.TypeGroup.Types.FirstOrDefault(t =>
                t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));
            if (type != null) return type;

            return FindTypeInGroups(software.TypeGroup.Groups, typeName);
        }

        private PlcType? FindTypeInGroups(PlcTypeUserGroupComposition groups, string typeName)
        {
            foreach (var group in groups)
            {
                var type = group.Types.FirstOrDefault(t =>
                    t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));
                if (type != null) return type;

                var found = FindTypeInGroups(group.Groups, typeName);
                if (found != null) return found;
            }
            return null;
        }
    }
}
