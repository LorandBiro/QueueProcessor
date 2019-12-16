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
            DateTime start = new DateTime(0, DateTimeKind.Utc);

            ClockStub clock = new ClockStub { Now = start };
            IntervalPollingStrategy strategy = new IntervalPollingStrategy(TimeSpan.FromSeconds(1.0), 10, clock);

            // Act
            DateTime end = start + TimeSpan.FromSeconds(1000.0);
            DateTime now = start;
            int count = 0;
            while (true)
            {
                clock.Now = now;
                TimeSpan delay = strategy.GetDelay(0);
                now += delay;
                if (now > end)
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
            DateTime now = new DateTime(0, DateTimeKind.Utc);

            ClockStub clock = new ClockStub { Now = now };
            IntervalPollingStrategy strategy = new IntervalPollingStrategy(TimeSpan.FromSeconds(1.0), 10, clock);

            // Right now the interval is between 0s and 1s. We let the time pass until 1.5s.
            now += TimeSpan.FromSeconds(1.5);
            clock.Now = now;

            // Act & Assert
            TimeSpan delay = strategy.GetDelay(0);
            Assert.Equal(TimeSpan.Zero, delay);
        }
    }
}
