using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Nerven.Taskuler
{
    [PublicAPI]
    public interface ITaskulerWorker
    {
        ITaskulerScheduleHandle Use(ITaskulerSchedule schedule);

        void Start();

        Task StopAsync();
    }
}
