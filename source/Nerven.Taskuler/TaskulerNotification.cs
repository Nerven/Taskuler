using System;
using JetBrains.Annotations;

namespace Nerven.Taskuler
{
    public struct TaskulerNotification
    {
        public TaskulerNotification(
            DateTimeOffset timestamp,
            string message,
            Exception exception)
        {
            Timestamp = timestamp;
            Message = message;
            Exception = exception;
        }

        [PublicAPI]
        public DateTimeOffset Timestamp { get; }

        [PublicAPI]
        public string Message { get; }

        [PublicAPI]
        public Exception Exception { get; }
    }
}
