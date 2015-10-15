using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Nerven.Taskuler
{
    [PublicAPI]
    public static class TaskulerTaskHandleExtensions
    {
        public static ITaskulerTaskHandle AddTask(this ITaskulerTaskHandle taskHandle, string taskName, Func<TaskulerTaskContext, CancellationToken, Task<TaskulerTaskResponse>> run)
        {
            return taskHandle.ScheduleHandle.AddTask(taskName, run);
        }

        public static ITaskulerTaskHandle AddTask(this ITaskulerTaskHandle taskHandle, string taskName, Func<TaskulerTaskContext, Task<TaskulerTaskResponse>> run)
        {
            return taskHandle.ScheduleHandle.AddTask(taskName, run);
        }

        public static ITaskulerTaskHandle AddTask(this ITaskulerTaskHandle taskHandle, string taskName, Func<CancellationToken, Task<TaskulerTaskResponse>> run)
        {
            return taskHandle.ScheduleHandle.AddTask(taskName, run);
        }

        public static ITaskulerTaskHandle AddTask(this ITaskulerTaskHandle taskHandle, string taskName, Func<Task<TaskulerTaskResponse>> run)
        {
            return taskHandle.ScheduleHandle.AddTask(taskName, run);
        }

        public static ITaskulerTaskHandle AddTask(this ITaskulerTaskHandle taskHandle, string taskName, Func<TaskulerTaskContext, CancellationToken, Task> run)
        {
            return taskHandle.ScheduleHandle.AddTask(taskName, run);
        }

        public static ITaskulerTaskHandle AddTask(this ITaskulerTaskHandle taskHandle, string taskName, Func<TaskulerTaskContext, Task> run)
        {
            return taskHandle.ScheduleHandle.AddTask(taskName, run);
        }

        public static ITaskulerTaskHandle AddTask(this ITaskulerTaskHandle taskHandle, string taskName, Func<CancellationToken, Task> run)
        {
            return taskHandle.ScheduleHandle.AddTask(taskName, run);
        }

        public static ITaskulerTaskHandle AddTask(this ITaskulerTaskHandle taskHandle, string taskName, Func<Task> run)
        {
            return taskHandle.ScheduleHandle.AddTask(taskName, run);
        }

        public static ITaskulerTaskHandle AddTask(this ITaskulerTaskHandle taskHandle, Func<TaskulerTaskContext, CancellationToken, Task<TaskulerTaskResponse>> run)
        {
            return taskHandle.ScheduleHandle.AddTask(run);
        }

        public static ITaskulerTaskHandle AddTask(this ITaskulerTaskHandle taskHandle, Func<TaskulerTaskContext, Task<TaskulerTaskResponse>> run)
        {
            return taskHandle.ScheduleHandle.AddTask(run);
        }

        public static ITaskulerTaskHandle AddTask(this ITaskulerTaskHandle taskHandle, Func<CancellationToken, Task<TaskulerTaskResponse>> run)
        {
            return taskHandle.ScheduleHandle.AddTask(run);
        }

        public static ITaskulerTaskHandle AddTask(this ITaskulerTaskHandle taskHandle, Func<Task<TaskulerTaskResponse>> run)
        {
            return taskHandle.ScheduleHandle.AddTask(run);
        }

        public static ITaskulerTaskHandle AddTask(this ITaskulerTaskHandle taskHandle, Func<TaskulerTaskContext, CancellationToken, Task> run)
        {
            return taskHandle.ScheduleHandle.AddTask(run);
        }

        public static ITaskulerTaskHandle AddTask(this ITaskulerTaskHandle taskHandle, Func<TaskulerTaskContext, Task> run)
        {
            return taskHandle.ScheduleHandle.AddTask(run);
        }

        public static ITaskulerTaskHandle AddTask(this ITaskulerTaskHandle taskHandle, Func<CancellationToken, Task> run)
        {
            return taskHandle.ScheduleHandle.AddTask(run);
        }

        public static ITaskulerTaskHandle AddTask(this ITaskulerTaskHandle taskHandle, Func<Task> run)
        {
            return taskHandle.ScheduleHandle.AddTask(run);
        }
    }
}
