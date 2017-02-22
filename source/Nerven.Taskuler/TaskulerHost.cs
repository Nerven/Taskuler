using System;
using System.Threading;
using JetBrains.Annotations;

namespace Nerven.Taskuler
{
    public sealed class TaskulerHost : ITaskulerHost
    {
        private readonly Func<ITaskulerWorkerFactory, ITaskulerWorker> _HostWorker;
        private readonly CancellationToken _CancellationToken;

        private TaskulerHost(Func<ITaskulerWorkerFactory, ITaskulerWorker> hostWorker, CancellationToken cancellationToken)
        {
            _HostWorker = hostWorker;
            _CancellationToken = cancellationToken;
        }

        [PublicAPI]
        public static ITaskulerHost Create(Func<ITaskulerWorkerFactory, ITaskulerWorker> hostWorker, CancellationToken cancellationToken = default(CancellationToken))
        {
            return new TaskulerHost(hostWorker, cancellationToken);
        }

        public ITaskulerWorker CreateHostedWorker(ITaskulerWorkerFactory workerFactory = null)
        {
            //// ReSharper disable once ImpureMethodCallOnReadonlyValueField
            _CancellationToken.ThrowIfCancellationRequested();
            return _HostWorker(workerFactory);
        }
    }
}
