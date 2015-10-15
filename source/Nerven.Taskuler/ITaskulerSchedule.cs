using System;
using JetBrains.Annotations;

namespace Nerven.Taskuler
{
    [PublicAPI]
    public interface ITaskulerSchedule
    {
        TaskulerScheduleResponse Tick(TimeSpan resolution, DateTimeOffset firstTick, TimeSpan? lastTick, TimeSpan currentTick);
    }
}
