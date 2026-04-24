using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using TiaMcpV2.Services;

namespace TiaMcpV2.Core
{
    /// <summary>
    /// Static service locator for MCP tool classes to access registered services.
    /// MCP tools are static methods, so they need a static way to reach the DI container.
    /// </summary>
    public static class ServiceAccessor
    {
        private static IServiceProvider? _services;
        public static ILogger? Logger { get; set; }

        public static void SetServiceProvider(IServiceProvider services)
        {
            _services = services;
        }

        private static T Get<T>() where T : class
        {
            return _services?.GetRequiredService<T>()
                ?? throw new InvalidOperationException($"Service {typeof(T).Name} not initialized. Ensure DI is configured.");
        }

        // Core
        public static PortalEngine Portal => Get<PortalEngine>();

        // Services
        public static HardwareService Hardware => Get<HardwareService>();
        public static NetworkService Network => Get<NetworkService>();
        public static BlockService Blocks => Get<BlockService>();
        public static TagService Tags => Get<TagService>();
        public static TypeService Types => Get<TypeService>();
        public static XmlGeneratorService XmlGenerator => Get<XmlGeneratorService>();
        public static SafetyService Safety => Get<SafetyService>();
        public static DriveService Drives => Get<DriveService>();
        public static HmiService Hmi => Get<HmiService>();
        public static LibraryService Library => Get<LibraryService>();
        public static DownloadService Download => Get<DownloadService>();
        public static CatalogService Catalog => Get<CatalogService>();

        // V2 New Services
        public static DiagnosticsService Diagnostics => Get<DiagnosticsService>();
        public static SclGeneratorService SclGenerator => Get<SclGeneratorService>();
        public static IoAddressService IoAddress => Get<IoAddressService>();
        public static CodeAnalysisService CodeAnalysis => Get<CodeAnalysisService>();
        public static TechnologyObjectService TechObjects => Get<TechnologyObjectService>();
        public static BlockAutonomyService BlockAutonomy => Get<BlockAutonomyService>();
        public static LadFbdGeneratorService LadFbdGenerator => Get<LadFbdGeneratorService>();
        public static ServoDriveService ServoDrive => Get<ServoDriveService>();
        public static CpuSelectorService CpuSelector => Get<CpuSelectorService>();
    }
}
