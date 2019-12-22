using System;
using Xunit;

namespace QueueProcessor.Timers
{
    public class RandomTimerTest
    {
        [Fact]
        public void GetDelay_AlwaysReturnsBetweenMinAndMax()
        {
            // Arrange
            TimeSpan minDelay = TimeSpan.FromSeconds(1.0);
            TimeSpan maxDelay = TimeSpan.FromSeconds(2.0);
            RandomTimer strategy = new RandomTimer(minDelay, maxDelay);

            for (int i = 0; i < 1000; i++)
            {
                // Act & Assert
                TimeSpan delay = strategy.GetDelay();
                Assert.True(delay >= minDelay && delay <= maxDelay);
            }
        }
    }
}
