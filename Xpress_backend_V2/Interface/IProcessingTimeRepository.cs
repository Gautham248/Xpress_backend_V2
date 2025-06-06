namespace Xpress_backend_V2.Interface
{
    public interface IProcessingTimeRepository
    {
        Task<(TimeSpan averageTime, int requestCount)> GetAverageProcessingTimeAsync();

    }
}
