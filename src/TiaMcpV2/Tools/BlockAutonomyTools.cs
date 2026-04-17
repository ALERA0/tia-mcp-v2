using System.ComponentModel;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using TiaMcpV2.Core;
using TiaMcpV2.Helpers;
using TiaMcpV2.Models;

namespace TiaMcpV2.Tools
{
    [McpServerToolType]
    public static class BlockAutonomyTools
    {
        [McpServerTool(Name = "write_block"), Description("Write/create a PLC block from SCL source code. This is the PRIMARY tool for programming. Supports ALL block types: FUNCTION_BLOCK, FUNCTION, ORGANIZATION_BLOCK, DATA_BLOCK, TYPE (UDT). If the block already exists it will be overwritten. No external file manipulation needed — just provide the SCL code and it goes straight into TIA Portal.")]
        public static string WriteBlock(
            string softwarePath,
            string groupPath,
            string sclCode,
            string blockName)
        {
            try
            {
                var result = ServiceAccessor.BlockAutonomy.WriteBlockFromScl(softwarePath, groupPath, sclCode, blockName);
                return JsonHelper.ToJson(new { Success = true, Message = result, BlockName = blockName });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "write_block_xml"), Description("Write/create a PLC block from SimaticML XML content. Use this for LAD/FBD blocks or when you have XML from a previous export. Supports FB, FC, OB, DB, UDT. Imports with override — existing block with same name is replaced.")]
        public static string WriteBlockXml(
            string softwarePath,
            string groupPath,
            string xmlContent,
            string blockName)
        {
            try
            {
                var result = ServiceAccessor.BlockAutonomy.WriteBlockFromXml(softwarePath, groupPath, xmlContent, blockName);
                return JsonHelper.ToJson(new { Success = true, Message = result, BlockName = blockName });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "read_block_xml"), Description("Read/export a block's full XML content (SimaticML format). Returns the complete XML string for any block type (FB, FC, OB, DB) in any programming language (SCL, LAD, FBD, STL, GRAPH). Use this to inspect block code, prepare modifications, or backup blocks.")]
        public static string ReadBlockXml(
            string softwarePath,
            string blockPath)
        {
            try
            {
                var xml = ServiceAccessor.BlockAutonomy.ReadBlockAsXml(softwarePath, blockPath);
                return JsonHelper.ToJson(new { Success = true, BlockPath = blockPath, XmlContent = xml, Length = xml.Length });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "read_type_xml"), Description("Read/export a UDT/Type's full XML content. Returns SimaticML XML for the data type definition.")]
        public static string ReadTypeXml(
            string softwarePath,
            string typeName)
        {
            try
            {
                var xml = ServiceAccessor.BlockAutonomy.ReadTypeAsXml(softwarePath, typeName);
                return JsonHelper.ToJson(new { Success = true, TypeName = typeName, XmlContent = xml, Length = xml.Length });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "update_block"), Description("Update an existing block with new SCL code. The old block is deleted and the new one is imported. Use this when you need to modify a block's code — provide the COMPLETE new SCL source (not just the diff).")]
        public static string UpdateBlock(
            string softwarePath,
            string blockPath,
            string sclCode)
        {
            try
            {
                var result = ServiceAccessor.BlockAutonomy.UpdateBlockFromScl(softwarePath, blockPath, sclCode);
                return JsonHelper.ToJson(new { Success = true, Message = result, BlockPath = blockPath });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "update_block_xml"), Description("Update an existing block with new XML content. Uses override import — the existing block is replaced. Works for all block types and programming languages.")]
        public static string UpdateBlockXml(
            string softwarePath,
            string blockPath,
            string xmlContent)
        {
            try
            {
                var result = ServiceAccessor.BlockAutonomy.UpdateBlockFromXml(softwarePath, blockPath, xmlContent);
                return JsonHelper.ToJson(new { Success = true, Message = result, BlockPath = blockPath });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "clone_block"), Description("Clone/copy a block to a new location with a new name. Exports the original, optionally renames it, and imports at the target. Works for all block types.")]
        public static string CloneBlock(
            string softwarePath,
            string sourceBlockPath,
            string targetGroupPath,
            string newName)
        {
            try
            {
                var result = ServiceAccessor.BlockAutonomy.CloneBlock(softwarePath, sourceBlockPath, targetGroupPath, newName);
                return JsonHelper.ToJson(new { Success = true, Message = result, Source = sourceBlockPath, NewName = newName });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "clone_type"), Description("Clone/copy a UDT/Type to a new location with a new name.")]
        public static string CloneType(
            string softwarePath,
            string sourceTypeName,
            string targetGroupPath,
            string newName)
        {
            try
            {
                var result = ServiceAccessor.BlockAutonomy.CloneType(softwarePath, sourceTypeName, targetGroupPath, newName);
                return JsonHelper.ToJson(new { Success = true, Message = result, Source = sourceTypeName, NewName = newName });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "write_multiple_blocks"), Description("Write multiple blocks from a single SCL source that contains several block declarations (FUNCTION_BLOCK, FUNCTION, DATA_BLOCK, TYPE). All blocks in the source are created at once. Use this for batch operations like creating a motor control library with FB + UDT + DB together.")]
        public static string WriteMultipleBlocks(
            string softwarePath,
            string groupPath,
            string sclCode)
        {
            try
            {
                var results = ServiceAccessor.BlockAutonomy.WriteMultipleBlocksFromScl(softwarePath, groupPath, sclCode);
                return JsonHelper.ToJson(new { Success = true, Results = results, Message = "Multiple blocks imported" });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "export_group_xml"), Description("Export all blocks in a group/folder and return their XML content as a dictionary. Use this to backup or inspect an entire group of blocks.")]
        public static string ExportGroupXml(
            string softwarePath,
            string groupPath)
        {
            try
            {
                var blocks = ServiceAccessor.BlockAutonomy.ExportGroupAsXml(softwarePath, groupPath);
                return JsonHelper.ToJson(new { Success = true, BlockCount = blocks.Count, Blocks = blocks.Keys.ToList(), Message = $"Exported {blocks.Count} blocks from group" });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }
    }
}
