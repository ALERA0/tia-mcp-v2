using System.ComponentModel;
using ModelContextProtocol.Server;
using System;
using System.Linq;
using TiaMcpV2.Core;
using TiaMcpV2.Helpers;
using TiaMcpV2.Models;

namespace TiaMcpV2.Tools
{
    [McpServerToolType]
    public static class DataTypeTools
    {
        [McpServerTool(Name = "list_data_types"), Description("List all supported PLC data types. category filter: 'Bit', 'Bit String', 'Integer', 'Float', 'Time', 'Char', 'String', 'Complex', 'Generic', 'System', 'System-Motion', 'System-Comm', 'System-PID' (or empty for all). Returns name, size, range, default, description, example.")]
        public static string ListDataTypes(string category)
        {
            try
            {
                var types = string.IsNullOrEmpty(category)
                    ? ServiceAccessor.DataType.GetAllDataTypes()
                    : ServiceAccessor.DataType.GetByCategory(category);
                return JsonHelper.ToJson(new { Success = true, Count = types.Count, DataTypes = types });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "get_data_type_info"), Description("Get detailed info about a specific data type. Examples: 'Bool', 'Int', 'DInt', 'Real', 'LReal', 'Time', 'DTL', 'String', 'Variant', 'TON_TIME', 'TO_PositioningAxis', 'PID_Compact', 'TCON_IP_v4'.")]
        public static string GetDataTypeInfo(string typeName)
        {
            try
            {
                var info = ServiceAccessor.DataType.GetByName(typeName);
                if (info == null)
                    return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = $"Type not found: {typeName}" });
                return JsonHelper.ToJson(new { Success = true, DataType = info });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "search_data_types"), Description("Search data types by keyword. Examples: 'integer', 'time', 'motion', 'modbus', 'string', 'pid', 'unicode'.")]
        public static string SearchDataTypes(string query)
        {
            try
            {
                var results = ServiceAccessor.DataType.Search(query);
                return JsonHelper.ToJson(new { Success = true, Count = results.Count, Results = results });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "get_conversion_info"), Description("Get info about converting between two data types. Returns conversion method, risk (overflow/truncation), and recommended SCL syntax. Example: get_conversion_info('Real', 'Int') returns 'REAL_TO_INT(ROUND(value))'.")]
        public static string GetConversionInfo(string fromType, string toType)
        {
            try
            {
                var info = ServiceAccessor.DataType.GetConversionInfo(fromType, toType);
                return JsonHelper.ToJson(new { Success = true, Conversion = info });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }
    }

    [McpServerToolType]
    public static class InstructionLibraryTools
    {
        [McpServerTool(Name = "list_instructions"), Description("List all SCL/PLC system instructions. category filter: 'Timer', 'Counter', 'Math', 'String', 'Conversion', 'Comparison', 'Logic', 'PID', 'Filter', 'Motion', 'Modbus', 'OpenComm', 'S7Comm', 'Edge', 'DBAccess', 'System' (or empty for all).")]
        public static string ListInstructions(string category)
        {
            try
            {
                var instructions = string.IsNullOrEmpty(category)
                    ? ServiceAccessor.Instructions.GetAll()
                    : ServiceAccessor.Instructions.GetByCategory(category);
                return JsonHelper.ToJson(new { Success = true, Count = instructions.Count, Instructions = instructions });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "get_instruction_info"), Description("Get detailed info about a specific instruction with SCL example. Examples: 'TON', 'TOF', 'CTU', 'NORM_X', 'SCALE_X', 'PID_Compact', 'MC_MoveAbsolute', 'MB_CLIENT', 'TSEND_C', 'GET', 'PUT'.")]
        public static string GetInstructionInfo(string instructionName)
        {
            try
            {
                var info = ServiceAccessor.Instructions.GetByName(instructionName);
                if (info == null)
                    return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = $"Instruction not found: {instructionName}" });
                return JsonHelper.ToJson(new { Success = true, Instruction = info });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "search_instructions"), Description("Search instructions by keyword. Examples: 'timer', 'modbus', 'analog scaling', 'motion', 'communication', 'string', 'pid', 'edge detection'.")]
        public static string SearchInstructions(string query)
        {
            try
            {
                var results = ServiceAccessor.Instructions.Search(query);
                return JsonHelper.ToJson(new { Success = true, Count = results.Count, Results = results });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }

        [McpServerTool(Name = "get_instruction_categories"), Description("List all instruction categories with example instructions in each. Use this to navigate the instruction library.")]
        public static string GetInstructionCategories()
        {
            try
            {
                var all = ServiceAccessor.Instructions.GetAll();
                var grouped = all.GroupBy(i => i.Category)
                    .Select(g => new
                    {
                        Category = g.Key,
                        Count = g.Count(),
                        Instructions = g.Select(i => i.Name).ToList()
                    })
                    .OrderBy(g => g.Category)
                    .ToList();
                return JsonHelper.ToJson(new { Success = true, Categories = grouped });
            }
            catch (Exception ex)
            {
                return JsonHelper.ToJson(new ResponseMessage { Success = false, Message = ex.Message });
            }
        }
    }
}
