using System.ComponentModel;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.Text.Json;
using TiaMcpV2.Core;
using TiaMcpV2.Helpers;
using TiaMcpV2.Models;

namespace TiaMcpV2.Tools
{
    [McpServerToolType]
    public static class TechnologyObjectTools
    {
        [McpServerTool(Name = "get_to_types"), Description("List all available Technology Object types that can be created: Motion axes (TO_SpeedAxis, TO_PositioningAxis, TO_SynchronousAxis, TO_ExternalEncoder), PID controllers (PID_Compact, PID_3Step, PID_Temp), Cams (TO_Cam, TO_CamTrack, TO_OutputCam), and more.")]
        public static string GetToTypes()
        {
            try
            {
                var types = ServiceAccessor.TechObjects.GetAvailableToTypes();
                return JsonHelper.ToJson(new { Success = true, Types = types, Count = types.Count, Message = "Use create_technology_object to create any of these types" });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "list_technology_objects"), Description("List all Technology Objects (motion axes, PID controllers, cams, encoders) in the PLC with their type, version, consistency status and parameters.")]
        public static string ListTechnologyObjects(string softwarePath)
        {
            try
            {
                var tos = ServiceAccessor.TechObjects.GetTechnologicalObjects(softwarePath);
                return JsonHelper.ToJson(new { Success = true, TechnologyObjects = tos, Count = tos.Count });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "create_technology_object"), Description("Create a Technology Object in the PLC. Types: TO_SpeedAxis, TO_PositioningAxis, TO_SynchronousAxis, TO_ExternalEncoder, PID_Compact, PID_3Step, PID_Temp, TO_Cam, TO_CamTrack, TO_OutputCam, TO_MeasuringInput, TO_Kinematics. Version must match TIA Portal version (e.g. '7.0' for V20 motion, '2.5' for PID in V20).")]
        public static string CreateTechnologyObject(
            string softwarePath,
            string toName,
            string toType,
            string version)
        {
            try
            {
                var to = ServiceAccessor.TechObjects.CreateTechnologicalObject(softwarePath, toName, toType, version);
                return JsonHelper.ToJson(new
                {
                    Success = true,
                    Message = $"Created Technology Object: {toName} ({toType} v{version})",
                    Name = to.Name,
                    Number = to.Number,
                    InstanceOfName = to.InstanceOfName,
                    OfSystemLibElement = to.OfSystemLibElement,
                    OfSystemLibVersion = to.OfSystemLibVersion?.ToString()
                });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "create_technology_object_in_group"), Description("Create a Technology Object inside a specific TO group/folder.")]
        public static string CreateTechnologyObjectInGroup(
            string softwarePath,
            string groupPath,
            string toName,
            string toType,
            string version)
        {
            try
            {
                var to = ServiceAccessor.TechObjects.CreateTechnologicalObjectInGroup(softwarePath, groupPath, toName, toType, version);
                return JsonHelper.ToJson(new { Success = true, Message = $"Created TO: {toName} in {groupPath}", Name = to.Name, Number = to.Number });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "create_to_group"), Description("Create a folder/group in the Technology Objects tree for organizing TOs.")]
        public static string CreateToGroup(
            string softwarePath,
            string parentPath,
            string groupName)
        {
            try
            {
                ServiceAccessor.TechObjects.CreateToGroup(softwarePath, parentPath, groupName);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Created TO group: {groupName}" });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "get_to_info"), Description("Get detailed information about a Technology Object: type, version, all configuration parameters, consistency status.")]
        public static string GetToInfo(
            string softwarePath,
            string toName)
        {
            try
            {
                var info = ServiceAccessor.TechObjects.GetToInfo(softwarePath, toName);
                return JsonHelper.ToJson(new { Success = true, TechnologyObject = info });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "get_to_parameters"), Description("Get all parameters of a Technology Object with their current values. Use this to see the full configuration of an axis, PID controller, or other TO.")]
        public static string GetToParameters(
            string softwarePath,
            string toName)
        {
            try
            {
                var parameters = ServiceAccessor.TechObjects.GetToParameters(softwarePath, toName);
                return JsonHelper.ToJson(new { Success = true, Parameters = parameters, Count = parameters.Count });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "get_to_parameter"), Description("Get a single parameter value from a Technology Object. Use get_to_parameters first to see all available parameter names.")]
        public static string GetToParameter(
            string softwarePath,
            string toName,
            string parameterName)
        {
            try
            {
                var value = ServiceAccessor.TechObjects.GetToParameter(softwarePath, toName, parameterName);
                return JsonHelper.ToJson(new { Success = true, Parameter = parameterName, Value = value });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "set_to_parameter"), Description("Set a single parameter on a Technology Object. Examples: axis max velocity, PID Kp/Ti/Td, encoder resolution, position limits. Use get_to_parameters to see parameter names first.")]
        public static string SetToParameter(
            string softwarePath,
            string toName,
            string parameterName,
            string value)
        {
            try
            {
                ServiceAccessor.TechObjects.SetToParameter(softwarePath, toName, parameterName, value);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Set {toName}.{parameterName} = {value}" });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "set_to_parameters"), Description("Set multiple parameters at once on a Technology Object. Pass a JSON object with parameter names and values.")]
        public static string SetToParameters(
            string softwarePath,
            string toName,
            string parametersJson)
        {
            try
            {
                var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(parametersJson);
                if (parameters == null)
                    return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = "Invalid JSON parameters" });

                // Convert JsonElement values to proper types
                var converted = new Dictionary<string, object>();
                foreach (var kvp in parameters)
                {
                    if (kvp.Value is JsonElement je)
                    {
                        switch (je.ValueKind)
                        {
                            case JsonValueKind.Number:
                                converted[kvp.Key] = je.TryGetDouble(out var d) ? (object)d : je.GetRawText();
                                break;
                            case JsonValueKind.True: case JsonValueKind.False:
                                converted[kvp.Key] = je.GetBoolean();
                                break;
                            default:
                                converted[kvp.Key] = je.GetRawText().Trim('"');
                                break;
                        }
                    }
                    else
                    {
                        converted[kvp.Key] = kvp.Value;
                    }
                }

                ServiceAccessor.TechObjects.SetToParameters(softwarePath, toName, converted);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Set {converted.Count} parameters on {toName}" });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "export_technology_object"), Description("Export a Technology Object configuration to XML file for backup or transfer.")]
        public static string ExportTechnologyObject(
            string softwarePath,
            string toName,
            string exportPath)
        {
            try
            {
                ServiceAccessor.TechObjects.ExportTo(softwarePath, toName, exportPath);
                return JsonHelper.ToJson(new ResponseExport { Success = true, ExportPath = exportPath, Message = $"Exported TO: {toName}" });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "import_technology_object"), Description("Import a Technology Object from an XML file.")]
        public static string ImportTechnologyObject(
            string softwarePath,
            string importFilePath,
            string groupPath)
        {
            try
            {
                ServiceAccessor.TechObjects.ImportTo(softwarePath, importFilePath, groupPath);
                return JsonHelper.ToJson(new ResponseImport { Success = true, Message = $"Imported TO from: {importFilePath}" });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "delete_technology_object"), Description("Delete a Technology Object from the PLC.")]
        public static string DeleteTechnologyObject(
            string softwarePath,
            string toName)
        {
            try
            {
                ServiceAccessor.TechObjects.DeleteTo(softwarePath, toName);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Deleted TO: {toName}" });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }
    }
}
