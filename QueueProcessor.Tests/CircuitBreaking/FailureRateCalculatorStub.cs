namespace QueueProcessor.CircuitBreaking
{
    public class FailureRateCalculatorStub : IFailureRateCalculator
    {
        public double FailureRate { get; set; }

        public double GetFailureRate() => this.FailureRate;

        public void OnFailure() { }

        public void OnSuccess() { }
    }
}
