namespace QueueProcessor.CircuitBreaking
{
    public interface IFailureRateCalculator
    {
        double GetFailureRate();
        void OnSuccess();
        void OnFailure();
    }
}
