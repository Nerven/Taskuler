using System.Collections.Generic;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;

namespace Nerven.Taskuler
{
    [PublicAPI]
    public static class TaskulerWorkerExtensions
    {
        public static void Start(this ITaskulerWorker worker, CancellationToken cancellationToken = default(CancellationToken))
        {
            worker.StartAsync(cancellationToken);
        }

        public static ITaskulerScheduleHandle AddSchedule(this ITaskulerWorker worker, ITaskulerSchedule schedule)
        {
            return worker?.AddSchedule(null, schedule);
        }

        public static IEnumerable<ITaskulerTaskHandle> GetAllTasks(this ITaskulerWorker worker)
        {
            return worker?.GetSchedules().SelectMany(_schedule => _schedule.GetTasks());
        }
    }
}
