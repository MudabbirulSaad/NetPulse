namespace NetPulse.Core.Models;

public enum ProbeErrorCode
{
    Timeout,
    DnsFailure,
    ConnectionRefused,
    TlsFailure,
    Cancellation,
    InvalidConfiguration,
    UnexpectedFailure,
}
