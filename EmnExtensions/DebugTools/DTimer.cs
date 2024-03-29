using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace EmnExtensions.DebugTools
{
    public sealed class DTimer : IDisposable
    {
        public DTimer(string actionLabel)
            => Start(actionLabel);

        public DTimer(Action<TimeSpan> resultSink)
            => Start(resultSink);

        public void NextAction(string newActionLabel)
        {
            Stop();
            Start(newActionLabel);
        }

        public void NextAction(Action<TimeSpan> nextResultSink)
        {
            Stop();
            Start(nextResultSink);
        }

        public void Start(string actionLabel)
            => Start(ts => Console.WriteLine("{0} took {1}", actionLabel, ts));

        public void Start(Action<TimeSpan> resultSink)
        {
            m_resultSink = resultSink;
            underlyingTimer = Stopwatch.StartNew();
        }

        public void Stop()
        {
            underlyingTimer.Stop();
            m_resultSink(underlyingTimer.Elapsed);
        }

        void IDisposable.Dispose()
        {
            underlyingTimer.Stop();
            m_resultSink(underlyingTimer.Elapsed);
        }

        Action<TimeSpan> m_resultSink;
        Stopwatch underlyingTimer;

        public static Task TimeTask(Func<Task> f, string actionLabel)
            => TimeTask(f, new DTimer(actionLabel));

        public static Task TimeTask(Func<Task> f, Action<TimeSpan> resultSink)
            => TimeTask(f, new DTimer(resultSink));

        static Task TimeTask(Func<Task> f, DTimer timer)
            => f().ContinueWith(
                t => {
                    ((IDisposable)timer).Dispose();
                    t.Wait();
                },
                TaskContinuationOptions.ExecuteSynchronously
            );

        public static T TimeFunc<T>(Func<T> f, string actionLabel)
        {
            using (new DTimer(actionLabel)) {
                return f();
            }
        }

        public static T TimeFunc<T>(Func<T> f, Action<TimeSpan> resultSink)
        {
            using (new DTimer(resultSink)) {
                return f();
            }
        }

        public static T TimeFunc<T, M>(Func<T> f, M key, Action<M, TimeSpan> resultsSink)
        {
            using (new DTimer(t => resultsSink(key, t))) {
                return f();
            }
        }

        public static TimeSpan BenchmarkAction(Action a, int repeats)
        {
            var times = new long[repeats];
            for (var i = 0; i < repeats; i++) {
                var w = Stopwatch.StartNew();
                a();
                times[i] = w.ElapsedTicks;
            }

            Array.Sort(times);
            return new(times[repeats / 4]);
        }

        public static TimeSpan TimeAction(Action a)
        {
            var w = Stopwatch.StartNew();
            a();
            return w.Elapsed;
        }
    }

    public static class DTimerExtensions
    {
        public static T TimeFunc<T>(this Func<T> f, string actionLabel)
            => DTimer.TimeFunc(f, actionLabel);

        public static T TimeFunc<T>(this Func<T> f, Action<TimeSpan> resultSink)
            => DTimer.TimeFunc(f, resultSink);

        public static T TimeFunc<T, M>(this Func<T> f, M key, Action<M, TimeSpan> resultsSink)
            => DTimer.TimeFunc(f, key, resultsSink);
    }
}
