using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MockQueueService.Tests
{
    public class MockQueueTests
    {
        [Fact]
        public async Task Initialize_Add_Read_Del_Dispose()
        {
            // Arrange
            var queue = new MockQueueService<MockQueueMessage>();
            var count = 10;

            // Act
            for (int i = 0; i < count; i++)
            {
                await queue.AddAsync(new MockQueueMessage { Message = $"Message {i}" });
                Assert.Equal(i + 1, queue.QueuedItems.Count());
            }

            // Assert
            for (int i = 0; i < count; i++)
            {
                var m = await queue.ReadAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(10));

                Assert.NotNull(m);
                Assert.Equal(count - 1 - i, queue.QueuedItems.Count());
                Assert.Single(queue.HiddenItems);

                await queue.DeleteAsync(m!.ReceiptHandle);
                Assert.Empty(queue.HiddenItems);
            }

            Assert.Empty(queue.QueuedItems);
            Assert.Empty(queue.HiddenItems);

            // Clean up
            queue.Dispose();
        }

        [Fact]
        public async Task ReadAsync_Timeout()
        {
            // Arrange
            var queue = new MockQueueService<MockQueueMessage>(TimeSpan.Zero);

            // Act & Assert
            var m1Task = queue.ReadAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(5));
            await Task.Delay(20);
            Assert.False(m1Task.IsCompleted);
            await Task.Delay(80);
            Assert.True(m1Task.IsCompleted);
            var m1 = await m1Task;
            Assert.Null(m1);

            // Clean up
            queue.Dispose();
        }

        [Fact]
        public async Task ReadAsync_Wait()
        {
            // Arrange
            var queue = new MockQueueService<MockQueueMessage>(TimeSpan.Zero);

            // Act
            var m1Task = queue.ReadAsync(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
            await Task.Delay(5);
            Assert.False(m1Task.IsCompleted);

            await queue.AddAsync(new MockQueueMessage { Message = $"Message 1" });
            await Task.Delay(1);
            Assert.True(m1Task.IsCompleted);
            var m1 = await m1Task;
            Assert.NotNull(m1);

            // Clean up
            queue.Dispose();
        }

        [Fact]
        public async Task ReadAsync_Visibility()
        {
            // Arrange
            var queue = new MockQueueService<MockQueueMessage>(TimeSpan.Zero);
            await queue.AddAsync(new MockQueueMessage { Message = $"Message 1" });

            // Act
            var m1 = await queue.ReadAsync(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(5));
            var m2 = await queue.ReadAsync(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(5));
            await Task.Delay(6);
            var m3 = await queue.ReadAsync(TimeSpan.FromMilliseconds(1), TimeSpan.FromSeconds(10));

            // Assert
            Assert.NotNull(m1);
            Assert.Null(m2);
            Assert.NotNull(m3);
            Assert.Equal(m1!.Message, m3!.Message);
            Assert.NotEqual(m1.ReceiptHandle, m3.ReceiptHandle);

            // Clean up
            queue.Dispose();
        }

        [Fact]
        public async Task ReadAsync_Cancel()
        {
            // Arrange
            var queue = new MockQueueService<MockQueueMessage>(TimeSpan.Zero);
            var cts = new CancellationTokenSource(10);

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => queue.ReadAsync(TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(5), cts.Token));

            // Clean up
            queue.Dispose();
        }

        [Fact]
        public async Task ChangeVisibilityAsync()
        {
            // Arrange
            var queue = new MockQueueService<MockQueueMessage>(TimeSpan.Zero);
            await queue.AddAsync(new MockQueueMessage { Message = $"Message 1" });

            // Act
            var m1 = await queue.ReadAsync(TimeSpan.FromMilliseconds(1), TimeSpan.FromHours(1));
            var m2 = await queue.ReadAsync(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(5));

            await queue.ChangeVisibilityAsync(m1!.ReceiptHandle, TimeSpan.FromMilliseconds(10));

            var m3 = await queue.ReadAsync(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10));

            // Assert
            Assert.NotNull(m1);
            Assert.Null(m2);
            Assert.NotNull(m3);
            Assert.Equal(m1!.Message, m3!.Message);
            Assert.NotEqual(m1.ReceiptHandle, m3.ReceiptHandle);

            // Clean up
            queue.Dispose();
        }
    }
}