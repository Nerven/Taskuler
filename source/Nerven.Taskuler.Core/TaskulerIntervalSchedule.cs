using System;
using JetBrains.Annotations;
using Nerven.Assertion;

namespace Nerven.Taskuler.Core
{
    [PublicAPI]
    public sealed class TaskulerIntervalSchedule : TaskulerScheduleBase
    {
        private readonly TimeSpan _Interval;
        private readonly bool _SkipTicks;

        private TaskulerIntervalSchedule(TimeSpan interval, bool skipTicks)
        {
            _Interval = interval;
            _SkipTicks = skipTicks;
        }

        public static ITaskulerSchedule Create(TimeSpan interval, bool skipTicks = false)
        {
            Must.Assertion.Assert<ArgumentOutOfRangeException>(interval > TimeSpan.Zero);

            return new TaskulerIntervalSchedule(interval, skipTicks);
        }

        public override TaskulerScheduleResponse Tick(TimeSpan resolution, DateTimeOffset epoch, TimeSpan? prevDuration, TimeSpan nextDuration)
        {
            Must.Assertion.Assert(resolution <= _Interval);

            if (_SkipTicks)
            {
                if (!prevDuration.HasValue || nextDuration.Subtract(prevDuration.Value) >= _Interval)
                {
                    return TaskulerScheduleResponse.Perform(new TaskulerTaskContext(epoch, nextDuration));
                }

                return TaskulerScheduleResponse.Wait();
            }

            var _intervalTicks = (double)_Interval.Ticks;
            var _earlierOccurrences = prevDuration.HasValue ? (long)Math.Floor(prevDuration.Value.Ticks / _intervalTicks) + 1 : 0;
            var _expectedOccurrences = (long)Math.Floor(nextDuration.Ticks / _intervalTicks) + 1;
            
            var _occurrencesSinceLastTick = _expectedOccurrences - _earlierOccurrences;
            if (_occurrencesSinceLastTick <= 0)
            {
                return TaskulerScheduleResponse.Wait();
            }

            var _taskContexts = new TaskulerTaskContext[_occurrencesSinceLastTick];
            for (var _i = 0; _i < _occurrencesSinceLastTick; _i++)
            {
                _taskContexts[_i] = new TaskulerTaskContext(epoch, TimeSpan.FromTicks(_Interval.Ticks * (_earlierOccurrences + _i)));
            }
            
            return TaskulerScheduleResponse.Perform(_taskContexts);
        }
    }
}
