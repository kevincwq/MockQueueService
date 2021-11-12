namespace MockQueueService
{
    public interface IQueueItem
    {
        string ReceiptHandle { get; set; }
    }
}