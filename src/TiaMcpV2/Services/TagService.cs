using Microsoft.Extensions.Logging;
using Siemens.Engineering.SW.Tags;
using System;
using System.Collections.Generic;
using System.Linq;
using TiaMcpV2.Core;
using TiaMcpV2.Models;

namespace TiaMcpV2.Services
{
    public class TagService
    {
        private readonly PortalEngine _portal;
        private readonly BlockService _blockService;
        private readonly ILogger<TagService>? _logger;

        public TagService(PortalEngine portal, BlockService blockService, ILogger<TagService>? logger = null)
        {
            _portal = portal;
            _blockService = blockService;
            _logger = logger;
        }

        public List<TagTableInfo> GetTagTables(string softwarePath)
        {
            var sw = _blockService.GetPlcSoftware(softwarePath);
            var result = new List<TagTableInfo>();

            foreach (var table in sw.TagTableGroup.TagTables)
            {
                result.Add(new TagTableInfo
                {
                    Name = table.Name,
                    TagCount = table.Tags.Count
                });
            }

            // Also traverse sub-groups
            CollectTagTablesFromGroups(sw.TagTableGroup.Groups, result);
            return result;
        }

        private void CollectTagTablesFromGroups(PlcTagTableUserGroupComposition groups, List<TagTableInfo> result)
        {
            foreach (var group in groups)
            {
                foreach (var table in group.TagTables)
                {
                    result.Add(new TagTableInfo
                    {
                        Name = table.Name,
                        TagCount = table.Tags.Count
                    });
                }
                CollectTagTablesFromGroups(group.Groups, result);
            }
        }

        public PlcTagTable CreateTagTable(string softwarePath, string tableName)
        {
            var sw = _blockService.GetPlcSoftware(softwarePath);
            var table = sw.TagTableGroup.TagTables.Create(tableName);
            _logger?.LogInformation("Created tag table: {Name}", tableName);
            return table;
        }

        public List<TagInfo> GetTags(string softwarePath, string tagTableName)
        {
            var table = FindTagTable(softwarePath, tagTableName);
            var result = new List<TagInfo>();

            foreach (var tag in table.Tags)
            {
                result.Add(new TagInfo
                {
                    Name = tag.Name,
                    DataType = tag.DataTypeName,
                    LogicalAddress = tag.LogicalAddress,
                    Comment = tag.Comment?.Items?.FirstOrDefault()?.Text
                });
            }

            return result;
        }

        public PlcTag CreateTag(string softwarePath, string tagTableName, string tagName, string dataType, string logicalAddress)
        {
            var table = FindTagTable(softwarePath, tagTableName);
            var tag = table.Tags.Create(tagName, dataType, logicalAddress);
            _logger?.LogInformation("Created tag: {Name} ({Type}) at {Addr}", tagName, dataType, logicalAddress);
            return tag;
        }

        public void DeleteTag(string softwarePath, string tagTableName, string tagName)
        {
            var table = FindTagTable(softwarePath, tagTableName);
            var tag = table.Tags.FirstOrDefault(t =>
                t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase));

            if (tag == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Tag not found: {tagName}");

            tag.Delete();
            _logger?.LogInformation("Deleted tag: {Name}", tagName);
        }

        public void SetTagAttribute(string softwarePath, string tagTableName, string tagName, string attributeName, object value)
        {
            var table = FindTagTable(softwarePath, tagTableName);
            var tag = table.Tags.FirstOrDefault(t =>
                t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase));

            if (tag == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Tag not found: {tagName}");

            tag.SetAttribute(attributeName, value);
            _logger?.LogInformation("Set tag attribute {Attr}={Val} on {Tag}", attributeName, value, tagName);
        }

        private PlcTagTable FindTagTable(string softwarePath, string tagTableName)
        {
            var sw = _blockService.GetPlcSoftware(softwarePath);

            // Search in root
            var table = sw.TagTableGroup.TagTables.FirstOrDefault(t =>
                t.Name.Equals(tagTableName, StringComparison.OrdinalIgnoreCase));

            if (table != null) return table;

            // Search in sub-groups
            table = FindTagTableInGroups(sw.TagTableGroup.Groups, tagTableName);
            if (table != null) return table;

            throw new PortalException(PortalErrorCode.NotFound, $"Tag table not found: {tagTableName}");
        }

        private PlcTagTable? FindTagTableInGroups(PlcTagTableUserGroupComposition groups, string name)
        {
            foreach (var group in groups)
            {
                var table = group.TagTables.FirstOrDefault(t =>
                    t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (table != null) return table;

                var found = FindTagTableInGroups(group.Groups, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
