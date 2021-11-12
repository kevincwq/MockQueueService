namespace MockQueueService.Tests
{
    public class MockQueueMessage : IQueueItem
    {
        public string ReceiptHandle { get; set; }

        public string Message { get; set; }
    }
}
