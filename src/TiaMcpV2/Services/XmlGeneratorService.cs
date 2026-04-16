using Microsoft.Extensions.Logging;
using Siemens.Engineering;
using Siemens.Engineering.SW;
using Siemens.Engineering.SW.Blocks;
using Siemens.Engineering.SW.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using TiaMcpV2.Core;
using TiaMcpV2.Models;

namespace TiaMcpV2.Services
{
    public class XmlGeneratorService
    {
        private readonly PortalEngine _portal;
        private readonly BlockService _blockService;
        private readonly ILogger<XmlGeneratorService>? _logger;

        private static readonly XNamespace SI = "http://www.siemens.com/automation/Openness/SW/Interface/v5";

        public XmlGeneratorService(PortalEngine portal, BlockService blockService, ILogger<XmlGeneratorService>? logger = null)
        {
            _portal = portal;
            _blockService = blockService;
            _logger = logger;
        }

        public string GenerateGlobalDbXml(string dbName, int dbNumber, List<MemberDefinition> members)
        {
            var doc = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement("Document",
                    new XElement("SW.Blocks.GlobalDB",
                        new XAttribute("ID", "0"),
                        new XElement("AttributeList",
                            new XElement("AutoNumber", dbNumber == 0 ? "true" : "false"),
                            new XElement("HeaderAuthor", ""),
                            new XElement("HeaderFamily", ""),
                            new XElement("HeaderName", ""),
                            new XElement("HeaderVersion", "0.1"),
                            new XElement("Interface",
                                new XElement(SI + "Sections",
                                    new XElement(SI + "Section",
                                        new XAttribute("Name", "Static"),
                                        GenerateMembers(members)
                                    )
                                )
                            ),
                            new XElement("MemoryLayout", "Optimized"),
                            new XElement("Name", dbName),
                            dbNumber > 0 ? new XElement("Number", dbNumber) : null
                        ),
                        new XElement("ObjectList")
                    )
                )
            );

            return doc.ToString();
        }

        public string GenerateUdtXml(string udtName, List<MemberDefinition> members)
        {
            var doc = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement("Document",
                    new XElement("SW.Types.PlcStruct",
                        new XAttribute("ID", "0"),
                        new XElement("AttributeList",
                            new XElement("Interface",
                                new XElement(SI + "Sections",
                                    new XElement(SI + "Section",
                                        new XAttribute("Name", "None"),
                                        GenerateMembers(members)
                                    )
                                )
                            ),
                            new XElement("Name", udtName)
                        ),
                        new XElement("ObjectList")
                    )
                )
            );

            return doc.ToString();
        }

        private IEnumerable<XElement> GenerateMembers(List<MemberDefinition> members)
        {
            return members.Select((m, i) => new XElement(SI + "Member",
                new XAttribute("Name", m.Name),
                new XAttribute("Datatype", m.DataType),
                !string.IsNullOrEmpty(m.StartValue)
                    ? new XElement(SI + "StartValue", m.StartValue)
                    : null,
                !string.IsNullOrEmpty(m.Comment)
                    ? new XElement(SI + "Comment",
                        new XElement("MultiLanguageText",
                            new XAttribute(XNamespace.Xml + "lang", "en-US"),
                            m.Comment))
                    : null
            ));
        }

        public void GenerateAndImportGlobalDb(string softwarePath, string groupPath, string dbName, int dbNumber, List<MemberDefinition> members)
        {
            var sw = _blockService.GetPlcSoftware(softwarePath);
            var group = _blockService.FindBlockGroup(sw, groupPath);

            var xml = GenerateGlobalDbXml(dbName, dbNumber, members);

            var tempDir = Path.Combine(Path.GetTempPath(), "TiaMcpV2");
            Directory.CreateDirectory(tempDir);
            var tempFile = Path.Combine(tempDir, $"{dbName}.xml");

            try
            {
                File.WriteAllText(tempFile, xml);
                group.Blocks.Import(new FileInfo(tempFile), ImportOptions.Override);
                _logger?.LogInformation("Generated and imported GlobalDB: {Name}", dbName);
            }
            finally
            {
                try { File.Delete(tempFile); } catch { }
            }
        }

        public void GenerateAndImportUdt(string softwarePath, string groupPath, string udtName, List<MemberDefinition> members)
        {
            var sw = _blockService.GetPlcSoftware(softwarePath);

            var xml = GenerateUdtXml(udtName, members);

            var tempDir = Path.Combine(Path.GetTempPath(), "TiaMcpV2");
            Directory.CreateDirectory(tempDir);
            var tempFile = Path.Combine(tempDir, $"{udtName}.xml");

            try
            {
                File.WriteAllText(tempFile, xml);

                if (string.IsNullOrEmpty(groupPath))
                {
                    sw.TypeGroup.Types.Import(new FileInfo(tempFile), ImportOptions.Override);
                }
                else
                {
                    var typeGroup = sw.TypeGroup.Groups.FirstOrDefault(g =>
                        g.Name.Equals(groupPath, StringComparison.OrdinalIgnoreCase));
                    if (typeGroup != null)
                        typeGroup.Types.Import(new FileInfo(tempFile), ImportOptions.Override);
                    else
                        sw.TypeGroup.Types.Import(new FileInfo(tempFile), ImportOptions.Override);
                }

                _logger?.LogInformation("Generated and imported UDT: {Name}", udtName);
            }
            finally
            {
                try { File.Delete(tempFile); } catch { }
            }
        }
    }
}
