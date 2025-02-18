using FlexLogStream.Logging.Managers;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace FlexLogStream.Logging.Configuration
{
    public class AsyncFlexLogStreamLoggingProvider : ILoggerProvider
    {
        private readonly AsyncLoggingManager _asyncLoggingManager;
        private readonly ConcurrentDictionary<string, FlexLogStreamLogger> _loggers = new ConcurrentDictionary<string, FlexLogStreamLogger>();
        public AsyncFlexLogStreamLoggingProvider(AsyncLoggingManager asyncLoggingManager)
        {
            _asyncLoggingManager = asyncLoggingManager;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new FlexLogStreamLogger(_asyncLoggingManager, name));
        }

        public void Dispose()
        {
            _loggers.Clear();
        }
    }
}
