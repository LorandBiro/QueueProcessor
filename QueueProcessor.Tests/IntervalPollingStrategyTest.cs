using QueueProcessor.Mocks;
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
            IntervalPollingStrategy strategy = new IntervalPollingStrategy(TimeSpan.FromSeconds(1.0), 10, new ClockStub());

            // Act & Assert
            TimeSpan delay = strategy.GetDelay(10);
            Assert.Equal(TimeSpan.Zero, delay);
        }

        [Fact]
        public void GetDelay_IntervalsArePredictable()
        {
            // Arrange
            ClockStub clock = new ClockStub();
            IntervalPollingStrategy strategy = new IntervalPollingStrategy(TimeSpan.FromSeconds(1.0), 10, clock);

            // Act
            DateTime end = clock.Now.AddSeconds(1000.0);
            int count = 0;
            while (true)
            {
                TimeSpan delay = strategy.GetDelay(0);
                clock.Add(delay);
                if (clock.Now > end)
                {
                    break;
                }

                count++;
            }

            // Assert
            Assert.Equal(1000, count);
        }

        [Fact]
        public void GetDelay_ReturnsZero_WhenNextItervalIsInThePast()
        {
            // Arrange
            ClockStub clock = new ClockStub();
            IntervalPollingStrategy strategy = new IntervalPollingStrategy(TimeSpan.FromSeconds(1.0), 10, clock);

            // Right now the interval is between 0s and 1s. We let the time pass beyond this interval.
            clock.AddSeconds(1.5);

            // Act & Assert
            TimeSpan delay = strategy.GetDelay(0);
            Assert.Equal(TimeSpan.Zero, delay);
        }
    }
}
