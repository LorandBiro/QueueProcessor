using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace QueueProcessor.Internal
{
    public class ReceiverLimiterTest
    {
        [Fact]
        public void WaitAsync_ReturnsCanceledTask_WhenAlreadyCanceled()
        {
            // Arrange
            ReceiverLimiter limiter = new ReceiverLimiter(100);
            
            // Act & Assert
            Task task = limiter.WaitAsync(new CancellationToken(true));
            Assert.Equal(TaskStatus.Canceled, task.Status);
        }

        [Fact]
        public void WaitAsync_CancelsTask_WhenWaitIsCanceled()
        {
            // Arrange
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                ReceiverLimiter limiter = new ReceiverLimiter(100);
                limiter.OnRecieved(100);

                // Act & Assert
                Task task = limiter.WaitAsync(cts.Token);
                Assert.Equal(TaskStatus.WaitingForActivation, task.Status);

                // Act & Assert
                cts.Cancel();
                Assert.Equal(TaskStatus.Canceled, task.Status);
            }
        }

        [Fact]
        public void WaitAsync_ReturnsCompletedTask_WhenUnderLimit()
        {
            // Arrange
            ReceiverLimiter limiter = new ReceiverLimiter(100);
            limiter.OnRecieved(50);

            // Act & Assert
            Task task = limiter.WaitAsync();
            Assert.Equal(TaskStatus.RanToCompletion, task.Status);
        }

        [Fact]
        public void WaitAsync_CompletesTask_WhenCountGoesUnderLimit()
        {
            // Arrange
            ReceiverLimiter limiter = new ReceiverLimiter(1);
            limiter.OnRecieved(2);

            // Act & Assert
            Task task = limiter.WaitAsync();
            Assert.Equal(TaskStatus.WaitingForActivation, task.Status);

            // Act & Assert
            limiter.OnClosed(1);
            Assert.Equal(TaskStatus.WaitingForActivation, task.Status);

            // Act & Assert
            limiter.OnClosed(1);
            Assert.Equal(TaskStatus.RanToCompletion, task.Status);
        }

        [Fact]
        public void WaitAsync_TasksCanBeCanceledIndependently()
        {
            // Arrange
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                ReceiverLimiter limiter = new ReceiverLimiter(1);
                limiter.OnRecieved(1);

                // Act & Assert
                Task task1 = limiter.WaitAsync();
                Task task2 = limiter.WaitAsync(cts.Token);
                Assert.Equal(TaskStatus.WaitingForActivation, task1.Status);
                Assert.Equal(TaskStatus.WaitingForActivation, task2.Status);

                // Act & Assert
                cts.Cancel();
                Assert.Equal(TaskStatus.WaitingForActivation, task1.Status);
                Assert.Equal(TaskStatus.Canceled, task2.Status);
            }
        }
    }
}
