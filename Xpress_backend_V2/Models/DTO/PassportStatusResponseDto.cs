namespace Xpress_backend_V2.Models.DTO
{
    public class PassportStatusResponseDto
    {
            public List<PassportStatusDto> PassportDetails { get; set; }
            public int ExpiredCount { get; set; }
            public int ExpiresIn45DaysCount { get; set; }
            public int ExpiresIn90DaysCount { get; set; }
        
    }
}
