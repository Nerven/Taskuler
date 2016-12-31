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

        public override TaskulerScheduleResponse Tick(TimeSpan resolution, DateTimeOffset firstTick, TimeSpan? lastTick, TimeSpan currentTick)
        {
            Must.Assertion.Assert(resolution <= TimeSpan.FromHours(6));

            return (!lastTick.HasValue || firstTick.Add(lastTick.Value).TimeOfDay < _TimeOfDay) && firstTick.Add(currentTick).TimeOfDay >= _TimeOfDay ?
                TaskulerScheduleResponse.Perform(new TaskulerTaskContext(firstTick, firstTick.Add(currentTick).Subtract(firstTick.Add(currentTick).TimeOfDay).Add(_TimeOfDay).Subtract(firstTick))) :
                TaskulerScheduleResponse.Wait();
        }
    }
}
