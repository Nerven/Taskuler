using System;
using JetBrains.Annotations;

namespace Nerven.Taskuler
{
    [PublicAPI]
    public interface ITaskulerSchedule
    {
        TaskulerScheduleAction Tick(
            TimeSpan resolution,
            DateTimeOffset current);
    }
}
