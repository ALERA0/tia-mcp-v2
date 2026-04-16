using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace TiaMcpV2.Core
{
    /// <summary>
    /// Dynamically resolves Siemens Engineering assemblies for TIA Portal V20 and V21.
    /// Reads the installation path from registry and loads the correct version DLLs.
    /// </summary>
    public static class OpennessResolver
    {
        public static int TiaMajorVersion { get; set; } = 20;

        private static readonly string[] AllKnownVersions = { "V13", "V14", "V15", "V16", "V17", "V18", "V19", "V20", "V21" };
        public static readonly int[] SupportedVersions = { 19, 20, 21 };

        public static Assembly? Resolve(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);
            if (!assemblyName.Name.StartsWith("Siemens.Engineering"))
                return null;

            var installPath = GetTiaInstallPath(TiaMajorVersion);
            if (string.IsNullOrEmpty(installPath))
            {
                // Fallback: try the other supported version
                var fallbackVersion = TiaMajorVersion == 20 ? 21 : 20;
                installPath = GetTiaInstallPath(fallbackVersion);
                if (string.IsNullOrEmpty(installPath))
                    throw new InvalidOperationException(
                        $"TIA Portal V{TiaMajorVersion} installation not found in registry. " +
                        $"Checked HKLM\\SOFTWARE\\Siemens\\Automation\\_InstalledSW\\TIAP{TiaMajorVersion}\\TIA_Opns");
            }

            var versionStr = TiaMajorVersion.ToString();
            var searchDirs = new[]
            {
                Path.Combine(installPath, "PublicAPI", $"V{versionStr}"),
                Path.Combine(installPath, "Bin", "PublicAPI"),
                Path.Combine(installPath, "PublicAPI")
            };

            var excludedVersions = AllKnownVersions.Where(v => v != $"V{versionStr}");

            foreach (var dir in searchDirs)
            {
                var found = FindAssemblyRecursive(dir, assemblyName.Name + ".dll", excludedVersions);
                if (found != null)
                    return Assembly.LoadFrom(found);
            }

            throw new FileNotFoundException(
                $"Could not find '{assemblyName.Name}.dll' for TIA Portal V{TiaMajorVersion} " +
                $"in directories: {string.Join(", ", searchDirs)}");
        }

        public static string? GetTiaInstallPath(int majorVersion)
        {
            var subKey = $@"SOFTWARE\Siemens\Automation\_InstalledSW\TIAP{majorVersion}\TIA_Opns";

            using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using (var tiaKey = baseKey.OpenSubKey(subKey))
            {
                return tiaKey?.GetValue("Path")?.ToString();
            }
        }

        public static bool IsTiaInstalled(int majorVersion)
        {
            return !string.IsNullOrEmpty(GetTiaInstallPath(majorVersion));
        }

        public static List<int> GetInstalledVersions()
        {
            var versions = new List<int>();
            for (int v = 13; v <= 21; v++)
            {
                if (IsTiaInstalled(v))
                    versions.Add(v);
            }
            return versions;
        }

        private static string? FindAssemblyRecursive(string directory, string fileName, IEnumerable<string> excludedVersions)
        {
            if (!Directory.Exists(directory))
                return null;

            var filePath = Path.Combine(directory, fileName);
            if (File.Exists(filePath))
                return filePath;

            foreach (var subDir in Directory.GetDirectories(directory))
            {
                var dirName = new DirectoryInfo(subDir).Name;
                if (excludedVersions.Contains(dirName))
                    continue;

                var result = FindAssemblyRecursive(subDir, fileName, excludedVersions);
                if (result != null)
                    return result;
            }

            return null;
        }
    }
}
