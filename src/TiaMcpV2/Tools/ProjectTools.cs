using ModelContextProtocol.Server;
using System;
using TiaMcpV2.Core;
using TiaMcpV2.Helpers;
using TiaMcpV2.Models;

namespace TiaMcpV2.Tools
{
using System.ComponentModel;
    [McpServerToolType]
    public static class ProjectTools
    {
        [McpServerTool(Name = "get_project"), Description("Get information about the currently open project (name, path, author, dates, attributes).")]
        public static string GetProject()
        {
            try
            {
                ServiceAccessor.Portal.EnsureProjectOpen();
                var project = ServiceAccessor.Portal.Project;
                return JsonHelper.ToJson(new ResponseProjectInfo
                {
                    Success = true,
                    Name = project?.Name,
                    Path = project?.Path?.ToString(),
                    Author = project?.Author,
                    Comment = project?.Comment?.ToString(),
                    CreationDate = project?.CreationTime,
                    LastModifiedDate = project?.LastModified,
                    Attributes = project != null ? AttributeHelper.GetAttributes(project) : null
                });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "open_project"), Description("Open a TIA Portal project from a file path (.ap20, .ap21, .als for multiuser sessions).")]
        public static string OpenProject(string projectPath)
        {
            try
            {
                ServiceAccessor.Portal.OpenProject(projectPath);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Opened project: {ServiceAccessor.Portal.State.ProjectName}" });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "create_project"), Description("Create a new TIA Portal project.")]
        public static string CreateProject(
            string projectPath,
            string projectName)
        {
            try
            {
                ServiceAccessor.Portal.CreateProject(projectPath, projectName);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Created project: {projectName}" });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "save_project"), Description("Save the currently open project.")]
        public static string SaveProject()
        {
            try
            {
                ServiceAccessor.Portal.SaveProject();
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = "Project saved" });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "save_as_project"), Description("Save the project to a new location.")]
        public static string SaveAsProject(string targetDirectory)
        {
            try
            {
                ServiceAccessor.Portal.SaveAsProject(targetDirectory);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Project saved to: {targetDirectory}" });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "close_project"), Description("Close the currently open project.")]
        public static string CloseProject()
        {
            try
            {
                ServiceAccessor.Portal.CloseProject();
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = "Project closed" });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }
    }
}
