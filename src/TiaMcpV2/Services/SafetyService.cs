using Microsoft.Extensions.Logging;
using Siemens.Engineering.Safety;
using System;
using System.Collections.Generic;
using System.Security;
using TiaMcpV2.Core;

namespace TiaMcpV2.Services
{
    public class SafetyService
    {
        private readonly PortalEngine _portal;
        private readonly BlockService _blockService;
        private readonly ILogger<SafetyService>? _logger;

        public SafetyService(PortalEngine portal, BlockService blockService, ILogger<SafetyService>? logger = null)
        {
            _portal = portal;
            _blockService = blockService;
            _logger = logger;
        }

        public void SafetyLogin(string softwarePath, string password)
        {
            var sw = _blockService.GetPlcSoftware(softwarePath);
            var safetyAdmin = sw.GetService<SafetyAdministration>();
            if (safetyAdmin == null)
                throw new PortalException(PortalErrorCode.NotSupported, $"Device {softwarePath} is not an F-CPU (no safety administration available).");

            var securePassword = new SecureString();
            foreach (char c in password)
                securePassword.AppendChar(c);
            securePassword.MakeReadOnly();

            safetyAdmin.GetType().GetMethod("Login")?.Invoke(safetyAdmin, new object[] { securePassword });
            _logger?.LogInformation("Safety login successful for: {Path}", softwarePath);
        }

        public void SafetyLogout(string softwarePath)
        {
            var sw = _blockService.GetPlcSoftware(softwarePath);
            var safetyAdmin = sw.GetService<SafetyAdministration>();
            if (safetyAdmin == null)
                throw new PortalException(PortalErrorCode.NotSupported, $"Device {softwarePath} is not an F-CPU.");

            safetyAdmin.GetType().GetMethod("Logout")?.Invoke(safetyAdmin, null);
            _logger?.LogInformation("Safety logout for: {Path}", softwarePath);
        }

        public Dictionary<string, object?> GetSafetyInfo(string softwarePath)
        {
            var sw = _blockService.GetPlcSoftware(softwarePath);
            var safetyAdmin = sw.GetService<SafetyAdministration>();
            if (safetyAdmin == null)
                throw new PortalException(PortalErrorCode.NotSupported, $"Device {softwarePath} is not an F-CPU.");

            var info = new Dictionary<string, object?>
            {
                ["IsLoggedIn"] = safetyAdmin.GetAttribute("IsLoggedIn")
            };

            try
            {
                var signature = safetyAdmin.GetAttribute("SafetySignature");
                info["SafetySignature"] = signature;
            }
            catch { }

            return info;
        }
    }
}
