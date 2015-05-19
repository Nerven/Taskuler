using System;
using JetBrains.Annotations;
using Nerven.Assertion;

namespace Nerven.Taskuler.Core
{
    [PublicAPI]
    public sealed class TaskulerIntervalSchedule : TaskulerScheduleBase
    {
        private readonly TimeSpan _Interval;

        private TaskulerIntervalSchedule(TimeSpan interval)
        {
            _Interval = interval;
        }

        public static ITaskulerSchedule Create(TimeSpan interval)
        {
            Must.Assert<ArgumentOutOfRangeException>(interval > TimeSpan.Zero);

            return new TaskulerIntervalSchedule(interval);
        }

        protected override TaskulerScheduleAction Tick()
        {
            Must.Assert(Resolution <= _Interval);

            return CurrentTick.Subtract(LastRunActionTick) > _Interval ? Run : NoAction;
        }
    }
}
