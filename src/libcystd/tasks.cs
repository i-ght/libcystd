using Optional;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LibCyStd
{
    public static class TaskUtils
    {
        public static async Task TimeoutAfter(this Task task, TimeSpan timeout)
        {
            using (var cts = new CancellationTokenSource())
            {
                var delay = Task.Delay(timeout, cts.Token);
#pragma warning disable RCS1090 // Call 'ConfigureAwait(false)'.
                var completed = await Task.WhenAny(task, delay);
#pragma warning restore RCS1090 // Call 'ConfigureAwait(false)'.
                if (completed == delay) ExnUtils.Timeout("The operation has timed out.");
                cts.Cancel();
                await task.ConfigureAwait(false);
            }
        }

        public static async Task<T> TimeoutAfter<T>(this Task<T> task, TimeSpan timeout)
        {
            using (var cts = new CancellationTokenSource())
            {
                var delay = Task.Delay(timeout, cts.Token);
#pragma warning disable RCS1090 // Call 'ConfigureAwait(false)'.
                var completed = await Task.WhenAny(task, delay);
#pragma warning restore RCS1090 // Call 'ConfigureAwait(false)'.
                if (completed == delay) ExnUtils.Timeout("The operation has timed out.");
                cts.Cancel();
                return await task.ConfigureAwait(false);
            }
        }

        public static async Task TryCancelled(Func<Task> task, Option<Action> onCancelled)
        {
            try { await task().ConfigureAwait(false); }
            catch (OperationCanceledException) { onCancelled.MatchSome(action => action()); }
        }
    }
}