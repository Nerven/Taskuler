﻿using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Nerven.Taskuler
{
    [PublicAPI]
    public class TaskulerScheduleResponse
    {
        private static readonly IReadOnlyCollection<TaskulerTaskContext> _EmptyScheduledOccurrences = new List<TaskulerTaskContext>().AsReadOnly();

        private TaskulerScheduleResponse(
            TaskulerTaskContext[] scheduledOccurrences,
            bool finished,
            Exception error)
        {
            ScheduledOccurrences = scheduledOccurrences == null || scheduledOccurrences.Length == 0 ?
                _EmptyScheduledOccurrences :
                new List<TaskulerTaskContext>(scheduledOccurrences).AsReadOnly();
            Finished = finished;
            Error = error;
        }

        public IReadOnlyCollection<TaskulerTaskContext> ScheduledOccurrences { get; set; }

        public bool Finished { get; set; }

        public Exception Error { get; }

        public static TaskulerScheduleResponse Perform(
            TaskulerTaskContext[] scheduledOccurrences,
            bool finished = false,
            Exception error = null)
        {
            return new TaskulerScheduleResponse(scheduledOccurrences, finished, error);
        }

        public static TaskulerScheduleResponse Perform(
            TaskulerTaskContext scheduledOccurrence,
            bool finished = false,
            Exception error = null)
        {
            return new TaskulerScheduleResponse(new[] { scheduledOccurrence }, finished, error);
        }

        public static TaskulerScheduleResponse Wait(
            bool finished = false,
            Exception error = null)
        {
            return new TaskulerScheduleResponse(null, finished, error);
        }
    }
}
