namespace TiaMcpV2.Core
{
    public enum PortalErrorCode
    {
        Unknown = 0,
        NotConnected = 1,
        AlreadyConnected = 2,
        NotFound = 3,
        InvalidState = 4,
        InvalidArgument = 5,
        OperationFailed = 6,
        NotSupported = 7,
        AccessDenied = 8,
        Timeout = 9,
        CompilationFailed = 10,
        ImportFailed = 11,
        ExportFailed = 12,
        SafetyError = 13,
        CommunicationError = 14,
        ProjectNotOpen = 15
    }

    public class PortalException : System.Exception
    {
        public PortalErrorCode ErrorCode { get; }

        public PortalException(PortalErrorCode code, string message)
            : base(message)
        {
            ErrorCode = code;
        }

        public PortalException(PortalErrorCode code, string message, System.Exception innerException)
            : base(message, innerException)
        {
            ErrorCode = code;
        }
    }
}
