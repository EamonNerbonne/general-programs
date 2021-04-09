using System;
using System.Linq;
using System.Threading.Tasks;

namespace EmnExtensions.Threading
{
    public static class Par
    {
        public static void Invoke(params Action[] actions) => Task.WaitAll(actions.Select(Task.Factory.StartNew).ToArray());

        public static Task Then(this Task t, Func<Task> startnext)
        {
            var whencomplete = new TaskCompletionSource<int>();
            Action<Task> continuationFunction = t1 => whencomplete.SetResult(0);
            PassUpErrors(t, whencomplete, t0 => PassUpErrors(startnext(), whencomplete, continuationFunction));
            return whencomplete.Task;
        }
        private static void PassUpErrors(Task t, TaskCompletionSource<int> parent, Action<Task> whenOk)
        {
            t.ContinueWith(t0 => parent.SetCanceled(), TaskContinuationOptions.OnlyOnCanceled);
            t.ContinueWith(t0 => parent.SetException(t0.Exception), TaskContinuationOptions.OnlyOnFaulted);
            t.ContinueWith(whenOk, TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        public static Task<TOut> Then<TIn, TOut>(this Task<TIn> t, Func<TIn, Task<TOut>> startnext)
        {
            var whencomplete = new TaskCompletionSource<TOut>();
            Action<Task<TOut>> continuationFunction = t1 => whencomplete.SetResult(t1.Result);
            PassUpErrors(t, whencomplete, t0 => PassUpErrors(startnext(t0.Result), whencomplete, continuationFunction));
            return whencomplete.Task;
        }

        private static void PassUpErrors<TIn, TOut>(Task<TIn> t, TaskCompletionSource<TOut> parent, Action<Task<TIn>> whenOk)
        {
            t.ContinueWith(t0 => parent.SetCanceled(), TaskContinuationOptions.OnlyOnCanceled);
            t.ContinueWith(t0 => parent.SetException(t0.Exception), TaskContinuationOptions.OnlyOnFaulted);
            t.ContinueWith(whenOk, TaskContinuationOptions.OnlyOnRanToCompletion);
        }
    }
}
