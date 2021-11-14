using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace MockQueueService
{
    public class MockLogger<T> : ILogger<T>, ITestLogger
    {
        readonly ConcurrentStack<ReceivedLog> _events = new();

        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _events.Push(new ReceivedLog(logLevel, formatter?.Invoke(state, exception), exception?.GetType()));
        }

        public int ReceivedCount => _events.Count;

        public IEnumerable<ReceivedLog> ReceivedLogs => _events.ToArray();

        public virtual bool IsEnabled(LogLevel logLevel) => true;

        public virtual IDisposable BeginScope<TState>(TState state) => null;

        public async Task WhenLogsReceived(int count)
        {
            while (ReceivedCount < count)
            {
                await Task.Delay(2);
            }
        }
    }
}
