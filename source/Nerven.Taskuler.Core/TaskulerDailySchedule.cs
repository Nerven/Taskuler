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
            Must.Assert<ArgumentOutOfRangeException>(timeOfDay >= TimeSpan.Zero && timeOfDay < TimeSpan.FromDays(1));

            return new TaskulerDailySchedule(timeOfDay);
        }

        protected override TaskulerScheduleAction Tick()
        {
            Must.Assert(Resolution <= TimeSpan.FromHours(6));

            return LastRunActionTick.TimeOfDay < _TimeOfDay && CurrentTick.TimeOfDay >= _TimeOfDay ? Run : NoAction;
        }
    }
}
