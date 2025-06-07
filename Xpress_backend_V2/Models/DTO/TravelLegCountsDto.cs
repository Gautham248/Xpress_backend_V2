namespace Xpress_backend_V2.Models.DTO
{
    public class TravelLegCountsDto
    {
        public int TodayOutboundDepartureCount { get; set; }
        public int TodayReturnArrivalCount { get; set; }
          public Dictionary<string, int> OutboundDepartureStatusCounts { get; set; }
    public Dictionary<string, int> ReturnArrivalStatusCounts { get; set; }
    }
}
