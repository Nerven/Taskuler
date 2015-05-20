using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Nerven.Taskuler
{
    [PublicAPI]
    public static class TaskulerScheduleHandleExtensions
    {
        public static ITaskulerTaskHandle Task(this ITaskulerScheduleHandle scheduleHandle, string taskName, Func<Task<TaskulerTaskResponse>> run)
        {
            return scheduleHandle.Task(taskName, _cancellationToken => run());
        }

        public static ITaskulerTaskHandle Task(this ITaskulerScheduleHandle scheduleHandle, string taskName, Func<Task> run)
        {
            return scheduleHandle.Task(taskName, async _cancellationToken =>
            {
                await run();
                return TaskulerTaskResponse.Continue();
            });
        }

        public static ITaskulerTaskHandle Task(this ITaskulerScheduleHandle scheduleHandle, Func<CancellationToken, Task<TaskulerTaskResponse>> run)
        {
            return scheduleHandle.Task(null, run);
        }

        public static ITaskulerTaskHandle Task(this ITaskulerScheduleHandle scheduleHandle, Func<Task<TaskulerTaskResponse>> run)
        {
            return scheduleHandle.Task(null, _cancellationToken => run());
        }

        public static ITaskulerTaskHandle Task(this ITaskulerScheduleHandle scheduleHandle, Func<Task> run)
        {
            return scheduleHandle.Task(null, async _cancellationToken =>
            {
                await run();
                return TaskulerTaskResponse.Continue();
            });
        }
    }
}
