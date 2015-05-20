using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Nerven.Taskuler
{
    [PublicAPI]
    public interface ITaskulerWorker
    {
        IEnumerable<ITaskulerScheduleHandle> GetSchedules();

        ITaskulerScheduleHandle Use(ITaskulerSchedule schedule);

        void Start();

        Task StopAsync();
    }
}
