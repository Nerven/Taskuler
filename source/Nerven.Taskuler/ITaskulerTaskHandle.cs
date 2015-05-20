using System;
using JetBrains.Annotations;

namespace Nerven.Taskuler
{
    [PublicAPI]
    public interface ITaskulerTaskHandle
    {
        ITaskulerScheduleHandle ScheduleHandle { get; }

        Guid Key { get; }

        string TaskName { get; }

        void RunManually();
    }
}
