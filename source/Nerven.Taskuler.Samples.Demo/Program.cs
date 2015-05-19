using System;
using System.Diagnostics;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Nerven.Taskuler.Core;

namespace Nerven.Taskuler.Samples.Demo
{
    public static class Program
    {
        private static readonly Stopwatch _Stopwatch = new Stopwatch();

        public static void Main()
        {
            var _worker = TaskulerWorker.Create(TimeSpan.FromMilliseconds(1));

            _worker.UseIntervalSchedule(TimeSpan.FromSeconds(10))
                .Do(async () =>
                {
                    _Echo("3");
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    _Echo("2");
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    _Echo("1");
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    _Echo("0");
                })
                .Do(async () =>
                {
                    _Echo(">");
                    await Task.Delay(TimeSpan.FromSeconds(15));
                    _Echo("<");
                });

            _worker.UseDailySchedule(TimeSpan.FromHours(((int)DateTimeOffset.Now.TimeOfDay.TotalHours) + 1))
                .Do(() =>
                {
                    _Echo("@");

                    return Task.FromResult(0);
                });

            _Echo("Starting ...");
            _worker.Start();
            _Stopwatch.Restart();
            _Echo("Started.");
            Console.ReadLine();
            _Echo("Stopping ...");
            _worker.StopAsync().Wait();
            _Echo("Stopped.");
            Console.ReadLine();
        }

        [StringFormatMethod("s")]
        private static void _Echo(string s, params object[] args)
        {
            var _s = string.Format(s, args);
            Console.WriteLine("[{0:0000.000}] {1}", Math.Round(_Stopwatch.Elapsed.TotalSeconds, 3, MidpointRounding.AwayFromZero), _s);
        }
    }
}
