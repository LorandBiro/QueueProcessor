using System;
using Xunit;

namespace QueueProcessor
{
    public class RandomPollingStrategyTest
    {
        [Fact]
        public void GetDelay_AlwaysReturnsBetweenMinAndMax()
        {
            // Arrange
            TimeSpan minDelay = TimeSpan.FromSeconds(1.0);
            TimeSpan maxDelay = TimeSpan.FromSeconds(2.0);
            RandomPollingStrategy strategy = new RandomPollingStrategy(minDelay, maxDelay);

            for (int i = 0; i < 1000; i++)
            {
                // Act & Assert
                TimeSpan delay = strategy.GetDelay(1);
                Assert.True(delay >= minDelay && delay <= maxDelay);
            }
        }

        [Fact]
        public void GetDelay_ReturnsZeroIfBatchSizeReachesLimit()
        {
            // Arrange
            RandomPollingStrategy strategy = new RandomPollingStrategy(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(2.0), 10);

            // Act & Assert
            TimeSpan delay = strategy.GetDelay(10);
            Assert.Equal(TimeSpan.Zero, delay);
        }
    }
}
