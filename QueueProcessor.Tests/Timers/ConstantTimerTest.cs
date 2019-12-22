using System;
using Xunit;

namespace QueueProcessor.Timers
{
    public class ConstantTimerTest
    {
        [Fact]
        public void GetDelay_ReturnsZero()
        {
            // Arrange
            TimeSpan delay = TimeSpan.FromSeconds(5.0);
            ConstantTimer timer = new ConstantTimer(delay);

            // Act & Assert
            Assert.Equal(delay, timer.GetDelay());
        }
    }
}
