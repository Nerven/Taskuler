using System;
using JetBrains.Annotations;

namespace Nerven.Taskuler.Core
{
    [PublicAPI]
    public sealed class TaskulerVoidSchedule : TaskulerScheduleBase
    {
        private static readonly TaskulerVoidSchedule _Instance = new TaskulerVoidSchedule();

        private TaskulerVoidSchedule()
        {
        }

        public static ITaskulerSchedule Create()
        {
            return _Instance;
        }

        public override TaskulerScheduleResponse Tick(TimeSpan resolution, DateTimeOffset firstTick, TimeSpan? lastTick, TimeSpan currentTick)
        {
            return TaskulerScheduleResponse.Wait();
        }
    }
}
