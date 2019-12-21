using QueueProcessor.Utils;
using System;
using Xunit;

namespace QueueProcessor
{
    public class IntervalPollingStrategyTest
    {
        [Fact]
        public void GetDelay_ReturnsZero_WhenBatchSizeReachesLimit()
        {
            // Arrange
            TimeSpan delay = TimeSpan.FromSeconds(1.0);
            IntervalPollingStrategy strategy = new IntervalPollingStrategy(10, new IntervalTimerStub { Delay = delay });

            // Act & Assert
            Assert.Equal(delay, strategy.GetDelay(9));
            Assert.Equal(TimeSpan.Zero, strategy.GetDelay(10));
            Assert.Equal(TimeSpan.Zero, strategy.GetDelay(11));
        }
    }
}
