namespace Xpress_backend_V2.Models
{
    public class Notification
    {
        public int NotificationId { get; set; } // PK
        public string NotificationTitle { get; set; }
        public string NotificationDescription { get; set; }
        public DateTime? NotificationTimestamp { get; set; }
        public int CreatedBy { get; set; } // FK → Users
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public User CreatedByUser { get; set; }
        public ICollection<UserNotification> UserNotifications { get; set; }
    }
}
