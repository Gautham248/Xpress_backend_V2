namespace Xpress_backend_V2.Models
{
    public class EmailActionToken
    {
        public int Id { get; set; }
        public string RequestId { get; set; }
        public string Action { get; set; }
        public string UserEmail { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string Token { get; set; }
        public bool IsUsed { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
