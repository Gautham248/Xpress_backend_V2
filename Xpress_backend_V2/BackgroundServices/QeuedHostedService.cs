using Xpress_backend_V2.Interface;

namespace Xpress_backend_V2.BackgroundServices
{
    public class QueuedHostedService : BackgroundService
    {
        private readonly ILogger<QueuedHostedService> _logger;
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly IServiceScopeFactory _scopeFactory;

        public QueuedHostedService(
            IBackgroundTaskQueue taskQueue,
            ILogger<QueuedHostedService> logger,
            IServiceScopeFactory scopeFactory)
        {
            _taskQueue = taskQueue;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Queued Hosted Service is running.");
            await BackgroundProcessing(stoppingToken);
        }

        private async Task BackgroundProcessing(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Wait for a work item to become available
                    int auditLogId = await _taskQueue.DequeueAsync(stoppingToken);

                    // Create a new scope to resolve scoped services
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var auditHandler = scope.ServiceProvider.GetRequiredService<IAuditLogHandlerService>();
                        var dbContext = scope.ServiceProvider.GetRequiredService<Data.ApiDbContext>();

                        // Find the AuditLog entry using the ID
                        var auditLogEntry = await dbContext.AuditLogs.FindAsync(auditLogId);

                        if (auditLogEntry != null)
                        {
                            _logger.LogInformation("Dequeued work item for AuditLogId: {Id}. Processing...", auditLogId);
                            await auditHandler.ProcessAuditLogEntryAsync(auditLogEntry);
                        }
                        else
                        {
                            _logger.LogWarning("Could not find AuditLog with ID: {Id} from the queue.", auditLogId);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Prevent throwing if stoppingToken was signaled
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing work item.");
                }
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Queued Hosted Service is stopping.");
            await base.StopAsync(stoppingToken);
        }
    }
}
