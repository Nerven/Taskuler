using System;
using JetBrains.Annotations;

namespace Nerven.Taskuler.Core
{
    [PublicAPI]
    public static class TaskulerWorkerExtensions
    {
        public static ITaskulerScheduleHandle UseIntervalSchedule(this ITaskulerWorker worker, TimeSpan interval)
        {
            return worker?.Use(TaskulerIntervalSchedule.Create(interval));
        }

        public static ITaskulerScheduleHandle UseDailySchedule(this ITaskulerWorker worker, TimeSpan timeOfDay)
        {
            return worker?.Use(TaskulerDailySchedule.Create(timeOfDay));
        }
    }
}
