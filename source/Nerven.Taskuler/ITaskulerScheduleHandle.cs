using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Nerven.Taskuler
{
    [PublicAPI]
    public interface ITaskulerScheduleHandle
    {
        Guid Key { get; }

        ITaskulerTaskHandle Do(string taskName, Func<CancellationToken, Task<TaskulerTaskResponse>> run);
    }
}
