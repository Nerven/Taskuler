using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Nerven.Taskuler
{
    [PublicAPI]
    public interface ITaskulerWorker
    {
        bool IsRunning { get; }

        IEnumerable<ITaskulerScheduleHandle> GetSchedules();

        ITaskulerScheduleHandle AddSchedule(string scheduleName, ITaskulerSchedule schedule);

        Task StartAsync();

        Task StopAsync();
    }
}
 