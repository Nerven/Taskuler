using System;

namespace Nerven.Taskuler
{
    public sealed class TaskulerWorkerFactory : ITaskulerWorkerFactory
    {
        private readonly Func<ITaskulerWorker> _Create;

        private TaskulerWorkerFactory(Func<ITaskulerWorker> create)
        {
            _Create = create;
        }

        public static ITaskulerWorkerFactory Create(Func<ITaskulerWorker> create) => new TaskulerWorkerFactory(create);

        public ITaskulerWorker Create()
        {
            return _Create();
        }
    }
}
