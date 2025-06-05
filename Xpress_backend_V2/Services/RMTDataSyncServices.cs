//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using System;
//using System.Collections.Generic;
//using System.Net.Http;
//using System.Text.Json;
//using System.Threading;
//using System.Threading.Tasks;
//using Xpress_backend_V2.Data;
//using Xpress_backend_V2.Models;

//namespace Xpress_backend_V2.Services
//{
//    // Model to deserialize the API response
//    public class ApiRmtResponse
//    {
//        public int StatusCode { get; set; }
//        public List<ApiRmtData> Data { get; set; }
//        public string Message { get; set; }
//    }

//    public class ApiRmtData
//    {
//        public int ProjectId { get; set; }
//        public string ProjectCode { get; set; }
//        public string ProjectName { get; set; }
//        public int DuId { get; set; }
//        public DateTime ProjectStartDate { get; set; }
//        public DateTime ProjectEndDate { get; set; }
//        public string ProjectManager { get; set; }
//        public string ProjectManagerEmail { get; set; }
//        public string ProjectStatus { get; set; }
//        public string DuHeadName { get; set; }
//        public string DuHeadEmail { get; set; }
//        // Ignore other fields not needed in RMT
//    }

//    public class RmtDataSyncService : BackgroundService
//    {
//        private readonly IServiceProvider _serviceProvider;
//        private readonly ILogger<RmtDataSyncService> _logger;
//        private readonly HttpClient _httpClient;
//        private readonly TimeSpan _syncInterval = TimeSpan.FromMinutes(1); // Sync every hour

//        public RmtDataSyncService(
//            IServiceProvider serviceProvider,
//            ILogger<RmtDataSyncService> logger,
//            HttpClient httpClient)
//        {
//            _serviceProvider = serviceProvider;
//            _logger = logger;
//            _httpClient = httpClient;
//        }

//        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            _logger.LogInformation("RMT Data Sync Service started at: {time}", DateTimeOffset.Now);

//            while (!stoppingToken.IsCancellationRequested)
//            {
//                try
//                {
//                    await SyncRmtDataAsync();
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "Error occurred while syncing RMT data at: {time}", DateTimeOffset.Now);
//                }

//                // Wait for the next sync interval
//                await Task.Delay(_syncInterval, stoppingToken);
//            }

//            _logger.LogInformation("RMT Data Sync Service stopped at: {time}", DateTimeOffset.Now);
//        }

//        private async Task SyncRmtDataAsync()
//        {
//            _logger.LogInformation("Starting RMT data sync at: {time}", DateTimeOffset.Now);

//            // Fetch data from the API (this is where the endpoint is specified)
//            HttpResponseMessage response = await _httpClient.GetAsync("employeeprojectsdetails/projects/list", HttpCompletionOption.ResponseContentRead);
//            if (!response.IsSuccessStatusCode)
//            {
//                _logger.LogWarning("API request failed with status code: {statusCode}", response.StatusCode);
//                return;
//            }

//            string jsonResponse = await response.Content.ReadAsStringAsync();
//            var apiData = JsonSerializer.Deserialize<ApiRmtResponse>(jsonResponse, new JsonSerializerOptions
//            {
//                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
//            });

//            if (apiData == null || apiData.StatusCode != 200 || apiData.Data == null || apiData.Data.Count == 0)
//            {
//                _logger.LogWarning("No valid data received from API. StatusCode: {statusCode}, Data Count: {count}",
//                    apiData?.StatusCode ?? 0, apiData?.Data?.Count ?? 0);
//                return;
//            }

//            // Use a scoped service for DbContext
//            using (var scope = _serviceProvider.CreateScope())
//            {
//                var dbContext = scope.ServiceProvider.GetRequiredService<ApiDbContext>();

//                foreach (var apiItem in apiData.Data)
//                {
//                    // Check if the project already exists
//                    var existingRmt = await dbContext.RMTs
//                        .FirstOrDefaultAsync(r => r.ProjectId == apiItem.ProjectId);

//                    if (existingRmt == null)
//                    {
//                        // Add new record
//                        var newRmt = new RMT
//                        {
//                            ProjectId = apiItem.ProjectId,
//                            ProjectCode = apiItem.ProjectCode,
//                            ProjectName = apiItem.ProjectName,
//                            DuId = apiItem.DuId,
//                            ProjectStartDate = apiItem.ProjectStartDate,
//                            ProjectEndDate = apiItem.ProjectEndDate,
//                            ProjectManager = apiItem.ProjectManager,
//                            ProjectManagerEmail = apiItem.ProjectManagerEmail,
//                            ProjectStatus = apiItem.ProjectStatus,
//                            DuHeadName = apiItem.DuHeadName,
//                            DuHeadEmail = apiItem.DuHeadEmail,
//                            IsActive = apiItem.ProjectStatus == "Active"
//                        };
//                        dbContext.RMTs.Add(newRmt);
//                        _logger.LogInformation("Added new RMT record with ProjectId: {projectId}", newRmt.ProjectId);
//                    }
//                    else
//                    {
//                        // Update existing record
//                        existingRmt.ProjectCode = apiItem.ProjectCode;
//                        existingRmt.ProjectName = apiItem.ProjectName;
//                        existingRmt.DuId = apiItem.DuId;
//                        existingRmt.ProjectStartDate = apiItem.ProjectStartDate;
//                        existingRmt.ProjectEndDate = apiItem.ProjectEndDate;
//                        existingRmt.ProjectManager = apiItem.ProjectManager;
//                        existingRmt.ProjectManagerEmail = apiItem.ProjectManagerEmail;
//                        existingRmt.ProjectStatus = apiItem.ProjectStatus;
//                        existingRmt.DuHeadName = apiItem.DuHeadName;
//                        existingRmt.DuHeadEmail = apiItem.DuHeadEmail;
//                        existingRmt.IsActive = apiItem.ProjectStatus == "Active";
//                        _logger.LogInformation("Updated RMT record with ProjectId: {projectId}", existingRmt.ProjectId);
//                    }
//                }

//                await dbContext.SaveChangesAsync();
//                _logger.LogInformation("Completed RMT data sync at: {time}. Processed {count} records.",
//                    DateTimeOffset.Now, apiData.Data.Count);
//            }
//        }

//        public override async Task StopAsync(CancellationToken cancellationToken)
//        {
//            _logger.LogInformation("RMT Data Sync Service is stopping at: {time}", DateTimeOffset.Now);
//            await base.StopAsync(cancellationToken);
//        }
//    }
//}