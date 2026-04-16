using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace TiaMcpV2.Helpers
{
    public static class JsonHelper
    {
        public static string Serialize<T>(T obj)
        {
            var serializer = new DataContractJsonSerializer(typeof(T));
            using (var ms = new MemoryStream())
            {
                serializer.WriteObject(ms, obj);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        public static string ToJson(object obj)
        {
            // Use System.Text.Json if available, otherwise fallback to DataContract
            try
            {
                return System.Text.Json.JsonSerializer.Serialize(obj, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
            }
            catch
            {
                return Serialize(obj);
            }
        }
    }
}
