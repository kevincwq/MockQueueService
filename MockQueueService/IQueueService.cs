namespace MockQueueService
{
    public interface IQueueService<T> where T : IQueueItem
    {
        Task ChangeVisibilityAsync(string receiptHandle, TimeSpan visibility);

        Task DeleteAsync(string receiptHandle);

        Task AddAsync(T workItem);

        Task<T?> ReadAsync(TimeSpan longPollDuration, TimeSpan visibility, CancellationToken cancellationToken = default);
    }
}