using System;
using JetBrains.Annotations;

namespace Nerven.Taskuler
{
    [PublicAPI]
    public class TaskulerTaskResponse
    {
        private TaskulerTaskResponse(bool continueScheduling, Exception error)
        {
            ContinueScheduling = continueScheduling;
            Error = error;
        }

        public bool ContinueScheduling { get; }

        public Exception Error { get; }

        public static TaskulerTaskResponse Continue(
            Exception error = null)
        {
            return new TaskulerTaskResponse(true, error);
        }

        public static TaskulerTaskResponse Stop(
            Exception error = null)
        {
            return new TaskulerTaskResponse(false, error);
        }
    }
}
