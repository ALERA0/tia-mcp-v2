using System.ComponentModel;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.Text.Json;
using TiaMcpV2.Core;
using TiaMcpV2.Helpers;
using TiaMcpV2.Models;
using TiaMcpV2.Services;

namespace TiaMcpV2.Tools
{
    [McpServerToolType]
    public static class LadFbdTools
    {
        [McpServerTool(Name = "create_lad_block"), Description("Create a LAD (Ladder Diagram) block. Provide block definition as JSON with: blockType (FB/FC/OB), blockName, blockNumber, interface members (inputs/outputs/inouts/statics/temps), and networks with instructions. Each instruction has: Type (Contact, Contact_NC, Coil, Set_Coil, Reset_Coil, TON, TOF, CTU, MOVE, ADD, CMP_GT, SR, RS, CALL), Operand (variable name), Inputs/Outputs (pin→variable mappings).")]
        public static string CreateLadBlock(
            string softwarePath,
            string groupPath,
            string blockDefinitionJson)
        {
            try
            {
                var def = JsonSerializer.Deserialize<BlockDefinition>(blockDefinitionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (def == null)
                    return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = "Invalid block definition JSON" });

                var xml = ServiceAccessor.LadFbdGenerator.GenerateLadBlockXml(
                    def.BlockType ?? "FB", def.BlockName ?? "NewBlock", def.BlockNumber,
                    def.Inputs ?? new List<LadFbdGeneratorService.MemberDef>(),
                    def.Outputs ?? new List<LadFbdGeneratorService.MemberDef>(),
                    def.InOuts ?? new List<LadFbdGeneratorService.MemberDef>(),
                    def.Statics ?? new List<LadFbdGeneratorService.MemberDef>(),
                    def.Temps ?? new List<LadFbdGeneratorService.MemberDef>(),
                    def.Constants ?? new List<LadFbdGeneratorService.MemberDef>(),
                    def.Networks ?? new List<LadFbdGeneratorService.NetworkDef>());

                ServiceAccessor.LadFbdGenerator.GenerateAndImport(softwarePath, groupPath, xml, def.BlockName ?? "NewBlock");

                return JsonHelper.ToJson(new { Success = true, Message = $"LAD block '{def.BlockName}' created and imported", BlockName = def.BlockName, Language = "LAD" });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "create_fbd_block"), Description("Create an FBD (Function Block Diagram) block. Same JSON structure as create_lad_block but generates FBD logic. Instruction types: AND, OR, XOR, NOT, MOVE, ADD, SUB, MUL, DIV, CMP_GT, CMP_LT, CMP_EQ, TON, TOF, CTU, SR, RS, CALL.")]
        public static string CreateFbdBlock(
            string softwarePath,
            string groupPath,
            string blockDefinitionJson)
        {
            try
            {
                var def = JsonSerializer.Deserialize<BlockDefinition>(blockDefinitionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (def == null)
                    return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = "Invalid block definition JSON" });

                var xml = ServiceAccessor.LadFbdGenerator.GenerateFbdBlockXml(
                    def.BlockType ?? "FB", def.BlockName ?? "NewBlock", def.BlockNumber,
                    def.Inputs ?? new List<LadFbdGeneratorService.MemberDef>(),
                    def.Outputs ?? new List<LadFbdGeneratorService.MemberDef>(),
                    def.InOuts ?? new List<LadFbdGeneratorService.MemberDef>(),
                    def.Statics ?? new List<LadFbdGeneratorService.MemberDef>(),
                    def.Temps ?? new List<LadFbdGeneratorService.MemberDef>(),
                    def.Constants ?? new List<LadFbdGeneratorService.MemberDef>(),
                    def.Networks ?? new List<LadFbdGeneratorService.NetworkDef>());

                ServiceAccessor.LadFbdGenerator.GenerateAndImport(softwarePath, groupPath, xml, def.BlockName ?? "NewBlock");

                return JsonHelper.ToJson(new { Success = true, Message = $"FBD block '{def.BlockName}' created and imported", BlockName = def.BlockName, Language = "FBD" });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "generate_lad_xml"), Description("Generate LAD block XML without importing — returns the SimaticML XML string. Use this to preview or modify the XML before importing with write_block_xml.")]
        public static string GenerateLadXml(string blockDefinitionJson)
        {
            try
            {
                var def = JsonSerializer.Deserialize<BlockDefinition>(blockDefinitionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (def == null)
                    return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = "Invalid JSON" });

                var xml = ServiceAccessor.LadFbdGenerator.GenerateLadBlockXml(
                    def.BlockType ?? "FB", def.BlockName ?? "NewBlock", def.BlockNumber,
                    def.Inputs ?? new List<LadFbdGeneratorService.MemberDef>(),
                    def.Outputs ?? new List<LadFbdGeneratorService.MemberDef>(),
                    def.InOuts ?? new List<LadFbdGeneratorService.MemberDef>(),
                    def.Statics ?? new List<LadFbdGeneratorService.MemberDef>(),
                    def.Temps ?? new List<LadFbdGeneratorService.MemberDef>(),
                    def.Constants ?? new List<LadFbdGeneratorService.MemberDef>(),
                    def.Networks ?? new List<LadFbdGeneratorService.NetworkDef>());

                return JsonHelper.ToJson(new { Success = true, XmlContent = xml, Language = "LAD", BlockName = def.BlockName });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "generate_fbd_xml"), Description("Generate FBD block XML without importing — returns the SimaticML XML string.")]
        public static string GenerateFbdXml(string blockDefinitionJson)
        {
            try
            {
                var def = JsonSerializer.Deserialize<BlockDefinition>(blockDefinitionJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (def == null)
                    return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = "Invalid JSON" });

                var xml = ServiceAccessor.LadFbdGenerator.GenerateFbdBlockXml(
                    def.BlockType ?? "FB", def.BlockName ?? "NewBlock", def.BlockNumber,
                    def.Inputs ?? new List<LadFbdGeneratorService.MemberDef>(),
                    def.Outputs ?? new List<LadFbdGeneratorService.MemberDef>(),
                    def.InOuts ?? new List<LadFbdGeneratorService.MemberDef>(),
                    def.Statics ?? new List<LadFbdGeneratorService.MemberDef>(),
                    def.Temps ?? new List<LadFbdGeneratorService.MemberDef>(),
                    def.Constants ?? new List<LadFbdGeneratorService.MemberDef>(),
                    def.Networks ?? new List<LadFbdGeneratorService.NetworkDef>());

                return JsonHelper.ToJson(new { Success = true, XmlContent = xml, Language = "FBD", BlockName = def.BlockName });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "get_lad_fbd_instruction_reference"), Description("Get a reference of all supported LAD/FBD instruction types with their pin names. Use this to understand what instruction types and pin connections are available for create_lad_block and create_fbd_block.")]
        public static string GetInstructionReference()
        {
            var reference = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object> { ["Type"] = "Contact", ["Description"] = "Normally Open contact (LAD)", ["Operand"] = "variable name", ["Pins"] = "in, out, operand" },
                new Dictionary<string, object> { ["Type"] = "Contact_NC", ["Description"] = "Normally Closed contact (LAD)", ["Operand"] = "variable name", ["Pins"] = "in, out, operand" },
                new Dictionary<string, object> { ["Type"] = "Coil", ["Description"] = "Output coil (LAD)", ["Operand"] = "variable name", ["Pins"] = "in, operand" },
                new Dictionary<string, object> { ["Type"] = "Set_Coil", ["Description"] = "Set (latch) coil (LAD)", ["Operand"] = "variable name", ["Pins"] = "in, operand" },
                new Dictionary<string, object> { ["Type"] = "Reset_Coil", ["Description"] = "Reset (unlatch) coil (LAD)", ["Operand"] = "variable name", ["Pins"] = "in, operand" },
                new Dictionary<string, object> { ["Type"] = "AND", ["Description"] = "AND gate (FBD)", ["Inputs"] = "in1, in2, in3...", ["Outputs"] = "out" },
                new Dictionary<string, object> { ["Type"] = "OR", ["Description"] = "OR gate (FBD)", ["Inputs"] = "in1, in2, in3...", ["Outputs"] = "out" },
                new Dictionary<string, object> { ["Type"] = "XOR", ["Description"] = "XOR gate (FBD)", ["Inputs"] = "in1, in2", ["Outputs"] = "out" },
                new Dictionary<string, object> { ["Type"] = "NOT", ["Description"] = "NOT gate (FBD)", ["Inputs"] = "in", ["Outputs"] = "out" },
                new Dictionary<string, object> { ["Type"] = "SR", ["Description"] = "Set-Reset flip-flop", ["Inputs"] = "S, R1", ["Outputs"] = "Q" },
                new Dictionary<string, object> { ["Type"] = "RS", ["Description"] = "Reset-Set flip-flop", ["Inputs"] = "R, S1", ["Outputs"] = "Q" },
                new Dictionary<string, object> { ["Type"] = "TON", ["Description"] = "On-delay timer", ["Inputs"] = "IN, PT (e.g. T#5s)", ["Outputs"] = "Q, ET" },
                new Dictionary<string, object> { ["Type"] = "TOF", ["Description"] = "Off-delay timer", ["Inputs"] = "IN, PT", ["Outputs"] = "Q, ET" },
                new Dictionary<string, object> { ["Type"] = "TP", ["Description"] = "Pulse timer", ["Inputs"] = "IN, PT", ["Outputs"] = "Q, ET" },
                new Dictionary<string, object> { ["Type"] = "CTU", ["Description"] = "Count up", ["Inputs"] = "CU, R, PV", ["Outputs"] = "Q, CV" },
                new Dictionary<string, object> { ["Type"] = "CTD", ["Description"] = "Count down", ["Inputs"] = "CD, LD, PV", ["Outputs"] = "Q, CV" },
                new Dictionary<string, object> { ["Type"] = "CTUD", ["Description"] = "Count up/down", ["Inputs"] = "CU, CD, R, LD, PV", ["Outputs"] = "QU, QD, CV" },
                new Dictionary<string, object> { ["Type"] = "MOVE", ["Description"] = "Move/assign value", ["Inputs"] = "in", ["Outputs"] = "out1" },
                new Dictionary<string, object> { ["Type"] = "ADD", ["Description"] = "Addition", ["Inputs"] = "in1, in2", ["Outputs"] = "out" },
                new Dictionary<string, object> { ["Type"] = "SUB", ["Description"] = "Subtraction", ["Inputs"] = "in1, in2", ["Outputs"] = "out" },
                new Dictionary<string, object> { ["Type"] = "MUL", ["Description"] = "Multiplication", ["Inputs"] = "in1, in2", ["Outputs"] = "out" },
                new Dictionary<string, object> { ["Type"] = "DIV", ["Description"] = "Division", ["Inputs"] = "in1, in2", ["Outputs"] = "out" },
                new Dictionary<string, object> { ["Type"] = "CMP_GT", ["Description"] = "Compare greater than", ["Inputs"] = "in1, in2", ["Outputs"] = "out" },
                new Dictionary<string, object> { ["Type"] = "CMP_LT", ["Description"] = "Compare less than", ["Inputs"] = "in1, in2", ["Outputs"] = "out" },
                new Dictionary<string, object> { ["Type"] = "CMP_EQ", ["Description"] = "Compare equal", ["Inputs"] = "in1, in2", ["Outputs"] = "out" },
                new Dictionary<string, object> { ["Type"] = "CMP_GE", ["Description"] = "Compare greater or equal", ["Inputs"] = "in1, in2", ["Outputs"] = "out" },
                new Dictionary<string, object> { ["Type"] = "CMP_LE", ["Description"] = "Compare less or equal", ["Inputs"] = "in1, in2", ["Outputs"] = "out" },
                new Dictionary<string, object> { ["Type"] = "CMP_NE", ["Description"] = "Compare not equal", ["Inputs"] = "in1, in2", ["Outputs"] = "out" },
                new Dictionary<string, object> { ["Type"] = "CALL", ["Description"] = "Call FB/FC", ["Operand"] = "block name", ["Inputs"] = "_blockType=FB/FC, _instanceDB=DB name, param1=val...", ["Outputs"] = "param=var" },
                new Dictionary<string, object> { ["Type"] = "ASSIGN", ["Description"] = "Assignment (FBD)", ["Inputs"] = "in", ["Outputs"] = "out1" },
            };

            return JsonHelper.ToJson(new { Success = true, Instructions = reference, Count = reference.Count,
                Message = "Use these instruction types in the 'networks[].instructions[]' array of create_lad_block / create_fbd_block. Variables prefixed with # are local, others are global. Constants: T#5s, 100, 3.14, true/false, 16#FF." });
        }
    }

    // JSON deserialization model
    public class BlockDefinition
    {
        public string? BlockType { get; set; }
        public string? BlockName { get; set; }
        public int BlockNumber { get; set; }
        public List<LadFbdGeneratorService.MemberDef>? Inputs { get; set; }
        public List<LadFbdGeneratorService.MemberDef>? Outputs { get; set; }
        public List<LadFbdGeneratorService.MemberDef>? InOuts { get; set; }
        public List<LadFbdGeneratorService.MemberDef>? Statics { get; set; }
        public List<LadFbdGeneratorService.MemberDef>? Temps { get; set; }
        public List<LadFbdGeneratorService.MemberDef>? Constants { get; set; }
        public List<LadFbdGeneratorService.NetworkDef>? Networks { get; set; }
    }
}
