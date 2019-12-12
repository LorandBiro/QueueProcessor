using System;
using Xunit;

namespace QueueProcessor
{
    public class ContinuousReceiverStrategyTest
    {
        [Fact]
        public void GetDelay_ReturnsZero()
        {
            // Arrange
            ContinuousReceiverStrategy strategy = new ContinuousReceiverStrategy();

            // Act & Assert
            TimeSpan delay = strategy.GetDelay(0);
            Assert.Equal(TimeSpan.Zero, delay);
        }
    }
}
