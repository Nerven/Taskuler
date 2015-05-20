using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Nerven.Assertion;

namespace Nerven.Taskuler.Core
{
    [PublicAPI]
    public sealed class TaskulerWorker : ITaskulerWorker, IDisposable
    {
        private readonly TimeSpan _Resolution;
        private readonly ConcurrentDictionary<Guid, _ScheduleHandle> _Schedules;
        private readonly object _CancellationSourceLock;
        private CancellationTokenSource _CancellationSource;

        private bool _Disposed;

        private TaskulerWorker(
            TimeSpan resolution)
        {
            _Resolution = resolution;
            _Schedules = new ConcurrentDictionary<Guid, _ScheduleHandle>();
            _CancellationSourceLock = new object();
        }

        public static TimeSpan DefaultResolution { get; } = TimeSpan.FromSeconds(1);

        public static ITaskulerWorker Create(
            TimeSpan? resolution = null)
        {
            return new TaskulerWorker(
                resolution ?? DefaultResolution);
        }

        public IEnumerable<ITaskulerScheduleHandle> GetSchedules()
        {
            return _Schedules.ToArray().Select(_schedule => _schedule.Value);
        }

        public ITaskulerScheduleHandle Use(ITaskulerSchedule schedule)
        {
            _ScheduleHandle _scheduleHandle;
            do
            {
                _scheduleHandle = new _ScheduleHandle(this, schedule);
            }
            while (!_Schedules.TryAdd(_scheduleHandle.Key, _scheduleHandle));

            return _scheduleHandle;
        }

        public void Start()
        {
            CancellationTokenSource _cancellationSource;
            lock (_CancellationSourceLock)
            {
                Must
                    .Assert<InvalidOperationException>(_CancellationSource == null)
                    .Assert<InvalidOperationException>(!_Disposed);

                _cancellationSource = _CancellationSource = new CancellationTokenSource();
            }

            var _tickLock = new object();

            Func<bool> _onDone = () =>
            {
                lock (_CancellationSourceLock)
                {
                    if (ReferenceEquals(_CancellationSource, _cancellationSource))
                    {
                        _CancellationSource.Dispose();
                        _CancellationSource = _cancellationSource = null;

                        return true;
                    }
                }

                return false;
            };
            Action<long> _onNext = _tick =>
            {
                var _currentTick = DateTimeOffset.Now;
                var _schedules = _Schedules.ToArray();

                lock (_tickLock)
                {
                    foreach (var _schedule in _schedules)
                    {
                        if (_cancellationSource.IsCancellationRequested)
                        {
                            break;
                        }

                        var _scheduleHandle = _schedule.Value;
                        var _tickResponse = _scheduleHandle.Schedule.Tick(_Resolution, _currentTick);

                        switch (_tickResponse)
                        {
                            case TaskulerScheduleAction.Run:
                                var _tasks = _scheduleHandle.Tasks.ToArray();

                                foreach (var _task in _tasks)
                                {
                                    _task.Value.Run(_cancellationSource.Token);
                                }
                                break;
                            case TaskulerScheduleAction.NoAction:
                                break;
                            case TaskulerScheduleAction.CancelSchedule:
                                throw new NotSupportedException();
                            default:
                                throw new NotSupportedException();
                        }
                    }
                }
            };
            Action _onCompleted = () =>
            {
                _onDone();
            };
            Action<Exception> _onError = _error =>
            {
                if (_onDone())
                {
                }
            };

            _cancellationSource.Token.Register(() => _onDone());
            Observable.Interval(_Resolution).Subscribe(_onNext, _onError, _onCompleted, _cancellationSource.Token);
        }

        public async Task StopAsync()
        {
            _CancellationSource?.Cancel();
            while (
                _CancellationSource != null ||
                _Schedules.ToArray().SelectMany(_schedule => _schedule.Value.Tasks.ToArray()).ToArray().Any(_task => !_task.Value.Instances.IsEmpty))
            {
                await Task.Delay(2);
            }
        }

        public void Dispose()
        {
            if (!_Disposed)
            {
                _Disposed = true;
                _CancellationSource?.Dispose();
            }
        }

        private sealed class _ScheduleHandle : ITaskulerScheduleHandle
        {
            private readonly TaskulerWorker _Worker;

            public _ScheduleHandle(TaskulerWorker worker, ITaskulerSchedule schedule)
            {
                _Worker = worker;
                Schedule = schedule;

                Key = Guid.NewGuid();
                Tasks = new ConcurrentDictionary<Guid, _TaskHandle>();
            }

            public ITaskulerSchedule Schedule { get; }

            public Guid Key { get; }

            public ConcurrentDictionary<Guid, _TaskHandle> Tasks { get; }

            public IEnumerable<ITaskulerTaskHandle> GetTasks()
            {
                return Tasks.ToArray().Select(_task => _task.Value);
            }

            public ITaskulerTaskHandle Task(string taskName, Func<CancellationToken, Task<TaskulerTaskResponse>> run)
            {
                _TaskHandle _taskHandle;
                do
                {
                    _taskHandle = new _TaskHandle(_Worker, this, taskName, run);
                }
                while (!Tasks.TryAdd(_taskHandle.Key, _taskHandle));

                return _taskHandle;
            }
        }

        private sealed class _TaskHandle : ITaskulerTaskHandle
        {
            private readonly TaskulerWorker _Worker;
            private readonly _ScheduleHandle _ScheduleHandle;
            private readonly Func<CancellationToken, Task<TaskulerTaskResponse>> _Run;

            public _TaskHandle(TaskulerWorker worker, _ScheduleHandle scheduleHandle, string taskName, Func<CancellationToken, Task<TaskulerTaskResponse>> run)
            {
                _Worker = worker;
                _ScheduleHandle = scheduleHandle;
                TaskName = taskName;
                _Run = run;

                Key = Guid.NewGuid();
                Instances = new ConcurrentDictionary<Guid, Task>();
            }

            public ITaskulerScheduleHandle ScheduleHandle => _ScheduleHandle;

            public string TaskName { get; }

            public Guid Key { get; }

            public ConcurrentDictionary<Guid, Task> Instances { get; }

            public void RunManually()
            {
                var _cancellationSource = _Worker._CancellationSource;

                Must.Assert(_cancellationSource != null);

                Run(_cancellationSource.Token);
            }

            public void Run(CancellationToken cancellationToken)
            {
                var _taskInstance = Task.Run(() => _Run(cancellationToken), cancellationToken);

                Guid _taskInstanceKey;
                do
                {
                    _taskInstanceKey = Guid.NewGuid();
                }
                while (!Instances.TryAdd(_taskInstanceKey, _taskInstance));

                _taskInstance.GetAwaiter().OnCompleted(() =>
                {
                    Task _taskInstance2;
                    Instances.TryRemove(_taskInstanceKey, out _taskInstance2);

                    var _response = _taskInstance.Result;
                    if (!_response.ContinueScheduling)
                    {
                        _TaskHandle _taskHandle2;
                        _ScheduleHandle.Tasks.TryRemove(Key, out _taskHandle2);
                    }

                    _taskInstance.Dispose();
                });
            }
        }
    }
}
