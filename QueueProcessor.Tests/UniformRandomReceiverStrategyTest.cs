using System;
using Xunit;

namespace QueueProcessor
{
    public class UniformRandomReceiverStrategyTest
    {
        [Fact]
        public void GetDelay_AlwaysReturnsBetweenMinAndMax()
        {
            // Arrange
            TimeSpan minDelay = TimeSpan.FromSeconds(1.0);
            TimeSpan maxDelay = TimeSpan.FromSeconds(2.0);
            UniformRandomReceiverStrategy strategy = new UniformRandomReceiverStrategy(minDelay, maxDelay);

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
            UniformRandomReceiverStrategy strategy = new UniformRandomReceiverStrategy(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(2.0), 10);

            // Act & Assert
            TimeSpan delay = strategy.GetDelay(10);
            Assert.Equal(TimeSpan.Zero, delay);
        }
    }
}
