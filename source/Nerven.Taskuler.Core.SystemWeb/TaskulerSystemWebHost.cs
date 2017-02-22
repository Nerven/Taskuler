using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Hosting;
using JetBrains.Annotations;

namespace Nerven.Taskuler.Core.SystemWeb
{
    public sealed class TaskulerSystemWebHost : ITaskulerHost
    {
        private readonly ITaskulerWorkerFactory _DefaultFactory;

        private TaskulerSystemWebHost(ITaskulerWorkerFactory defaultFactory = null)
        {
            _DefaultFactory = defaultFactory ?? TaskulerWorker.Factory();
        }

        [PublicAPI]
        public static ITaskulerHost Default { get; } = new TaskulerSystemWebHost();

        [PublicAPI]
        public static ITaskulerHost Create(ITaskulerWorkerFactory defaultFactory = null) => new TaskulerSystemWebHost(defaultFactory);

        [PublicAPI]
        public static void ConfigureHosting(ITaskulerWorkerFactory defaultFactory = null) => TaskulerHosting.Configure(Create(defaultFactory));

        public ITaskulerWorker CreateHostedWorker(ITaskulerWorkerFactory workerFactory = null)
        {
            var _factory = workerFactory ?? _DefaultFactory;
            ITaskulerWorker _worker = new _TaskulerWorkerWrapper(_factory.Create());
            return _worker;
        }

        private sealed class _TaskulerWorkerWrapper : ITaskulerWorker
        {
            private readonly ITaskulerWorker _WrappedWorker;

            public _TaskulerWorkerWrapper(ITaskulerWorker wrappedWorker)
            {
                _WrappedWorker = wrappedWorker;
            }

            public bool IsRunning => _WrappedWorker.IsRunning;

            public IObservable<TaskulerNotification> NotificationsSource => _WrappedWorker.NotificationsSource;

            public IEnumerable<ITaskulerScheduleHandle> GetSchedules() => _WrappedWorker.GetSchedules();

            public ITaskulerScheduleHandle AddSchedule(string scheduleName, ITaskulerSchedule schedule) => _WrappedWorker.AddSchedule(scheduleName, schedule);

            public Task StartAsync(CancellationToken cancellationToken)
            {
                return Task.Run(async () =>
                    {
                        var _startedCompletionSource = new TaskCompletionSource<int>();
                        await _WrappedWorker.StartAsync(cancellationToken).ConfigureAwait(false);
                        
                        HostingEnvironment.QueueBackgroundWorkItem(async _cancellationToken =>
                            {
                                try
                                {
                                    using (_cancellationToken.Register(() => _WrappedWorker.StopAsync()))
                                    {
                                        _startedCompletionSource.SetResult(0);
                                        await _WrappedWorker.WaitAsync().ConfigureAwait(false);
                                    }
                                }
                                catch (Exception _error)
                                {
                                    if (!_startedCompletionSource.TrySetException(_error))
                                    {
                                        throw;
                                    }
                                }
                            });

                        await _startedCompletionSource.Task.ConfigureAwait(false);
                    }, cancellationToken);
            }

            public Task StopAsync() => _WrappedWorker.StopAsync();

            public Task WaitAsync() => _WrappedWorker.WaitAsync();
        }
    }
}
