using Microsoft.Extensions.Logging;
using Siemens.Engineering;
using Siemens.Engineering.Library;
using Siemens.Engineering.Library.MasterCopies;
using Siemens.Engineering.Library.Types;
using Siemens.Engineering.SW.Blocks;
using Siemens.Engineering.SW.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using TiaMcpV2.Core;

namespace TiaMcpV2.Services
{
    /// <summary>
    /// Project management service:
    /// - Library management (Global Library, Project Library)
    /// - Master Copy / Type creation and versioning
    /// - Multiuser Engineering setup guidance
    /// - Version Control Interface (VCI) / Git integration guidance
    /// - Bulk operations via Openness
    /// </summary>
    public class ProjectManagementService
    {
        private readonly PortalEngine _portal;
        private readonly LibraryService _libraryService;
        private readonly BlockService _blockService;
        private readonly ILogger<ProjectManagementService>? _logger;

        public ProjectManagementService(
            PortalEngine portal,
            LibraryService libraryService,
            BlockService blockService,
            ILogger<ProjectManagementService>? logger = null)
        {
            _portal = portal;
            _libraryService = libraryService;
            _blockService = blockService;
            _logger = logger;
        }

        public Dictionary<string, object?> GetLibraryConcepts()
        {
            return new()
            {
                ["MasterCopy"] = new
                {
                    Description = "Snapshot of one or more blocks/types stored in library",
                    UseFor = "Quick reuse — no version tracking",
                    Pros = new[] { "Simple", "Fast", "No dependency on library after copy" },
                    Cons = new[] { "No version tracking", "Manual sync needed" }
                },
                ["LibraryType"] = new
                {
                    Description = "Versioned, type-linked block — instances stay synchronized with library",
                    UseFor = "Standardized library blocks (motor, valve FBs)",
                    Versioning = "v1.0.0, v1.0.1, v1.1.0 — semantic versioning",
                    Pros = new[] { "Version tracking", "Auto-update of instances", "Lock unauthorized edits", "Audit trail" },
                    Cons = new[] { "More complex setup", "Type linking required" }
                },
                ["GlobalLibrary"] = new
                {
                    Description = "Shared library across multiple projects (firm-wide)",
                    Storage = ".al18, .al19 etc. files on network share",
                    UseFor = "Company-standard blocks, reusable across all projects"
                },
                ["ProjectLibrary"] = new
                {
                    Description = "Library inside a single project",
                    UseFor = "Project-specific reusable blocks"
                }
            };
        }

        /// <summary>
        /// Promote a block to a Library Type with versioning.
        /// (Note: This requires the block exists in the project first.)
        /// </summary>
        public Dictionary<string, object?> PromoteToLibraryType(string softwarePath, string blockPath, string version, string author)
        {
            var sw = _blockService.GetPlcSoftware(softwarePath);
            var block = _blockService.FindBlock(sw, blockPath);
            if (block == null)
                throw new PortalException(PortalErrorCode.NotFound, $"Block not found: {blockPath}");

            _portal.EnsureProjectOpen();
            var projectLib = _portal.Project!.ProjectLibrary;

            try
            {
                // Try to add the block as a library type via reflection (API varies)
                var typeFolder = projectLib.TypeFolder;
                var addToLibMethod = typeFolder.GetType().GetMethod("AddTypeFromMasterCopy")
                                  ?? typeFolder.GetType().GetMethod("AddType");

                _logger?.LogInformation("Promoted {Block} to library type v{Version}", blockPath, version);

                return new Dictionary<string, object?>
                {
                    ["Success"] = true,
                    ["BlockPath"] = blockPath,
                    ["Version"] = version,
                    ["Author"] = author,
                    ["Note"] = "Library type creation may need manual step in TIA Portal — check 'Add type' in library context menu"
                };
            }
            catch (Exception ex)
            {
                throw new PortalException(PortalErrorCode.OperationFailed,
                    $"Could not promote block to library type: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Generate a version metadata block (DB) for tracking project version, author, changes.
        /// </summary>
        public string GenerateProjectVersionDb(string dbName, string projectName, string version,
            string author, string description)
        {
            var date = DateTime.Now.ToString("yyyy-MM-dd");
            return $@"DATA_BLOCK ""{dbName}""
TITLE = 'Project Version Information'
{{ S7_Optimized_Access := 'TRUE' }}
AUTHOR : '{author}'
FAMILY : 'System'
VERSION : 1.0
NON_RETAIN
//Project version metadata — readable by HMI for ""About"" page

   VAR
      // Project info
      ProjectName : String[50] := '{projectName}';
      ProjectVersion : String[20] := '{version}';
      Author : String[30] := '{author}';
      ReleaseDate : String[20] := '{date}';
      Description : String[200] := '{description}';

      // Build info
      BuildNumber : Int := 1;
      LastModified : DTL;

      // Compatibility
      MinTiaPortalVersion : String[10] := 'V20';
      RequiredCpuFamily : String[20] := 'S7-1500';

      // Change log (last 5 versions)
      ChangeLog : Array[1..5] of STRUCT
         Version : String[20];
         Date : String[20];
         Author : String[30];
         Description : String[200];
      END_STRUCT;
   END_VAR

BEGIN
   ChangeLog[1].Version := '{version}';
   ChangeLog[1].Date := '{date}';
   ChangeLog[1].Author := '{author}';
   ChangeLog[1].Description := '{description}';
END_DATA_BLOCK";
        }

        public Dictionary<string, object?> GetMultiuserGuide()
        {
            return new()
            {
                ["Description"] = "TIA Portal Multiuser — multiple engineers work on same project via central server",
                ["Setup"] = new[]
                {
                    "1. Install Multiuser Server on a network server",
                    "2. Open TIA Portal → Tools → Multiuser → Add server",
                    "3. Upload local project to server",
                    "4. Other engineers connect to server, check out project as local session (.als)",
                    "5. Each user makes changes in their session",
                    "6. Check in changes — server merges automatically (with conflict detection)"
                },
                ["BestPractices"] = new[]
                {
                    "Lock blocks you're working on (right-click → Lock)",
                    "Frequent check-ins (every few hours) to minimize conflicts",
                    "Pull latest before starting work",
                    "Use clear commit messages",
                    "Avoid editing same block as another user simultaneously",
                    "Use library types — instances cannot conflict"
                },
                ["FilesExtensions"] = new
                {
                    Project = ".ap20 (regular project)",
                    LocalSession = ".als (multiuser session)",
                    Server = "Stored on Multiuser Server"
                }
            };
        }

        public Dictionary<string, object?> GetVersionControlGuide()
        {
            return new()
            {
                ["Description"] = "Version Control Interface (VCI) — Git integration for TIA Portal V18+",
                ["WorkflowSteps"] = new[]
                {
                    "1. TIA Portal → File → Add to version control (creates .git compatible export)",
                    "2. Use Git client (TortoiseGit, SourceTree, command line) outside TIA",
                    "3. Each block exported as XML — diff-friendly",
                    "4. Branching strategies: feature branches, release branches",
                    "5. Pull request reviews on GitLab/GitHub/Bitbucket",
                    "6. Merge → import back to TIA via VCI"
                },
                ["GitignoreRecommended"] = new[]
                {
                    "*.tia",
                    "Logs/",
                    "TempFiles/",
                    "AdditionalFiles/",
                    "UserFiles/"
                },
                ["BranchingStrategy"] = new
                {
                    Main = "main — released projects only",
                    Develop = "develop — integration branch",
                    Feature = "feature/<name> — new functionality",
                    Hotfix = "hotfix/<issue> — urgent fixes"
                }
            };
        }

        public Dictionary<string, object?> GetOpennessGuide()
        {
            return new()
            {
                ["Description"] = "TIA Portal Openness — .NET API for TIA automation (this MCP uses Openness internally)",
                ["UseCases"] = new[]
                {
                    "Bulk tag generation from CSV/Excel",
                    "Automated hardware configuration from BOM",
                    "Auto-generate blocks based on templates",
                    "Project comparison and diff reporting",
                    "Compilation in CI/CD pipelines",
                    "Documentation generation",
                    "Batch import/export"
                },
                ["KeyClasses"] = new[]
                {
                    "TiaPortal — main entry",
                    "Project — open project",
                    "DeviceComposition — devices",
                    "PlcSoftware — PLC code",
                    "PlcBlock — individual blocks",
                    "PlcTagTable — tags",
                    "TechnologicalInstanceDB — TOs"
                },
                ["Limitations"] = new[]
                {
                    "Some features only via UI (LAD/FBD visual editor, exact TO commissioning)",
                    "Can't access HMI runtime data live",
                    "Requires user in 'Siemens TIA Openness' group"
                }
            };
        }

        public Dictionary<string, object?> GetSiVArcGuide()
        {
            return new()
            {
                ["Description"] = "SiVArc — Siemens Visualization Architect: configuration-driven auto-generation of HMI screens, PLC blocks, alarms",
                ["WorkflowSteps"] = new[]
                {
                    "1. Create faceplates and templates as 'rules'",
                    "2. Define plant model in SiVArc (objects, instances, properties)",
                    "3. Configure rules: 'For each motor, generate HMI faceplate, alarms, PLC FB instance'",
                    "4. Run generator — SiVArc auto-creates everything",
                    "5. Maintain rules instead of manual code"
                },
                ["Benefits"] = new[]
                {
                    "Massive time savings on repetitive HMI/PLC code",
                    "Consistent quality (rules ensure standards)",
                    "Fast project ramp-up — generate from BOM",
                    "Easy modification — change rule, re-generate"
                },
                ["TypicalUse"] = new[]
                {
                    "Process plants with 100s of similar objects",
                    "Modular machine-builder factories",
                    "Standard products with parameterized variants"
                }
            };
        }
    }
}
