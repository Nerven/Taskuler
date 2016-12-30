using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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
            var _counter = 0;

            _worker.AddIntervalSchedule("Six times each minute", TimeSpan.FromSeconds(10))
                .AddTask(async () =>
                    {
                        _Echo("3");
                        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                        _Echo("2");
                        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                        _Echo("1");
                        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                        _Echo("0:" + _counter);
                    })
                .AddTask(async () =>
                    {
                        _Echo(">");
                        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                        _Echo("<");
                    });

            var _exclamationTask = _worker.AddVoidSchedule()
                .AddTask("Exclamation Task", () =>
                    {
                        _Echo("!");

                        return Task.FromResult(0);
                    });

            var _timeOfDay = TimeSpan.FromHours((int)DateTimeOffset.Now.TimeOfDay.TotalHours + 1);
            _timeOfDay -= TimeSpan.FromDays(Math.Floor(_timeOfDay.TotalDays));
            var _atTask = _worker.AddDailySchedule(_timeOfDay)
                .AddTask(() =>
                    {
                        _Echo("@");

                        _worker.GetAllTasks().Single(_task => _task.Key == _exclamationTask.Key).RunManually();
                        return Task.FromResult(0);
                    });

            var _highFreqOffset = TimeSpan.FromSeconds(5);
            var _highFreqFreq = 1;
            var _highFreqFramesLock = new object();
            var _highFreqFrames = new List<int>();
            _worker.AddIntervalSchedule("High Freq", TimeSpan.FromMilliseconds(_highFreqFreq))
                .AddTask(_ =>
                    {
                        if (_.ScheduledAfter > _highFreqOffset && _.ScheduledAfter <= _highFreqOffset.Add(TimeSpan.FromSeconds(10)))
                        {
                            lock (_highFreqFramesLock)
                            {
                                _highFreqFrames.Add((int)_.ScheduledAfter.TotalMilliseconds);
                            }

                            Console.WriteLine($"Tick {_counter++}, elapsed (official): {_.ScheduledAfter.TotalMilliseconds} ms, elapsed (seen): {_Stopwatch.ElapsedMilliseconds} ms.");
                        }

                        return Task.FromResult(_.ScheduledAfter > _highFreqOffset.Add(TimeSpan.FromSeconds(15)) ? TaskulerTaskResponse.Stop() : TaskulerTaskResponse.Continue());
                    });

            Console.WriteLine("SCHEDULES AND THEIR TASKS");
            foreach (var _scheduleHandle in _worker.GetSchedules())
            {
                Console.WriteLine($"\t* {_scheduleHandle.ScheduleName ?? "---"} ({_scheduleHandle.Key})");

                foreach (var _taskHandle in _scheduleHandle.GetTasks())
                {
                    Console.WriteLine($"\t\t* {_taskHandle.TaskName ?? "---"} ({_taskHandle.Key})");
                }
            }

            Console.WriteLine("ALL TASKS");
            foreach (var _taskHandle in _worker.GetAllTasks())
            {
                Console.WriteLine($"\t* {_taskHandle.TaskName} ({_taskHandle.Key})");
            }

            Console.WriteLine();
            Console.WriteLine();

            _Echo("Starting ...");
            _worker.StartAsync().Wait();
            _Stopwatch.Restart();
            _Echo("Started.");
            Thread.Sleep(100);
            _atTask.RunManually();
            Console.ReadLine();
            _Echo("Stopping ...");
            _worker.StopAsync().Wait();
            _Echo("Stopped.");
            _highFreqFrames.Sort();

            lock (_highFreqFramesLock)
            {
                for (var _i = 0; _i < _highFreqFrames.Count - 1; _i++)
                {
                    if (_highFreqFrames[_i] != _highFreqFrames[_i + 1] - _highFreqFreq)
                    {
                        Console.WriteLine($"[frames] {_i} -> {_i + 1} / {_highFreqFrames[_i]} -> {_highFreqFrames[_i + 1]} / {_highFreqFrames[_i + 1] - _highFreqFrames[_i] - _highFreqFreq}");
                    }
                }
            }

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
