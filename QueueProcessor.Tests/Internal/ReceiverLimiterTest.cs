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
            ReceiverLimiter limiter = new ReceiverLimiter();
            
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
                ReceiverLimiter limiter = new ReceiverLimiter();
                limiter.Enable();

                // Act & Assert
                Task task = limiter.WaitAsync(cts.Token);
                Assert.Equal(TaskStatus.WaitingForActivation, task.Status);

                // Act & Assert
                cts.Cancel();
                Assert.Equal(TaskStatus.Canceled, task.Status);
            }
        }

        [Fact]
        public void WaitAsync_CompletesTask_WhenCountGoesUnderLimit()
        {
            // Arrange
            ReceiverLimiter limiter = new ReceiverLimiter();
            limiter.Enable();

            // Act & Assert
            Task task = limiter.WaitAsync();
            Assert.Equal(TaskStatus.WaitingForActivation, task.Status);

            // Act & Assert
            limiter.Disable();
            Assert.Equal(TaskStatus.RanToCompletion, task.Status);
        }

        [Fact]
        public void WaitAsync_TasksCanBeCanceledIndependently()
        {
            // Arrange
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                ReceiverLimiter limiter = new ReceiverLimiter();
                limiter.Enable();

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
