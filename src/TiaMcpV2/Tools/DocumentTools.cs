using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.IO;
using TiaMcpV2.Core;
using TiaMcpV2.Helpers;
using TiaMcpV2.Models;

namespace TiaMcpV2.Tools
{
using System.ComponentModel;
    [McpServerToolType]
    public static class DocumentTools
    {
        [McpServerTool(Name = "export_as_documents"), Description("Export a block as a SimaticML document (XML with full metadata).")]
        public static string ExportAsDocuments(
            string softwarePath,
            string blockPath,
            string exportPath)
        {
            try
            {
                ServiceAccessor.Blocks.ExportAsDocument(softwarePath, blockPath, exportPath);
                return JsonHelper.ToJson(new ResponseExport { Success = true, ExportPath = exportPath });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "export_blocks_as_documents"), Description("Export all blocks as SimaticML documents.")]
        public static string ExportBlocksAsDocuments(
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

        [McpServerTool(Name = "import_from_documents"), Description("Import a block from a SimaticML document.")]
        public static string ImportFromDocuments(
            string softwarePath,
            string importFilePath,
            string groupPath,
            string importOption)
        {
            try
            {
                ServiceAccessor.Blocks.ImportFromDocument(softwarePath, importFilePath, groupPath, importOption);
                return JsonHelper.ToJson(new ResponseImport { Success = true, Message = "Document imported" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "import_blocks_from_documents"), Description("Import multiple blocks from SimaticML documents in a directory.")]
        public static string ImportBlocksFromDocuments(
            string softwarePath,
            string importDirectory,
            string groupPath,
            string importOption)
        {
            try
            {
                int count = 0;
                var imported = new List<string>();
                foreach (var file in Directory.GetFiles(importDirectory, "*.xml"))
                {
                    try
                    {
                        ServiceAccessor.Blocks.ImportFromDocument(softwarePath, file, groupPath, importOption);
                        imported.Add(Path.GetFileName(file));
                        count++;
                    }
                    catch { }
                }
                return JsonHelper.ToJson(new ResponseImportMultiple { Success = true, ImportedCount = count, ImportedItems = imported });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }
    }
}
