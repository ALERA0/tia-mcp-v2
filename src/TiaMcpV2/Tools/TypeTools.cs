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
    public static class TypeTools
    {
        [McpServerTool(Name = "get_types"), Description("List all PLC types (UDTs).")]
        public static string GetTypes(string softwarePath)
        {
            try { return JsonHelper.ToJson(new ResponseTypes { Success = true, Types = ServiceAccessor.Types.GetTypes(softwarePath) }); }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "create_type"), Description("Create a UDT (User Defined Type) with specified member fields.")]
        public static string CreateType(
            string softwarePath,
            string groupPath,
            string typeName,
            [Description("JSON array of members: [{\"Name\":\"Field1\",\"DataType\":\"Int\"},{\"Name\":\"Field2\",\"DataType\":\"Real\"}]")] string membersJson)
        {
            try
            {
                var members = JsonSerializer.Deserialize<List<MemberDefinition>>(membersJson);
                ServiceAccessor.XmlGenerator.GenerateAndImportUdt(softwarePath, groupPath, typeName, members);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Created UDT: {typeName}" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "create_type_group"), Description("Create a type group (folder) in the PLC types tree.")]
        public static string CreateTypeGroup(
            string softwarePath,
            string parentGroupPath,
            string groupName)
        {
            try
            {
                ServiceAccessor.Types.CreateTypeGroup(softwarePath, parentGroupPath, groupName);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Created type group: {groupName}" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "export_type"), Description("Export a PLC type (UDT) to XML.")]
        public static string ExportType(
            string softwarePath,
            string typeName,
            string exportPath)
        {
            try
            {
                ServiceAccessor.Types.ExportType(softwarePath, typeName, exportPath);
                return JsonHelper.ToJson(new ResponseExport { Success = true, ExportPath = exportPath });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "export_types"), Description("Export all PLC types to a directory.")]
        public static string ExportTypes(
            string softwarePath,
            string exportPath)
        {
            try
            {
                var count = ServiceAccessor.Types.ExportAllTypes(softwarePath, exportPath);
                return JsonHelper.ToJson(new ResponseExportAll { Success = true, ExportedCount = count, ExportDirectory = exportPath });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "import_type"), Description("Import a PLC type (UDT) from XML.")]
        public static string ImportType(
            string softwarePath,
            string importFilePath,
            string groupPath)
        {
            try
            {
                ServiceAccessor.Types.ImportType(softwarePath, importFilePath, groupPath);
                return JsonHelper.ToJson(new ResponseImport { Success = true, Message = "Type imported" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }
    }
}
