using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TiaMcpV2.Services
{
    /// <summary>
    /// Comprehensive Siemens module catalog for all module families:
    /// - I/O modules (DI, DQ, DI/DQ, AI, AQ — incl. RTD, TC, IRC, IO-Link)
    /// - Counter & position modules (TM Count, TM PosInput, TM Timer)
    /// - Weighing modules (SIWAREX)
    /// - Communication modules (CP, CM)
    /// - Distributed I/O head modules (IM 155-x)
    /// - Network devices (SCALANCE switches/routers/wireless)
    ///
    /// Each module has detailed specs (channels, resolution, sample rate, sensor types, etc.)
    /// and known parameter names that can be configured via SetAttribute.
    /// </summary>
    public class ModuleCatalogService
    {
        private readonly ILogger<ModuleCatalogService>? _logger;
        private static readonly List<ModuleSpec> _modules;

        static ModuleCatalogService()
        {
            _modules = BuildModuleDatabase();
        }

        public ModuleCatalogService(ILogger<ModuleCatalogService>? logger = null)
        {
            _logger = logger;
        }

        public List<ModuleSpec> GetAllModules() => _modules;

        public List<ModuleSpec> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return _modules;
            var q = query.ToUpperInvariant();
            return _modules.Where(m =>
                (m.Description?.ToUpperInvariant().Contains(q) == true) ||
                (m.OrderNumber?.ToUpperInvariant().Contains(q) == true) ||
                (m.Category?.ToUpperInvariant().Contains(q) == true) ||
                (m.SubCategory?.ToUpperInvariant().Contains(q) == true) ||
                (m.Tags?.Any(t => t.ToUpperInvariant().Contains(q)) == true)
            ).ToList();
        }

        public List<ModuleSpec> GetByCategory(string category)
        {
            return _modules.Where(m => m.Category?.Equals(category, StringComparison.OrdinalIgnoreCase) == true).ToList();
        }

        public ModuleSpec? GetByOrderNumber(string orderNumber)
        {
            return _modules.FirstOrDefault(m => m.OrderNumber?.Equals(orderNumber, StringComparison.OrdinalIgnoreCase) == true);
        }

        /// <summary>
        /// Recommend an I/O module based on requirements (channel count, signal type, accuracy class).
        /// </summary>
        public List<ModuleSpec> RecommendIoModule(IoModuleRequirements req)
        {
            var candidates = _modules.Where(m =>
                m.Category == req.Category &&
                m.ChannelCount >= req.MinChannels &&
                (req.PreferredSubCategory == null || m.SubCategory == req.PreferredSubCategory)
            ).ToList();

            // For analog: filter by signal type
            if (!string.IsNullOrEmpty(req.SignalType) && (req.Category == "AI" || req.Category == "AQ"))
            {
                candidates = candidates.Where(m =>
                    m.SupportedSignalTypes?.Contains(req.SignalType, StringComparer.OrdinalIgnoreCase) == true
                ).ToList();
            }

            // Sort by channel count match (prefer exact match) then accuracy class
            return candidates
                .OrderBy(m => Math.Abs(m.ChannelCount - req.MinChannels))
                .ThenByDescending(m => m.SubCategory == "HF" ? 3 : m.SubCategory == "HS" ? 2 : 1)
                .ToList();
        }

        #region Module Database

        private static List<ModuleSpec> BuildModuleDatabase()
        {
            var list = new List<ModuleSpec>();

            // ────────────── S7-1500 DIGITAL INPUT MODULES ──────────────
            list.AddRange(new[]
            {
                new ModuleSpec { OrderNumber = "6ES7 521-1BH00-0AB0", Description = "DI 16x24VDC HF", Category = "DI", SubCategory = "HF", ChannelCount = 16, Family = "S7-1500", AddressLengthBytes = 2,
                    Tags = new[]{"DI","16-channel","24VDC","high-feature"},
                    Parameters = new[]{"InputDelay","HardwareInterrupt","DiagnosticInterrupt","NoSensorSupplyVoltage","WireBreak","InputRange"} },
                new ModuleSpec { OrderNumber = "6ES7 521-1BH50-0AA0", Description = "DI 16x24VDC HF (V2)", Category = "DI", SubCategory = "HF", ChannelCount = 16, Family = "S7-1500", AddressLengthBytes = 2,
                    Tags = new[]{"DI","16-channel","24VDC"},
                    Parameters = new[]{"InputDelay","HardwareInterrupt","DiagnosticInterrupt"} },
                new ModuleSpec { OrderNumber = "6ES7 521-1BL00-0AB0", Description = "DI 32x24VDC HF", Category = "DI", SubCategory = "HF", ChannelCount = 32, Family = "S7-1500", AddressLengthBytes = 4,
                    Tags = new[]{"DI","32-channel","24VDC"},
                    Parameters = new[]{"InputDelay","DiagnosticInterrupt","ChannelInputDelay"} },
                new ModuleSpec { OrderNumber = "6ES7 521-1BH10-0AA0", Description = "DI 16x24VDC SRC BA", Category = "DI", SubCategory = "BA", ChannelCount = 16, Family = "S7-1500", AddressLengthBytes = 2,
                    Tags = new[]{"DI","16-channel","24VDC","sourcing","basic"} },
                new ModuleSpec { OrderNumber = "6ES7 521-1FH00-0AA0", Description = "DI 16x230VAC", Category = "DI", SubCategory = "BA", ChannelCount = 16, Family = "S7-1500", AddressLengthBytes = 2,
                    Tags = new[]{"DI","16-channel","230VAC"} },
                new ModuleSpec { OrderNumber = "6ES7 521-7EH00-0AB0", Description = "DI 16x24..125VUC HF", Category = "DI", SubCategory = "HF", ChannelCount = 16, Family = "S7-1500", AddressLengthBytes = 2,
                    Tags = new[]{"DI","16-channel","wide-range"} },
            });

            // ────────────── S7-1500 DIGITAL OUTPUT MODULES ──────────────
            list.AddRange(new[]
            {
                new ModuleSpec { OrderNumber = "6ES7 522-1BH00-0AB0", Description = "DQ 16x24VDC/0.5A HF", Category = "DQ", SubCategory = "HF", ChannelCount = 16, Family = "S7-1500", AddressLengthBytes = 2,
                    Tags = new[]{"DQ","16-channel","24VDC","0.5A"},
                    Parameters = new[]{"DiagnosticInterrupt","ShortCircuit","WireBreak","ReactionToCpuStop","SubstituteValue"} },
                new ModuleSpec { OrderNumber = "6ES7 522-1BL01-0AB0", Description = "DQ 32x24VDC/0.5A HF", Category = "DQ", SubCategory = "HF", ChannelCount = 32, Family = "S7-1500", AddressLengthBytes = 4,
                    Tags = new[]{"DQ","32-channel","24VDC","0.5A"} },
                new ModuleSpec { OrderNumber = "6ES7 522-5HH00-0AB0", Description = "DQ 16x230VAC/2A RLY", Category = "DQ", SubCategory = "RLY", ChannelCount = 16, Family = "S7-1500", AddressLengthBytes = 2,
                    Tags = new[]{"DQ","16-channel","230VAC","relay","2A"} },
                new ModuleSpec { OrderNumber = "6ES7 522-1BH10-0AA0", Description = "DQ 16x24VDC/0.5A BA", Category = "DQ", SubCategory = "BA", ChannelCount = 16, Family = "S7-1500", AddressLengthBytes = 2,
                    Tags = new[]{"DQ","16-channel","basic"} },
                new ModuleSpec { OrderNumber = "6ES7 522-1BF00-0AB0", Description = "DQ 8x24VDC/2A HF", Category = "DQ", SubCategory = "HF", ChannelCount = 8, Family = "S7-1500", AddressLengthBytes = 1,
                    Tags = new[]{"DQ","8-channel","24VDC","2A","high-current"} },
                new ModuleSpec { OrderNumber = "6ES7 523-1BL00-0AA0", Description = "DI 16x24VDC / DQ 16x24VDC/0.5A", Category = "DI/DQ", SubCategory = "BA", ChannelCount = 32, Family = "S7-1500", AddressLengthBytes = 4,
                    Tags = new[]{"DI/DQ","mixed","basic"} },
            });

            // ────────────── S7-1500 ANALOG INPUT MODULES ──────────────
            list.AddRange(new[]
            {
                new ModuleSpec { OrderNumber = "6ES7 531-7KF00-0AB0", Description = "AI 8xU/I/RTD/TC ST", Category = "AI", SubCategory = "ST", ChannelCount = 8, Family = "S7-1500", AddressLengthBytes = 16,
                    SupportedSignalTypes = new[]{"Voltage","Current","RTD","Thermocouple"},
                    Tags = new[]{"AI","8-channel","universal"},
                    Parameters = new[]{"MeasurementType","MeasurementRange","Filter","Smoothing","WireBreakDetection","UpperLimit","LowerLimit","DiagnosticInterrupt","HardwareInterrupt","TemperatureCoefficient"} },
                new ModuleSpec { OrderNumber = "6ES7 531-7NF10-0AB0", Description = "AI 4xU/I/RTD/TC HF", Category = "AI", SubCategory = "HF", ChannelCount = 4, Family = "S7-1500", AddressLengthBytes = 8,
                    SupportedSignalTypes = new[]{"Voltage","Current","RTD","Thermocouple","Resistance"},
                    Tags = new[]{"AI","4-channel","high-feature","16bit"},
                    Parameters = new[]{"MeasurementType","MeasurementRange","Filter","Smoothing","WireBreakDetection","UpperLimit","LowerLimit","DiagnosticInterrupt","HardwareInterrupt"} },
                new ModuleSpec { OrderNumber = "6ES7 531-7PF00-0AB0", Description = "AI 8xU/I HS", Category = "AI", SubCategory = "HS", ChannelCount = 8, Family = "S7-1500", AddressLengthBytes = 16,
                    SupportedSignalTypes = new[]{"Voltage","Current"},
                    Tags = new[]{"AI","8-channel","high-speed","isochronous"},
                    Parameters = new[]{"MeasurementType","MeasurementRange","Filter","Smoothing","SamplingTime"} },
                new ModuleSpec { OrderNumber = "6ES7 531-7QF00-0AB0", Description = "AI 8xU ST (voltage only)", Category = "AI", SubCategory = "ST", ChannelCount = 8, Family = "S7-1500", AddressLengthBytes = 16,
                    SupportedSignalTypes = new[]{"Voltage"} },
                new ModuleSpec { OrderNumber = "6ES7 531-7QD00-0AB0", Description = "AI 4xU/I ST", Category = "AI", SubCategory = "ST", ChannelCount = 4, Family = "S7-1500", AddressLengthBytes = 8,
                    SupportedSignalTypes = new[]{"Voltage","Current"} },
            });

            // ────────────── S7-1500 ANALOG OUTPUT MODULES ──────────────
            list.AddRange(new[]
            {
                new ModuleSpec { OrderNumber = "6ES7 532-5HF00-0AB0", Description = "AQ 4xU/I HF", Category = "AQ", SubCategory = "HF", ChannelCount = 4, Family = "S7-1500", AddressLengthBytes = 8,
                    SupportedSignalTypes = new[]{"Voltage","Current"},
                    Tags = new[]{"AQ","4-channel","high-feature","16bit"},
                    Parameters = new[]{"OutputType","OutputRange","DiagnosticInterrupt","ReactionToCpuStop","SubstituteValue","Smoothing"} },
                new ModuleSpec { OrderNumber = "6ES7 532-5NB00-0AB0", Description = "AQ 2xU/I ST", Category = "AQ", SubCategory = "ST", ChannelCount = 2, Family = "S7-1500", AddressLengthBytes = 4,
                    SupportedSignalTypes = new[]{"Voltage","Current"} },
                new ModuleSpec { OrderNumber = "6ES7 532-5HD00-0AB0", Description = "AQ 4xU HS", Category = "AQ", SubCategory = "HS", ChannelCount = 4, Family = "S7-1500", AddressLengthBytes = 8,
                    SupportedSignalTypes = new[]{"Voltage"},
                    Tags = new[]{"AQ","high-speed","isochronous"} },
            });

            // ────────────── S7-1500 SAFETY (F-) MODULES ──────────────
            list.AddRange(new[]
            {
                new ModuleSpec { OrderNumber = "6ES7 526-1BH00-0AB0", Description = "F-DI 16x24VDC PROFIsafe", Category = "F-DI", ChannelCount = 16, Family = "S7-1500 F", AddressLengthBytes = 6,
                    Tags = new[]{"F-DI","safety","SIL3","PROFIsafe"},
                    Parameters = new[]{"FShortCircuitTest","DiscrepancyTime","ChannelEvaluation","FParameterSignature"} },
                new ModuleSpec { OrderNumber = "6ES7 526-2BF00-0AB0", Description = "F-DQ 8x24VDC/2A PPM PROFIsafe", Category = "F-DQ", ChannelCount = 8, Family = "S7-1500 F", AddressLengthBytes = 6,
                    Tags = new[]{"F-DQ","safety","SIL3","PROFIsafe","2A"},
                    Parameters = new[]{"FShortCircuitTest","FParameterSignature","ReactionTime"} },
                new ModuleSpec { OrderNumber = "6ES7 136-6PA00-0BC0", Description = "F-AI 4xU/I HART (ET 200SP)", Category = "F-AI", ChannelCount = 4, Family = "ET 200SP F", AddressLengthBytes = 16,
                    SupportedSignalTypes = new[]{"Voltage","Current","HART"} },
            });

            // ────────────── COUNTER & POSITION (TM) MODULES ──────────────
            list.AddRange(new[]
            {
                new ModuleSpec { OrderNumber = "6ES7 550-1AA00-0AB0", Description = "TM Count 1x24V (1-channel counter)", Category = "Counter", SubCategory = "TM Count", ChannelCount = 1, Family = "S7-1500", AddressLengthBytes = 16,
                    Tags = new[]{"counter","high-speed","incremental","encoder"},
                    Parameters = new[]{"OperatingMode","CountingDirection","SignalType","SignalEvaluation","FilterFrequency","HysteresisCount","ReferencePoint","HardwareInterrupt"} },
                new ModuleSpec { OrderNumber = "6ES7 551-1AB00-0AB0", Description = "TM PosInput 1 (1-channel position input)", Category = "Counter", SubCategory = "TM PosInput", ChannelCount = 1, Family = "S7-1500", AddressLengthBytes = 16,
                    Tags = new[]{"position","SSI","incremental","encoder"},
                    Parameters = new[]{"EncoderType","SignalType","BaudRate","DataLength","CodeType","Resolution"} },
                new ModuleSpec { OrderNumber = "6ES7 552-1AA00-0AB0", Description = "TM PosInput 2 (2-channel position input)", Category = "Counter", SubCategory = "TM PosInput", ChannelCount = 2, Family = "S7-1500", AddressLengthBytes = 24 },
                new ModuleSpec { OrderNumber = "6ES7 553-1AA00-0AB0", Description = "TM Timer DIDQ 16x24V", Category = "Timer", SubCategory = "TM Timer", ChannelCount = 16, Family = "S7-1500", AddressLengthBytes = 16,
                    Tags = new[]{"timer","cam","precision-timing"},
                    Parameters = new[]{"OperatingMode","TimerBase","CamType"} },
            });

            // ────────────── IO-LINK MASTER ──────────────
            list.AddRange(new[]
            {
                new ModuleSpec { OrderNumber = "6ES7 137-6BD00-0BA0", Description = "CM 4xIO-Link (ET 200SP IO-Link Master)", Category = "Communication", SubCategory = "IO-Link", ChannelCount = 4, Family = "ET 200SP", AddressLengthBytes = 32,
                    Tags = new[]{"IO-Link","master","V1.1","sensor-actuator"},
                    Parameters = new[]{"PortMode","BaudRate","CycleTime","ProcessDataInputLength","ProcessDataOutputLength","DeviceID","VendorID"} },
                new ModuleSpec { OrderNumber = "6GK5 142-1BD00-0AA0", Description = "CM 8xIO-Link (S7-1500 IO-Link Master)", Category = "Communication", SubCategory = "IO-Link", ChannelCount = 8, Family = "S7-1500" },
            });

            // ────────────── SIWAREX (Weighing) ──────────────
            list.AddRange(new[]
            {
                new ModuleSpec { OrderNumber = "7MH4 980-2AA01", Description = "SIWAREX WP321 (1-channel scale)", Category = "Weighing", SubCategory = "SIWAREX", ChannelCount = 1, Family = "S7-1500",
                    Tags = new[]{"weighing","scale","load-cell","level-monitoring"},
                    Parameters = new[]{"NominalLoad","NominalCharacteristic","CalibrationWeight","FilterType","FilterDepth","StandstillRange","StandstillTime"} },
                new ModuleSpec { OrderNumber = "7MH4 980-1AA01", Description = "SIWAREX WP231 (1-channel batching/filling)", Category = "Weighing", SubCategory = "SIWAREX", ChannelCount = 1, Family = "S7-1500" },
                new ModuleSpec { OrderNumber = "7MH4 134-6LA00", Description = "SIWAREX WP251 (1-channel checkweigher)", Category = "Weighing", SubCategory = "SIWAREX", ChannelCount = 1, Family = "S7-1500" },
            });

            // ────────────── COMMUNICATION PROCESSORS (CP) ──────────────
            list.AddRange(new[]
            {
                new ModuleSpec { OrderNumber = "6GK7 543-1AX00-0XE0", Description = "CP 1543-1 (Industrial Ethernet/PROFINET, OPC UA, SECURITY)", Category = "Communication", SubCategory = "CP", Family = "S7-1500",
                    Tags = new[]{"PROFINET","Ethernet","OPC UA","TCP/IP","security","firewall","VPN"},
                    Parameters = new[]{"IpAddress","SubnetMask","RouterAddress","FirewallEnable","VpnConfig"} },
                new ModuleSpec { OrderNumber = "6GK7 543-6WX00-0XE0", Description = "CP 1543SP-1 (Industrial Ethernet for ET 200SP)", Category = "Communication", SubCategory = "CP", Family = "ET 200SP",
                    Tags = new[]{"PROFINET","Ethernet","ET 200SP"} },
                new ModuleSpec { OrderNumber = "6GK7 343-1EX30-0XE0", Description = "CP 343-1 Lean (Industrial Ethernet for S7-300)", Category = "Communication", SubCategory = "CP", Family = "S7-300" },
                new ModuleSpec { OrderNumber = "6GK7 443-1EX30-0XE0", Description = "CP 443-1 Advanced (Industrial Ethernet for S7-400)", Category = "Communication", SubCategory = "CP", Family = "S7-400" },
                new ModuleSpec { OrderNumber = "6GK7 542-5DX00-0XE0", Description = "CP 1542SP-1 IRC (Industrial Remote Communication)", Category = "Communication", SubCategory = "CP", Family = "ET 200SP",
                    Tags = new[]{"remote","TeleControl","DNP3","IEC 60870-5"} },
            });

            // ────────────── COMMUNICATION MODULES (CM) ──────────────
            list.AddRange(new[]
            {
                new ModuleSpec { OrderNumber = "6GK7 542-1AX00-0XE0", Description = "CM 1542-1 (PROFINET/Industrial Ethernet for S7-1500)", Category = "Communication", SubCategory = "CM", Family = "S7-1500" },
                new ModuleSpec { OrderNumber = "6GK7 542-5DX00-0XE0", Description = "CM 1542-5 (PROFIBUS DP master/slave for S7-1500)", Category = "Communication", SubCategory = "CM", Family = "S7-1500",
                    Tags = new[]{"PROFIBUS","DP","master","slave"},
                    Parameters = new[]{"BaudRate","StationAddress","HighestStationAddress","RetryLimit"} },
                new ModuleSpec { OrderNumber = "6ES7 540-1AD00-0AA0", Description = "CM PtP RS232 BA (Point-to-Point)", Category = "Communication", SubCategory = "CM PtP", Family = "S7-1500",
                    Tags = new[]{"RS232","point-to-point","Modbus RTU","Freeport"},
                    Parameters = new[]{"BaudRate","DataBits","Parity","StopBits","Protocol","FlowControl"} },
                new ModuleSpec { OrderNumber = "6ES7 540-1AB00-0AA0", Description = "CM PtP RS422/485 BA", Category = "Communication", SubCategory = "CM PtP", Family = "S7-1500",
                    Tags = new[]{"RS422","RS485","Modbus RTU","USS"} },
                new ModuleSpec { OrderNumber = "6ES7 541-1AD00-0AB0", Description = "CM PtP RS232 HF", Category = "Communication", SubCategory = "CM PtP", Family = "S7-1500" },
                new ModuleSpec { OrderNumber = "6ES7 541-1AB00-0AB0", Description = "CM PtP RS422/485 HF", Category = "Communication", SubCategory = "CM PtP", Family = "S7-1500" },
                new ModuleSpec { OrderNumber = "6GK7 142-1BX00-0XE0", Description = "CM 1243-5 (PROFIBUS DP master for S7-1200)", Category = "Communication", SubCategory = "CM", Family = "S7-1200" },
                new ModuleSpec { OrderNumber = "6GK7 142-1AX01-0XE0", Description = "CM AS-i Master ST (AS-i master for S7-1200)", Category = "Communication", SubCategory = "CM AS-i", Family = "S7-1200",
                    Tags = new[]{"AS-i","AS-Interface","master"} },
            });

            // ────────────── ET 200SP HEAD MODULES (IM) ──────────────
            list.AddRange(new[]
            {
                new ModuleSpec { OrderNumber = "6ES7 155-6AU01-0BN0", Description = "IM 155-6 PN ST (PROFINET interface, Standard)", Category = "InterfaceModule", SubCategory = "IM", Family = "ET 200SP",
                    Tags = new[]{"PROFINET","head-module","ET200SP","standard"},
                    Parameters = new[]{"DeviceName","IpAddress","SubnetMask","UpdateTime","WatchdogTime","SharedDevice","MrpRole"} },
                new ModuleSpec { OrderNumber = "6ES7 155-6AU01-0CN0", Description = "IM 155-6 PN HF (PROFINET interface, High-Feature)", Category = "InterfaceModule", SubCategory = "IM", Family = "ET 200SP",
                    Tags = new[]{"PROFINET","head-module","ET200SP","high-feature","MRP","Shared Device"} },
                new ModuleSpec { OrderNumber = "6ES7 155-6AU01-0DN0", Description = "IM 155-6 PN/2 HF (Dual PROFINET, R1 redundant)", Category = "InterfaceModule", SubCategory = "IM", Family = "ET 200SP",
                    Tags = new[]{"PROFINET","head-module","R1-redundant","S2-redundant","MRP"} },
                new ModuleSpec { OrderNumber = "6ES7 155-6BU01-0CN0", Description = "IM 155-6 DP HF (PROFIBUS DP interface)", Category = "InterfaceModule", SubCategory = "IM", Family = "ET 200SP",
                    Tags = new[]{"PROFIBUS","DP","head-module"} },
                // ET 200MP
                new ModuleSpec { OrderNumber = "6ES7 155-5AA00-0AC0", Description = "IM 155-5 PN ST (ET 200MP head, S7-1500 modules)", Category = "InterfaceModule", SubCategory = "IM", Family = "ET 200MP",
                    Tags = new[]{"PROFINET","ET200MP","S7-1500-modules"} },
                new ModuleSpec { OrderNumber = "6ES7 155-5AA00-0AD0", Description = "IM 155-5 PN HF (ET 200MP high-feature)", Category = "InterfaceModule", SubCategory = "IM", Family = "ET 200MP" },
                // ET 200AL
                new ModuleSpec { OrderNumber = "6ES7 157-1AB00-0AB0", Description = "IM 157-1 PN (ET 200AL IP65 head)", Category = "InterfaceModule", SubCategory = "IM", Family = "ET 200AL",
                    Tags = new[]{"PROFINET","ET200AL","IP65","field"} },
                // ET 200pro
                new ModuleSpec { OrderNumber = "6ES7 154-1AA01-0AB0", Description = "IM 154-1 PN (ET 200pro head)", Category = "InterfaceModule", SubCategory = "IM", Family = "ET 200pro",
                    Tags = new[]{"PROFINET","ET200pro","IP65","heavy-industry"} },
                // ET 200eco
                new ModuleSpec { OrderNumber = "6ES7 144-3FX00-0XB0", Description = "ET 200eco PN (compact field box)", Category = "InterfaceModule", SubCategory = "ET200eco", Family = "ET 200eco PN",
                    Tags = new[]{"PROFINET","ET200eco","M8","M12","compact"} },
            });

            // ────────────── ET 200SP I/O MODULES ──────────────
            list.AddRange(new[]
            {
                new ModuleSpec { OrderNumber = "6ES7 131-6BF00-0BA0", Description = "DI 8x24VDC ST (ET 200SP)", Category = "DI", SubCategory = "ST", ChannelCount = 8, Family = "ET 200SP", AddressLengthBytes = 1 },
                new ModuleSpec { OrderNumber = "6ES7 131-6BH00-0BA0", Description = "DI 16x24VDC ST (ET 200SP)", Category = "DI", SubCategory = "ST", ChannelCount = 16, Family = "ET 200SP", AddressLengthBytes = 2 },
                new ModuleSpec { OrderNumber = "6ES7 131-6BF61-0AA0", Description = "DI 8x24VDC HF (ET 200SP)", Category = "DI", SubCategory = "HF", ChannelCount = 8, Family = "ET 200SP", AddressLengthBytes = 1,
                    Parameters = new[]{"InputDelay","DiagnosticInterrupt","HardwareInterrupt","ChannelDiagnostic","NoSensorSupplyVoltage","WireBreak"} },
                new ModuleSpec { OrderNumber = "6ES7 132-6BF00-0BA0", Description = "DQ 8x24VDC/0.5A ST (ET 200SP)", Category = "DQ", SubCategory = "ST", ChannelCount = 8, Family = "ET 200SP", AddressLengthBytes = 1 },
                new ModuleSpec { OrderNumber = "6ES7 132-6BH00-0BA0", Description = "DQ 16x24VDC/0.5A ST (ET 200SP)", Category = "DQ", SubCategory = "ST", ChannelCount = 16, Family = "ET 200SP", AddressLengthBytes = 2 },
                new ModuleSpec { OrderNumber = "6ES7 134-6JD00-0CA1", Description = "AI 4xU/I 2-wire ST (ET 200SP)", Category = "AI", SubCategory = "ST", ChannelCount = 4, Family = "ET 200SP", AddressLengthBytes = 8,
                    SupportedSignalTypes = new[]{"Voltage","Current"},
                    Parameters = new[]{"MeasurementType","MeasurementRange","Filter","Smoothing","DiagnosticInterrupt"} },
                new ModuleSpec { OrderNumber = "6ES7 134-6HD00-0BA1", Description = "AI 4xRTD/TC HF (ET 200SP)", Category = "AI", SubCategory = "HF", ChannelCount = 4, Family = "ET 200SP", AddressLengthBytes = 8,
                    SupportedSignalTypes = new[]{"RTD","Thermocouple","Resistance"},
                    Parameters = new[]{"MeasurementType","RTDType","TCType","ConnectionType","Filter","DiagnosticInterrupt"} },
                new ModuleSpec { OrderNumber = "6ES7 135-6HD00-0BA1", Description = "AQ 4xU/I ST (ET 200SP)", Category = "AQ", SubCategory = "ST", ChannelCount = 4, Family = "ET 200SP", AddressLengthBytes = 8,
                    SupportedSignalTypes = new[]{"Voltage","Current"} },
                new ModuleSpec { OrderNumber = "6ES7 138-6CG00-0BA0", Description = "TM Count 1x24V (ET 200SP)", Category = "Counter", SubCategory = "TM Count", ChannelCount = 1, Family = "ET 200SP",
                    Parameters = new[]{"OperatingMode","CountingDirection","SignalType","SignalEvaluation","FilterFrequency"} },
            });

            // ────────────── SCALANCE NETWORK DEVICES ──────────────
            list.AddRange(new[]
            {
                new ModuleSpec { OrderNumber = "6GK5 005-0BA10-1AA3", Description = "SCALANCE XB005 (5-port unmanaged switch)", Category = "Network", SubCategory = "SCALANCE", Family = "Network",
                    Tags = new[]{"switch","unmanaged","5-port","DIN-rail"} },
                new ModuleSpec { OrderNumber = "6GK5 008-0BA10-1AB2", Description = "SCALANCE XB008 (8-port unmanaged switch)", Category = "Network", SubCategory = "SCALANCE", Family = "Network",
                    Tags = new[]{"switch","unmanaged","8-port"} },
                new ModuleSpec { OrderNumber = "6GK5 208-0BA00-2AC2", Description = "SCALANCE XB208 (8-port managed switch L2)", Category = "Network", SubCategory = "SCALANCE", Family = "Network",
                    Tags = new[]{"switch","managed","8-port","VLAN","MRP"},
                    Parameters = new[]{"DeviceName","IpAddress","Vlan","MrpRole","PortMirroring","SNMP"} },
                new ModuleSpec { OrderNumber = "6GK5 216-4BS00-2AC2", Description = "SCALANCE XB216 (16-port managed switch)", Category = "Network", SubCategory = "SCALANCE", Family = "Network" },
                new ModuleSpec { OrderNumber = "6GK5 408-4GP00-2AM2", Description = "SCALANCE X408-2 (gigabit, 4-port managed L2)", Category = "Network", SubCategory = "SCALANCE", Family = "Network",
                    Tags = new[]{"switch","gigabit","4-port","L2"} },
                new ModuleSpec { OrderNumber = "6GK5 308-2FP10-2AA3", Description = "SCALANCE XC208 (8-port managed L2/L3 PROFINET)", Category = "Network", SubCategory = "SCALANCE", Family = "Network",
                    Tags = new[]{"switch","XC","PROFINET","L2"} },
                new ModuleSpec { OrderNumber = "6GK5 778-1GY00-0AA0", Description = "SCALANCE W778-1 (Wireless AP, IEEE 802.11ac)", Category = "Network", SubCategory = "SCALANCE-W", Family = "Network",
                    Tags = new[]{"wireless","WLAN","802.11ac","access-point"} },
                new ModuleSpec { OrderNumber = "6GK5 763-1AL00-7DA0", Description = "SCALANCE W763-1 (Industrial WLAN client)", Category = "Network", SubCategory = "SCALANCE-W", Family = "Network",
                    Tags = new[]{"wireless","WLAN","client"} },
                new ModuleSpec { OrderNumber = "6GK5 874-3AA00-2AA2", Description = "SCALANCE M874-3 (3G/4G mobile router)", Category = "Network", SubCategory = "SCALANCE-M", Family = "Network",
                    Tags = new[]{"mobile","4G","router","cellular"} },
                new ModuleSpec { OrderNumber = "6GK5 626-2GS00-2AC2", Description = "SCALANCE S626-2C (industrial firewall/VPN)", Category = "Network", SubCategory = "SCALANCE-S", Family = "Network",
                    Tags = new[]{"firewall","VPN","security","S-series"} },
                new ModuleSpec { OrderNumber = "6GK1 411-3AA20", Description = "RUGGEDCOM RX1500 (modular L2/L3 router/switch)", Category = "Network", SubCategory = "RUGGEDCOM", Family = "Network",
                    Tags = new[]{"ruggedcom","substation","extreme-environment"} },
            });

            return list;
        }

        #endregion

        #region Data Models

        public class ModuleSpec
        {
            public string OrderNumber { get; set; } = "";
            public string Description { get; set; } = "";
            public string Category { get; set; } = "";        // DI, DQ, AI, AQ, F-DI, F-DQ, Counter, Communication, etc.
            public string? SubCategory { get; set; }           // ST/HF/HS/BA, IM, CP, CM, etc.
            public string Family { get; set; } = "";          // S7-1500, S7-1200, ET 200SP, etc.
            public int ChannelCount { get; set; }
            public int AddressLengthBytes { get; set; }
            public string[]? SupportedSignalTypes { get; set; } // Voltage, Current, RTD, TC, etc.
            public string[]? Tags { get; set; }
            public string[]? Parameters { get; set; }           // Configurable parameter names

            public string TypeIdentifier => string.IsNullOrEmpty(OrderNumber) ? "" : $"OrderNumber:{OrderNumber}";
        }

        public class IoModuleRequirements
        {
            public string Category { get; set; } = "";
            public string? PreferredSubCategory { get; set; }
            public int MinChannels { get; set; }
            public string? SignalType { get; set; }
            public string? Family { get; set; }
        }

        #endregion
    }
}
