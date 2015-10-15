using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Nerven.Taskuler
{
    [PublicAPI]
    public static class TaskulerScheduleHandleExtensions
    {
        public static ITaskulerTaskHandle AddTask(this ITaskulerScheduleHandle scheduleHandle, string taskName, Func<TaskulerTaskContext, Task<TaskulerTaskResponse>> run)
        {
            return scheduleHandle.AddTask(taskName, (_context, _cancellationToken) => run(_context));
        }

        public static ITaskulerTaskHandle AddTask(this ITaskulerScheduleHandle scheduleHandle, string taskName, Func<CancellationToken, Task<TaskulerTaskResponse>> run)
        {
            return scheduleHandle.AddTask(taskName, (_context, _cancellationToken) => run(_cancellationToken));
        }

        public static ITaskulerTaskHandle AddTask(this ITaskulerScheduleHandle scheduleHandle, string taskName, Func<Task<TaskulerTaskResponse>> run)
        {
            return scheduleHandle.AddTask(taskName, (_context, _cancellationToken) => run());
        }

        public static ITaskulerTaskHandle AddTask(this ITaskulerScheduleHandle scheduleHandle, string taskName, Func<TaskulerTaskContext, CancellationToken, Task> run)
        {
            return scheduleHandle.AddTask(taskName, async (_context, _cancellationToken) =>
                {
                    await run(_context, _cancellationToken);
                    return TaskulerTaskResponse.Continue();
                });
        }

        public static ITaskulerTaskHandle AddTask(this ITaskulerScheduleHandle scheduleHandle, string taskName, Func<TaskulerTaskContext, Task> run)
        {
            return scheduleHandle.AddTask(taskName, async (_context, _cancellationToken) =>
                {
                    await run(_context);
                    return TaskulerTaskResponse.Continue();
                });
        }

        public static ITaskulerTaskHandle AddTask(this ITaskulerScheduleHandle scheduleHandle, string taskName, Func<CancellationToken, Task> run)
        {
            return scheduleHandle.AddTask(taskName, async (_context, _cancellationToken) =>
                {
                    await run(_cancellationToken);
                    return TaskulerTaskResponse.Continue();
                });
        }

        public static ITaskulerTaskHandle AddTask(this ITaskulerScheduleHandle scheduleHandle, string taskName, Func<Task> run)
        {
            return scheduleHandle.AddTask(taskName, async (_context, _cancellationToken) =>
                {
                    await run();
                    return TaskulerTaskResponse.Continue();
                });
        }

        public static ITaskulerTaskHandle AddTask(this ITaskulerScheduleHandle scheduleHandle, Func<TaskulerTaskContext, CancellationToken, Task<TaskulerTaskResponse>> run)
        {
            return scheduleHandle.AddTask(null, run);
        }

        public static ITaskulerTaskHandle AddTask(this ITaskulerScheduleHandle scheduleHandle, Func<TaskulerTaskContext, Task<TaskulerTaskResponse>> run)
        {
            return scheduleHandle.AddTask(null, (_context, _cancellationToken) => run(_context));
        }

        public static ITaskulerTaskHandle AddTask(this ITaskulerScheduleHandle scheduleHandle, Func<CancellationToken, Task<TaskulerTaskResponse>> run)
        {
            return scheduleHandle.AddTask(null, (_context, _cancellationToken) => run(_cancellationToken));
        }

        public static ITaskulerTaskHandle AddTask(this ITaskulerScheduleHandle scheduleHandle, Func<Task<TaskulerTaskResponse>> run)
        {
            return scheduleHandle.AddTask(null, (_context, _cancellationToken) => run());
        }

        public static ITaskulerTaskHandle AddTask(this ITaskulerScheduleHandle scheduleHandle, Func<TaskulerTaskContext, CancellationToken, Task> run)
        {
            return scheduleHandle.AddTask(null, async (_context, _cancellationToken) =>
                {
                    await run(_context, _cancellationToken);
                    return TaskulerTaskResponse.Continue();
                });
        }

        public static ITaskulerTaskHandle AddTask(this ITaskulerScheduleHandle scheduleHandle, Func<TaskulerTaskContext, Task> run)
        {
            return scheduleHandle.AddTask(null, async (_context, _cancellationToken) =>
                {
                    await run(_context);
                    return TaskulerTaskResponse.Continue();
                });
        }

        public static ITaskulerTaskHandle AddTask(this ITaskulerScheduleHandle scheduleHandle, Func<CancellationToken, Task> run)
        {
            return scheduleHandle.AddTask(null, async (_context, _cancellationToken) =>
                {
                    await run(_cancellationToken);
                    return TaskulerTaskResponse.Continue();
                });
        }

        public static ITaskulerTaskHandle AddTask(this ITaskulerScheduleHandle scheduleHandle, Func<Task> run)
        {
            return scheduleHandle.AddTask(null, async (_context, _cancellationToken) =>
                {
                    await run();
                    return TaskulerTaskResponse.Continue();
                });
        }
    }
}
