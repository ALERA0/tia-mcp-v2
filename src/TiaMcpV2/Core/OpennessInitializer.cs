using System;
using System.Linq;
using System.Management;
using System.Security.Principal;
using System.Threading.Tasks;

namespace TiaMcpV2.Core
{
    /// <summary>
    /// Initializes the Siemens Openness API and verifies user permissions.
    /// </summary>
    public static class OpennessInitializer
    {
        private const string OpennessGroupName = "Siemens TIA Openness";

        public static void Initialize(int tiaMajorVersion)
        {
            try
            {
                // Use the Siemens Openness resolver package to initialize
                var type = Type.GetType("Siemens.Collaboration.Net.TiaPortal.Openness.Api, Siemens.Collaboration.Net.TiaPortal.Openness.Resolver");
                if (type != null)
                {
                    var global = type.GetProperty("Global")?.GetValue(null);
                    var openness = global?.GetType().GetMethod("Openness")?.Invoke(global, null);
                    openness?.GetType().GetMethod("Initialize", new[] { typeof(int) })?.Invoke(openness, new object[] { tiaMajorVersion });
                }
            }
            catch (Exception ex)
            {
                throw new PortalException(PortalErrorCode.OperationFailed,
                    $"Failed to initialize TIA Openness for version {tiaMajorVersion}: {ex.Message}", ex);
            }
        }

        public static Task<bool> IsUserInOpennessGroup()
        {
            return Task.Run(() =>
            {
                try
                {
                    var identity = WindowsIdentity.GetCurrent();
                    var principal = new WindowsPrincipal(identity);

                    // Check local group membership
                    using (var searcher = new ManagementObjectSearcher(
                        new ObjectQuery($"SELECT * FROM Win32_GroupUser WHERE GroupComponent=\"Win32_Group.Domain='{Environment.MachineName}',Name='{OpennessGroupName}'\"")))
                    {
                        var members = searcher.Get();
                        var currentUser = Environment.UserDomainName + "\\" + Environment.UserName;

                        foreach (var member in members)
                        {
                            var partComponent = member["PartComponent"]?.ToString() ?? "";
                            if (partComponent.Contains(Environment.UserName))
                                return true;
                        }
                    }

                    return false;
                }
                catch
                {
                    // If WMI query fails, assume user has access
                    return true;
                }
            });
        }
    }
}
