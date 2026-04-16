using Microsoft.Extensions.Logging;
using Siemens.Engineering;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using TiaMcpV2.Core;
using TiaMcpV2.Models;

namespace TiaMcpV2.Services
{
    public class NetworkService
    {
        private readonly PortalEngine _portal;
        private readonly ILogger<NetworkService>? _logger;

        public NetworkService(PortalEngine portal, ILogger<NetworkService>? logger = null)
        {
            _portal = portal;
            _logger = logger;
        }

        public Subnet CreateSubnet(string typeIdentifier, string name)
        {
            _portal.EnsureProjectOpen();
            var subnet = _portal.Project!.Subnets.Create(typeIdentifier, name);
            _logger?.LogInformation("Created subnet: {Name}", name);
            return subnet;
        }

        public List<SubnetInfo> GetSubnets()
        {
            _portal.EnsureProjectOpen();
            var result = new List<SubnetInfo>();
            foreach (var subnet in _portal.Project!.Subnets)
            {
                result.Add(new SubnetInfo
                {
                    Name = subnet.Name,
                    TypeIdentifier = subnet.TypeIdentifier
                });
            }
            return result;
        }

        public void ConnectToSubnet(string deviceItemPath, string subnetName)
        {
            var item = _portal.FindDeviceItem(deviceItemPath);
            if (item == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Device item not found: {deviceItemPath}");

            var subnet = _portal.FindSubnet(subnetName);
            if (subnet == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Subnet not found: {subnetName}");

            var networkInterface = item.GetService<NetworkInterface>();
            if (networkInterface == null)
                throw new PortalException(PortalErrorCode.NotFound, $"No network interface found on: {deviceItemPath}");

            var node = networkInterface.Nodes.FirstOrDefault();
            if (node == null)
                throw new PortalException(PortalErrorCode.NotFound, $"No network node found on: {deviceItemPath}");

            node.ConnectToSubnet(subnet);
            _logger?.LogInformation("Connected {Path} to subnet {Subnet}", deviceItemPath, subnetName);
        }

        public IoSystem CreateIoSystem(string deviceItemPath, string ioSystemName)
        {
            var item = _portal.FindDeviceItem(deviceItemPath);
            if (item == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Device item not found: {deviceItemPath}");

            var networkInterface = item.GetService<NetworkInterface>();
            if (networkInterface == null)
                throw new PortalException(PortalErrorCode.NotFound, $"No network interface found on: {deviceItemPath}");

            var ioControllers = networkInterface.IoControllers;
            if (ioControllers.Count == 0)
                throw new PortalException(PortalErrorCode.NotFound, $"No IO controller found on: {deviceItemPath}");

            var controller = ioControllers.First();
            var ioSystem = controller.CreateIoSystem(ioSystemName);
            _logger?.LogInformation("Created IO system: {Name}", ioSystemName);
            return ioSystem;
        }

        public void ConnectToIoSystem(string deviceItemPath, string ioSystemName)
        {
            var item = _portal.FindDeviceItem(deviceItemPath);
            if (item == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Device item not found: {deviceItemPath}");

            _portal.EnsureProjectOpen();
            IoSystem? targetSystem = null;

            foreach (var subnet in _portal.Project!.Subnets)
            {
                foreach (var ioSys in subnet.IoSystems)
                {
                    if (ioSys.Name.Equals(ioSystemName, StringComparison.OrdinalIgnoreCase))
                    {
                        targetSystem = ioSys;
                        break;
                    }
                }
                if (targetSystem != null) break;
            }

            if (targetSystem == null)
                throw new PortalException(PortalErrorCode.NotFound, $"IO system not found: {ioSystemName}");

            var networkInterface = item.GetService<NetworkInterface>();
            if (networkInterface == null)
                throw new PortalException(PortalErrorCode.NotFound, $"No network interface found on: {deviceItemPath}");

            var ioConnectors = networkInterface.IoConnectors;
            if (ioConnectors.Count == 0)
                throw new PortalException(PortalErrorCode.NotFound, $"No IO connector found on: {deviceItemPath}");

            ioConnectors.First().ConnectToIoSystem(targetSystem);
            _logger?.LogInformation("Connected {Path} to IO system {IoSystem}", deviceItemPath, ioSystemName);
        }

        public void SetIpAddress(string deviceItemPath, string ipAddress, string subnetMask, string routerAddress)
        {
            var item = _portal.FindDeviceItem(deviceItemPath);
            if (item == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Device item not found: {deviceItemPath}");

            var networkInterface = item.GetService<NetworkInterface>();
            if (networkInterface == null)
                throw new PortalException(PortalErrorCode.NotFound, $"No network interface on: {deviceItemPath}");

            var node = networkInterface.Nodes.FirstOrDefault();
            if (node == null)
                throw new PortalException(PortalErrorCode.NotFound, $"No network node on: {deviceItemPath}");

            node.SetAttribute("Address", ipAddress);
            node.SetAttribute("SubnetMask", subnetMask);
            if (!string.IsNullOrEmpty(routerAddress))
                node.SetAttribute("RouterAddress", routerAddress);

            _logger?.LogInformation("Set IP {IP}/{Mask} on {Path}", ipAddress, subnetMask, deviceItemPath);
        }

        public void SetProfinetDeviceName(string deviceItemPath, string deviceName)
        {
            var item = _portal.FindDeviceItem(deviceItemPath);
            if (item == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Device item not found: {deviceItemPath}");

            var networkInterface = item.GetService<NetworkInterface>();
            if (networkInterface == null)
                throw new PortalException(PortalErrorCode.NotFound, $"No network interface on: {deviceItemPath}");

            networkInterface.SetAttribute("ProfinetDeviceName", deviceName);
            _logger?.LogInformation("Set PROFINET device name: {Name} on {Path}", deviceName, deviceItemPath);
        }

        public List<NetworkInterfaceInfo> GetNetworkInterfaces(string deviceItemPath)
        {
            var item = _portal.FindDeviceItem(deviceItemPath);
            if (item == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Device item not found: {deviceItemPath}");

            var result = new List<NetworkInterfaceInfo>();
            CollectNetworkInterfaces(item, result);
            return result;
        }

        private void CollectNetworkInterfaces(DeviceItem item, List<NetworkInterfaceInfo> result)
        {
            var ni = item.GetService<NetworkInterface>();
            if (ni != null)
            {
                var info = new NetworkInterfaceInfo
                {
                    Name = item.Name,
                    InterfaceType = ni.InterfaceType.ToString()
                };

                var node = ni.Nodes.FirstOrDefault();
                if (node != null)
                {
                    try { info.IpAddress = node.GetAttribute("Address")?.ToString(); } catch { }
                    try { info.SubnetMask = node.GetAttribute("SubnetMask")?.ToString(); } catch { }
                }

                result.Add(info);
            }

            foreach (var sub in item.DeviceItems)
            {
                CollectNetworkInterfaces(sub, result);
            }
        }

        public List<IoSystemInfo> GetIoSystems()
        {
            _portal.EnsureProjectOpen();
            var result = new List<IoSystemInfo>();

            foreach (var subnet in _portal.Project!.Subnets)
            {
                foreach (var ioSys in subnet.IoSystems)
                {
                    result.Add(new IoSystemInfo
                    {
                        Name = ioSys.Name,
                        Number = ioSys.Number.ToString()
                    });
                }
            }

            return result;
        }

        public void ConfigureMrp(string deviceItemPath, string mrpRole)
        {
            var item = _portal.FindDeviceItem(deviceItemPath);
            if (item == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Device item not found: {deviceItemPath}");

            var networkInterface = item.GetService<NetworkInterface>();
            if (networkInterface == null)
                throw new PortalException(PortalErrorCode.NotFound, $"No network interface on: {deviceItemPath}");

            networkInterface.SetAttribute("MrpRole", mrpRole);
            _logger?.LogInformation("Set MRP role {Role} on {Path}", mrpRole, deviceItemPath);
        }
    }
}
