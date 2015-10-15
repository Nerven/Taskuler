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
            Must.Assert<ArgumentOutOfRangeException>(interval > TimeSpan.Zero);

            return new TaskulerIntervalSchedule(interval, skipTicks);
        }

        public override TaskulerScheduleResponse Tick(TimeSpan resolution, DateTimeOffset firstTick, TimeSpan? lastTick, TimeSpan currentTick)
        {
            Must.Assert(resolution <= _Interval);

            if (_SkipTicks)
            {
                if (!lastTick.HasValue || currentTick.Subtract(lastTick.Value) >= _Interval)
                {
                    return TaskulerScheduleResponse.Perform(new TaskulerTaskContext(firstTick, currentTick));
                }

                return TaskulerScheduleResponse.Wait();
            }

            var _intervalTicks = (double)_Interval.Ticks;
            var _earlierOccurrences = lastTick.HasValue ? (long)Math.Floor(lastTick.Value.Ticks / _intervalTicks) + 1 : 0;
            var _expectedOccurrences = (long)Math.Floor(currentTick.Ticks / _intervalTicks) + 1;
            
            var _occurrencesSinceLastTick = _expectedOccurrences - _earlierOccurrences;
            if (_occurrencesSinceLastTick <= 0)
            {
                return TaskulerScheduleResponse.Wait();
            }

            var _taskContexts = new TaskulerTaskContext[_occurrencesSinceLastTick];
            for (var _i = 0; _i < _occurrencesSinceLastTick; _i++)
            {
                _taskContexts[_i] = new TaskulerTaskContext(firstTick, TimeSpan.FromTicks(_Interval.Ticks * (_earlierOccurrences + _i)));
            }
            
            return TaskulerScheduleResponse.Perform(_taskContexts);
        }
    }
}
