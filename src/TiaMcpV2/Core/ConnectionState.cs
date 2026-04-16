namespace TiaMcpV2.Core
{
    public class ConnectionState
    {
        public bool IsConnected { get; set; }
        public string? ProjectName { get; set; }
        public string? ProjectPath { get; set; }
        public string? SessionId { get; set; }
        public int TiaVersion { get; set; }
        public bool IsLocalSession { get; set; }
    }
}
