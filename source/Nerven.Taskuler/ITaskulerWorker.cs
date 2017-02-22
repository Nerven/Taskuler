using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Nerven.Taskuler
{
    [PublicAPI]
    public interface ITaskulerWorker
    {
        bool IsRunning { get; }

        IObservable<TaskulerNotification> NotificationsSource { get; }
            
        IEnumerable<ITaskulerScheduleHandle> GetSchedules();

        ITaskulerScheduleHandle AddSchedule(string scheduleName, ITaskulerSchedule schedule);

        Task StartAsync(CancellationToken cancellationToken);

        Task StopAsync();

        Task WaitAsync();
    }
}
