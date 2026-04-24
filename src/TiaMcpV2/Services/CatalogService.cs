using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using TiaMcpV2.Models;

namespace TiaMcpV2.Services
{
    /// <summary>
    /// Hardware catalog with known Siemens order numbers for common devices.
    /// Provides search functionality for CreateDevice/PlugModule operations.
    /// </summary>
    public class CatalogService
    {
        private readonly ILogger<CatalogService>? _logger;
        private static readonly List<CatalogSearchResult> _catalog;

        static CatalogService()
        {
            _catalog = new List<CatalogSearchResult>
            {
                // S7-1500 CPUs
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6ES7 511-1AK02-0AB0/V2.9", OrderNumber = "6ES7 511-1AK02-0AB0", Description = "CPU 1511-1 PN", Category = "CPU" },
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6ES7 512-1CK01-0AB0/V2.9", OrderNumber = "6ES7 512-1CK01-0AB0", Description = "CPU 1512C-1 PN", Category = "CPU" },
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6ES7 513-1AL02-0AB0/V2.9", OrderNumber = "6ES7 513-1AL02-0AB0", Description = "CPU 1513-1 PN", Category = "CPU" },
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6ES7 515-2AM02-0AB0/V2.9", OrderNumber = "6ES7 515-2AM02-0AB0", Description = "CPU 1515-2 PN", Category = "CPU" },
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6ES7 516-3AN02-0AB0/V2.9", OrderNumber = "6ES7 516-3AN02-0AB0", Description = "CPU 1516-3 PN/DP", Category = "CPU" },
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6ES7 517-3AP00-0AB0/V2.9", OrderNumber = "6ES7 517-3AP00-0AB0", Description = "CPU 1517-3 PN/DP", Category = "CPU" },
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6ES7 518-4AP00-0AB0/V2.9", OrderNumber = "6ES7 518-4AP00-0AB0", Description = "CPU 1518-4 PN/DP", Category = "CPU" },

                // S7-1500 F-CPUs (Safety)
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6ES7 511-1FK02-0AB0/V2.9", OrderNumber = "6ES7 511-1FK02-0AB0", Description = "CPU 1511F-1 PN (Safety)", Category = "F-CPU" },
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6ES7 513-1FL02-0AB0/V2.9", OrderNumber = "6ES7 513-1FL02-0AB0", Description = "CPU 1513F-1 PN (Safety)", Category = "F-CPU" },
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6ES7 516-3FN02-0AB0/V2.9", OrderNumber = "6ES7 516-3FN02-0AB0", Description = "CPU 1516F-3 PN/DP (Safety)", Category = "F-CPU" },

                // S7-1200 CPUs
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6ES7 211-1AE40-0XB0/V4.5", OrderNumber = "6ES7 211-1AE40-0XB0", Description = "CPU 1211C DC/DC/DC", Category = "CPU" },
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6ES7 212-1AE40-0XB0/V4.5", OrderNumber = "6ES7 212-1AE40-0XB0", Description = "CPU 1212C DC/DC/DC", Category = "CPU" },
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6ES7 214-1AG40-0XB0/V4.5", OrderNumber = "6ES7 214-1AG40-0XB0", Description = "CPU 1214C DC/DC/DC", Category = "CPU" },
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6ES7 215-1AG40-0XB0/V4.5", OrderNumber = "6ES7 215-1AG40-0XB0", Description = "CPU 1215C DC/DC/DC", Category = "CPU" },

                // S7-1500 Digital Input Modules
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6ES7 521-1BH50-0AA0", OrderNumber = "6ES7 521-1BH50-0AA0", Description = "DI 16x24VDC HF", Category = "DI" },
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6ES7 521-1BL00-0AB0", OrderNumber = "6ES7 521-1BL00-0AB0", Description = "DI 32x24VDC HF", Category = "DI" },
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6ES7 521-1BH10-0AA0", OrderNumber = "6ES7 521-1BH10-0AA0", Description = "DI 16x24VDC SRC BA", Category = "DI" },

                // S7-1500 Digital Output Modules
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6ES7 522-1BH50-0AA0", OrderNumber = "6ES7 522-1BH50-0AA0", Description = "DQ 16x24VDC/0.5A HF", Category = "DQ" },
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6ES7 522-1BL01-0AB0", OrderNumber = "6ES7 522-1BL01-0AB0", Description = "DQ 32x24VDC/0.5A HF", Category = "DQ" },
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6ES7 522-5HH00-0AB0", OrderNumber = "6ES7 522-5HH00-0AB0", Description = "DQ 16x230VAC/2A RLY", Category = "DQ" },

                // S7-1500 Analog Input Modules
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6ES7 531-7KF00-0AB0", OrderNumber = "6ES7 531-7KF00-0AB0", Description = "AI 8xU/I/RTD/TC ST", Category = "AI" },
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6ES7 531-7NF10-0AB0", OrderNumber = "6ES7 531-7NF10-0AB0", Description = "AI 4xU/I/RTD/TC HF", Category = "AI" },
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6ES7 531-7PF00-0AB0", OrderNumber = "6ES7 531-7PF00-0AB0", Description = "AI 8xU/I HS", Category = "AI" },

                // S7-1500 Analog Output Modules
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6ES7 532-5HF00-0AB0", OrderNumber = "6ES7 532-5HF00-0AB0", Description = "AQ 4xU/I HF", Category = "AQ" },
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6ES7 532-5NB00-0AB0", OrderNumber = "6ES7 532-5NB00-0AB0", Description = "AQ 2xU/I ST", Category = "AQ" },

                // Safety Modules
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6ES7 526-1BH00-0AB0", OrderNumber = "6ES7 526-1BH00-0AB0", Description = "F-DI 16x24VDC", Category = "F-DI" },
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6ES7 526-2BF00-0AB0", OrderNumber = "6ES7 526-2BF00-0AB0", Description = "F-DQ 8x24VDC/2A PPM", Category = "F-DQ" },

                // Power Supply
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6ES7 505-0KA00-0AB0", OrderNumber = "6ES7 505-0KA00-0AB0", Description = "PS 25W 24VDC", Category = "PS" },
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6ES7 507-0RA00-0AB0", OrderNumber = "6ES7 507-0RA00-0AB0", Description = "PS 60W 120/230VAC", Category = "PS" },

                // Communication Modules
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6GK7 543-1AX00-0XE0", OrderNumber = "6GK7 543-1AX00-0XE0", Description = "CP 1543-1", Category = "CP" },

                // ET200SP Interface Modules
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6ES7 155-6AU01-0BN0", OrderNumber = "6ES7 155-6AU01-0BN0", Description = "IM 155-6 PN ST", Category = "ET200SP" },
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6ES7 155-6AU01-0CN0", OrderNumber = "6ES7 155-6AU01-0CN0", Description = "IM 155-6 PN HF", Category = "ET200SP" },

                // HMI Panels
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6AV2 124-0GC01-0AX0", OrderNumber = "6AV2 124-0GC01-0AX0", Description = "KTP700 Basic", Category = "HMI" },
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6AV2 124-0JC01-0AX0", OrderNumber = "6AV2 124-0JC01-0AX0", Description = "KTP900 Basic", Category = "HMI" },
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6AV2 124-0MC01-0AX0", OrderNumber = "6AV2 124-0MC01-0AX0", Description = "KTP1200 Basic", Category = "HMI" },
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6AV2 125-2GB23-0AX0", OrderNumber = "6AV2 125-2GB23-0AX0", Description = "TP700 Comfort", Category = "HMI" },

                // ─────────── SINAMICS S210 (PROFINET Servo Drive) ───────────
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6SL3 210-5HB10-2UF0", OrderNumber = "6SL3 210-5HB10-2UF0", Description = "SINAMICS S210 400V 0.4kW 1.3A", Category = "Drive-Servo" },
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6SL3 210-5HB10-4UF0", OrderNumber = "6SL3 210-5HB10-4UF0", Description = "SINAMICS S210 400V 0.75kW 2.6A", Category = "Drive-Servo" },
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6SL3 210-5HB10-8UF0", OrderNumber = "6SL3 210-5HB10-8UF0", Description = "SINAMICS S210 400V 1.5kW 4.7A", Category = "Drive-Servo" },
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6SL3 210-5HB11-0UF0", OrderNumber = "6SL3 210-5HB11-0UF0", Description = "SINAMICS S210 400V 2.2kW 7.1A", Category = "Drive-Servo" },
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6SL3 210-5HB11-8UF0", OrderNumber = "6SL3 210-5HB11-8UF0", Description = "SINAMICS S210 400V 4kW 13.2A", Category = "Drive-Servo" },
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6SL3 210-5HE10-8UF0", OrderNumber = "6SL3 210-5HE10-8UF0", Description = "SINAMICS S210 400V 7.5kW 18A", Category = "Drive-Servo" },

                // ─────────── SINAMICS V90 (PROFINET Servo Drive) ───────────
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6SL3 210-5FB10-1UA0", OrderNumber = "6SL3 210-5FB10-1UA0", Description = "SINAMICS V90 PN 0.1kW", Category = "Drive-Servo" },
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6SL3 210-5FB10-2UA0", OrderNumber = "6SL3 210-5FB10-2UA0", Description = "SINAMICS V90 PN 0.2kW", Category = "Drive-Servo" },
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6SL3 210-5FB10-4UA0", OrderNumber = "6SL3 210-5FB10-4UA0", Description = "SINAMICS V90 PN 0.4kW", Category = "Drive-Servo" },
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6SL3 210-5FB10-8UA0", OrderNumber = "6SL3 210-5FB10-8UA0", Description = "SINAMICS V90 PN 0.75kW", Category = "Drive-Servo" },
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6SL3 210-5FB11-0UA0", OrderNumber = "6SL3 210-5FB11-0UA0", Description = "SINAMICS V90 PN 1kW", Category = "Drive-Servo" },
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6SL3 210-5FB11-5UA0", OrderNumber = "6SL3 210-5FB11-5UA0", Description = "SINAMICS V90 PN 1.5kW", Category = "Drive-Servo" },
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6SL3 210-5FB12-0UA0", OrderNumber = "6SL3 210-5FB12-0UA0", Description = "SINAMICS V90 PN 2kW", Category = "Drive-Servo" },

                // ─────────── SINAMICS G120 (Standard VFD) ───────────
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6SL3 210-1KE13-2UP2", OrderNumber = "6SL3 210-1KE13-2UP2", Description = "SINAMICS G120 PM240-2 0.75kW", Category = "Drive-VFD" },
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6SL3 210-1KE14-3UP2", OrderNumber = "6SL3 210-1KE14-3UP2", Description = "SINAMICS G120 PM240-2 1.5kW", Category = "Drive-VFD" },
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6SL3 210-1KE15-8UP2", OrderNumber = "6SL3 210-1KE15-8UP2", Description = "SINAMICS G120 PM240-2 2.2kW", Category = "Drive-VFD" },
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6SL3 210-1KE17-5UP2", OrderNumber = "6SL3 210-1KE17-5UP2", Description = "SINAMICS G120 PM240-2 3kW", Category = "Drive-VFD" },
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6SL3 210-1KE18-8UP2", OrderNumber = "6SL3 210-1KE18-8UP2", Description = "SINAMICS G120 PM240-2 4kW", Category = "Drive-VFD" },
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6SL3 210-1KE21-3UP2", OrderNumber = "6SL3 210-1KE21-3UP2", Description = "SINAMICS G120 PM240-2 5.5kW", Category = "Drive-VFD" },
                new CatalogSearchResult { TypeIdentifier = "OrderNumber:6SL3 210-1KE22-6UP2", OrderNumber = "6SL3 210-1KE22-6UP2", Description = "SINAMICS G120 PM240-2 11kW", Category = "Drive-VFD" },

                // ─────────── SIMOTICS Servo Motors (for S210) ───────────
                new CatalogSearchResult { TypeIdentifier = "Motor:1FK2103-4AG", OrderNumber = "1FK2103-4AG00-0MA0", Description = "SIMOTICS S-1FK2 0.4kW 3000rpm 1.27Nm (low inertia)", Category = "Motor-Servo" },
                new CatalogSearchResult { TypeIdentifier = "Motor:1FK2104-5AG", OrderNumber = "1FK2104-5AG00-0MA0", Description = "SIMOTICS S-1FK2 0.75kW 3000rpm 2.4Nm (low inertia)", Category = "Motor-Servo" },
                new CatalogSearchResult { TypeIdentifier = "Motor:1FK2106-3AG", OrderNumber = "1FK2106-3AG00-0MA0", Description = "SIMOTICS S-1FK2 1.5kW 3000rpm 4.78Nm (low inertia)", Category = "Motor-Servo" },
                new CatalogSearchResult { TypeIdentifier = "Motor:1FK2108-5AG", OrderNumber = "1FK2108-5AG00-0MA0", Description = "SIMOTICS S-1FK2 2.2kW 3000rpm 7.0Nm (low inertia)", Category = "Motor-Servo" },
                new CatalogSearchResult { TypeIdentifier = "Motor:1FK2110-4AK", OrderNumber = "1FK2110-4AK00-0MA0", Description = "SIMOTICS S-1FK2 4kW 3000rpm 12.7Nm (low inertia)", Category = "Motor-Servo" },
                new CatalogSearchResult { TypeIdentifier = "Motor:1FK7032-5AK", OrderNumber = "1FK7032-5AK71-1AH0", Description = "SIMOTICS S-1FK7 Compact 0.8kW 3000rpm 2.6Nm", Category = "Motor-Servo" },
                new CatalogSearchResult { TypeIdentifier = "Motor:1FK7042-5AK", OrderNumber = "1FK7042-5AK71-1AH0", Description = "SIMOTICS S-1FK7 Compact 1.3kW 3000rpm 4.2Nm", Category = "Motor-Servo" },
                new CatalogSearchResult { TypeIdentifier = "Motor:1FK7063-5AH", OrderNumber = "1FK7063-5AH71-1AH0", Description = "SIMOTICS S-1FK7 Compact 2.6kW 3000rpm 8.3Nm", Category = "Motor-Servo" },

                // V90 compatible motors
                new CatalogSearchResult { TypeIdentifier = "Motor:1FL6022-2AF", OrderNumber = "1FL6022-2AF21-1AG1", Description = "SIMOTICS S-1FL6 0.05kW 3000rpm 0.16Nm (V90)", Category = "Motor-Servo" },
                new CatalogSearchResult { TypeIdentifier = "Motor:1FL6024-2AF", OrderNumber = "1FL6024-2AF21-1AG1", Description = "SIMOTICS S-1FL6 0.1kW 3000rpm 0.32Nm (V90)", Category = "Motor-Servo" },
                new CatalogSearchResult { TypeIdentifier = "Motor:1FL6042-1AF", OrderNumber = "1FL6042-1AF61-2AB1", Description = "SIMOTICS S-1FL6 0.4kW 3000rpm 1.27Nm (V90)", Category = "Motor-Servo" },
                new CatalogSearchResult { TypeIdentifier = "Motor:1FL6044-1AF", OrderNumber = "1FL6044-1AF61-2AB1", Description = "SIMOTICS S-1FL6 0.75kW 3000rpm 2.39Nm (V90)", Category = "Motor-Servo" },
                new CatalogSearchResult { TypeIdentifier = "Motor:1FL6064-1AC", OrderNumber = "1FL6064-1AC61-2AB1", Description = "SIMOTICS S-1FL6 1.5kW 2000rpm 7.16Nm (V90)", Category = "Motor-Servo" },
                new CatalogSearchResult { TypeIdentifier = "Motor:1FL6090-1AC", OrderNumber = "1FL6090-1AC61-2AB1", Description = "SIMOTICS S-1FL6 2kW 2000rpm 9.55Nm (V90)", Category = "Motor-Servo" },
            };
        }

        public CatalogService(ILogger<CatalogService>? logger = null)
        {
            _logger = logger;
        }

        public List<CatalogSearchResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return _catalog;

            var q = query.ToUpperInvariant();
            return _catalog.Where(c =>
                (c.Description?.ToUpperInvariant().Contains(q) == true) ||
                (c.OrderNumber?.ToUpperInvariant().Contains(q) == true) ||
                (c.Category?.ToUpperInvariant().Contains(q) == true) ||
                (c.TypeIdentifier?.ToUpperInvariant().Contains(q) == true)
            ).ToList();
        }
    }
}
