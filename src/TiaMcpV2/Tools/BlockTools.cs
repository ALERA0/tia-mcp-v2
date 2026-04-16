using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.Text.Json;
using TiaMcpV2.Core;
using TiaMcpV2.Helpers;
using TiaMcpV2.Models;

namespace TiaMcpV2.Tools
{
using System.ComponentModel;
    [McpServerToolType]
    public static class BlockTools
    {
        [McpServerTool(Name = "get_blocks"), Description("List all blocks (FB, FC, OB, DB) in a PLC. softwarePath is the device name e.g. 'PLC_1'.")]
        public static string GetBlocks(string softwarePath)
        {
            try
            {
                var blocks = ServiceAccessor.Blocks.GetBlocks(softwarePath);
                return JsonHelper.ToJson(new ResponseBlocks { Success = true, Blocks = blocks, Message = $"Found {blocks.Count} blocks" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "get_blocks_with_hierarchy"), Description("Get blocks organized in their folder hierarchy.")]
        public static string GetBlocksWithHierarchy(string softwarePath)
        {
            try
            {
                var hierarchy = ServiceAccessor.Blocks.GetBlocksWithHierarchy(softwarePath);
                return JsonHelper.ToJson(new ResponseBlocksHierarchy { Success = true, Hierarchy = hierarchy });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "get_block_info"), Description("Get detailed information about a specific block.")]
        public static string GetBlockInfo(
            string softwarePath,
            string blockPath)
        {
            try
            {
                var sw = ServiceAccessor.Blocks.GetPlcSoftware(softwarePath);
                var block = ServiceAccessor.Blocks.FindBlock(sw, blockPath);
                if (block == null) return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = $"Block not found: {blockPath}" });

                return JsonHelper.ToJson(new ResponseBlockDetail
                {
                    Success = true,
                    Name = block.Name,
                    TypeName = block.GetType().Name,
                    Number = block.Number,
                    ProgrammingLanguage = block.ProgrammingLanguage.ToString(),
                    MemoryLayout = block.MemoryLayout.ToString(),
                    IsConsistent = block.IsConsistent,
                    ModifiedDate = block.ModifiedDate,
                    IsKnowHowProtected = block.IsKnowHowProtected,
                    Attributes = AttributeHelper.GetAttributes(block)
                });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "import_scl_source"), Description("Import SCL source code to create blocks. This is the PRIMARY way to program PLC blocks. Write complete SCL code including FUNCTION_BLOCK/FUNCTION/ORGANIZATION_BLOCK/DATA_BLOCK/TYPE declaration with full interface and code body.")]
        public static string ImportSclSource(
            string softwarePath,
            string groupPath,
            string sclSourceCode,
            string sourceName)
        {
            try
            {
                ServiceAccessor.Blocks.ImportSclSource(softwarePath, groupPath, sclSourceCode, sourceName);
                return JsonHelper.ToJson(new ResponseImport { Success = true, ImportedName = sourceName, Message = "SCL source imported and compiled successfully" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "create_block_group"), Description("Create a block group (folder) in the program blocks tree.")]
        public static string CreateBlockGroup(
            string softwarePath,
            string parentGroupPath,
            string groupName)
        {
            try
            {
                ServiceAccessor.Blocks.CreateBlockGroup(softwarePath, parentGroupPath, groupName);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Created group: {groupName}" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "delete_block"), Description("Delete a block from the PLC program.")]
        public static string DeleteBlock(
            string softwarePath,
            string blockPath)
        {
            try
            {
                ServiceAccessor.Blocks.DeleteBlock(softwarePath, blockPath);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Deleted block: {blockPath}" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "export_block"), Description("Export a block to XML file.")]
        public static string ExportBlock(
            string softwarePath,
            string blockPath,
            string exportPath)
        {
            try
            {
                ServiceAccessor.Blocks.ExportBlock(softwarePath, blockPath, exportPath);
                return JsonHelper.ToJson(new ResponseExport { Success = true, ExportPath = exportPath });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "export_blocks"), Description("Export all blocks to a directory.")]
        public static string ExportBlocks(
            string softwarePath,
            string exportPath)
        {
            try
            {
                var count = ServiceAccessor.Blocks.ExportAllBlocks(softwarePath, exportPath);
                return JsonHelper.ToJson(new ResponseExportAll { Success = true, ExportedCount = count, ExportDirectory = exportPath });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "import_block"), Description("Import a block from an XML file.")]
        public static string ImportBlock(
            string softwarePath,
            string importFilePath,
            string groupPath)
        {
            try
            {
                ServiceAccessor.Blocks.ImportBlock(softwarePath, importFilePath, groupPath);
                return JsonHelper.ToJson(new ResponseImport { Success = true, Message = "Block imported" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "generate_block_xml"), Description("Generate and import a GlobalDB with specified member variables.")]
        public static string GenerateBlockXml(
            string softwarePath,
            string groupPath,
            string blockName,
            int blockNumber,
            [Description("JSON array of members: [{\"Name\":\"Var1\",\"DataType\":\"Real\",\"StartValue\":\"0.0\",\"Comment\":\"desc\"}]")] string membersJson)
        {
            try
            {
                var members = JsonSerializer.Deserialize<List<MemberDefinition>>(membersJson);
                ServiceAccessor.XmlGenerator.GenerateAndImportGlobalDb(softwarePath, groupPath, blockName, blockNumber, members);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Generated GlobalDB: {blockName}" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }
    }
}
