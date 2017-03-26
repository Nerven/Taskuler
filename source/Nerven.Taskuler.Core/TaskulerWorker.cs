using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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
        private readonly Func<DateTimeOffset, TimeSpan> _GetTimestamp;
        private readonly int _MaxSimultaneousTasks;
        private readonly ConcurrentDictionary<Guid, _ScheduleHandle> _Schedules;
        private readonly object _StartStopLock;

        private bool _Disposed;
        private bool _Faulted;
        private bool _Running;
        private Task _MainTask;
        private Action _RequestCancellation;
        private SemaphoreSlim _StopSignal;
        private SemaphoreSlim _ActiveTasks;
        private ConcurrentBag<_TaskWorkHandle> _TaskWorkHandles;
        private CancellationToken _CancellationToken;
        private DateTimeOffset _Epoch;
        private TimeSpan? _PrevDuration;
        private Subject<TaskulerNotification> _Notifications;

        private TaskulerWorker(
            TimeSpan resolution,
            Func<DateTimeOffset, TimeSpan> getTimestamp,
            int maxSimultaneousTasks)
        {
            _Resolution = resolution;
            _GetTimestamp = getTimestamp;
            _MaxSimultaneousTasks = maxSimultaneousTasks;
            _Schedules = new ConcurrentDictionary<Guid, _ScheduleHandle>();
            _StartStopLock = new object();

            _Notifications = new Subject<TaskulerNotification>();
            NotificationsSource = _Notifications.AsObservable();
        }

        public static TimeSpan DefaultResolution { get; } = TimeSpan.FromSeconds(1);

        public static int DefaultMaxSimultaneousTasks { get; } = 50;

        public bool IsRunning => !_Faulted && _Running;

        public IObservable<TaskulerNotification> NotificationsSource { get; }

        public static ITaskulerWorker Create(
            TimeSpan? resolution = null,
            Func<DateTimeOffset, TimeSpan> getTimestamp = null,
            int? maxSimultaneousTasks = null)
        {
            return new TaskulerWorker(
                resolution ?? DefaultResolution,
                getTimestamp ?? (_firstTick => DateTimeOffset.Now.Subtract(_firstTick)),
                maxSimultaneousTasks ?? DefaultMaxSimultaneousTasks);
        }

        public static void ConfigureHosting(Action<ITaskulerWorker> attach = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var _factory = Factory();
            TaskulerHosting.Configure(TaskulerHost.Create(_workerFactory =>
                {
                    var _worker = (_workerFactory ?? _factory).Create();
                    attach?.Invoke(_worker);
                    return _worker;
                }, cancellationToken));
        }

        public static IDisposable ConfigureStoppableHosting()
        {
            var _cancellationTokenSource = new CancellationTokenSource();
            var _workers = new ConcurrentBag<ITaskulerWorker>();
                
            ConfigureHosting(_workers.Add, _cancellationTokenSource.Token);

            return Disposable.Create(() =>
                {
                    _cancellationTokenSource.Cancel();
                    Task.WaitAll(_workers.ToArray().Select(_worker => _worker.StopAsync()).ToArray());
                    _cancellationTokenSource.Dispose();
                });
        }

        public static ITaskulerWorkerFactory Factory(
            TimeSpan? resolution = null,
            Func<DateTimeOffset, TimeSpan> getTimestamp = null,
            int? maxSimultaneousTasks = null)
        {
            return TaskulerWorkerFactory.Create(() => Create(resolution, getTimestamp, maxSimultaneousTasks));
        }

        public IEnumerable<ITaskulerScheduleHandle> GetSchedules()
        {
            return _Schedules.ToArray().Select(_schedule => _schedule.Value);
        }

        public ITaskulerScheduleHandle AddSchedule(string scheduleName, ITaskulerSchedule schedule)
        {
            _ScheduleHandle _scheduleHandle;
            do
            {
                _scheduleHandle = new _ScheduleHandle(this, schedule, scheduleName);
            }
            while (!_Schedules.TryAdd(_scheduleHandle.Key, _scheduleHandle));

            return _scheduleHandle;
        }

        public Task StartAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var _startedCompletionSource = new TaskCompletionSource<int>();
            var _completionSource = new TaskCompletionSource<int>();

            //// ReSharper disable once RedundantAssignment
            var _mainTask = Task.Run(async () =>
                {
                    try
                    {
                        //// ReSharper disable AccessToDisposedClosure
                        using (var _cancellationTokenSource = new CancellationTokenSource())
                        using (_cancellationTokenSource.Token.Register(() => _Guarded(() => _StopSignal.Release(), $"Release {nameof(_StopSignal)} on cancellation.")))
                        {
                            //// ReSharper disable once AccessToModifiedClosure
                            _Setup(_completionSource.Task, _cancellationTokenSource);
                            _Start();
                            _startedCompletionSource.SetResult(0);
                            await _StopSignal.WaitAsync().ConfigureAwait(false);
                            await _Stop().ConfigureAwait(false);
                        }

                        _Teardown(false);
                    }
                    catch (Exception _error)
                    {
                        try
                        {
                            _Faulted = true;
                            _startedCompletionSource.TrySetException(_error);
                            _Teardown(true);
                        }
                        catch (Exception _errorHandlingError)
                        {
                            throw new AggregateException(_error, _errorHandlingError);
                        }

                        throw;
                    }
                }, cancellationToken);

            _mainTask.ContinueWith(_task =>
                {
                    if (_task.IsFaulted)
                    {
                        _completionSource.SetException(_task.Exception);
                    }
                    else
                    {
                        _completionSource.SetResult(0);
                    }
                });

            return _startedCompletionSource.Task;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            await StartAsync(cancellationToken).ConfigureAwait(false);

            using (cancellationToken.Register(() => Task.Run(StopAsync)))
            {
                await WaitAsync().ConfigureAwait(false);
            }
        }

        public Task StopAsync()
        {
            lock (_StartStopLock)
            {
                Must.Assertion
                    .Assert<InvalidOperationException>(!_Disposed)
                    .Assert<InvalidOperationException>(_MainTask != null);

                _RequestCancellation?.Invoke();
                return _MainTask;
            }
        }

        public async Task WaitAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            Task _mainTask;
            lock (_StartStopLock)
            {
                Must.Assertion
                    .Assert<InvalidOperationException>(!_Disposed)
                    .Assert<InvalidOperationException>(_MainTask != null);

                _mainTask = _MainTask;
            }

            var _cancellationTaskSource = new TaskCompletionSource<int>();
            using (cancellationToken.Register(() => _cancellationTaskSource.SetCanceled()))
            {
                await Task.WhenAny(_cancellationTaskSource.Task, _mainTask).ConfigureAwait(false);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "_StopSignal")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "_ActiveTasks")]
        public void Dispose()
        {
            if (!_Disposed)
            {
                _Disposed = true;
                _StopSignal?.Dispose();
                _ActiveTasks?.Dispose();
                _Notifications.Dispose();

                var _taskWorkHandles = _TaskWorkHandles?.ToArray();
                if (_taskWorkHandles != null)
                {
                    foreach (var _taskWorkHandle in _taskWorkHandles)
                    {
                        _taskWorkHandle.Dispose();
                    }
                }
            }
        }

        private void _Setup(Task mainTask, CancellationTokenSource cancellationTokenSource)
        {
            var _startedAt = DateTimeOffset.MinValue.Add(_GetTimestamp(DateTimeOffset.MinValue));

            lock (_StartStopLock)
            {
                Must.Assertion
                    .Assert<InvalidOperationException>(!_Disposed)
                    .Assert<InvalidOperationException>(!_Faulted)
                    .Assert<InvalidOperationException>(!_Running)
                    .Assert<InvalidOperationException>(_TaskWorkHandles == null)
                    .Assume(() => mainTask != null);

                _MainTask = mainTask;
                _RequestCancellation = cancellationTokenSource.Cancel;
                _StopSignal?.Dispose();
                _StopSignal = new SemaphoreSlim(0, 1);
                _ActiveTasks?.Dispose();
                _ActiveTasks = new SemaphoreSlim(_MaxSimultaneousTasks, _MaxSimultaneousTasks);
                _Running = true;

                _CancellationToken = cancellationTokenSource.Token;

                _PrevDuration = null;
                _Epoch = _startedAt;

                _TaskWorkHandles = new ConcurrentBag<_TaskWorkHandle>();
                foreach (var _scheduleHandle in _Schedules.Values)
                {
                    foreach (var _taskHandle in _scheduleHandle._Tasks.Values)
                    {
                        var _taskWorkHandle = new _TaskWorkHandle(this, _taskHandle);
                        _TaskWorkHandles.Add(_taskWorkHandle);
                        _scheduleHandle._TaskWorkHandles.TryAdd(_taskHandle.Key, _taskWorkHandle);
                    }
                }
            }
        }

        private void _Start()
        {
            var _cancellationToken = _CancellationToken;
            var _tickLock = new object();
            Action<long> _onNext = _tickNotUsed => _OnNext(_tickLock, _cancellationToken);

            Observable.Interval(_Resolution).Concat(Observable.Return(0L))
                .Subscribe(_onNext, _PrepareEscalateError($"{nameof(Observable)} subscription"), _cancellationToken);
        }

        private void _OnNext(object tickLock, CancellationToken cancellationToken)
        {
            var _epoch = _Epoch;
            var _nextDuration = _GetTimestamp(_epoch);
            lock (tickLock)
            {
                var _schedules = _Schedules.ToArray();
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var _prevDuration = _PrevDuration;

                foreach (var _schedule in _schedules)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    var _scheduleHandle = _schedule.Value;
                    var _tickResponse = _scheduleHandle.Schedule.Tick(_Resolution, _epoch, _prevDuration, _nextDuration);

                    if (_tickResponse.ScheduledOccurrences.Count != 0)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        var _taskWorkHandles = _scheduleHandle._TaskWorkHandles.Values.ToArray();
                        foreach (var _scheduledOccurrence in _tickResponse.ScheduledOccurrences)
                        {
                            foreach (var _taskWorkHandle in _taskWorkHandles)
                            {
                                _taskWorkHandle._RunScheduled(_scheduledOccurrence);
                            }
                        }
                    }

                    if (_tickResponse.Finished)
                    {
                        _ScheduleHandle _removedScheduleHandle;
                        if (!_Schedules.TryRemove(_schedule.Key, out _removedScheduleHandle) ||
                            !ReferenceEquals(_removedScheduleHandle, _schedule.Value))
                        {
                            Must.Assertion.AssertNever();
                        }
                    }
                }

                _PrevDuration = _nextDuration;
            }
        }

        private async Task _Stop()
        {
            if (!_CancellationToken.IsCancellationRequested)
            {
                _RequestCancellation?.Invoke();
                await _StopSignal.WaitAsync().ConfigureAwait(false);
            }

            var _runningTasks = _TaskWorkHandles
                .Select(_taskWorkHandle => _taskWorkHandle._WaitAsync())
                .ToArray();
            await Task.WhenAll(_runningTasks).ConfigureAwait(false);
        }

        private void _Teardown(bool faulted)
        {
            lock (_StartStopLock)
            {
                foreach (var _taskWorkHandle in _TaskWorkHandles)
                {
                    if (!faulted)
                    {
                        _TaskWorkHandle _taskWorkHandle2;
                        _taskWorkHandle._TaskHandle._ScheduleHandle._TaskWorkHandles.TryRemove(_taskWorkHandle._TaskHandle.Key, out _taskWorkHandle2);
                    }

                    _taskWorkHandle.Dispose();
                }

                _TaskWorkHandles = null;
                _RequestCancellation = null;
                _Running = false;
            }
        }

        private void _Guarded(Action action, string description)
        {
            try
            {
                action();
            }
            catch (Exception _error)
            {
                _EscalateError(description, _error);
            }
        }

        private void _EscalateError(string description, Exception exception)
        {
            _Faulted = true;

            var _message = $"{GetType().FullName} failed unexpectedly: {description} resulted in {exception.Message}.";
            _NotifiyError(_message, exception);
            Environment.FailFast(_message, exception);
        }

        private void _NotifiyError(string message, Exception exception)
        {
            _Notifications.OnNext(new TaskulerNotification(DateTimeOffset.UtcNow, message, exception));
        }

        private Action<Exception> _PrepareEscalateError(string description)
        {
            return _exception => _EscalateError(description, _exception);
        }

        private static Task<T> _TypedTask<T>(Task task)
        {
            if (task.IsFaulted)
            {
                return _FaultedTask<T>(task.Exception);
            }

            if (task.IsCanceled)
            {
                return _CanceledTask<T>();
            }

            return Task.FromResult(default(T));
        }

        private static Task<T> _CanceledTask<T>()
        {
            var _completionSource = new TaskCompletionSource<T>();
            _completionSource.SetCanceled();
            return _completionSource.Task;
        }

        private static Task<T> _FaultedTask<T>(Exception exception)
        {
            var _completionSource = new TaskCompletionSource<T>();
            _completionSource.SetException(exception);
            return _completionSource.Task;
        }

        private sealed class _ScheduleHandle : ITaskulerScheduleHandle
        {
            private readonly TaskulerWorker _Worker;

            public _ScheduleHandle(TaskulerWorker worker, ITaskulerSchedule schedule, string scheduleName)
            {
                _Worker = worker;
                Schedule = schedule;
                ScheduleName = scheduleName;

                Key = Guid.NewGuid();
                _Tasks = new ConcurrentDictionary<Guid, _TaskHandle>();
                _TaskWorkHandles = new ConcurrentDictionary<Guid, _TaskWorkHandle>();
            }

            public ITaskulerSchedule Schedule { get; }

            public Guid Key { get; }

            public string ScheduleName { get; }

            internal ConcurrentDictionary<Guid, _TaskHandle> _Tasks { get; }

            internal ConcurrentDictionary<Guid, _TaskWorkHandle> _TaskWorkHandles { get; }

            public IEnumerable<ITaskulerTaskHandle> GetTasks()
            {
                return _Tasks.ToArray().Select(_task => _task.Value);
            }

            public ITaskulerTaskHandle AddTask(string taskName, Func<TaskulerTaskContext, CancellationToken, Task<TaskulerTaskResponse>> run)
            {
                _TaskHandle _taskHandle;
                do
                {
                    _taskHandle = new _TaskHandle(_Worker, this, taskName, run);
                }
                while (!_Tasks.TryAdd(_taskHandle.Key, _taskHandle));

                lock (_Worker._StartStopLock)
                {
                    if (!_Worker._Faulted && _Worker._Running)
                    {
                        var _taskWorkHandler = new _TaskWorkHandle(_Worker, _taskHandle);
                        _Worker._TaskWorkHandles.Add(_taskWorkHandler);
                        _TaskWorkHandles.TryAdd(_taskHandle.Key, _taskWorkHandler);
                    }
                }

                return _taskHandle;
            }
        }

        private sealed class _TaskHandle : ITaskulerTaskHandle
        {
            private readonly TaskulerWorker _Worker;
            private bool _Removed;

            public _TaskHandle(TaskulerWorker worker, _ScheduleHandle scheduleHandle, string taskName, Func<TaskulerTaskContext, CancellationToken, Task<TaskulerTaskResponse>> executeTask)
            {
                _Worker = worker;
                _ScheduleHandle = scheduleHandle;
                TaskName = taskName;
                _ExecuteTask = executeTask;

                Key = Guid.NewGuid();
            }

            public ITaskulerScheduleHandle ScheduleHandle => _ScheduleHandle;

            public string TaskName { get; }

            public Guid Key { get; }

            internal _ScheduleHandle _ScheduleHandle { get; }

            internal Func<TaskulerTaskContext, CancellationToken, Task<TaskulerTaskResponse>> _ExecuteTask { get; }

            public Task RunManuallyAsync()
            {
                var _firstTick = _Worker._Epoch;

                Must.Assertion
                    .Assert(_Worker.IsRunning)
                    .Assert(_ScheduleHandle._Tasks.ContainsKey(Key))
                    .Assert<InvalidOperationException>(!_Removed);

                _TaskWorkHandle _taskWorkHandle;
                if (!_ScheduleHandle._TaskWorkHandles.TryGetValue(Key, out _taskWorkHandle))
                {
                    throw Must.Assertion.AssertNever<InvalidOperationException>();
                }

                return _taskWorkHandle._RunAsync(new TaskulerTaskContext(_firstTick, _Worker._GetTimestamp(_firstTick)));
            }

            public void Remove()
            {
                Must.Assertion
                    .Assert<InvalidOperationException>(!_Removed);

                _Removed = true;
                _TaskHandle _taskHandle;
                _ScheduleHandle._Tasks.TryRemove(Key, out _taskHandle);

                lock (_Worker._StartStopLock)
                {
                    if (!_Worker._Faulted && _Worker._Running)
                    {
                        _TaskWorkHandle _taskWorkHandler;
                        _ScheduleHandle._TaskWorkHandles.TryRemove(Key, out _taskWorkHandler);
                    }
                }
            }
        }

        private sealed class _TaskWorkHandle : IDisposable
        {
            private readonly TaskulerWorker _Worker;
            private readonly CancellationToken _CancellationToken;
            private readonly object _RunningTasksLock;
            private readonly SemaphoreSlim _FinishedSignal;
            private readonly SemaphoreSlim _ActiveTasks;

            private int _RunningTasks;

            public _TaskWorkHandle(TaskulerWorker worker, _TaskHandle taskHandle)
            {
                _Worker = worker;
                _TaskHandle = taskHandle;
                _CancellationToken = _Worker._CancellationToken;
                _ActiveTasks = _Worker._ActiveTasks;
                _RunningTasksLock = new object();
                _FinishedSignal = new SemaphoreSlim(0, 1);
            }

            internal _TaskHandle _TaskHandle { get; }

            public void Dispose()
            {
                _FinishedSignal.Dispose();
            }

            internal void _RunScheduled(TaskulerTaskContext context)
            {
                _RunAsync(context).ContinueWith(_task =>
                    {
                        if (_task.IsFaulted)
                        {
                            var _aggregateException = _task.Exception.Flatten();
                            var _message = _aggregateException.InnerExceptions.Count == 1
                                ? $"Scheduled task failed with error: {_aggregateException.InnerException.Message}"
                                : $"Scheduled task failed with errors: {string.Join(", ", _aggregateException.InnerExceptions.Select(_innerException => _innerException.Message))}";
                            
                            _Worker._NotifiyError(_message, _task.Exception);
                        }

                        return Task.FromResult(0);
                    });
            }

            internal Task _RunAsync(TaskulerTaskContext context)
            {
                var _taskInstance = _Start(context);
                return _taskInstance.ContinueWith(_HandleCompletion).Unwrap();
            }

            internal Task _WaitAsync()
            {
                lock (_RunningTasksLock)
                {
                    Must.Assertion
                        .Assert(_Worker._CancellationToken.IsCancellationRequested);

                    if (_RunningTasks == 0)
                    {
                        return Task.FromResult(0);
                    }

                    return _FinishedSignal.WaitAsync();
                }
            }

            private Task<TaskulerTaskResponse> _Start(TaskulerTaskContext context)
            {
                Task<TaskulerTaskResponse> _taskInstance;
                lock (_RunningTasksLock)
                {
                    if (_CancellationToken.IsCancellationRequested || _FinishedSignal == null)
                    {
                        return _CanceledTask<TaskulerTaskResponse>();
                    }

                    _RunningTasks++;
                    try
                    {
                        _taskInstance =
                            Task.WhenAny(
                                    _ActiveTasks.WaitAsync(_CancellationToken).ContinueWith(_task => true, _CancellationToken),
                                    Task.Delay(30 * 1000, _CancellationToken).ContinueWith(_task => false, _CancellationToken))
                                .Unwrap()
                                .ContinueWith(_task =>
                                    {
                                        if (_task.Status == TaskStatus.RanToCompletion)
                                        {
                                            if (_task.Result)
                                            {
                                                return _TaskHandle._ExecuteTask(context, _CancellationToken);
                                            }

                                            // TODO: Better error message
                                            return _FaultedTask<TaskulerTaskResponse>(new Exception("Deadlock"));
                                        }

                                        return _TypedTask<TaskulerTaskResponse>(_task);
                                    }, _CancellationToken)
                                .Unwrap();
                    }
                    catch
                    {
                        _RunningTasks--;

                        throw;
                    }
                }
                return _taskInstance;
            }

            private Task _HandleCompletion(Task<TaskulerTaskResponse> taskInstance)
            {
                lock (_RunningTasksLock)
                {
                    _RunningTasks--;

                    if (_RunningTasks == 0 && _CancellationToken.IsCancellationRequested)
                    {
                        _FinishedSignal.Release();
                    }
                }

                if (!_CancellationToken.IsCancellationRequested)
                {
                    _ActiveTasks.Release();
                }

                if (taskInstance.Status == TaskStatus.RanToCompletion)
                {
                    var _response = taskInstance.Result;
                    if (_response != null && !_response.ContinueScheduling)
                    {
                        _TaskHandle _taskHandle;
                        _TaskWorkHandle _taskWorkHandle;
                        _TaskHandle._ScheduleHandle._Tasks.TryRemove(_TaskHandle.Key, out _taskHandle);
                        _TaskHandle._ScheduleHandle._TaskWorkHandles.TryRemove(_TaskHandle.Key, out _taskWorkHandle);
                    }

                    if (_response?.Error != null)
                    {
                        return _FaultedTask<int>(_response.Error);
                    }

                    return Task.FromResult(0);
                }

                return taskInstance;
            }
        }
    }
}
