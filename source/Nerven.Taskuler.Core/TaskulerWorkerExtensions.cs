using System;
using JetBrains.Annotations;

namespace Nerven.Taskuler.Core
{
    [PublicAPI]
    public static class TaskulerWorkerExtensions
    {
        public static ITaskulerScheduleHandle AddIntervalSchedule(this ITaskulerWorker worker, string scheduleName, TimeSpan interval)
        {
            return worker?.AddSchedule(scheduleName, TaskulerIntervalSchedule.Create(interval));
        }

        public static ITaskulerScheduleHandle AddIntervalSchedule(this ITaskulerWorker worker, TimeSpan interval)
        {
            return AddIntervalSchedule(worker, null, interval);
        }

        public static ITaskulerScheduleHandle AddDailySchedule(this ITaskulerWorker worker, string scheduleName, TimeSpan timeOfDay)
        {
            return worker?.AddSchedule(scheduleName, TaskulerDailySchedule.Create(timeOfDay));
        }

        public static ITaskulerScheduleHandle AddDailySchedule(this ITaskulerWorker worker, TimeSpan timeOfDay)
        {
            return AddDailySchedule(worker, null, timeOfDay);
        }

        public static ITaskulerScheduleHandle AddVoidSchedule(this ITaskulerWorker worker, string scheduleName = null)
        {
            return worker?.AddSchedule(scheduleName, TaskulerVoidSchedule.Create());
        }
    }
}
