using QueueProcessor.Mocks;
using QueueProcessor.Timers;
using Xunit;

namespace QueueProcessor
{
    public class CircuitBreakerTest
    {
        [Fact]
        public void ReturnNull_WhenClosed()
        {
            // Arrange
            TimerStub timer = new TimerStub();
            FailureRateCalculatorStub failureRateCalculator = new FailureRateCalculatorStub();
            ClockStub clock = new ClockStub();
            CircuitBreaker circuitBreaker = new CircuitBreaker(0.5, timer, failureRateCalculator);

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
