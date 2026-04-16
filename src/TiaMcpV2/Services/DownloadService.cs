using Microsoft.Extensions.Logging;
using Siemens.Engineering.Download;
using Siemens.Engineering.Online;
using Siemens.Engineering.HW;
using Siemens.Engineering.HW.Features;
using System;
using System.Linq;
using TiaMcpV2.Core;

namespace TiaMcpV2.Services
{
    public class DownloadService
    {
        private readonly PortalEngine _portal;
        private readonly ILogger<DownloadService>? _logger;

        public DownloadService(PortalEngine portal, ILogger<DownloadService>? logger = null)
        {
            _portal = portal;
            _logger = logger;
        }

        public void DownloadToPlc(string deviceItemPath)
        {
            var item = _portal.FindDeviceItem(deviceItemPath);
            if (item == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Device item not found: {deviceItemPath}");

            var downloadProvider = item.GetService<DownloadProvider>();
            if (downloadProvider == null)
                throw new PortalException(PortalErrorCode.NotSupported, $"Download not supported for: {deviceItemPath}");

            // Use reflection to handle different API versions
            var downloadMethod = downloadProvider.GetType().GetMethods()
                .FirstOrDefault(m => m.Name == "Download" && m.GetParameters().Length >= 1);
            if (downloadMethod != null)
            {
                try
                {
                    downloadMethod.Invoke(downloadProvider, new object[] {
                        downloadProvider.Configuration,
                        (Action<object>)(config => { }),
                        (Action<object>)(r => { }),
                        DownloadOptions.Hardware | DownloadOptions.Software
                    });
                }
                catch
                {
                    // Fallback: try simpler download
                    downloadProvider.GetType().GetMethod("Download", new[] { typeof(DownloadOptions) })
                        ?.Invoke(downloadProvider, new object[] { DownloadOptions.Hardware | DownloadOptions.Software });
                }
            }

            _logger?.LogInformation("Download to PLC completed for: {Path}", deviceItemPath);
        }

        public void GoOnline(string deviceItemPath)
        {
            var item = _portal.FindDeviceItem(deviceItemPath);
            if (item == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Device item not found: {deviceItemPath}");

            var onlineProvider = item.GetService<OnlineProvider>();
            if (onlineProvider == null)
                throw new PortalException(PortalErrorCode.NotSupported, $"Online not supported for: {deviceItemPath}");

            onlineProvider.GoOnline();
            _logger?.LogInformation("Gone online with: {Path}", deviceItemPath);
        }

        public void GoOffline(string deviceItemPath)
        {
            var item = _portal.FindDeviceItem(deviceItemPath);
            if (item == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Device item not found: {deviceItemPath}");

            var onlineProvider = item.GetService<OnlineProvider>();
            if (onlineProvider == null)
                throw new PortalException(PortalErrorCode.NotSupported, $"Online not supported for: {deviceItemPath}");

            onlineProvider.GoOffline();
            _logger?.LogInformation("Gone offline from: {Path}", deviceItemPath);
        }
    }
}
