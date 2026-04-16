using ModelContextProtocol.Server;
using System;
using TiaMcpV2.Core;
using TiaMcpV2.Helpers;
using TiaMcpV2.Models;

namespace TiaMcpV2.Tools
{
using System.ComponentModel;
    [McpServerToolType]
    public static class TagTools
    {
        [McpServerTool(Name = "get_tag_tables"), Description("List all PLC tag tables.")]
        public static string GetTagTables(string softwarePath)
        {
            try { return JsonHelper.ToJson(new ResponseTagTables { Success = true, TagTables = ServiceAccessor.Tags.GetTagTables(softwarePath) }); }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "create_tag_table"), Description("Create a new PLC tag table.")]
        public static string CreateTagTable(
            string softwarePath,
            string tableName)
        {
            try
            {
                ServiceAccessor.Tags.CreateTagTable(softwarePath, tableName);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Created tag table: {tableName}" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "get_tags"), Description("List all tags in a PLC tag table.")]
        public static string GetTags(
            string softwarePath,
            string tagTableName)
        {
            try { return JsonHelper.ToJson(new ResponseTags { Success = true, Tags = ServiceAccessor.Tags.GetTags(softwarePath, tagTableName) }); }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "create_tag"), Description("Create a new PLC tag with address.")]
        public static string CreateTag(
            string softwarePath,
            string tagTableName,
            string tagName,
            string dataType,
            string logicalAddress)
        {
            try
            {
                ServiceAccessor.Tags.CreateTag(softwarePath, tagTableName, tagName, dataType, logicalAddress);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Created tag: {tagName} at {logicalAddress}" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "delete_tag"), Description("Delete a PLC tag.")]
        public static string DeleteTag(
            string softwarePath,
            string tagTableName,
            string tagName)
        {
            try
            {
                ServiceAccessor.Tags.DeleteTag(softwarePath, tagTableName, tagName);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Deleted tag: {tagName}" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "set_tag_attribute"), Description("Set an attribute on a PLC tag.")]
        public static string SetTagAttribute(
            string softwarePath,
            string tagTableName,
            string tagName,
            string attributeName,
            string value)
        {
            try
            {
                ServiceAccessor.Tags.SetTagAttribute(softwarePath, tagTableName, tagName, attributeName, value);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Set {attributeName} = {value} on {tagName}" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }
    }
}
