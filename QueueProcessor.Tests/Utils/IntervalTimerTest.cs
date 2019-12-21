using QueueProcessor.Mocks;
using System;
using Xunit;

namespace QueueProcessor.Utils
{
    public class IntervalTimerTest
    {

        [Fact]
        public void GetDelay_IntervalsArePredictable()
        {
            // Arrange
            ClockStub clock = new ClockStub();
            IntervalTimer timer = new IntervalTimer(TimeSpan.FromSeconds(1.0), clock);

            // Act
            DateTime end = clock.Now.AddSeconds(1000.0);
            int count = 0;
            while (true)
            {
                TimeSpan delay = timer.GetDelay();
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
            IntervalTimer timer = new IntervalTimer(TimeSpan.FromSeconds(1.0), clock);

            // Right now the interval is between 0s and 1s. We let the time pass beyond this interval.
            clock.AddSeconds(1.5);

            // Act & Assert
            TimeSpan delay = timer.GetDelay();
            Assert.Equal(TimeSpan.Zero, delay);
        }
    }
}
