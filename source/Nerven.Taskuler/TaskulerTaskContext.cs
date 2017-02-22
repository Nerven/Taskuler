using System;
using JetBrains.Annotations;

namespace Nerven.Taskuler
{
    [PublicAPI]
    public sealed class TaskulerTaskContext
    {
        public TaskulerTaskContext(
            DateTimeOffset epoch,
            TimeSpan duration)
        {
            Epoch = epoch;
            Duration = duration;
        }

        public DateTimeOffset Epoch { get; }

        public TimeSpan Duration { get; }

        public DateTimeOffset Timestamp => Epoch.Add(Duration);
    }
}
