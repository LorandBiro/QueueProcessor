using QueueProcessor.Mocks;
using System;
using Xunit;

namespace QueueProcessor.Internal
{
    public class FailureRateCalculatorTest
    {
        [Fact]
        public void CompexScenario()
        {
            ClockStub clock = new ClockStub();
            FailureRateCalculator calculator = new FailureRateCalculator(4, TimeSpan.FromSeconds(1.0), clock);

            void Step(double timestamp, Action<FailureRateCalculator> update, double expectedFailureRate)
            {
                clock.SetSeconds(timestamp);
                update(calculator);
                Assert.Equal(expectedFailureRate, calculator.GetFailureRate(), 4);
            }

            Step(0.0, x => x.OnSuccess(), 0.0);
            Step(1.0, x => x.OnSuccess(), 0.0);
            Step(2.0, x => x.OnSuccess(), 0.0);
            Step(3.0, x => x.OnSuccess(), 0.0);
            Step(4.0, x => x.OnFailure(), 0.25);
            Step(5.0, x => x.OnFailure(), 0.5);
            Step(6.0, x => x.OnFailure(), 0.75);
            Step(7.0, x => x.OnFailure(), 1.0);
        }

        [Fact]
        public void ForgetsAfterWindowDuration()
        {
            ClockStub clock = new ClockStub();
            FailureRateCalculator calculator = new FailureRateCalculator(4, TimeSpan.FromSeconds(1.0), clock);

            calculator.OnFailure();
            calculator.OnSuccess();
            clock.SetSeconds(3.9);
            Assert.Equal(0.5, calculator.GetFailureRate(), 4);

            clock.SetSeconds(4.0);
            Assert.Equal(0.0, calculator.GetFailureRate(), 4);
        }
    }
}
