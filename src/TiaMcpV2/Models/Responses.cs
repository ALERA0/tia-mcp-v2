using System;
using System.Collections.Generic;

namespace TiaMcpV2.Models
{
    // ─── Base ─────────────────────────────────────────────────────────
    public class ResponseMessage
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }

    public class ResponseWithAttributes : ResponseMessage
    {
        public IEnumerable<AttributeInfo>? Attributes { get; set; }
    }

    // ─── Connection ───────────────────────────────────────────────────
    public class ResponseConnect : ResponseMessage
    {
        public string? SessionId { get; set; }
        public string? ProjectName { get; set; }
        public int TiaVersion { get; set; }
    }

    public class ResponseState : ResponseMessage
    {
        public bool IsConnected { get; set; }
        public string? ProjectName { get; set; }
        public string? SessionId { get; set; }
        public int TiaVersion { get; set; }
    }

    // ─── Project ──────────────────────────────────────────────────────
    public class ResponseProjectInfo : ResponseMessage
    {
        public string? Name { get; set; }
        public string? Path { get; set; }
        public string? Author { get; set; }
        public string? Comment { get; set; }
        public DateTime? CreationDate { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public IEnumerable<AttributeInfo>? Attributes { get; set; }
    }

    public class ResponseProjectTree : ResponseMessage
    {
        public object? Tree { get; set; }
    }

    // ─── Devices ──────────────────────────────────────────────────────
    public class ResponseDevices : ResponseMessage
    {
        public IEnumerable<DeviceInfo>? Devices { get; set; }
    }

    public class ResponseDeviceInfo : ResponseWithAttributes
    {
        public string? Name { get; set; }
        public string? TypeName { get; set; }
    }

    public class ResponseCreateDevice : ResponseMessage
    {
        public string? DeviceName { get; set; }
        public string? TypeIdentifier { get; set; }
    }

    public class ResponseRackSlots : ResponseMessage
    {
        public IEnumerable<RackSlotInfo>? Slots { get; set; }
    }

    public class ResponseGetAttribute : ResponseMessage
    {
        public string? AttributeName { get; set; }
        public object? Value { get; set; }
    }

    public class ResponseSearchCatalog : ResponseMessage
    {
        public IEnumerable<CatalogSearchResult>? Results { get; set; }
    }

    // ─── Software ─────────────────────────────────────────────────────
    public class ResponseSoftwareInfo : ResponseWithAttributes
    {
        public string? Name { get; set; }
        public string? TypeName { get; set; }
    }

    public class ResponseCompile : ResponseMessage
    {
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
        public IEnumerable<object>? Messages { get; set; }
    }

    public class ResponseSoftwareTree : ResponseMessage
    {
        public object? Tree { get; set; }
    }

    // ─── Blocks ───────────────────────────────────────────────────────
    public class ResponseBlocks : ResponseMessage
    {
        public IEnumerable<BlockInfo>? Blocks { get; set; }
    }

    public class ResponseBlocksHierarchy : ResponseMessage
    {
        public BlockGroupInfo? Hierarchy { get; set; }
    }

    public class ResponseBlockDetail : ResponseWithAttributes
    {
        public string? Name { get; set; }
        public string? TypeName { get; set; }
        public int Number { get; set; }
        public string? ProgrammingLanguage { get; set; }
        public string? MemoryLayout { get; set; }
        public bool IsConsistent { get; set; }
        public DateTime ModifiedDate { get; set; }
        public bool IsKnowHowProtected { get; set; }
    }

    public class ResponseExport : ResponseMessage
    {
        public string? ExportPath { get; set; }
        public string? Content { get; set; }
    }

    public class ResponseExportAll : ResponseMessage
    {
        public int ExportedCount { get; set; }
        public string? ExportDirectory { get; set; }
        public IEnumerable<string>? ExportedFiles { get; set; }
    }

    public class ResponseImport : ResponseMessage
    {
        public string? ImportedName { get; set; }
    }

    public class ResponseImportMultiple : ResponseMessage
    {
        public int ImportedCount { get; set; }
        public IEnumerable<string>? ImportedItems { get; set; }
    }

    // ─── Tags ─────────────────────────────────────────────────────────
    public class ResponseTagTables : ResponseMessage
    {
        public IEnumerable<TagTableInfo>? TagTables { get; set; }
    }

    public class ResponseTags : ResponseMessage
    {
        public IEnumerable<TagInfo>? Tags { get; set; }
    }

    // ─── Types/UDT ────────────────────────────────────────────────────
    public class ResponseTypes : ResponseMessage
    {
        public IEnumerable<TypeInfo>? Types { get; set; }
    }

    // ─── Network ──────────────────────────────────────────────────────
    public class ResponseSubnets : ResponseMessage
    {
        public IEnumerable<SubnetInfo>? Subnets { get; set; }
    }

    public class ResponseIoSystems : ResponseMessage
    {
        public IEnumerable<IoSystemInfo>? IoSystems { get; set; }
    }

    public class ResponseNetworkInterfaces : ResponseMessage
    {
        public IEnumerable<NetworkInterfaceInfo>? Interfaces { get; set; }
    }

    // ─── Safety ───────────────────────────────────────────────────────
    public class ResponseSafetyInfo : ResponseMessage
    {
        public object? SafetyData { get; set; }
    }

    // ─── Drives ───────────────────────────────────────────────────────
    public class ResponseDriveObjects : ResponseMessage
    {
        public IEnumerable<object>? DriveObjects { get; set; }
    }

    public class ResponseTechObjects : ResponseMessage
    {
        public IEnumerable<object>? TechnologicalObjects { get; set; }
    }

    // ─── HMI ──────────────────────────────────────────────────────────
    public class ResponseHmiTargets : ResponseMessage
    {
        public IEnumerable<object>? HmiTargets { get; set; }
    }

    public class ResponseHmiScreens : ResponseMessage
    {
        public IEnumerable<object>? Screens { get; set; }
    }

    // ─── Library ──────────────────────────────────────────────────────
    public class ResponseProjectLibrary : ResponseMessage
    {
        public object? Library { get; set; }
    }

    public class ResponseGlobalLibraries : ResponseMessage
    {
        public IEnumerable<object>? Libraries { get; set; }
    }

    // ─── V2 NEW: SCL Generator ────────────────────────────────────────
    public class ResponseSclGenerate : ResponseMessage
    {
        public string? SclCode { get; set; }
        public string? BlockName { get; set; }
        public string? TemplateName { get; set; }
    }

    public class ResponseSclTemplates : ResponseMessage
    {
        public IEnumerable<object>? Templates { get; set; }
    }

    // ─── V2 NEW: Diagnostics ──────────────────────────────────────────
    public class ResponseDiagnostics : ResponseMessage
    {
        public IEnumerable<DiagnosticEntry>? Entries { get; set; }
    }

    public class ResponseCycleTime : ResponseMessage
    {
        public object? CycleTimeData { get; set; }
    }

    // ─── V2 NEW: I/O Address Plan ─────────────────────────────────────
    public class ResponseIoAddressPlan : ResponseMessage
    {
        public IEnumerable<IoAddressEntry>? AddressPlan { get; set; }
        public int TotalInputBytes { get; set; }
        public int TotalOutputBytes { get; set; }
    }

    // ─── V2 NEW: Code Analysis ────────────────────────────────────────
    public class ResponseCodeAnalysis : ResponseMessage
    {
        public int Score { get; set; }
        public IEnumerable<object>? Issues { get; set; }
        public IEnumerable<object>? Suggestions { get; set; }
    }
}
