namespace Xpress_backend_V2.BackgroundServices
{
    public interface IBackgroundTaskQueue
    {
        ValueTask QueueBackgroundWorkItemAsync(int workItem); // <--- CHANGE to int
        ValueTask<int> DequeueAsync(CancellationToken cancellationToken); // <--- CHANGE to int
    }
}
