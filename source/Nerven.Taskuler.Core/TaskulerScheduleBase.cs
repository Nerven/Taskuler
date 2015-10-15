using System;
using JetBrains.Annotations;

namespace Nerven.Taskuler.Core
{
    [PublicAPI]
    public abstract class TaskulerScheduleBase : ITaskulerSchedule
    {
        public abstract TaskulerScheduleResponse Tick(TimeSpan resolution, DateTimeOffset firstTick, TimeSpan? lastTick, TimeSpan currentTick);
    }
}
