using System;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Nerven.Taskuler
{
    [PublicAPI]
    public interface ITaskulerTaskHandle
    {
        ITaskulerScheduleHandle ScheduleHandle { get; }

        Guid Key { get; }

        string TaskName { get; }

        Task RunManuallyAsync();

        void Remove();
    }
}
