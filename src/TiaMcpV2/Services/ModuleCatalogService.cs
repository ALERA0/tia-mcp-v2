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

            // ────────────── HMI: BASIC PANELS (KTP series) ──────────────
            list.AddRange(new[]
            {
                new ModuleSpec { OrderNumber = "6AV2 123-2DB03-0AX0", Description = "KTP400 Basic (4\" mono touch)", Category = "HMI", SubCategory = "Basic", Family = "HMI",
                    Tags = new[]{"HMI","Basic","KTP400","4inch","mono","touch"},
                    Parameters = new[]{"DeviceName","IpAddress","Resolution","ScreenSaver","Brightness"} },
                new ModuleSpec { OrderNumber = "6AV2 123-2GB03-0AX0", Description = "KTP700 Basic (7\" color touch)", Category = "HMI", SubCategory = "Basic", Family = "HMI",
                    Tags = new[]{"HMI","Basic","KTP700","7inch","color","touch"} },
                new ModuleSpec { OrderNumber = "6AV2 123-2GA03-0AX0", Description = "KTP700 Basic DP (7\" color, PROFIBUS)", Category = "HMI", SubCategory = "Basic", Family = "HMI",
                    Tags = new[]{"HMI","Basic","KTP700","PROFIBUS"} },
                new ModuleSpec { OrderNumber = "6AV2 123-2JB03-0AX0", Description = "KTP900 Basic (9\" color touch)", Category = "HMI", SubCategory = "Basic", Family = "HMI",
                    Tags = new[]{"HMI","Basic","KTP900","9inch"} },
                new ModuleSpec { OrderNumber = "6AV2 123-2MB03-0AX0", Description = "KTP1200 Basic (12\" color touch)", Category = "HMI", SubCategory = "Basic", Family = "HMI",
                    Tags = new[]{"HMI","Basic","KTP1200","12inch"} },

                // ────────────── HMI: COMFORT PANELS ──────────────
                new ModuleSpec { OrderNumber = "6AV2 124-1DC01-0AX0", Description = "TP700 Comfort (7\" widescreen touch)", Category = "HMI", SubCategory = "Comfort", Family = "HMI",
                    Tags = new[]{"HMI","Comfort","TP700","7inch","widescreen","touch"},
                    Parameters = new[]{"DeviceName","IpAddress","Resolution","SDCardPath","WebServer","DataLogging","Recipes"} },
                new ModuleSpec { OrderNumber = "6AV2 124-1GC01-0AX0", Description = "TP900 Comfort (9\" widescreen touch)", Category = "HMI", SubCategory = "Comfort", Family = "HMI" },
                new ModuleSpec { OrderNumber = "6AV2 124-0JC01-0AX0", Description = "TP1200 Comfort (12\" widescreen touch)", Category = "HMI", SubCategory = "Comfort", Family = "HMI" },
                new ModuleSpec { OrderNumber = "6AV2 124-0MC01-0AX0", Description = "TP1500 Comfort (15\" widescreen touch)", Category = "HMI", SubCategory = "Comfort", Family = "HMI" },
                new ModuleSpec { OrderNumber = "6AV2 124-0QC02-0AX0", Description = "TP1900 Comfort (19\" widescreen touch)", Category = "HMI", SubCategory = "Comfort", Family = "HMI" },
                new ModuleSpec { OrderNumber = "6AV2 124-0XC02-0AX0", Description = "TP2200 Comfort (22\" widescreen touch)", Category = "HMI", SubCategory = "Comfort", Family = "HMI" },
                new ModuleSpec { OrderNumber = "6AV2 124-1JC01-0AX0", Description = "KP700 Comfort (7\" key-touch)", Category = "HMI", SubCategory = "Comfort", Family = "HMI",
                    Tags = new[]{"HMI","Comfort","keypad","KP700"} },
                new ModuleSpec { OrderNumber = "6AV2 124-1MC01-0AX0", Description = "KP900 Comfort (9\" key-touch)", Category = "HMI", SubCategory = "Comfort", Family = "HMI" },

                // ────────────── HMI: UNIFIED COMFORT PANELS ──────────────
                new ModuleSpec { OrderNumber = "6AV2 128-3GB06-0AX0", Description = "MTP700 Unified Comfort (7\" web-based)", Category = "HMI", SubCategory = "Unified Comfort", Family = "HMI",
                    Tags = new[]{"HMI","Unified","MTP700","HTML5","WinCC Unified","web-based"},
                    Parameters = new[]{"DeviceName","IpAddress","Resolution","HttpsCertificate","WebServerPort","UserManagement"} },
                new ModuleSpec { OrderNumber = "6AV2 128-3JB06-0AX0", Description = "MTP1000 Unified Comfort (10\" web)", Category = "HMI", SubCategory = "Unified Comfort", Family = "HMI" },
                new ModuleSpec { OrderNumber = "6AV2 128-3MB06-0AX0", Description = "MTP1500 Unified Comfort (15\" web)", Category = "HMI", SubCategory = "Unified Comfort", Family = "HMI" },
                new ModuleSpec { OrderNumber = "6AV2 128-3QB06-0AX0", Description = "MTP1900 Unified Comfort (19\" web)", Category = "HMI", SubCategory = "Unified Comfort", Family = "HMI" },
                new ModuleSpec { OrderNumber = "6AV2 128-3XB06-0AX0", Description = "MTP2200 Unified Comfort (22\" web)", Category = "HMI", SubCategory = "Unified Comfort", Family = "HMI" },

                // ────────────── HMI: MOBILE PANELS ──────────────
                new ModuleSpec { OrderNumber = "6AV2 125-2GB03-0AX0", Description = "KTP700 F Mobile (7\" handheld, e-stop, enable)", Category = "HMI", SubCategory = "Mobile", Family = "HMI",
                    Tags = new[]{"HMI","Mobile","KTP700F","handheld","E-Stop","enable-button","Safety","PROFIsafe"},
                    Parameters = new[]{"DeviceName","IpAddress","FAddress","SafetyPassword"} },
                new ModuleSpec { OrderNumber = "6AV2 125-2JB03-0AX0", Description = "KTP900 F Mobile (9\" handheld, e-stop, enable)", Category = "HMI", SubCategory = "Mobile", Family = "HMI" },
                new ModuleSpec { OrderNumber = "6AV6 645-0DD01-0AX1", Description = "Mobile Panel 277F IWLAN (wireless safety)", Category = "HMI", SubCategory = "Mobile", Family = "HMI",
                    Tags = new[]{"HMI","Mobile","wireless","WLAN","Safety"} },

                // ────────────── HMI: SIMATIC IPC ──────────────
                new ModuleSpec { OrderNumber = "6AG4 022-xxxxx", Description = "SIMATIC IPC127E (Box PC, fanless)", Category = "HMI", SubCategory = "IPC", Family = "IPC",
                    Tags = new[]{"IPC","BoxPC","WinCC RT","industrial-PC"} },
                new ModuleSpec { OrderNumber = "6AG4 131-xxxxx", Description = "SIMATIC IPC227E (Nanobox PC)", Category = "HMI", SubCategory = "IPC", Family = "IPC" },
                new ModuleSpec { OrderNumber = "6AG4 141-xxxxx", Description = "SIMATIC IPC427E (Microbox PC)", Category = "HMI", SubCategory = "IPC", Family = "IPC" },
                new ModuleSpec { OrderNumber = "6AV7 250-xxxxx", Description = "SIMATIC IPC677E Panel PC (15-22\")", Category = "HMI", SubCategory = "IPC", Family = "IPC",
                    Tags = new[]{"IPC","Panel-PC","WinCC RT Pro","WinCC Unified PC"} },
                new ModuleSpec { OrderNumber = "6AV7 240-xxxxx", Description = "SIMATIC IPC547G (Rack PC)", Category = "HMI", SubCategory = "IPC", Family = "IPC" },
            });

            // ────────────── DRIVES: SINAMICS G120 VARIANTS ──────────────
            list.AddRange(new[]
            {
                // G120C - all-in-one compact
                new ModuleSpec { OrderNumber = "6SL3 210-1KE13-2UB1", Description = "SINAMICS G120C 0.55kW PN (compact all-in-one)", Category = "Drive-VFD", SubCategory = "G120C", Family = "Drive",
                    Tags = new[]{"VFD","G120C","compact","all-in-one","PROFINET","pump","fan","conveyor"},
                    Parameters = new[]{"P0700","P0701","P1080","P1082","P1120","P1121","P1300","P2000","Telegram"} },
                new ModuleSpec { OrderNumber = "6SL3 210-1KE13-8UB1", Description = "SINAMICS G120C 0.75kW PN", Category = "Drive-VFD", SubCategory = "G120C", Family = "Drive" },
                new ModuleSpec { OrderNumber = "6SL3 210-1KE15-8UB1", Description = "SINAMICS G120C 2.2kW PN", Category = "Drive-VFD", SubCategory = "G120C", Family = "Drive" },
                new ModuleSpec { OrderNumber = "6SL3 210-1KE17-5UB1", Description = "SINAMICS G120C 3kW PN", Category = "Drive-VFD", SubCategory = "G120C", Family = "Drive" },
                new ModuleSpec { OrderNumber = "6SL3 210-1KE21-3UB1", Description = "SINAMICS G120C 5.5kW PN", Category = "Drive-VFD", SubCategory = "G120C", Family = "Drive" },

                // G120D - distributed (IP65)
                new ModuleSpec { OrderNumber = "6SL3 525-0PE17-5AA0", Description = "SINAMICS G120D 0.75kW PN IP65", Category = "Drive-VFD", SubCategory = "G120D", Family = "Drive",
                    Tags = new[]{"VFD","G120D","distributed","IP65","field-mount","conveyor"} },
                new ModuleSpec { OrderNumber = "6SL3 525-0PE21-1AA0", Description = "SINAMICS G120D 4kW PN IP65", Category = "Drive-VFD", SubCategory = "G120D", Family = "Drive" },

                // G120X - water/wastewater
                new ModuleSpec { OrderNumber = "6SL3 220-3YE12-0UB0", Description = "SINAMICS G120X 0.75kW (water/HVAC)", Category = "Drive-VFD", SubCategory = "G120X", Family = "Drive",
                    Tags = new[]{"VFD","G120X","water","HVAC","ESP","essential-services"} },
                new ModuleSpec { OrderNumber = "6SL3 220-3YE16-0UB0", Description = "SINAMICS G120X 2.2kW (water/HVAC)", Category = "Drive-VFD", SubCategory = "G120X", Family = "Drive" },
                new ModuleSpec { OrderNumber = "6SL3 220-3YE20-0UB0", Description = "SINAMICS G120X 7.5kW (water/HVAC)", Category = "Drive-VFD", SubCategory = "G120X", Family = "Drive" },

                // G115D - small distributed
                new ModuleSpec { OrderNumber = "6SL3 200-6AE10-0AA0", Description = "SINAMICS G115D 0.75kW (compact distributed)", Category = "Drive-VFD", SubCategory = "G115D", Family = "Drive",
                    Tags = new[]{"VFD","G115D","compact","intralogistics","conveyor"} },
            });

            // ────────────── DRIVES: SINAMICS S120 ──────────────
            list.AddRange(new[]
            {
                new ModuleSpec { OrderNumber = "6SL3 100-0BE21-6AB0", Description = "SINAMICS S120 CU310-2 PN (Control Unit)", Category = "Drive-Servo", SubCategory = "S120", Family = "Drive",
                    Tags = new[]{"Servo","S120","CU310-2","single-axis","high-performance","motion","PROFINET-IRT"},
                    Parameters = new[]{"P0700","P1000","P1500","P2000","Telegram","SafetyIntegration"} },
                new ModuleSpec { OrderNumber = "6SL3 040-1MA01-0AA0", Description = "SINAMICS S120 CU320-2 PN (multi-axis)", Category = "Drive-Servo", SubCategory = "S120", Family = "Drive",
                    Tags = new[]{"Servo","S120","CU320-2","multi-axis","up-to-6-axes","DRIVE-CLiQ"} },
                new ModuleSpec { OrderNumber = "6SL3 120-1TE21-0AC0", Description = "SINAMICS S120 Single Motor Module 9A", Category = "Drive-Servo", SubCategory = "S120", Family = "Drive",
                    Tags = new[]{"Servo","S120","motor-module","DRIVE-CLiQ"} },
                new ModuleSpec { OrderNumber = "6SL3 120-1TE21-8AC0", Description = "SINAMICS S120 Single Motor Module 18A", Category = "Drive-Servo", SubCategory = "S120", Family = "Drive" },
                new ModuleSpec { OrderNumber = "6SL3 120-2TE21-0AC0", Description = "SINAMICS S120 Double Motor Module 9A/9A", Category = "Drive-Servo", SubCategory = "S120", Family = "Drive" },
            });

            // ────────────── SIMOTICS MOTOR FAMILIES ──────────────
            list.AddRange(new[]
            {
                // SIMOTICS S-1FK7 (Servo, Compact)
                new ModuleSpec { OrderNumber = "1FK7022-5AK71", Description = "SIMOTICS S-1FK7022 0.4kW 6000rpm 1.0Nm", Category = "Motor-Servo", SubCategory = "1FK7", Family = "Motor",
                    Tags = new[]{"motor","servo","1FK7","compact","permanent-magnet"} },
                // SIMOTICS S-1FT7 (Servo, High-dynamic)
                new ModuleSpec { OrderNumber = "1FT7034-5AK71", Description = "SIMOTICS S-1FT7034 1.0kW high-dynamic", Category = "Motor-Servo", SubCategory = "1FT7", Family = "Motor" },
                // SIMOTICS GP (Asynchronous, general purpose)
                new ModuleSpec { OrderNumber = "1LE1003-1BB52-2AA4", Description = "SIMOTICS GP 1LE1 0.55kW 1500rpm IE3", Category = "Motor-VFD", SubCategory = "1LE1", Family = "Motor",
                    Tags = new[]{"motor","asynchronous","general-purpose","IE3","IE4","squirrel-cage"} },
                new ModuleSpec { OrderNumber = "1LE1003-1CA52-2AA4", Description = "SIMOTICS GP 1LE1 0.75kW 1500rpm IE3", Category = "Motor-VFD", SubCategory = "1LE1", Family = "Motor" },
                new ModuleSpec { OrderNumber = "1LE1003-1DB52-2AA4", Description = "SIMOTICS GP 1LE1 1.5kW 1500rpm IE3", Category = "Motor-VFD", SubCategory = "1LE1", Family = "Motor" },
                new ModuleSpec { OrderNumber = "1LE1003-1EB52-2AA4", Description = "SIMOTICS GP 1LE1 2.2kW 1500rpm IE3", Category = "Motor-VFD", SubCategory = "1LE1", Family = "Motor" },
                new ModuleSpec { OrderNumber = "1LE1003-2DA52-2AA4", Description = "SIMOTICS GP 1LE1 4kW 1500rpm IE3", Category = "Motor-VFD", SubCategory = "1LE1", Family = "Motor" },
                new ModuleSpec { OrderNumber = "1LE1003-2DB52-2AA4", Description = "SIMOTICS GP 1LE1 5.5kW 1500rpm IE3", Category = "Motor-VFD", SubCategory = "1LE1", Family = "Motor" },
                new ModuleSpec { OrderNumber = "1LE1003-3AA52-2AA4", Description = "SIMOTICS GP 1LE1 7.5kW 1500rpm IE3", Category = "Motor-VFD", SubCategory = "1LE1", Family = "Motor" },
                new ModuleSpec { OrderNumber = "1LE1003-3AB52-2AA4", Description = "SIMOTICS GP 1LE1 11kW 1500rpm IE3", Category = "Motor-VFD", SubCategory = "1LE1", Family = "Motor" },
                // SIMOTICS HV
                new ModuleSpec { OrderNumber = "1LA8XXX", Description = "SIMOTICS HV 1LA8 (high-voltage motors 1MW+)", Category = "Motor-VFD", SubCategory = "1LA8", Family = "Motor",
                    Tags = new[]{"motor","high-voltage","industry","large-power"} },
                // SIMOTICS XP (Hazardous areas)
                new ModuleSpec { OrderNumber = "1MJ7XXX", Description = "SIMOTICS XP 1MJ (Ex-protected ATEX)", Category = "Motor-VFD", SubCategory = "1MJ", Family = "Motor",
                    Tags = new[]{"motor","Ex-protection","ATEX","hazardous-area"} },
            });

            // ────────────── SAFETY: RELAYS, BUTTONS, LIGHT CURTAINS ──────────────
            list.AddRange(new[]
            {
                // SIRIUS 3SK1 Safety Relays (basic)
                new ModuleSpec { OrderNumber = "3SK1 111-1AB30", Description = "SIRIUS 3SK1 Basic safety relay (E-Stop monitor)", Category = "Safety", SubCategory = "Safety-Relay", Family = "Safety",
                    Tags = new[]{"safety-relay","3SK1","E-Stop","SIL3","PLe","ISO 13849"},
                    Parameters = new[]{"OperatingMode","StartType","DiscrepancyTime"} },
                new ModuleSpec { OrderNumber = "3SK1 122-1AB40", Description = "SIRIUS 3SK1 Standard (light curtain monitor)", Category = "Safety", SubCategory = "Safety-Relay", Family = "Safety" },
                // SIRIUS 3SK2 Modular Safety
                new ModuleSpec { OrderNumber = "3SK2 111-1AA10", Description = "SIRIUS 3SK2 Advanced modular safety system", Category = "Safety", SubCategory = "Safety-Relay", Family = "Safety",
                    Tags = new[]{"safety-relay","3SK2","modular","SIL3","PLe","programmable"} },
                // E-Stop buttons
                new ModuleSpec { OrderNumber = "3SU1 050-1HB20-0AA0", Description = "SIRIUS 3SU E-Stop button 22mm red mushroom", Category = "Safety", SubCategory = "E-Stop", Family = "Safety",
                    Tags = new[]{"E-Stop","emergency-stop","button","22mm","red","mushroom"} },
                new ModuleSpec { OrderNumber = "3SU1 100-1HB20-1FA0", Description = "SIRIUS 3SU illuminated E-Stop", Category = "Safety", SubCategory = "E-Stop", Family = "Safety" },
                new ModuleSpec { OrderNumber = "3SE5 132-0PC02", Description = "SIRIUS 3SE5 safety position switch", Category = "Safety", SubCategory = "Safety-Switch", Family = "Safety",
                    Tags = new[]{"safety-switch","position-switch","door","mechanical"} },
                // F-Door switches
                new ModuleSpec { OrderNumber = "3SE63 14-0BB", Description = "SIRIUS 3SE63 RFID safety door switch", Category = "Safety", SubCategory = "Safety-Switch", Family = "Safety",
                    Tags = new[]{"safety-switch","RFID","coded","tamper-proof","PLe"} },
                new ModuleSpec { OrderNumber = "3SE7 140-1BD11", Description = "SIRIUS 3SE7 safety hinge switch", Category = "Safety", SubCategory = "Safety-Switch", Family = "Safety" },
                // Cable-pull switches
                new ModuleSpec { OrderNumber = "3SE7 140-1BG31", Description = "SIRIUS 3SE7 cable-pull e-stop switch", Category = "Safety", SubCategory = "Safety-Switch", Family = "Safety",
                    Tags = new[]{"cable-pull","rope-pull","E-Stop","conveyor"} },
                // Two-hand controls
                new ModuleSpec { OrderNumber = "3SB3 801-7AA0", Description = "SIRIUS 3SB3 two-hand control panel", Category = "Safety", SubCategory = "Safety-Control", Family = "Safety",
                    Tags = new[]{"two-hand","press-control","TWO_HAND"} },
                // Light curtains
                new ModuleSpec { OrderNumber = "3RG7 8XX-XXAX", Description = "SIMATIC FS400 Type 4 light curtain (finger/hand protection)", Category = "Safety", SubCategory = "Light-Curtain", Family = "Safety",
                    Tags = new[]{"light-curtain","Type 4","finger-detection","hand-detection","PLe"},
                    Parameters = new[]{"Resolution","Range","Muting","Blanking","TestInputs"} },
                new ModuleSpec { OrderNumber = "3RG7 9XX-XXAX", Description = "SIMATIC FS600 area scanner (laser scanner)", Category = "Safety", SubCategory = "Laser-Scanner", Family = "Safety",
                    Tags = new[]{"laser-scanner","area-scanner","AGV","mobile-platform","PLe"} },
                // Safety monitors / control units
                new ModuleSpec { OrderNumber = "3RK1 405-0BG00-0AA2", Description = "SIRIUS 3RK1 ASIsafe monitor", Category = "Safety", SubCategory = "Safety-Monitor", Family = "Safety",
                    Tags = new[]{"AS-i","ASIsafe","safety-monitor"} },
                // Enabling switches
                new ModuleSpec { OrderNumber = "3SU1 100-XXXX-XXXX", Description = "SIRIUS 3SU enabling switch (3-position)", Category = "Safety", SubCategory = "Safety-Control", Family = "Safety",
                    Tags = new[]{"enabling-switch","3-position","jog-mode","ENABLE_SWITCH"} },
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
