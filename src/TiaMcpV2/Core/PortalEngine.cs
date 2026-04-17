using Microsoft.Extensions.Logging;
using Siemens.Engineering;
using Siemens.Engineering.Compiler;
using Siemens.Engineering.Hmi;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using Siemens.Engineering.Multiuser;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;
using Siemens.Engineering.SW.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TiaMcpV2.Core
{
    /// <summary>
    /// Main facade for TIA Portal interaction via Openness API.
    /// Manages connection lifecycle, project operations, and provides
    /// access to all TIA Portal objects.
    /// </summary>
    public class PortalEngine : IDisposable
    {
        private TiaPortal? _portal;
        private ProjectBase? _project;
        private LocalSession? _session;
        private readonly ILogger<PortalEngine>? _logger;

        public ConnectionState State { get; } = new ConnectionState();
        public TiaPortal? TiaPortalInstance => _portal;
        public ProjectBase? CurrentProject => _project;
        public Project? Project => _project as Project;

        public PortalEngine(ILogger<PortalEngine>? logger = null)
        {
            _logger = logger;
        }

        #region Connection

        public void Connect()
        {
            if (_portal != null)
                throw new PortalException(PortalErrorCode.AlreadyConnected, "Already connected to TIA Portal.");

            var processes = TiaPortal.GetProcesses();
            if (processes.Count == 0)
                throw new PortalException(PortalErrorCode.NotFound, "No running TIA Portal instance found. Please start TIA Portal first.");

            _portal = processes.First().Attach();
            State.IsConnected = true;
            State.SessionId = _portal.GetHashCode().ToString();
            State.TiaVersion = OpennessResolver.TiaMajorVersion;

            _logger?.LogInformation("Connected to TIA Portal V{Version}", OpennessResolver.TiaMajorVersion);

            // Auto-detect open project or session
            if (_portal.Projects.Count > 0)
            {
                _project = _portal.Projects.First();
                State.ProjectName = _project.Name;
                State.IsLocalSession = false;
                _logger?.LogInformation("Found open project: {Project}", _project.Name);
            }
            else if (_portal.LocalSessions.Count > 0)
            {
                _session = _portal.LocalSessions[0];
                _project = _session.Project;
                State.ProjectName = _project.Name;
                State.SessionId = _session.Project.Name;
                State.IsLocalSession = true;
                _logger?.LogInformation("Found open session: {Session}", _session.Project.Name);
            }
        }

        public void Disconnect()
        {
            if (_portal == null)
                throw new PortalException(PortalErrorCode.NotConnected, "Not connected to TIA Portal.");

            _project = null;
            _session = null;
            _portal.Dispose();
            _portal = null;

            State.IsConnected = false;
            State.ProjectName = null;
            State.ProjectPath = null;
            State.SessionId = null;
            State.IsLocalSession = false;

            _logger?.LogInformation("Disconnected from TIA Portal");
        }

        #endregion

        #region Project Management

        public void EnsureConnected()
        {
            if (_portal == null || !State.IsConnected)
                throw new PortalException(PortalErrorCode.NotConnected, "Not connected to TIA Portal. Call Connect first.");
        }

        public void EnsureProjectOpen()
        {
            EnsureConnected();
            if (_project == null)
                throw new PortalException(PortalErrorCode.ProjectNotOpen, "No project is open. Call OpenProject first.");
        }

        public void OpenProject(string projectPath)
        {
            EnsureConnected();

            if (!File.Exists(projectPath))
                throw new PortalException(PortalErrorCode.NotFound, $"Project file not found: {projectPath}");

            var fileInfo = new FileInfo(projectPath);

            if (fileInfo.Extension.EndsWith("als", StringComparison.OrdinalIgnoreCase))
            {
                // Multiuser session
                _session = _portal!.LocalSessions.Open(fileInfo);
                _project = _session.Project;
                State.IsLocalSession = true;
            }
            else
            {
                // Standard project
                _project = _portal!.Projects.Open(fileInfo);
                State.IsLocalSession = false;
            }

            State.ProjectName = _project.Name;
            State.ProjectPath = projectPath;
            _logger?.LogInformation("Opened project: {Project}", _project.Name);
        }

        public void CreateProject(string directory, string projectName)
        {
            EnsureConnected();

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var targetDir = new DirectoryInfo(Path.Combine(directory, projectName));
            _project = _portal!.Projects.Create(targetDir, projectName);
            State.ProjectName = projectName;
            State.ProjectPath = targetDir.FullName;
            State.IsLocalSession = false;

            _logger?.LogInformation("Created project: {Project} at {Path}", projectName, targetDir.FullName);
        }

        public void SaveProject()
        {
            EnsureProjectOpen();
            if (_project is Project project)
            {
                project.Save();
                _logger?.LogInformation("Project saved: {Project}", project.Name);
            }
        }

        public void SaveAsProject(string targetDirectory)
        {
            EnsureProjectOpen();
            if (_project is Project project)
            {
                var targetDir = new DirectoryInfo(targetDirectory);
                project.SaveAs(targetDir);
                _logger?.LogInformation("Project saved as: {Path}", targetDirectory);
            }
        }

        public void CloseProject()
        {
            EnsureProjectOpen();

            if (_session != null)
            {
                _session.Close();
                _session = null;
            }
            else if (_project is Project project)
            {
                project.Close();
            }

            _project = null;
            State.ProjectName = null;
            State.ProjectPath = null;
            State.IsLocalSession = false;

            _logger?.LogInformation("Project closed");
        }

        #endregion

        #region Device Access

        public DeviceComposition GetDevices()
        {
            EnsureProjectOpen();
            return _project!.Devices;
        }

        public Device? FindDevice(string deviceName)
        {
            EnsureProjectOpen();

            // 1. Exact match
            var device = _project!.Devices.FirstOrDefault(d =>
                d.Name.Equals(deviceName, StringComparison.OrdinalIgnoreCase));
            if (device != null) return device;

            // 2. Try trimmed input (user may pass "PLC_1" which is a DeviceItem name, not Device name)
            //    Iterate all devices, check if any DeviceItem inside matches
            foreach (var dev in _project!.Devices)
            {
                foreach (var item in dev.DeviceItems)
                {
                    if (item.Name.Equals(deviceName, StringComparison.OrdinalIgnoreCase))
                        return dev;

                    // Also check sub-items (for rack-based configurations)
                    foreach (var sub in item.DeviceItems)
                    {
                        if (sub.Name.Equals(deviceName, StringComparison.OrdinalIgnoreCase))
                            return dev;
                    }
                }
            }

            // 3. Partial match — device name may contain "/" which is also the path separator
            //    Try matching the full input against device names that contain "/"
            device = _project!.Devices.FirstOrDefault(d =>
                d.Name.Replace("/", "").Replace("\\", "")
                    .Equals(deviceName.Replace("/", "").Replace("\\", ""), StringComparison.OrdinalIgnoreCase));
            if (device != null) return device;

            return null;
        }

        /// <summary>
        /// Resolves a device from a path string, handling device names that contain "/" characters.
        /// Returns the device and the remaining path parts after the device name.
        /// </summary>
        private (Device? device, string[] remainingParts) ResolveDeviceFromPath(string path)
        {
            EnsureProjectOpen();
            var normalized = path.Replace('\\', '/').Trim('/');
            if (string.IsNullOrEmpty(normalized))
                return (null, Array.Empty<string>());

            // Strategy 1: Try full path as exact device name (handles "S7-1500/ET200MP-Station_1")
            var device = _project!.Devices.FirstOrDefault(d =>
                d.Name.Equals(normalized, StringComparison.OrdinalIgnoreCase));
            if (device != null)
                return (device, Array.Empty<string>());

            // Strategy 2: Try progressively longer prefixes as device name
            // For "S7-1500/ET200MP-Station_1/PLC_1/SubItem" try:
            //   "S7-1500" → "S7-1500/ET200MP-Station_1" → "S7-1500/ET200MP-Station_1/PLC_1" → ...
            var segments = normalized.Split('/');
            for (int i = segments.Length; i >= 1; i--)
            {
                var candidateName = string.Join("/", segments.Take(i));
                device = _project!.Devices.FirstOrDefault(d =>
                    d.Name.Equals(candidateName, StringComparison.OrdinalIgnoreCase));
                if (device != null)
                {
                    var remaining = segments.Skip(i).ToArray();
                    return (device, remaining);
                }
            }

            // Strategy 3: Try first segment as simple device name
            device = FindDevice(segments[0]);
            if (device != null)
                return (device, segments.Skip(1).ToArray());

            return (null, Array.Empty<string>());
        }

        public DeviceItem? FindDeviceItem(string path)
        {
            var (device, remainingParts) = ResolveDeviceFromPath(path);
            if (device == null)
                return null;

            if (remainingParts.Length == 0)
                return device.DeviceItems.FirstOrDefault();

            DeviceItem? current = null;
            var items = device.DeviceItems;

            foreach (var part in remainingParts)
            {
                current = items.FirstOrDefault(di =>
                    di.Name.Equals(part, StringComparison.OrdinalIgnoreCase));

                if (current == null)
                {
                    // Fallback: search recursively in case of nested items
                    current = FindDeviceItemRecursive(items, part);
                    if (current == null)
                        return null;
                }

                items = current.DeviceItems;
            }

            return current;
        }

        private DeviceItem? FindDeviceItemRecursive(DeviceItemComposition items, string name)
        {
            foreach (var item in items)
            {
                if (item.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return item;

                var found = FindDeviceItemRecursive(item.DeviceItems, name);
                if (found != null)
                    return found;
            }
            return null;
        }

        #endregion

        #region Software Access

        public PlcSoftware? FindPlcSoftware(string devicePath)
        {
            // Strategy 1: Resolve through the smart device path resolver
            var (device, _) = ResolveDeviceFromPath(devicePath);
            if (device != null)
            {
                foreach (var item in device.DeviceItems)
                {
                    var sw = GetSoftwareFrom(item);
                    if (sw != null)
                        return sw;
                }
            }

            // Strategy 2: Input might be a DeviceItem name like "PLC_1"
            //  Search ALL devices for a DeviceItem with PLC software matching the name
            EnsureProjectOpen();
            foreach (var dev in _project!.Devices)
            {
                foreach (var item in dev.DeviceItems)
                {
                    if (item.Name.Equals(devicePath, StringComparison.OrdinalIgnoreCase))
                    {
                        var sw = GetSoftwareFrom(item);
                        if (sw != null) return sw;
                    }

                    // Check sub-items (the actual CPU module is often nested)
                    foreach (var sub in item.DeviceItems)
                    {
                        if (sub.Name.Equals(devicePath, StringComparison.OrdinalIgnoreCase))
                        {
                            var sw = GetSoftwareFrom(sub);
                            if (sw != null) return sw;
                        }
                    }
                }
            }

            // Strategy 3: Brute force — search ALL software in project
            foreach (var dev in _project!.Devices)
            {
                foreach (var item in dev.DeviceItems)
                {
                    var sw = GetSoftwareFrom(item);
                    if (sw != null && sw.Name.Equals(devicePath, StringComparison.OrdinalIgnoreCase))
                        return sw;
                }
            }

            return null;
        }

        private PlcSoftware? GetSoftwareFrom(DeviceItem item)
        {
            var softwareContainer = item.GetService<SoftwareContainer>();
            if (softwareContainer?.Software is PlcSoftware plcSw)
                return plcSw;

            foreach (var sub in item.DeviceItems)
            {
                var sw = GetSoftwareFrom(sub);
                if (sw != null)
                    return sw;
            }

            return null;
        }

        public HmiTarget? FindHmiTarget(string devicePath)
        {
            // Use smart resolver
            var (device, _) = ResolveDeviceFromPath(devicePath);
            if (device != null)
            {
                foreach (var item in device.DeviceItems)
                {
                    var hmi = GetHmiTargetFrom(item);
                    if (hmi != null)
                        return hmi;
                }
            }

            // Fallback: search all devices
            EnsureProjectOpen();
            foreach (var dev in _project!.Devices)
            {
                foreach (var item in dev.DeviceItems)
                {
                    if (item.Name.Equals(devicePath, StringComparison.OrdinalIgnoreCase))
                    {
                        var hmi = GetHmiTargetFrom(item);
                        if (hmi != null) return hmi;
                    }
                }
            }

            return null;
        }

        private HmiTarget? GetHmiTargetFrom(DeviceItem item)
        {
            var softwareContainer = item.GetService<SoftwareContainer>();
            if (softwareContainer?.Software is HmiTarget hmiTarget)
                return hmiTarget;

            foreach (var sub in item.DeviceItems)
            {
                var hmi = GetHmiTargetFrom(sub);
                if (hmi != null)
                    return hmi;
            }

            return null;
        }

        #endregion

        #region Subnet Access

        public SubnetComposition GetSubnets()
        {
            EnsureProjectOpen();
            return (_project as Project)!.Subnets;
        }

        public Subnet? FindSubnet(string name)
        {
            EnsureProjectOpen();
            return (_project as Project)?.Subnets.FirstOrDefault(s =>
                s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        #endregion

        #region Compilation

        public CompilerResult CompileSoftware(string devicePath)
        {
            var software = FindPlcSoftware(devicePath);
            if (software == null)
                throw new PortalException(PortalErrorCode.NotFound, $"PLC software not found for device: {devicePath}");

            var compiler = software.GetService<ICompilable>();
            if (compiler == null)
                throw new PortalException(PortalErrorCode.NotSupported, "Software does not support compilation.");

            return compiler.Compile();
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            try
            {
                _project = null;
                _session = null;

                if (_portal != null)
                {
                    _portal.Dispose();
                    _portal = null;
                }

                State.IsConnected = false;
                State.ProjectName = null;
                State.SessionId = null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error disposing PortalEngine");
            }
        }

        #endregion
    }
}
