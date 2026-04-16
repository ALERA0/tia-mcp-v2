using Microsoft.Extensions.Logging;
using Siemens.Engineering.Hmi;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using TiaMcpV2.Core;

namespace TiaMcpV2.Services
{
    public class HmiService
    {
        private readonly PortalEngine _portal;
        private readonly ILogger<HmiService>? _logger;

        public HmiService(PortalEngine portal, ILogger<HmiService>? logger = null)
        {
            _portal = portal;
            _logger = logger;
        }

        public List<Dictionary<string, object?>> GetHmiTargets()
        {
            _portal.EnsureProjectOpen();
            var result = new List<Dictionary<string, object?>>();

            foreach (var device in _portal.GetDevices())
            {
                foreach (var item in device.DeviceItems)
                {
                    CollectHmiTargets(item, result, device.Name);
                }
            }

            return result;
        }

        private void CollectHmiTargets(DeviceItem item, List<Dictionary<string, object?>> result, string deviceName)
        {
            var container = item.GetService<SoftwareContainer>();
            if (container?.Software is HmiTarget hmiTarget)
            {
                result.Add(new Dictionary<string, object?>
                {
                    ["DeviceName"] = deviceName,
                    ["Name"] = hmiTarget.Name,
                    ["Type"] = hmiTarget.GetType().Name
                });
            }

            foreach (var sub in item.DeviceItems)
            {
                CollectHmiTargets(sub, result, deviceName);
            }
        }

        public List<Dictionary<string, object?>> GetHmiScreens(string hmiPath)
        {
            var hmiTarget = _portal.FindHmiTarget(hmiPath);
            if (hmiTarget == null)
                throw new PortalException(PortalErrorCode.NotFound, $"HMI target not found: {hmiPath}");

            var result = new List<Dictionary<string, object?>>();

            foreach (var screen in hmiTarget.ScreenFolder.Screens)
            {
                result.Add(new Dictionary<string, object?>
                {
                    ["Name"] = screen.Name,
                    ["Type"] = screen.GetType().Name
                });
            }

            return result;
        }

        public void CreateHmiConnection(string hmiPath, string plcPath, string connectionName)
        {
            // HMI connection creation is limited in Openness API
            throw new PortalException(PortalErrorCode.NotSupported,
                "HMI connection creation is not fully supported via Openness API. " +
                "Please create the connection manually in TIA Portal.");
        }
    }
}
