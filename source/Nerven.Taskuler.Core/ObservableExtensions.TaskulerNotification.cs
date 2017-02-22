using System;
using JetBrains.Annotations;

namespace Nerven.Taskuler.Core
{
    public static class ObservableExtensions
    {
        [PublicAPI]
        public static IDisposable SubscribeConsole(this IObservable<TaskulerNotification> notificationsSource, Action<string> write)
        {
            return notificationsSource?
                .Subscribe(_notification =>
                    {
                        write($"{_notification.Message}");
                    });
        }
    }
}
