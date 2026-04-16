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
