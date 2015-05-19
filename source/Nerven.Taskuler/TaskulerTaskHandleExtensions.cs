using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Nerven.Taskuler
{
    [PublicAPI]
    public static class TaskulerTaskHandleExtensions
    {
        public static ITaskulerTaskHandle Do(this ITaskulerTaskHandle taskHandle, string taskName, Func<CancellationToken, Task<TaskulerTaskResponse>> run)
        {
            return taskHandle.ScheduleHandle.Do(taskName, run);
        }

        public static ITaskulerTaskHandle Do(this ITaskulerTaskHandle taskHandle, string taskName, Func<Task<TaskulerTaskResponse>> run)
        {
            return taskHandle.ScheduleHandle.Do(taskName, run);
        }

        public static ITaskulerTaskHandle Do(this ITaskulerTaskHandle taskHandle, string taskName, Func<Task> run)
        {
            return taskHandle.ScheduleHandle.Do(taskName, run);
        }

        public static ITaskulerTaskHandle Do(this ITaskulerTaskHandle taskHandle, Func<CancellationToken, Task<TaskulerTaskResponse>> run)
        {
            return taskHandle.ScheduleHandle.Do(run);
        }

        public static ITaskulerTaskHandle Do(this ITaskulerTaskHandle taskHandle, Func<Task<TaskulerTaskResponse>> run)
        {
            return taskHandle.ScheduleHandle.Do(run);
        }

        public static ITaskulerTaskHandle Do(this ITaskulerTaskHandle taskHandle, Func<Task> run)
        {
            return taskHandle.ScheduleHandle.Do(run);
        }
    }
}
