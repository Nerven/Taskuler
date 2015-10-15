using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Nerven.Taskuler
{
    [PublicAPI]
    public interface ITaskulerScheduleHandle
    {
        Guid Key { get; }

        string ScheduleName { get; }

        IEnumerable<ITaskulerTaskHandle> GetTasks();

        ITaskulerTaskHandle AddTask(string taskName, Func<TaskulerTaskContext, CancellationToken, Task<TaskulerTaskResponse>> run);
    }
}
