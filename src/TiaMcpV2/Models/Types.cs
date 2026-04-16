using System;
using System.Collections.Generic;

namespace TiaMcpV2.Models
{
    // Attribute info from TIA objects
    public class AttributeInfo
    {
        public string? Name { get; set; }
        public object? Value { get; set; }
        public string? AccessMode { get; set; }
    }

    // Block hierarchy
    public class BlockGroupInfo
    {
        public string? Name { get; set; }
        public string? Path { get; set; }
        public IEnumerable<BlockGroupInfo>? Groups { get; set; }
        public IEnumerable<BlockInfo>? Blocks { get; set; }
    }

    public class BlockInfo
    {
        public string? Name { get; set; }
        public string? TypeName { get; set; }
        public int Number { get; set; }
        public string? ProgrammingLanguage { get; set; }
        public string? MemoryLayout { get; set; }
        public bool IsConsistent { get; set; }
        public DateTime ModifiedDate { get; set; }
        public bool IsKnowHowProtected { get; set; }
        public string? GroupPath { get; set; }
    }

    // Type/UDT info
    public class TypeInfo
    {
        public string? Name { get; set; }
        public string? GroupPath { get; set; }
        public IEnumerable<AttributeInfo>? Attributes { get; set; }
    }

    // Tag info
    public class TagTableInfo
    {
        public string? Name { get; set; }
        public int TagCount { get; set; }
    }

    public class TagInfo
    {
        public string? Name { get; set; }
        public string? DataType { get; set; }
        public string? LogicalAddress { get; set; }
        public string? Comment { get; set; }
    }

    // Network info
    public class SubnetInfo
    {
        public string? Name { get; set; }
        public string? TypeIdentifier { get; set; }
    }

    public class IoSystemInfo
    {
        public string? Name { get; set; }
        public string? Number { get; set; }
    }

    public class NetworkInterfaceInfo
    {
        public string? Name { get; set; }
        public string? InterfaceType { get; set; }
        public string? IpAddress { get; set; }
        public string? SubnetMask { get; set; }
    }

    // Hardware info
    public class RackSlotInfo
    {
        public int SlotNumber { get; set; }
        public string? ModuleName { get; set; }
        public string? TypeIdentifier { get; set; }
        public bool IsOccupied { get; set; }
    }

    public class DeviceInfo
    {
        public string? Name { get; set; }
        public string? TypeName { get; set; }
        public string? TypeIdentifier { get; set; }
        public IEnumerable<AttributeInfo>? Attributes { get; set; }
    }

    // XML Generator
    public class MemberDefinition
    {
        public string Name { get; set; } = "";
        public string DataType { get; set; } = "Int";
        public string? StartValue { get; set; }
        public string? Comment { get; set; }
    }

    // SCL Generator - V2 NEW
    public class SclGenerationRequest
    {
        public string TemplateName { get; set; } = "";
        public string BlockName { get; set; } = "";
        public Dictionary<string, string>? Parameters { get; set; }
    }

    // I/O Address Plan - V2 NEW
    public class IoAddressEntry
    {
        public string? ModuleName { get; set; }
        public string? ModuleType { get; set; }
        public int Slot { get; set; }
        public int StartByte { get; set; }
        public int Length { get; set; }
        public string? AddressRange { get; set; }
        public string? Description { get; set; }
    }

    // Diagnostics - V2 NEW
    public class DiagnosticEntry
    {
        public DateTime Timestamp { get; set; }
        public string? EventType { get; set; }
        public string? Description { get; set; }
        public string? Source { get; set; }
    }

    // Catalog
    public class CatalogSearchResult
    {
        public string? TypeIdentifier { get; set; }
        public string? OrderNumber { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
    }
}
