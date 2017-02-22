using System;
using JetBrains.Annotations;
using Nerven.Assertion;

namespace Nerven.Taskuler.Core
{
    [PublicAPI]
    public sealed class TaskulerDailySchedule : TaskulerScheduleBase
    {
        private readonly TimeSpan _TimeOfDay;

        private TaskulerDailySchedule(TimeSpan timeOfDay)
        {
            _TimeOfDay = timeOfDay;
        }

        public static ITaskulerSchedule Create(TimeSpan timeOfDay)
        {
            Must.Assertion.Assert<ArgumentOutOfRangeException>(timeOfDay >= TimeSpan.Zero && timeOfDay < TimeSpan.FromDays(1));

            return new TaskulerDailySchedule(timeOfDay);
        }

        public override TaskulerScheduleResponse Tick(TimeSpan resolution, DateTimeOffset epoch, TimeSpan? prevDuration, TimeSpan nextDuration)
        {
            Must.Assertion.Assert(resolution <= TimeSpan.FromHours(6));

            return (!prevDuration.HasValue || epoch.Add(prevDuration.Value).TimeOfDay < _TimeOfDay) && epoch.Add(nextDuration).TimeOfDay >= _TimeOfDay ?
                TaskulerScheduleResponse.Perform(new TaskulerTaskContext(epoch, epoch.Add(nextDuration).Subtract(epoch.Add(nextDuration).TimeOfDay).Add(_TimeOfDay).Subtract(epoch))) :
                TaskulerScheduleResponse.Wait();
        }
    }
}
