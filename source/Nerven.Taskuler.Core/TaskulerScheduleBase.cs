using System;
using JetBrains.Annotations;
using Nerven.Assertion;

namespace Nerven.Taskuler.Core
{
    [PublicAPI]
    public abstract class TaskulerScheduleBase : ITaskulerSchedule
    {
        private readonly object _TickLock;
        private bool _FirstTicked;

        protected TaskulerScheduleBase()
        {
            _TickLock = new object();
        }

        protected static TaskulerScheduleAction Run => TaskulerScheduleAction.Run;

        protected static TaskulerScheduleAction NoAction => TaskulerScheduleAction.NoAction;

        protected static TaskulerScheduleAction CancelSchedule => TaskulerScheduleAction.CancelSchedule;

        protected TimeSpan Resolution { get; private set; }

        protected DateTimeOffset FirstTick { get; private set; }

        protected DateTimeOffset LastTick { get; private set; }

        protected DateTimeOffset LastRunActionTick { get; private set; }

        protected DateTimeOffset CurrentTick { get; private set; }

        protected DateTimeOffset ExpectedNextTick { get; private set; }

        public TaskulerScheduleAction Tick(TimeSpan resolution, DateTimeOffset current)
        {
            lock (_TickLock)
            {
                if (!_FirstTicked)
                {
                    _FirstTicked = true;

                    Resolution = resolution;
                    FirstTick = current;
                }

                Must.Assert(Resolution == resolution);

                LastTick = CurrentTick;
                CurrentTick = current;
                ExpectedNextTick = CurrentTick.Add(resolution);

                var _action = Tick();
                if (_action == TaskulerScheduleAction.Run)
                {
                    LastRunActionTick = CurrentTick;
                }

                return _action;
            }
        }

        protected abstract TaskulerScheduleAction Tick();
    }
}
