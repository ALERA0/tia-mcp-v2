using ModelContextProtocol.Server;
using System;
using TiaMcpV2.Core;
using TiaMcpV2.Helpers;
using TiaMcpV2.Models;

namespace TiaMcpV2.Tools
{
using System.ComponentModel;
    [McpServerToolType]
    public static class NetworkTools
    {
        [McpServerTool(Name = "get_subnets"), Description("List all subnets (PROFINET/Ethernet) in the project.")]
        public static string GetSubnets()
        {
            try { return JsonHelper.ToJson(new ResponseSubnets { Success = true, Subnets = ServiceAccessor.Network.GetSubnets() }); }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "create_subnet"), Description("Create a PROFINET/Ethernet subnet. typeIdentifier: 'System:Subnet.Ethernet'.")]
        public static string CreateSubnet(
            string name,
            string typeIdentifier)
        {
            try
            {
                ServiceAccessor.Network.CreateSubnet(typeIdentifier, name);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Created subnet: {name}" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "connect_to_subnet"), Description("Connect a device's network interface to a subnet.")]
        public static string ConnectToSubnet(
            string deviceItemPath,
            string subnetName)
        {
            try
            {
                ServiceAccessor.Network.ConnectToSubnet(deviceItemPath, subnetName);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Connected to subnet: {subnetName}" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "create_io_system"), Description("Create a PROFINET IO system on a controller's network interface.")]
        public static string CreateIoSystem(
            string deviceItemPath,
            string ioSystemName)
        {
            try
            {
                ServiceAccessor.Network.CreateIoSystem(deviceItemPath, ioSystemName);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Created IO system: {ioSystemName}" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "connect_to_io_system"), Description("Connect an IO device to a PROFINET IO system.")]
        public static string ConnectToIoSystem(
            string deviceItemPath,
            string ioSystemName)
        {
            try
            {
                ServiceAccessor.Network.ConnectToIoSystem(deviceItemPath, ioSystemName);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Connected to IO system: {ioSystemName}" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "set_ip_address"), Description("Set IP address on a device's network interface.")]
        public static string SetIpAddress(
            string deviceItemPath,
            string ipAddress,
            string subnetMask,
            string routerAddress)
        {
            try
            {
                ServiceAccessor.Network.SetIpAddress(deviceItemPath, ipAddress, subnetMask, routerAddress);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Set IP: {ipAddress}/{subnetMask}" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "set_profinet_device_name"), Description("Set PROFINET device name on a device's network interface.")]
        public static string SetProfinetDeviceName(
            string deviceItemPath,
            string profinetName)
        {
            try
            {
                ServiceAccessor.Network.SetProfinetDeviceName(deviceItemPath, profinetName);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Set PROFINET name: {profinetName}" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "get_network_interfaces"), Description("Get all network interfaces and IP info of a device.")]
        public static string GetNetworkInterfaces(string deviceItemPath)
        {
            try
            {
                var interfaces = ServiceAccessor.Network.GetNetworkInterfaces(deviceItemPath);
                return JsonHelper.ToJson(new ResponseNetworkInterfaces { Success = true, Interfaces = interfaces });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "get_io_systems"), Description("List all PROFINET IO systems in the project.")]
        public static string GetIoSystems()
        {
            try { return JsonHelper.ToJson(new ResponseIoSystems { Success = true, IoSystems = ServiceAccessor.Network.GetIoSystems() }); }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }

        [McpServerTool(Name = "configure_mrp"), Description("Configure MRP (Media Redundancy Protocol) ring redundancy on a network interface.")]
        public static string ConfigureMrp(
            string deviceItemPath,
            string mrpRole)
        {
            try
            {
                ServiceAccessor.Network.ConfigureMrp(deviceItemPath, mrpRole);
                return JsonHelper.ToJson(new ResponseMessage { Success = true, Message = $"Set MRP role: {mrpRole}" });
            }
            catch (Exception ex) { return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message }); }
        }
    }
}
