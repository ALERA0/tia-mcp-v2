using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using TiaMcpV2.Core;

namespace TiaMcpV2.Services
{
    /// <summary>
    /// Generates SimaticML XML for LAD, FBD, and STL blocks.
    /// Produces valid import-ready XML that TIA Portal V19/V20/V21 accepts.
    ///
    /// LAD elements: Contact (NO/NC), Coil (normal/set/reset), Timer, Counter, Move, Compare, Math, FB/FC Call
    /// FBD elements: AND/OR/XOR/NOT gates, Assignment, Timer, Counter, Move, Compare, Math, FB/FC Call
    /// STL elements: Statement list text
    /// </summary>
    public class LadFbdGeneratorService
    {
        private readonly PortalEngine _portal;
        private readonly BlockService _blockService;
        private readonly ILogger<LadFbdGeneratorService>? _logger;

        private static readonly XNamespace SI = "http://www.siemens.com/automation/Openness/SW/Interface/v5";
        private static readonly XNamespace FLG = "http://www.siemens.com/automation/Openness/SW/NetworkSource/FlgNet/v4";
        private static readonly XNamespace STL_NS = "http://www.siemens.com/automation/Openness/SW/NetworkSource/StatementList/v4";
        private static readonly XNamespace SCL_NS = "http://www.siemens.com/automation/Openness/SW/NetworkSource/StructuredText/v3";

        private int _uidCounter;

        public LadFbdGeneratorService(PortalEngine portal, BlockService blockService, ILogger<LadFbdGeneratorService>? logger = null)
        {
            _portal = portal;
            _blockService = blockService;
            _logger = logger;
        }

        private int NextUid() => ++_uidCounter;

        #region Public API

        /// <summary>
        /// Generate a complete LAD block XML.
        /// networks: list of network descriptions, each containing instructions.
        /// </summary>
        public string GenerateLadBlockXml(string blockType, string blockName, int blockNumber,
            List<MemberDef> inputs, List<MemberDef> outputs, List<MemberDef> inouts,
            List<MemberDef> statics, List<MemberDef> temps, List<MemberDef> constants,
            List<NetworkDef> networks)
        {
            return GenerateBlockXml("LAD", blockType, blockName, blockNumber,
                inputs, outputs, inouts, statics, temps, constants, networks);
        }

        /// <summary>
        /// Generate a complete FBD block XML.
        /// </summary>
        public string GenerateFbdBlockXml(string blockType, string blockName, int blockNumber,
            List<MemberDef> inputs, List<MemberDef> outputs, List<MemberDef> inouts,
            List<MemberDef> statics, List<MemberDef> temps, List<MemberDef> constants,
            List<NetworkDef> networks)
        {
            return GenerateBlockXml("FBD", blockType, blockName, blockNumber,
                inputs, outputs, inouts, statics, temps, constants, networks);
        }

        /// <summary>
        /// Generate and import a LAD/FBD/STL block directly into TIA Portal.
        /// </summary>
        public void GenerateAndImport(string softwarePath, string groupPath, string xml, string blockName)
        {
            var sw = _blockService.GetPlcSoftware(softwarePath);
            var group = _blockService.FindBlockGroup(sw, groupPath);

            var tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "TiaMcpV2", "ladfbd");
            System.IO.Directory.CreateDirectory(tempDir);
            var tempFile = System.IO.Path.Combine(tempDir, $"{blockName}_{Guid.NewGuid():N}.xml");

            try
            {
                System.IO.File.WriteAllText(tempFile, xml, Encoding.UTF8);
                group.Blocks.Import(new System.IO.FileInfo(tempFile),
                    Siemens.Engineering.ImportOptions.Override);
                _logger?.LogInformation("Imported {Block} as LAD/FBD/STL", blockName);
            }
            finally
            {
                try { System.IO.File.Delete(tempFile); } catch { }
            }
        }

        #endregion

        #region XML Generation Core

        private string GenerateBlockXml(string language, string blockType, string blockName, int blockNumber,
            List<MemberDef> inputs, List<MemberDef> outputs, List<MemberDef> inouts,
            List<MemberDef> statics, List<MemberDef> temps, List<MemberDef> constants,
            List<NetworkDef> networks)
        {
            _uidCounter = 0;
            var swBlockType = GetSwBlockType(blockType);

            var doc = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement("Document",
                    new XElement(swBlockType,
                        new XAttribute("ID", "0"),
                        new XElement("AttributeList",
                            new XElement("AutoNumber", blockNumber == 0 ? "true" : "false"),
                            new XElement("HeaderAuthor", "TiaMcpV2"),
                            new XElement("HeaderFamily", ""),
                            new XElement("HeaderName", ""),
                            new XElement("HeaderVersion", "0.1"),
                            GenerateInterface(blockType, inputs, outputs, inouts, statics, temps, constants),
                            new XElement("MemoryLayout", "Optimized"),
                            new XElement("Name", blockName),
                            blockNumber > 0 ? new XElement("Number", blockNumber) : null,
                            new XElement("ProgrammingLanguage", language)
                        ),
                        new XElement("ObjectList",
                            networks.Select((net, idx) => GenerateCompileUnit(language, net, idx + 1))
                        )
                    )
                )
            );

            return doc.ToString();
        }

        private string GetSwBlockType(string blockType)
        {
            switch (blockType.ToUpperInvariant())
            {
                case "FB": return "SW.Blocks.FB";
                case "FC": return "SW.Blocks.FC";
                case "OB": return "SW.Blocks.OB";
                default: return "SW.Blocks.FB";
            }
        }

        private XElement GenerateInterface(string blockType, List<MemberDef> inputs, List<MemberDef> outputs,
            List<MemberDef> inouts, List<MemberDef> statics, List<MemberDef> temps, List<MemberDef> constants)
        {
            var sections = new List<XElement>();

            sections.Add(GenerateSection("Input", inputs));
            sections.Add(GenerateSection("Output", outputs));
            sections.Add(GenerateSection("InOut", inouts));

            if (blockType.ToUpperInvariant() == "FB")
                sections.Add(GenerateSection("Static", statics));

            sections.Add(GenerateSection("Temp", temps));

            if (constants != null && constants.Count > 0)
                sections.Add(GenerateSection("Constant", constants));

            return new XElement("Interface",
                new XElement(SI + "Sections", sections));
        }

        private XElement GenerateSection(string name, List<MemberDef> members)
        {
            var section = new XElement(SI + "Section", new XAttribute("Name", name));
            if (members != null)
            {
                foreach (var m in members)
                {
                    var memberEl = new XElement(SI + "Member",
                        new XAttribute("Name", m.Name),
                        new XAttribute("Datatype", m.DataType));
                    if (!string.IsNullOrEmpty(m.StartValue))
                        memberEl.Add(new XElement(SI + "StartValue", m.StartValue));
                    if (!string.IsNullOrEmpty(m.Comment))
                        memberEl.Add(new XElement(SI + "Comment",
                            new XElement("MultiLanguageText",
                                new XAttribute(XNamespace.Xml + "lang", "en-US"), m.Comment)));
                    section.Add(memberEl);
                }
            }
            return section;
        }

        private XElement GenerateCompileUnit(string language, NetworkDef network, int cuId)
        {
            _uidCounter = 0; // Reset per network

            var cu = new XElement("SW.Blocks.CompileUnit",
                new XAttribute("ID", cuId.ToString("X")),
                new XAttribute("CompositionName", "CompileUnits"),
                new XElement("AttributeList",
                    new XElement("NetworkSource",
                        language == "LAD" || language == "FBD"
                            ? GenerateFlgNet(network)
                            : GenerateStlSource(network)
                    ),
                    new XElement("ProgrammingLanguage", language)
                ),
                new XElement("ObjectList",
                    !string.IsNullOrEmpty(network.Title)
                        ? new XElement("MultilingualText",
                            new XAttribute("ID", (cuId * 100).ToString("X")),
                            new XAttribute("CompositionName", "Title"),
                            new XElement("ObjectList",
                                new XElement("MultilingualTextItem",
                                    new XAttribute("ID", (cuId * 100 + 1).ToString("X")),
                                    new XAttribute("CompositionName", "Items"),
                                    new XElement("AttributeList",
                                        new XElement("Culture", "en-US"),
                                        new XElement("Text", network.Title)
                                    )
                                )
                            )
                        )
                        : null
                )
            );

            return cu;
        }

        #endregion

        #region LAD/FBD FlgNet Generation

        private XElement GenerateFlgNet(NetworkDef network)
        {
            var parts = new List<XElement>();
            var wires = new List<XElement>();

            foreach (var instr in network.Instructions)
            {
                GenerateInstruction(instr, parts, wires);
            }

            return new XElement(FLG + "FlgNet",
                new XElement(FLG + "Parts", parts),
                new XElement(FLG + "Wires", wires)
            );
        }

        private void GenerateInstruction(InstructionDef instr, List<XElement> parts, List<XElement> wires)
        {
            switch (instr.Type.ToUpperInvariant())
            {
                case "CONTACT":
                    GenerateContact(instr, parts, wires);
                    break;
                case "CONTACT_NC":
                    GenerateContact(instr, parts, wires, negated: true);
                    break;
                case "COIL":
                    GenerateCoil(instr, parts, wires);
                    break;
                case "SET_COIL":
                    GenerateCoil(instr, parts, wires, "SCoil");
                    break;
                case "RESET_COIL":
                    GenerateCoil(instr, parts, wires, "RCoil");
                    break;
                case "TON":
                case "TOF":
                case "TP":
                    GenerateTimer(instr, parts, wires, instr.Type.ToUpperInvariant());
                    break;
                case "CTU":
                case "CTD":
                case "CTUD":
                    GenerateCounter(instr, parts, wires, instr.Type.ToUpperInvariant());
                    break;
                case "MOVE":
                    GenerateMove(instr, parts, wires);
                    break;
                case "ADD": case "SUB": case "MUL": case "DIV":
                    GenerateMath(instr, parts, wires, instr.Type.ToUpperInvariant());
                    break;
                case "CMP_GT": case "CMP_LT": case "CMP_EQ": case "CMP_GE": case "CMP_LE": case "CMP_NE":
                    GenerateCompare(instr, parts, wires, instr.Type.ToUpperInvariant().Replace("CMP_", ""));
                    break;
                case "AND": case "OR": case "XOR":
                    GenerateLogicGate(instr, parts, wires, instr.Type.ToUpperInvariant());
                    break;
                case "NOT":
                    GenerateNot(instr, parts, wires);
                    break;
                case "SR": case "RS":
                    GenerateFlipFlop(instr, parts, wires, instr.Type.ToUpperInvariant());
                    break;
                case "ASSIGN":
                    GenerateAssign(instr, parts, wires);
                    break;
                case "CALL":
                    GenerateCall(instr, parts, wires);
                    break;
                default:
                    // Generic Part
                    GenerateGenericPart(instr, parts, wires);
                    break;
            }
        }

        private XElement MakeAccess(string varName, string scope = "GlobalVariable")
        {
            var uid = NextUid();
            // Determine scope
            if (varName.StartsWith("#"))
            {
                scope = "LocalVariable";
                varName = varName.TrimStart('#');
            }
            else if (varName.StartsWith("\"") && varName.EndsWith("\""))
            {
                scope = "GlobalVariable";
                varName = varName.Trim('"');
            }
            else if (varName.Contains("."))
            {
                scope = "GlobalVariable";
            }

            var components = varName.Split('.');
            var symbol = new XElement(FLG + "Symbol");
            foreach (var comp in components)
            {
                symbol.Add(new XElement(FLG + "Component", new XAttribute("Name", comp)));
            }

            return new XElement(FLG + "Access",
                new XAttribute("Scope", scope),
                new XAttribute("UId", uid),
                symbol);
        }

        private XElement MakeConstant(string value, string dataType = "")
        {
            var uid = NextUid();
            var access = new XElement(FLG + "Access",
                new XAttribute("Scope", "LiteralConstant"),
                new XAttribute("UId", uid),
                new XElement(FLG + "Constant",
                    new XElement(FLG + "ConstantValue", value)
                )
            );
            return access;
        }

        private void GenerateContact(InstructionDef instr, List<XElement> parts, List<XElement> wires, bool negated = false)
        {
            var varAccess = MakeAccess(instr.Operand ?? instr.Inputs?.FirstOrDefault().Value ?? "");
            var contactUid = NextUid();
            var contact = new XElement(FLG + "Part",
                new XAttribute("Name", "Contact"),
                new XAttribute("UId", contactUid));
            if (negated)
                contact.Add(new XElement(FLG + "Negated", new XAttribute("Name", "operand")));

            parts.Add(varAccess);
            parts.Add(contact);

            // Wire: powerrail → contact.in
            wires.Add(new XElement(FLG + "Wire",
                new XAttribute("UId", NextUid()),
                new XElement(FLG + "Powerrail"),
                new XElement(FLG + "NameCon", new XAttribute("UId", contactUid), new XAttribute("Name", "in"))));

            // Wire: varAccess → contact.operand
            wires.Add(new XElement(FLG + "Wire",
                new XAttribute("UId", NextUid()),
                new XElement(FLG + "IdentCon", new XAttribute("UId", int.Parse(varAccess.Attribute("UId").Value))),
                new XElement(FLG + "NameCon", new XAttribute("UId", contactUid), new XAttribute("Name", "operand"))));
        }

        private void GenerateCoil(InstructionDef instr, List<XElement> parts, List<XElement> wires, string coilType = "Coil")
        {
            var varAccess = MakeAccess(instr.Operand ?? instr.Outputs?.FirstOrDefault().Value ?? "");
            var coilUid = NextUid();
            var coil = new XElement(FLG + "Part",
                new XAttribute("Name", coilType),
                new XAttribute("UId", coilUid));

            parts.Add(coil);
            parts.Add(varAccess);

            // Wire: coil.operand → var
            wires.Add(new XElement(FLG + "Wire",
                new XAttribute("UId", NextUid()),
                new XElement(FLG + "NameCon", new XAttribute("UId", coilUid), new XAttribute("Name", "operand")),
                new XElement(FLG + "IdentCon", new XAttribute("UId", int.Parse(varAccess.Attribute("UId").Value)))));
        }

        private void GenerateTimer(InstructionDef instr, List<XElement> parts, List<XElement> wires, string timerType)
        {
            var timerUid = NextUid();
            var timer = new XElement(FLG + "Part",
                new XAttribute("Name", timerType),
                new XAttribute("UId", timerUid),
                new XAttribute("Version", "1.0"));

            parts.Add(timer);

            // Connect inputs if provided
            if (instr.Inputs != null)
            {
                foreach (var kvp in instr.Inputs)
                {
                    var accessEl = IsConstant(kvp.Value) ? MakeConstant(kvp.Value) : MakeAccess(kvp.Value);
                    parts.Add(accessEl);
                    wires.Add(new XElement(FLG + "Wire",
                        new XAttribute("UId", NextUid()),
                        new XElement(FLG + "IdentCon", new XAttribute("UId", int.Parse(accessEl.Attribute("UId").Value))),
                        new XElement(FLG + "NameCon", new XAttribute("UId", timerUid), new XAttribute("Name", kvp.Key))));
                }
            }

            // Connect outputs
            if (instr.Outputs != null)
            {
                foreach (var kvp in instr.Outputs)
                {
                    var accessEl = MakeAccess(kvp.Value);
                    parts.Add(accessEl);
                    wires.Add(new XElement(FLG + "Wire",
                        new XAttribute("UId", NextUid()),
                        new XElement(FLG + "NameCon", new XAttribute("UId", timerUid), new XAttribute("Name", kvp.Key)),
                        new XElement(FLG + "IdentCon", new XAttribute("UId", int.Parse(accessEl.Attribute("UId").Value)))));
                }
            }
        }

        private void GenerateCounter(InstructionDef instr, List<XElement> parts, List<XElement> wires, string ctrType)
        {
            GenerateTimer(instr, parts, wires, ctrType); // Same structure
        }

        private void GenerateMove(InstructionDef instr, List<XElement> parts, List<XElement> wires)
        {
            var moveUid = NextUid();
            parts.Add(new XElement(FLG + "Part",
                new XAttribute("Name", "Move"),
                new XAttribute("UId", moveUid),
                new XAttribute("Version", "1.0")));

            if (instr.Inputs?.ContainsKey("in") == true)
            {
                var src = IsConstant(instr.Inputs["in"]) ? MakeConstant(instr.Inputs["in"]) : MakeAccess(instr.Inputs["in"]);
                parts.Add(src);
                wires.Add(new XElement(FLG + "Wire",
                    new XAttribute("UId", NextUid()),
                    new XElement(FLG + "IdentCon", new XAttribute("UId", int.Parse(src.Attribute("UId").Value))),
                    new XElement(FLG + "NameCon", new XAttribute("UId", moveUid), new XAttribute("Name", "in"))));
            }

            if (instr.Outputs?.ContainsKey("out1") == true)
            {
                var dst = MakeAccess(instr.Outputs["out1"]);
                parts.Add(dst);
                wires.Add(new XElement(FLG + "Wire",
                    new XAttribute("UId", NextUid()),
                    new XElement(FLG + "NameCon", new XAttribute("UId", moveUid), new XAttribute("Name", "out1")),
                    new XElement(FLG + "IdentCon", new XAttribute("UId", int.Parse(dst.Attribute("UId").Value)))));
            }
        }

        private void GenerateMath(InstructionDef instr, List<XElement> parts, List<XElement> wires, string op)
        {
            var opUid = NextUid();
            parts.Add(new XElement(FLG + "Part",
                new XAttribute("Name", op),
                new XAttribute("UId", opUid),
                new XAttribute("Version", "1.0")));

            ConnectInputsOutputs(instr, opUid, parts, wires);
        }

        private void GenerateCompare(InstructionDef instr, List<XElement> parts, List<XElement> wires, string cmpType)
        {
            var cmpUid = NextUid();
            parts.Add(new XElement(FLG + "Part",
                new XAttribute("Name", cmpType),
                new XAttribute("UId", cmpUid),
                new XAttribute("Version", "1.0")));

            ConnectInputsOutputs(instr, cmpUid, parts, wires);
        }

        private void GenerateLogicGate(InstructionDef instr, List<XElement> parts, List<XElement> wires, string gate)
        {
            var gateUid = NextUid();
            parts.Add(new XElement(FLG + "Part",
                new XAttribute("Name", gate),
                new XAttribute("UId", gateUid)));

            ConnectInputsOutputs(instr, gateUid, parts, wires);
        }

        private void GenerateNot(InstructionDef instr, List<XElement> parts, List<XElement> wires)
        {
            GenerateLogicGate(instr, parts, wires, "Not");
        }

        private void GenerateFlipFlop(InstructionDef instr, List<XElement> parts, List<XElement> wires, string ffType)
        {
            var ffUid = NextUid();
            parts.Add(new XElement(FLG + "Part",
                new XAttribute("Name", ffType),
                new XAttribute("UId", ffUid),
                new XAttribute("Version", "1.0")));

            ConnectInputsOutputs(instr, ffUid, parts, wires);
        }

        private void GenerateAssign(InstructionDef instr, List<XElement> parts, List<XElement> wires)
        {
            GenerateMove(instr, parts, wires);
        }

        private void GenerateCall(InstructionDef instr, List<XElement> parts, List<XElement> wires)
        {
            var callUid = NextUid();
            var callEl = new XElement(FLG + "Call",
                new XAttribute("UId", callUid),
                new XElement(FLG + "CallInfo",
                    new XAttribute("Name", instr.Operand ?? ""),
                    new XAttribute("BlockType", instr.Inputs?.ContainsKey("_blockType") == true ? instr.Inputs["_blockType"] : "FB"))
            );

            // If it's an FB call, add instance DB name
            if (instr.Inputs?.ContainsKey("_instanceDB") == true)
            {
                callEl.Element(FLG + "CallInfo")?.Add(
                    new XElement(FLG + "Instance",
                        new XAttribute("Scope", "GlobalVariable"),
                        new XElement(FLG + "Component", new XAttribute("Name", instr.Inputs["_instanceDB"]))));
            }

            parts.Add(callEl);

            // Connect parameters (exclude internal metadata keys starting with _)
            if (instr.Inputs != null)
            {
                foreach (var kvp in instr.Inputs.Where(k => !k.Key.StartsWith("_")))
                {
                    var accessEl = IsConstant(kvp.Value) ? MakeConstant(kvp.Value) : MakeAccess(kvp.Value);
                    parts.Add(accessEl);
                    wires.Add(new XElement(FLG + "Wire",
                        new XAttribute("UId", NextUid()),
                        new XElement(FLG + "IdentCon", new XAttribute("UId", int.Parse(accessEl.Attribute("UId").Value))),
                        new XElement(FLG + "NameCon", new XAttribute("UId", callUid), new XAttribute("Name", kvp.Key))));
                }
            }

            if (instr.Outputs != null)
            {
                foreach (var kvp in instr.Outputs)
                {
                    var accessEl = MakeAccess(kvp.Value);
                    parts.Add(accessEl);
                    wires.Add(new XElement(FLG + "Wire",
                        new XAttribute("UId", NextUid()),
                        new XElement(FLG + "NameCon", new XAttribute("UId", callUid), new XAttribute("Name", kvp.Key)),
                        new XElement(FLG + "IdentCon", new XAttribute("UId", int.Parse(accessEl.Attribute("UId").Value)))));
                }
            }
        }

        private void GenerateGenericPart(InstructionDef instr, List<XElement> parts, List<XElement> wires)
        {
            var partUid = NextUid();
            var part = new XElement(FLG + "Part",
                new XAttribute("Name", instr.Type),
                new XAttribute("UId", partUid));
            if (!string.IsNullOrEmpty(instr.Version))
                part.Add(new XAttribute("Version", instr.Version));
            parts.Add(part);

            ConnectInputsOutputs(instr, partUid, parts, wires);
        }

        private void ConnectInputsOutputs(InstructionDef instr, int partUid, List<XElement> parts, List<XElement> wires)
        {
            if (instr.Inputs != null)
            {
                foreach (var kvp in instr.Inputs.Where(k => !k.Key.StartsWith("_")))
                {
                    var accessEl = IsConstant(kvp.Value) ? MakeConstant(kvp.Value) : MakeAccess(kvp.Value);
                    parts.Add(accessEl);
                    wires.Add(new XElement(FLG + "Wire",
                        new XAttribute("UId", NextUid()),
                        new XElement(FLG + "IdentCon", new XAttribute("UId", int.Parse(accessEl.Attribute("UId").Value))),
                        new XElement(FLG + "NameCon", new XAttribute("UId", partUid), new XAttribute("Name", kvp.Key))));
                }
            }

            if (instr.Outputs != null)
            {
                foreach (var kvp in instr.Outputs)
                {
                    var accessEl = MakeAccess(kvp.Value);
                    parts.Add(accessEl);
                    wires.Add(new XElement(FLG + "Wire",
                        new XAttribute("UId", NextUid()),
                        new XElement(FLG + "NameCon", new XAttribute("UId", partUid), new XAttribute("Name", kvp.Key)),
                        new XElement(FLG + "IdentCon", new XAttribute("UId", int.Parse(accessEl.Attribute("UId").Value)))));
                }
            }
        }

        private bool IsConstant(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            if (value.StartsWith("T#") || value.StartsWith("t#")) return true;
            if (value.StartsWith("16#")) return true;
            if (double.TryParse(value, out _)) return true;
            if (value == "true" || value == "false" || value == "TRUE" || value == "FALSE") return true;
            return false;
        }

        #endregion

        #region STL Source

        private XElement GenerateStlSource(NetworkDef network)
        {
            // STL is text-based
            var stlText = network.StlCode ?? "";
            return new XElement(STL_NS + "StatementList",
                new XElement(STL_NS + "Statement", stlText));
        }

        #endregion

        #region Data Types for JSON deserialization

        public class MemberDef
        {
            public string Name { get; set; } = "";
            public string DataType { get; set; } = "Bool";
            public string? StartValue { get; set; }
            public string? Comment { get; set; }
        }

        public class NetworkDef
        {
            public string? Title { get; set; }
            public string? Comment { get; set; }
            public List<InstructionDef> Instructions { get; set; } = new List<InstructionDef>();
            public string? StlCode { get; set; } // For STL language
        }

        public class InstructionDef
        {
            public string Type { get; set; } = "";          // Contact, Coil, TON, MOVE, ADD, CMP_GT, CALL, etc.
            public string? Operand { get; set; }             // Variable name for Contact/Coil, or FB name for CALL
            public string? Version { get; set; }             // Instruction version
            public Dictionary<string, string>? Inputs { get; set; }   // Pin name → variable/constant
            public Dictionary<string, string>? Outputs { get; set; }  // Pin name → variable
        }

        #endregion
    }
}
