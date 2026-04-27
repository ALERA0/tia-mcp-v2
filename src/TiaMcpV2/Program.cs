using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using System;
using System.Threading.Tasks;
using TiaMcpV2.Core;
using TiaMcpV2.Services;

namespace TiaMcpV2
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var options = CliOptions.ParseArgs(args);
            var tiaMajorVersion = options.TiaMajorVersion ?? DetectTiaVersion();
            Console.Error.WriteLine($"Supported TIA Portal versions: V19, V20, V21");

            Console.Error.WriteLine($"TIA MCP V2 starting for TIA Portal V{tiaMajorVersion}...");

            // Initialize TIA Portal Openness
            AppDomain.CurrentDomain.AssemblyResolve += OpennessResolver.Resolve;
            OpennessResolver.TiaMajorVersion = tiaMajorVersion;
            OpennessInitializer.Initialize(tiaMajorVersion);

            // Check user group membership
            if (!await OpennessInitializer.IsUserInOpennessGroup())
            {
                Console.Error.WriteLine("WARNING: User may not be in the 'Siemens TIA Openness' group.");
                Console.Error.WriteLine("Add the user to this Windows group and restart if connection fails.");
            }

            var builder = Host.CreateApplicationBuilder(args);

            // Configure logging
            var logLevel = options.Logging.HasValue
                ? (LogLevel)options.Logging.Value
                : LogLevel.Warning;
            builder.Logging.SetMinimumLevel(logLevel);

            // Register core engine
            builder.Services.AddSingleton<PortalEngine>();

            // Register base services (from V1, improved)
            builder.Services.AddSingleton<HardwareService>();
            builder.Services.AddSingleton<NetworkService>();
            builder.Services.AddSingleton<BlockService>();
            builder.Services.AddSingleton<TagService>();
            builder.Services.AddSingleton<TypeService>();
            builder.Services.AddSingleton<XmlGeneratorService>();
            builder.Services.AddSingleton<SafetyService>();
            builder.Services.AddSingleton<DriveService>();
            builder.Services.AddSingleton<HmiService>();
            builder.Services.AddSingleton<LibraryService>();
            builder.Services.AddSingleton<DownloadService>();
            builder.Services.AddSingleton<CatalogService>();

            // Register V2 NEW services
            builder.Services.AddSingleton<DiagnosticsService>();
            builder.Services.AddSingleton<SclGeneratorService>();
            builder.Services.AddSingleton<IoAddressService>();
            builder.Services.AddSingleton<CodeAnalysisService>();
            builder.Services.AddSingleton<TechnologyObjectService>();
            builder.Services.AddSingleton<BlockAutonomyService>();
            builder.Services.AddSingleton<LadFbdGeneratorService>();
            builder.Services.AddSingleton<ServoDriveService>();
            builder.Services.AddSingleton<CpuSelectorService>();
            builder.Services.AddSingleton<ModuleCatalogService>();
            builder.Services.AddSingleton<ModuleConfigService>();
            builder.Services.AddSingleton<DistributedIoService>();
            builder.Services.AddSingleton<HmiSetupService>();
            builder.Services.AddSingleton<SafetyHardwareService>();
            builder.Services.AddSingleton<ProgrammingGuideService>();
            builder.Services.AddSingleton<DataTypeService>();
            builder.Services.AddSingleton<InstructionLibraryService>();
            builder.Services.AddSingleton<TagManagementService>();
            builder.Services.AddSingleton<ObManagerService>();
            builder.Services.AddSingleton<MotionControlService>();
            builder.Services.AddSingleton<SafetyProgrammingService>();
            builder.Services.AddSingleton<HmiProgrammingService>();
            builder.Services.AddSingleton<CommunicationProtocolService>();

            // Configure MCP Server with stdio transport
            builder.Services
                .AddMcpServer()
                .WithStdioServerTransport()
                .WithToolsFromAssembly();

            var app = builder.Build();

            // Initialize the static service accessor for MCP tool classes
            ServiceAccessor.SetServiceProvider(app.Services);
            ServiceAccessor.Logger = app.Services.GetService<ILoggerFactory>()?.CreateLogger("McpTools");

            Console.Error.WriteLine($"TIA MCP V2 ready. {CountTools()} tools available.");
            Console.Error.WriteLine($"Supported TIA Portal versions: V20, V21 (current: V{tiaMajorVersion})");

            await app.RunAsync();
        }

        private static int DetectTiaVersion()
        {
            // Try V21, V20, V19 in order
            if (OpennessResolver.IsTiaInstalled(21)) return 21;
            if (OpennessResolver.IsTiaInstalled(20)) return 20;
            if (OpennessResolver.IsTiaInstalled(19)) return 19;

            Console.Error.WriteLine("WARNING: No TIA Portal V19/V20/V21 installation detected.");
            return 20;
        }

        private static int CountTools()
        {
            // Approximate count based on registered tool classes
            return 95; // ~95 tools across 19 tool classes
        }
    }
}
