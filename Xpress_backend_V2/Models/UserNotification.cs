namespace Xpress_backend_V2.Models
{
    public class UserNotification
    {
        public int UserNotificationId { get; set; } // PK
        public int UserId { get; set; } // FK → Users
        public int NotificationId { get; set; } // FK → Notifications
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }

        // Navigation properties
        public User User { get; set; }
        public Notification Notification { get; set; }
    }
}
