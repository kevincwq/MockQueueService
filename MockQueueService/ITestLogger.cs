using Microsoft.Extensions.Logging;

namespace MockQueueService
{
    public record ReceivedLog(LogLevel Level, string Message, Type ExceptionType);

    public interface ITestLogger
    {
        int ReceivedCount { get; }

        IEnumerable<ReceivedLog> ReceivedLogs { get; }

        Task WhenLogsReceived(int count);
    }
}
