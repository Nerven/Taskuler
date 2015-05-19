using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Nerven.Taskuler
{
    [PublicAPI]
    public static class TaskulerScheduleHandleExtensions
    {
        public static ITaskulerTaskHandle Do(this ITaskulerScheduleHandle scheduleHandle, string taskName, Func<Task<TaskulerTaskResponse>> run)
        {
            return scheduleHandle.Do(taskName, _cancellationToken => run());
        }

        public static ITaskulerTaskHandle Do(this ITaskulerScheduleHandle scheduleHandle, string taskName, Func<Task> run)
        {
            return scheduleHandle.Do(taskName, async _cancellationToken =>
            {
                await run();
                return TaskulerTaskResponse.Continue();
            });
        }

        public static ITaskulerTaskHandle Do(this ITaskulerScheduleHandle scheduleHandle, Func<CancellationToken, Task<TaskulerTaskResponse>> run)
        {
            return scheduleHandle.Do(null, run);
        }

        public static ITaskulerTaskHandle Do(this ITaskulerScheduleHandle scheduleHandle, Func<Task<TaskulerTaskResponse>> run)
        {
            return scheduleHandle.Do(null, _cancellationToken => run());
        }

        public static ITaskulerTaskHandle Do(this ITaskulerScheduleHandle scheduleHandle, Func<Task> run)
        {
            return scheduleHandle.Do(null, async _cancellationToken =>
            {
                await run();
                return TaskulerTaskResponse.Continue();
            });
        }
    }
}
