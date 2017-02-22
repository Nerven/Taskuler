using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Nerven.Assertion;
using Nerven.Taskuler.Core;

namespace Nerven.Taskuler.Samples.Demo
{
    public static class Program
    {
        private const int _HighFreqFreq = 1;
        private static readonly Stopwatch _Stopwatch = new Stopwatch();

        public static void Main()
        {
            TaskulerWorker.ConfigureHosting();
            var _random = new Random();
            var _worker = TaskulerHosting.CreateHostedWorker(TaskulerWorker.Factory(TimeSpan.FromMilliseconds(1), null, 10000));
            MustAssertionApi.Configure(new MustAssertionOptions
                {
                    EvaluateAssumptions = true,
                    ThrowOnFailedAssumptions = true,
                });

            _worker.AddIntervalSchedule("High Freq", TimeSpan.FromMilliseconds(_HighFreqFreq));

            _worker.AddIntervalSchedule("Six times each minute", TimeSpan.FromSeconds(10))
                .AddTask(async () =>
                    {
                        _Echo("3");
                        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                        _Echo("2");
                        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                        _Echo("1");
                        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
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
                .AddTask(async () =>
                    {
                        _Echo("@");

                        await _worker.GetAllTasks().Single(_task => _task.Key == _exclamationTask.Key).RunManuallyAsync().ConfigureAwait(false);
                    });

            var _cycles = 420;
            for (var _cycle = 0; _cycle < _cycles; _cycle++)
            {
                var _cycleNumber = _cycle + 1;
                var _cancelAt = _random.Next(0, 22 * 1000);
                Console.WriteLine($"<[#{_cycleNumber}/{_cycles}] cancelAt={_cancelAt}>");
                if (_cycleNumber % 10 == 0)
                {
                    Thread.Sleep(5000);
                }

                //// ReSharper disable AccessToDisposedClosure
                using (var _cancellationSource = new CancellationTokenSource())
                {
                    var _cycleThread = new Thread(() => _RunCycle(_worker, _atTask, _cancellationSource.Token));
                    var _cancelThread = new Thread(() =>
                        {
                            Thread.Sleep(_cancelAt);
                            _cancellationSource.Cancel();
                        });

                    _cycleThread.Name = "CYCLE_THREAD";
                    _cancelThread.Name = "CANCEL_THREAD";

                    _cancelThread.Start();
                    _cycleThread.Start();

                    _cycleThread.Join();
                    _cancelThread.Join();
                }
                //// ReSharper restore AccessToDisposedClosure
                
                Console.WriteLine($"</[#{_cycleNumber}/{_cycles}] cancelAt={_cancelAt}>");
            }

            Console.ReadLine();
            Console.ReadLine();
        }

        private static void _RunCycle(ITaskulerWorker worker, ITaskulerTaskHandle atTask, CancellationToken cancellationToken)
        {
            var _counter = 0;
            var _highFreqOffset = TimeSpan.FromSeconds(5);
            var _highFreqFramesLock = new object();
            var _highFreqFrames = new List<int>();

            var _extraTask1 = worker.GetSchedules().Single(_schedule => _schedule.ScheduleName == "Six times each minute")
                .AddTask(() =>
                    {
                        _Echo("0:" + _counter);
                        return Task.FromResult(0);
                    });

            var _extraTask2 = worker.GetSchedules().Single(_schedule => _schedule.ScheduleName == "High Freq")
                .AddTask(_ =>
                    {
                        if (_.Duration > _highFreqOffset && _.Duration <= _highFreqOffset.Add(TimeSpan.FromSeconds(10)))
                        {
                            lock (_highFreqFramesLock)
                            {
                                _highFreqFrames.Add((int)_.Duration.TotalMilliseconds);
                            }

                            Console.WriteLine($"Tick {_counter++}, elapsed (official): {_.Duration.TotalMilliseconds} ms, elapsed (seen): {_Stopwatch.ElapsedMilliseconds} ms.");
                        }

                        return Task.FromResult(_.Duration > _highFreqOffset.Add(TimeSpan.FromSeconds(15)) ? TaskulerTaskResponse.Stop() : TaskulerTaskResponse.Continue());
                    });

            Console.WriteLine("SCHEDULES AND THEIR TASKS");
            foreach (var _scheduleHandle in worker.GetSchedules())
            {
                Console.WriteLine($"\t* {_scheduleHandle.ScheduleName ?? "---"} ({_scheduleHandle.Key})");

                foreach (var _taskHandle in _scheduleHandle.GetTasks())
                {
                    Console.WriteLine($"\t\t* {_taskHandle.TaskName ?? "---"} ({_taskHandle.Key})");
                }
            }

            Console.WriteLine("ALL TASKS");
            foreach (var _taskHandle in worker.GetAllTasks())
            {
                Console.WriteLine($"\t* {_taskHandle.TaskName} ({_taskHandle.Key})");
            }

            Console.WriteLine();
            Console.WriteLine();

            //// ReSharper disable once AccessToDisposedClosure
            using (var _cancellationSource = new CancellationTokenSource())
            using (var _stopSignal = new SemaphoreSlim(0, 1))
            using (cancellationToken.Register(() => _stopSignal.Release()))
            {
                _Echo("Starting ...");
                worker.StartAsync(_cancellationSource.Token).Wait();
                _Stopwatch.Restart();
                _Echo("Started.");
                try
                {
                    atTask.RunManuallyAsync().Wait();
                }
                catch (Exception _exception)
                {
                    Console.WriteLine(_exception);
                }

                _stopSignal.Wait();
                _Echo("Stopping ...");
                worker.StopAsync().Wait();
                _Echo("Stopped.");
            }

            _extraTask1.Remove();
            _extraTask2.Remove();

            _highFreqFrames.Sort();
            lock (_highFreqFramesLock)
            {
                for (var _i = 0; _i < _highFreqFrames.Count - 1; _i++)
                {
                    if (_highFreqFrames[_i] != _highFreqFrames[_i + 1] - _HighFreqFreq)
                    {
                        Console.WriteLine($"[frames] {_i} -> {_i + 1} / {_highFreqFrames[_i]} -> {_highFreqFrames[_i + 1]} / {_highFreqFrames[_i + 1] - _highFreqFrames[_i] - _HighFreqFreq}");
                    }
                }
            }
        }

        [StringFormatMethod("s")]
        private static void _Echo(string s, params object[] args)
        {
            var _s = string.Format(s, args);
            Console.WriteLine("[{0:0000.000}] {1}", Math.Round(_Stopwatch.Elapsed.TotalSeconds, 3, MidpointRounding.AwayFromZero), _s);
        }
    }
}
