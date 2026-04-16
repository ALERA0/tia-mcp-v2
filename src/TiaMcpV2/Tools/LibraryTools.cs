using ModelContextProtocol.Server;
using System;
using TiaMcpV2.Core;
using TiaMcpV2.Helpers;
using TiaMcpV2.Models;

namespace TiaMcpV2.Tools
{
using System.ComponentModel;
    [McpServerToolType]
    public static class LibraryTools
    {
        [McpServerTool(Name = "get_project_library"), Description("Get the project library contents (master copies and library types).")]
        public static string GetProjectLibrary()
        {
            try { return JsonHelper.ToJson(new ResponseProjectLibrary { Success = true, Library = ServiceAccessor.Library.GetProjectLibrary() }); }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "get_global_libraries"), Description("List all open global libraries.")]
        public static string GetGlobalLibraries()
        {
            try { return JsonHelper.ToJson(new ResponseGlobalLibraries { Success = true, Libraries = ServiceAccessor.Library.GetGlobalLibraries() }); }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "copy_from_library"), Description("Copy a master copy from the project library to a PLC's program blocks.")]
        public static string CopyFromLibrary(
            string masterCopyPath,
            string targetPath)
        {
            try
            {
                ServiceAccessor.Library.CopyFromLibrary(masterCopyPath, targetPath);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Copied: {masterCopyPath}" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }
    }
}
