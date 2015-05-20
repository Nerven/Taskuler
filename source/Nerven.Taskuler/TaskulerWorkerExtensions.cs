using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Nerven.Taskuler
{
    [PublicAPI]
    public static class TaskulerWorkerExtensions
    {
        public static IEnumerable<ITaskulerTaskHandle> GetAllTasks(this ITaskulerWorker worker)
        {
            return worker?.GetSchedules().SelectMany(_schedule => _schedule.GetTasks());
        }
    }
}
