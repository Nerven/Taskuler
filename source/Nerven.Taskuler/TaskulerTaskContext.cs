using System;
using JetBrains.Annotations;

namespace Nerven.Taskuler
{
    // TODO: Better member names
    [PublicAPI]
    public sealed class TaskulerTaskContext
    {
        public TaskulerTaskContext(
            DateTimeOffset baseTime,
            TimeSpan scheduledAfter)
        {
            BaseTime = baseTime;
            ScheduledAfter = scheduledAfter;
        }

        public DateTimeOffset BaseTime { get; }

        public TimeSpan ScheduledAfter { get; }

        public DateTimeOffset ScheduledAt => BaseTime.Add(ScheduledAfter);
    }
}
