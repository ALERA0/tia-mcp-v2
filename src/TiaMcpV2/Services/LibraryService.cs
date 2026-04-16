using Microsoft.Extensions.Logging;
using Siemens.Engineering.Library;
using Siemens.Engineering.Library.MasterCopies;
using Siemens.Engineering.Library.Types;
using Siemens.Engineering.SW;
using System;
using System.Collections.Generic;
using System.Linq;
using TiaMcpV2.Core;

namespace TiaMcpV2.Services
{
    public class LibraryService
    {
        private readonly PortalEngine _portal;
        private readonly BlockService _blockService;
        private readonly ILogger<LibraryService>? _logger;

        public LibraryService(PortalEngine portal, BlockService blockService, ILogger<LibraryService>? logger = null)
        {
            _portal = portal;
            _blockService = blockService;
            _logger = logger;
        }

        public Dictionary<string, object?> GetProjectLibrary()
        {
            _portal.EnsureProjectOpen();
            var lib = _portal.Project!.ProjectLibrary;

            return new Dictionary<string, object?>
            {
                ["MasterCopies"] = BuildMasterCopyTree(lib.MasterCopyFolder),
                ["Types"] = BuildTypeTree(lib.TypeFolder)
            };
        }

        private Dictionary<string, object?> BuildMasterCopyTree(MasterCopyFolder folder)
        {
            var copies = new List<string>();
            foreach (var mc in folder.MasterCopies)
            {
                copies.Add(mc.Name);
            }

            var subFolders = new List<Dictionary<string, object?>>();
            foreach (var sub in folder.Folders)
            {
                subFolders.Add(BuildMasterCopyTree(sub));
            }

            return new Dictionary<string, object?>
            {
                ["Name"] = folder.Name,
                ["MasterCopies"] = copies,
                ["Folders"] = subFolders
            };
        }

        private Dictionary<string, object?> BuildTypeTree(LibraryTypeFolder folder)
        {
            var types = new List<string>();
            foreach (var type in folder.Types)
            {
                types.Add(type.Name);
            }

            var subFolders = new List<Dictionary<string, object?>>();
            foreach (var sub in folder.Folders)
            {
                subFolders.Add(BuildTypeTree(sub));
            }

            return new Dictionary<string, object?>
            {
                ["Name"] = folder.Name,
                ["Types"] = types,
                ["Folders"] = subFolders
            };
        }

        public List<Dictionary<string, object?>> GetGlobalLibraries()
        {
            _portal.EnsureConnected();
            var result = new List<Dictionary<string, object?>>();

            foreach (var lib in _portal.TiaPortalInstance!.GlobalLibraries)
            {
                result.Add(new Dictionary<string, object?>
                {
                    ["Name"] = lib.Name,
                    ["IsReadOnly"] = lib.IsReadOnly
                });
            }

            return result;
        }

        public void CopyFromLibrary(string masterCopyPath, string targetDevicePath)
        {
            _portal.EnsureProjectOpen();
            var lib = _portal.Project!.ProjectLibrary;

            var masterCopy = FindMasterCopy(lib.MasterCopyFolder, masterCopyPath);
            if (masterCopy == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Master copy not found: {masterCopyPath}");

            var sw = _blockService.GetPlcSoftware(targetDevicePath);
            sw.BlockGroup.Blocks.CreateFrom(masterCopy);

            _logger?.LogInformation("Copied master copy '{Copy}' to {Target}", masterCopyPath, targetDevicePath);
        }

        private MasterCopy? FindMasterCopy(MasterCopyFolder folder, string path)
        {
            var parts = path.Replace('\\', '/').Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            var currentFolder = folder;
            for (int i = 0; i < parts.Length - 1; i++)
            {
                currentFolder = currentFolder.Folders.FirstOrDefault(f =>
                    f.Name.Equals(parts[i], StringComparison.OrdinalIgnoreCase));
                if (currentFolder == null) return null;
            }

            return currentFolder.MasterCopies.FirstOrDefault(mc =>
                mc.Name.Equals(parts.Last(), StringComparison.OrdinalIgnoreCase));
        }
    }
}
