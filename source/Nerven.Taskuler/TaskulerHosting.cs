using System;
using JetBrains.Annotations;

namespace Nerven.Taskuler
{
    public static class TaskulerHosting
    {
        private static readonly object _ConfigureHostLock = new object();
        private static bool _ConfiguredHostUsed;
        private static ITaskulerHost _ConfiguredHost;

        private static readonly Lazy<ITaskulerHost> _Host = new Lazy<ITaskulerHost>(() =>
            {
                lock (_ConfigureHostLock)
                {
                    _ConfiguredHostUsed = true;
                    return _ConfiguredHost;
                }
            });

        [PublicAPI]
        public static void Configure(ITaskulerHost host, bool overrideConfigured = true)
        {
            lock (_ConfigureHostLock)
            {
                if (!overrideConfigured && (_ConfiguredHost != null || _ConfiguredHostUsed))
                {
                    return;
                }

                if (_ConfiguredHostUsed)
                {
                    throw new InvalidOperationException();
                }

                _ConfiguredHost = host;
            }
        }

        [PublicAPI]
        public static ITaskulerWorker CreateHostedWorker(ITaskulerWorkerFactory workerFactory = null)
        {
            var _host = _Host.Value;
            if (_host == null)
            {
                throw new InvalidOperationException($"No {nameof(ITaskulerHost)} is configured.");
            }

            return _host.CreateHostedWorker(workerFactory);
        }

        [PublicAPI]
        public static ITaskulerWorker TryCreateHostedWorker(ITaskulerWorkerFactory workerFactory = null)
        {
            var _host = _Host.Value;
            return _host?.CreateHostedWorker(workerFactory);
        }
    }
}
