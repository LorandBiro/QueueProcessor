using NSubstitute;
using QueueProcessor.Internal;
using System;
using Xunit;

namespace QueueProcessor
{
    public class ConstantRateRandomReceiverStrategyTest
    {
        [Fact]
        public void GetDelay_ReturnsZero_WhenBatchSizeReachesLimit()
        {
            // Arrange
            ConstantRateRandomReceiverStrategy strategy = new ConstantRateRandomReceiverStrategy(Substitute.For<IClock>(), TimeSpan.FromSeconds(1.0), 10);

            // Act & Assert
            TimeSpan delay = strategy.GetDelay(10);
            Assert.Equal(TimeSpan.Zero, delay);
        }

        [Fact]
        public void GetDelay_IntervalsArePredictable()
        {
            // Arrange
            DateTime start = new DateTime(0, DateTimeKind.Utc);

            IClock clock = Substitute.For<IClock>();
            clock.GetCurrentInstant().Returns(start);

            ConstantRateRandomReceiverStrategy strategy = new ConstantRateRandomReceiverStrategy(clock, TimeSpan.FromSeconds(1.0), 10);

            // Act
            DateTime end = start + TimeSpan.FromSeconds(1000.0);
            DateTime now = start;
            int count = 0;
            while (true)
            {
                clock.GetCurrentInstant().Returns(now);
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

            IClock clock = Substitute.For<IClock>();
            clock.GetCurrentInstant().Returns(now);

            ConstantRateRandomReceiverStrategy strategy = new ConstantRateRandomReceiverStrategy(clock, TimeSpan.FromSeconds(1.0), 10);

            // Right now the interval is between 0s and 1s. We let the time pass until 1.5s.
            now += TimeSpan.FromSeconds(1.5);
            clock.GetCurrentInstant().Returns(now);

            // Act & Assert
            TimeSpan delay = strategy.GetDelay(0);
            Assert.Equal(TimeSpan.Zero, delay);
        }
    }
}
