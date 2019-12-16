using System;
using Xunit;

namespace QueueProcessor
{
    public class LongPollingStrategyTest
    {
        [Fact]
        public void GetDelay_ReturnsZero()
        {
            // Arrange
            LongPollingStrategy strategy = new LongPollingStrategy();

            // Act & Assert
            TimeSpan delay = strategy.GetDelay(0);
            Assert.Equal(TimeSpan.Zero, delay);
        }
    }
}
