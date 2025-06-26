using System.Threading.Channels;

namespace Xpress_backend_V2.BackgroundServices
{
    public class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        private readonly Channel<int> _queue;

        public BackgroundTaskQueue(IConfiguration configuration)
        {
            
            var options = new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _queue = Channel.CreateBounded<int>(options);
        }

        public async ValueTask QueueBackgroundWorkItemAsync(int workItem)
        {
            if (workItem == 0)
            {
                throw new ArgumentNullException(nameof(workItem), "AuditLog ID cannot be zero.");
            }

            await _queue.Writer.WriteAsync(workItem);
        }

        public async ValueTask<int> DequeueAsync(CancellationToken cancellationToken)
        {
            var workItem = await _queue.Reader.ReadAsync(cancellationToken);
            return workItem;
        }
    }
}
