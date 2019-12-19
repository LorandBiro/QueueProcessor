using QueueProcessor.Mocks;
using System;
using Xunit;

namespace QueueProcessor
{
    public class CircuitBreakerTest
    {
        [Fact]
        public void ReturnZero_WhenClosed()
        {
            // Arrange
            FailureRateCalculatorStub failureRateCalculator = new FailureRateCalculatorStub();
            ClockStub clock = new ClockStub();
            CircuitBreaker circuitBreaker = new CircuitBreaker(0.5, TimeSpan.FromSeconds(5.0), failureRateCalculator, clock);

            failureRateCalculator.FailureRate = 0.25;
            Assert.Null(circuitBreaker.GetDelay());
            failureRateCalculator.FailureRate = 0.5;
            Assert.Null(circuitBreaker.GetDelay());
            failureRateCalculator.FailureRate = 0.75;
            Assert.NotNull(circuitBreaker.GetDelay());
            failureRateCalculator.FailureRate = 0.5;
            Assert.NotNull(circuitBreaker.GetDelay());
            failureRateCalculator.FailureRate = 0.25;
            Assert.Null(circuitBreaker.GetDelay());
        }
    }
}
