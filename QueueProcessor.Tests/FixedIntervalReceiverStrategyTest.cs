using QueueProcessor.Mocks;
using System;
using Xunit;

namespace QueueProcessor
{
    public class FixedIntervalReceiverStrategyTest
    {
        [Fact]
        public void GetDelay_ReturnsZero_WhenBatchSizeReachesLimit()
        {
            // Arrange
            FixedIntervalReceiverStrategy strategy = new FixedIntervalReceiverStrategy(new ClockStub(), TimeSpan.FromSeconds(1.0), 10);

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
            FixedIntervalReceiverStrategy strategy = new FixedIntervalReceiverStrategy(clock, TimeSpan.FromSeconds(1.0), 10);

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
            FixedIntervalReceiverStrategy strategy = new FixedIntervalReceiverStrategy(clock, TimeSpan.FromSeconds(1.0), 10);

            // Right now the interval is between 0s and 1s. We let the time pass until 1.5s.
            now += TimeSpan.FromSeconds(1.5);
            clock.Now = now;

            // Act & Assert
            TimeSpan delay = strategy.GetDelay(0);
            Assert.Equal(TimeSpan.Zero, delay);
        }
    }
}
