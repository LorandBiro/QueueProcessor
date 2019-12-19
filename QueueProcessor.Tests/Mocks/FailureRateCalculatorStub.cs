using QueueProcessor.Internal;

namespace QueueProcessor.Mocks
{
    public class FailureRateCalculatorStub : IFailureRateCalculator
    {
        public double FailureRate { get; set; }

        public double GetFailureRate() => this.FailureRate;

        public void OnFailure() { }

        public void OnSuccess() { }
    }
}
