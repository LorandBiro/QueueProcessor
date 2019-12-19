namespace QueueProcessor.Internal
{
    public interface IFailureRateCalculator
    {
        double GetFailureRate();
        void OnSuccess();
        void OnFailure();
    }
}
