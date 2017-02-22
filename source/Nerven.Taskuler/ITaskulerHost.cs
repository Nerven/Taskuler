namespace Nerven.Taskuler
{
    public interface ITaskulerHost
    {
        ITaskulerWorker CreateHostedWorker(ITaskulerWorkerFactory workerFactory = null);
    }
}
