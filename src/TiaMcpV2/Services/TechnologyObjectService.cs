using Microsoft.Extensions.Logging;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;
using Siemens.Engineering.SW.TechnologicalObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TiaMcpV2.Core;
using TiaMcpV2.Helpers;

namespace TiaMcpV2.Services
{
    /// <summary>
    /// Technology Object service — creates, reads, modifies, exports/imports
    /// TO_SpeedAxis, TO_PositioningAxis, TO_ExternalEncoder, PID_Compact, PID_3Step, etc.
    /// </summary>
    public class TechnologyObjectService
    {
        private readonly PortalEngine _portal;
        private readonly BlockService _blockService;
        private readonly ILogger<TechnologyObjectService>? _logger;

        // Known system library element names for TO creation
        public static readonly Dictionary<string, string> KnownToTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Motion Control axes
            ["TO_SpeedAxis"]                = "TO_SpeedAxis",
            ["TO_PositioningAxis"]          = "TO_PositioningAxis",
            ["TO_SynchronousAxis"]          = "TO_SynchronousAxis",
            ["TO_ExternalEncoder"]          = "TO_ExternalEncoder",
            ["TO_OutputCam"]                = "TO_OutputCam",
            ["TO_CamTrack"]                 = "TO_CamTrack",
            ["TO_MeasuringInput"]           = "TO_MeasuringInput",
            ["TO_Cam"]                      = "TO_Cam",
            ["TO_Kinematics"]              = "TO_Kinematics_2D",
            ["TO_LeadingAxisProxy"]         = "TO_LeadingAxisProxy",
            // PID controllers
            ["PID_Compact"]                 = "PID_Compact",
            ["PID_3Step"]                   = "PID_3Step",
            ["PID_Temp"]                    = "PID_Temp",
            // Counting/Measurement
            ["TO_HighSpeedCounter"]         = "High_Speed_Counter",
        };

        public TechnologyObjectService(PortalEngine portal, BlockService blockService, ILogger<TechnologyObjectService>? logger = null)
        {
            _portal = portal;
            _blockService = blockService;
            _logger = logger;
        }

        #region List & Find

        /// <summary>
        /// Get all Technology Objects from the TechnologicalObjectGroup.
        /// </summary>
        public List<Dictionary<string, object?>> GetTechnologicalObjects(string softwarePath)
        {
            var sw = _blockService.GetPlcSoftware(softwarePath);
            var toGroup = sw.TechnologicalObjectGroup;
            var result = new List<Dictionary<string, object?>>();

            CollectTOs(toGroup, result, "");
            return result;
        }

        private void CollectTOs(TechnologicalInstanceDBGroup group, List<Dictionary<string, object?>> result, string parentPath)
        {
            var currentPath = string.IsNullOrEmpty(parentPath) ? "" : parentPath;

            foreach (var to in group.TechnologicalObjects)
            {
                var info = new Dictionary<string, object?>
                {
                    ["Name"] = to.Name,
                    ["Number"] = to.Number,
                    ["InstanceOfName"] = to.InstanceOfName,
                    ["OfSystemLibElement"] = to.OfSystemLibElement,
                    ["OfSystemLibVersion"] = to.OfSystemLibVersion?.ToString(),
                    ["IsConsistent"] = to.IsConsistent,
                    ["ModifiedDate"] = to.ModifiedDate.ToString("yyyy-MM-dd HH:mm:ss"),
                    ["Path"] = string.IsNullOrEmpty(currentPath) ? to.Name : $"{currentPath}/{to.Name}"
                };
                result.Add(info);
            }

            foreach (var sub in group.Groups)
            {
                var subPath = string.IsNullOrEmpty(currentPath) ? sub.Name : $"{currentPath}/{sub.Name}";
                CollectTOs(sub, result, subPath);
            }
        }

        /// <summary>
        /// Find a specific TO by name.
        /// </summary>
        public TechnologicalInstanceDB? FindTO(PlcSoftware software, string toName)
        {
            return FindTOInGroup(software.TechnologicalObjectGroup, toName);
        }

        private TechnologicalInstanceDB? FindTOInGroup(TechnologicalInstanceDBGroup group, string name)
        {
            var found = group.TechnologicalObjects.FirstOrDefault(t =>
                t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (found != null) return found;

            foreach (var sub in group.Groups)
            {
                found = FindTOInGroup(sub, name);
                if (found != null) return found;
            }
            return null;
        }

        #endregion

        #region Create

        /// <summary>
        /// Create a Technology Object.
        /// toType examples: TO_PositioningAxis, TO_SpeedAxis, PID_Compact, PID_3Step, TO_ExternalEncoder
        /// version: e.g. "7.0" for V20 motion objects, "2.5" for PID
        /// </summary>
        public TechnologicalInstanceDB CreateTechnologicalObject(string softwarePath, string toName, string toType, string version)
        {
            var sw = _blockService.GetPlcSoftware(softwarePath);

            // Resolve the system library element name
            string sysLibElement;
            if (KnownToTypes.ContainsKey(toType))
                sysLibElement = KnownToTypes[toType];
            else
                sysLibElement = toType; // User passed the exact system lib element name

            var ver = Version.Parse(version);

            var toComposition = sw.TechnologicalObjectGroup.TechnologicalObjects;
            var newTo = toComposition.Create(toName, sysLibElement, ver);

            _logger?.LogInformation("Created Technology Object: {Name} ({Type} v{Version})", toName, sysLibElement, version);
            return newTo;
        }

        /// <summary>
        /// Create a TO in a user group/folder.
        /// </summary>
        public TechnologicalInstanceDB CreateTechnologicalObjectInGroup(string softwarePath, string groupPath, string toName, string toType, string version)
        {
            var sw = _blockService.GetPlcSoftware(softwarePath);
            var sysLibElement = KnownToTypes.ContainsKey(toType) ? KnownToTypes[toType] : toType;
            var ver = Version.Parse(version);

            TechnologicalInstanceDBComposition toComposition;

            if (string.IsNullOrEmpty(groupPath))
            {
                toComposition = sw.TechnologicalObjectGroup.TechnologicalObjects;
            }
            else
            {
                var group = FindToGroup(sw.TechnologicalObjectGroup, groupPath);
                if (group == null)
                    throw new PortalException(PortalErrorCode.NotFound, $"TO group not found: {groupPath}");
                toComposition = group.TechnologicalObjects;
            }

            var newTo = toComposition.Create(toName, sysLibElement, ver);
            _logger?.LogInformation("Created TO: {Name} in group {Group}", toName, groupPath);
            return newTo;
        }

        private TechnologicalInstanceDBUserGroup? FindToGroup(TechnologicalInstanceDBGroup root, string path)
        {
            var parts = path.Replace('\\', '/').Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            TechnologicalInstanceDBUserGroupComposition groups = root.Groups;
            TechnologicalInstanceDBUserGroup? current = null;

            foreach (var part in parts)
            {
                current = groups.FirstOrDefault(g =>
                    g.Name.Equals(part, StringComparison.OrdinalIgnoreCase));
                if (current == null) return null;
                groups = current.Groups;
            }
            return current;
        }

        /// <summary>
        /// Create a user group/folder in the TO tree.
        /// </summary>
        public TechnologicalInstanceDBUserGroup CreateToGroup(string softwarePath, string parentPath, string groupName)
        {
            var sw = _blockService.GetPlcSoftware(softwarePath);
            TechnologicalInstanceDBUserGroupComposition groups;

            if (string.IsNullOrEmpty(parentPath))
            {
                groups = sw.TechnologicalObjectGroup.Groups;
            }
            else
            {
                var parent = FindToGroup(sw.TechnologicalObjectGroup, parentPath);
                if (parent == null)
                    throw new PortalException(PortalErrorCode.NotFound, $"TO group not found: {parentPath}");
                groups = parent.Groups;
            }

            var newGroup = groups.Create(groupName);
            _logger?.LogInformation("Created TO group: {Name}", groupName);
            return newGroup;
        }

        #endregion

        #region Read Parameters

        /// <summary>
        /// Get all parameters of a Technology Object.
        /// </summary>
        public List<Dictionary<string, object?>> GetToParameters(string softwarePath, string toName)
        {
            var sw = _blockService.GetPlcSoftware(softwarePath);
            var to = FindTO(sw, toName);
            if (to == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Technology Object not found: {toName}");

            var result = new List<Dictionary<string, object?>>();

            foreach (var param in to.Parameters)
            {
                try
                {
                    result.Add(new Dictionary<string, object?>
                    {
                        ["Name"] = param.Name,
                        ["Value"] = param.Value
                    });
                }
                catch
                {
                    result.Add(new Dictionary<string, object?>
                    {
                        ["Name"] = param.Name,
                        ["Value"] = "<unreadable>"
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// Get a specific parameter value from a Technology Object.
        /// </summary>
        public object? GetToParameter(string softwarePath, string toName, string parameterName)
        {
            var sw = _blockService.GetPlcSoftware(softwarePath);
            var to = FindTO(sw, toName);
            if (to == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Technology Object not found: {toName}");

            var param = to.Parameters.FirstOrDefault(p =>
                p.Name.Equals(parameterName, StringComparison.OrdinalIgnoreCase));

            if (param != null)
                return param.Value;

            // Fallback: try GetAttribute
            return to.GetAttribute(parameterName);
        }

        /// <summary>
        /// Get detailed info about a specific TO.
        /// </summary>
        public Dictionary<string, object?> GetToInfo(string softwarePath, string toName)
        {
            var sw = _blockService.GetPlcSoftware(softwarePath);
            var to = FindTO(sw, toName);
            if (to == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Technology Object not found: {toName}");

            var info = new Dictionary<string, object?>
            {
                ["Name"] = to.Name,
                ["Number"] = to.Number,
                ["InstanceOfName"] = to.InstanceOfName,
                ["OfSystemLibElement"] = to.OfSystemLibElement,
                ["OfSystemLibVersion"] = to.OfSystemLibVersion?.ToString(),
                ["IsConsistent"] = to.IsConsistent,
                ["ProgrammingLanguage"] = to.ProgrammingLanguage.ToString(),
                ["MemoryLayout"] = to.MemoryLayout.ToString(),
                ["ModifiedDate"] = to.ModifiedDate.ToString("yyyy-MM-dd HH:mm:ss"),
                ["IsKnowHowProtected"] = to.IsKnowHowProtected,
                ["ParameterCount"] = to.Parameters.Count
            };

            // Read key parameters
            var keyParams = new Dictionary<string, object?>();
            foreach (var param in to.Parameters)
            {
                try
                {
                    keyParams[param.Name] = param.Value;
                }
                catch
                {
                    keyParams[param.Name] = "<unreadable>";
                }
            }
            info["Parameters"] = keyParams;

            return info;
        }

        #endregion

        #region Modify Parameters

        /// <summary>
        /// Set a single parameter on a Technology Object.
        /// </summary>
        public void SetToParameter(string softwarePath, string toName, string parameterName, object value)
        {
            var sw = _blockService.GetPlcSoftware(softwarePath);
            var to = FindTO(sw, toName);
            if (to == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Technology Object not found: {toName}");

            var param = to.Parameters.FirstOrDefault(p =>
                p.Name.Equals(parameterName, StringComparison.OrdinalIgnoreCase));

            if (param != null)
            {
                param.Value = ConvertParameterValue(param, value);
                _logger?.LogInformation("Set TO parameter {TO}.{Param} = {Val}", toName, parameterName, value);
            }
            else
            {
                // Fallback: try SetAttribute
                to.SetAttribute(parameterName, value);
                _logger?.LogInformation("Set TO attribute {TO}.{Attr} = {Val}", toName, parameterName, value);
            }
        }

        /// <summary>
        /// Set multiple parameters at once on a Technology Object.
        /// </summary>
        public void SetToParameters(string softwarePath, string toName, Dictionary<string, object> parameters)
        {
            var sw = _blockService.GetPlcSoftware(softwarePath);
            var to = FindTO(sw, toName);
            if (to == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Technology Object not found: {toName}");

            var errors = new List<string>();

            foreach (var kvp in parameters)
            {
                try
                {
                    var param = to.Parameters.FirstOrDefault(p =>
                        p.Name.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase));

                    if (param != null)
                        param.Value = ConvertParameterValue(param, kvp.Value);
                    else
                        to.SetAttribute(kvp.Key, kvp.Value);
                }
                catch (Exception ex)
                {
                    errors.Add($"{kvp.Key}: {ex.Message}");
                }
            }

            _logger?.LogInformation("Set {Count} parameters on TO {Name} ({Errors} errors)",
                parameters.Count, toName, errors.Count);

            if (errors.Count > 0)
                throw new PortalException(PortalErrorCode.OperationFailed,
                    $"Some parameters failed: {string.Join("; ", errors)}");
        }

        private object ConvertParameterValue(TechnologicalParameter param, object value)
        {
            // Try to match the existing value type
            var currentValue = param.Value;
            if (currentValue == null) return value;

            var targetType = currentValue.GetType();
            var strValue = value.ToString();

            try
            {
                if (targetType == typeof(double))
                    return double.Parse(strValue);
                if (targetType == typeof(float))
                    return float.Parse(strValue);
                if (targetType == typeof(int))
                    return int.Parse(strValue);
                if (targetType == typeof(bool))
                    return bool.Parse(strValue);
                if (targetType == typeof(long))
                    return long.Parse(strValue);
                if (targetType == typeof(uint))
                    return uint.Parse(strValue);
            }
            catch { }

            return value;
        }

        #endregion

        #region Export & Import

        public void ExportTo(string softwarePath, string toName, string exportPath)
        {
            var sw = _blockService.GetPlcSoftware(softwarePath);
            var to = FindTO(sw, toName);
            if (to == null)
                throw new PortalException(PortalErrorCode.NotFound, $"TO not found: {toName}");

            Directory.CreateDirectory(exportPath);
            var filePath = Path.Combine(exportPath, $"{to.Name}.xml");
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Exists) fileInfo.Delete();

            to.Export(fileInfo, ExportOptions.WithDefaults);
            _logger?.LogInformation("Exported TO: {Name} to {Path}", toName, filePath);
        }

        public void ImportTo(string softwarePath, string importFilePath, string groupPath)
        {
            var sw = _blockService.GetPlcSoftware(softwarePath);

            if (!File.Exists(importFilePath))
                throw new PortalException(PortalErrorCode.NotFound, $"File not found: {importFilePath}");

            TechnologicalInstanceDBComposition toComposition;

            if (string.IsNullOrEmpty(groupPath))
            {
                toComposition = sw.TechnologicalObjectGroup.TechnologicalObjects;
            }
            else
            {
                var group = FindToGroup(sw.TechnologicalObjectGroup, groupPath);
                if (group == null)
                    throw new PortalException(PortalErrorCode.NotFound, $"TO group not found: {groupPath}");
                toComposition = group.TechnologicalObjects;
            }

            toComposition.Import(new FileInfo(importFilePath), ImportOptions.Override);
            _logger?.LogInformation("Imported TO from: {Path}", importFilePath);
        }

        public void DeleteTo(string softwarePath, string toName)
        {
            var sw = _blockService.GetPlcSoftware(softwarePath);
            var to = FindTO(sw, toName);
            if (to == null)
                throw new PortalException(PortalErrorCode.NotFound, $"TO not found: {toName}");

            to.Delete();
            _logger?.LogInformation("Deleted TO: {Name}", toName);
        }

        #endregion

        #region Available Types

        public List<Dictionary<string, string>> GetAvailableToTypes()
        {
            var types = new List<Dictionary<string, string>>();
            foreach (var kvp in KnownToTypes)
            {
                string category, description;

                if (kvp.Key.StartsWith("TO_") && (kvp.Key.Contains("Axis") || kvp.Key.Contains("Encoder")))
                {
                    category = "Motion Control";
                    description = GetMotionDescription(kvp.Key);
                }
                else if (kvp.Key.StartsWith("PID"))
                {
                    category = "PID Control";
                    description = GetPidDescription(kvp.Key);
                }
                else if (kvp.Key.Contains("Cam") || kvp.Key.Contains("Track"))
                {
                    category = "Motion - Cam";
                    description = GetCamDescription(kvp.Key);
                }
                else
                {
                    category = "Other";
                    description = kvp.Key;
                }

                types.Add(new Dictionary<string, string>
                {
                    ["TypeName"] = kvp.Key,
                    ["SystemLibElement"] = kvp.Value,
                    ["Category"] = category,
                    ["Description"] = description
                });
            }
            return types;
        }

        private string GetMotionDescription(string type)
        {
            switch (type)
            {
                case "TO_SpeedAxis": return "Speed-controlled axis (velocity mode, no position control)";
                case "TO_PositioningAxis": return "Position-controlled axis (absolute/relative positioning)";
                case "TO_SynchronousAxis": return "Synchronous axis (electronic gear, electronic cam)";
                case "TO_ExternalEncoder": return "External encoder (position feedback without drive)";
                case "TO_Kinematics": return "2D/3D kinematics (cartesian, SCARA, delta)";
                case "TO_LeadingAxisProxy": return "Leading axis proxy for cross-CPU synchronization";
                case "TO_MeasuringInput": return "Measuring input (position latching, high-speed capture)";
                default: return type;
            }
        }

        private string GetPidDescription(string type)
        {
            switch (type)
            {
                case "PID_Compact": return "PID controller with auto-tuning (continuous output 0-100%)";
                case "PID_3Step": return "PID 3-step controller for motorized valves (open/close/stop)";
                case "PID_Temp": return "PID temperature controller (heating/cooling zones)";
                default: return type;
            }
        }

        private string GetCamDescription(string type)
        {
            switch (type)
            {
                case "TO_OutputCam": return "Output cam (position-dependent digital output switching)";
                case "TO_CamTrack": return "Cam track (multiple output cams on one track)";
                case "TO_Cam": return "Cam profile (electronic cam motion profile)";
                default: return type;
            }
        }

        #endregion
    }
}
