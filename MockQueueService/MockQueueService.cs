using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;

[assembly: InternalsVisibleTo("MockQueueService.Tests")]

namespace MockQueueService
{
    public class MockQueueService<T> : IQueueService<T>, ITestQueue<T>, IDisposable where T : IQueueItem
    {
        private readonly BlockingCollection<T> queue = new();

        private readonly ConcurrentDictionary<string, (T, CancellationTokenSource)> hiddenItems = new();

        private readonly TimeSpan timeSpan;

        public MockQueueService(TimeSpan mockDelay)
        {
            timeSpan = mockDelay;
        }

        public MockQueueService() : this(TimeSpan.FromMilliseconds(10))
        {

        }

        public int QueuedCount => queue.Count;

        public int HiddenCount => hiddenItems.Count;

        public Action<string> OnMethodExecuting { get; set; }

        public IEnumerable<T> QueuedItems => queue.ToArray();

        public IEnumerable<T> HiddenItems => hiddenItems.Select(x => x.Value.Item1).ToArray();

        public async Task WhenEmptied()
        {
            while (true)
            {
                if (QueuedCount == 0 && HiddenCount == 0)
                {
                    await Task.Delay(1);
                    // a chance the item is been moving between queue and hidden list.
                    if (QueuedCount == 0 && HiddenCount == 0)
                    {
                        break;
                    }
                }
                await Task.Delay(2);
            }
        }

        public async Task ChangeVisibilityAsync(string receiptHandle, TimeSpan visibility)
        {
            await Task.Delay(timeSpan).ContinueWith(t =>
            {
                OnMethodExecuting?.Invoke(nameof(ChangeVisibilityAsync));

                if (TryRemoveFromHidden(receiptHandle, out T workItem))
                    AddToHidden(workItem!, visibilityTimeSpan: visibility);
            }).ConfigureAwait(false);
        }

        public async Task DeleteAsync(string receiptHandle)
        {
            await Task.Delay(timeSpan).ContinueWith(t =>
            {
                OnMethodExecuting?.Invoke(nameof(DeleteAsync));

                TryRemoveFromHidden(receiptHandle, out _);
            }).ConfigureAwait(false);
        }

        public async Task AddAsync(T workItem)
        {
            await Task.Delay(timeSpan).ContinueWith(t =>
            {
                OnMethodExecuting?.Invoke(nameof(AddAsync));

                queue.Add(Clone(workItem));
            }).ConfigureAwait(false);
        }

        public async Task<T?> ReadAsync(TimeSpan longPollDuration, TimeSpan visibility, CancellationToken cancellationToken = default)
        {
            return await Task.Delay(timeSpan, cancellationToken).ContinueWith(t =>
             {
                 OnMethodExecuting?.Invoke(nameof(ReadAsync));

                 if (queue.TryTake(out T item, (int)longPollDuration.TotalMilliseconds, cancellationToken))
                 {
                     item.ReceiptHandle = Guid.NewGuid().ToString();
                     AddToHidden(item, visibility);
                     return Clone(item);
                 }
                 return default;
             }).ConfigureAwait(false);
        }

        private void AddToHidden(T item, TimeSpan visibilityTimeSpan)
        {
            var cts = new CancellationTokenSource();
            var receiptHandle = item.ReceiptHandle;
            Task.Delay(visibilityTimeSpan, cts.Token)
                 .ContinueWith(t =>
                 {
                     if (t.IsCompletedSuccessfully)
                     {
                         if (hiddenItems.Remove(receiptHandle, out (T, CancellationTokenSource) item))
                         {
                             item.Item1.ReceiptHandle = default;
                             queue.Add(item.Item1);
                         }
                     }
                 }, cts.Token);
            hiddenItems.TryAdd(receiptHandle, (item, cts));
        }

        private bool TryRemoveFromHidden(string receiptHandle, out T workItem)
        {
            workItem = default;
            if (hiddenItems.Remove(receiptHandle, out (T, CancellationTokenSource) item))
            {
                var cts = item.Item2;
                if (!cts.IsCancellationRequested)
                    cts.Cancel();
                workItem = item.Item1;
                return true;
            }
            return false;
        }

        public void Dispose()
        {
            queue.CompleteAdding();
            foreach (var item in hiddenItems)
            {
                var cts = item.Value.Item2;
                if (!cts.IsCancellationRequested)
                {
                    cts.Cancel();
                }
            }
            hiddenItems.Clear();
            queue.Dispose();
        }

        private static T Clone(T item)
        {
            if (item == null)
            {
                return default;
            }
            var json = JsonSerializer.Serialize(item);
            return JsonSerializer.Deserialize<T>(json);
        }
    }
}