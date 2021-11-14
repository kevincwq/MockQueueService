namespace MockQueueService
{
    public interface ITestQueue<T>
    {
        int QueuedCount { get; }

        int HiddenCount { get; }

        IEnumerable<T> QueuedItems { get; }

        IEnumerable<T> HiddenItems { get; }

        Action<string> OnMethodExecuting { get; set; }

        Task WhenEmptied(bool includeHiddenItems = true);
    }
}
